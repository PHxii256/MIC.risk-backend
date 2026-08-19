using MIC.risk.Data;
using MIC.risk.DTOs;
using MIC.risk.DTOs.Auth;
using MIC.risk.Extensions;
using MIC.risk.Interfaces;
using MIC.risk.Mappers;
using MIC.risk.Models;
using MIC.risk.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MIC.risk.Controllers;

[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ApplicationDBContext _context;
    private readonly JwtOptions _jwtOptions;

    public AccountController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService,
        ApplicationDBContext context,
        IOptions<JwtOptions> jwtOptions)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
        _context = context;
        _jwtOptions = jwtOptions.Value;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var appUser = await _userManager.FindByEmailAsync(dto.Email);
        if (appUser == null)
        {
            return this.UnauthorizedProblem("Invalid credentials.");
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(appUser, dto.Password, false);
        if (!signInResult.Succeeded)
        {
            return this.UnauthorizedProblem("Invalid credentials.");
        }

        var employee = await LoadEmployeeAsync(appUser.Id, cancellationToken);
        if (employee == null)
        {
            return this.UnauthorizedProblem("No employee profile is linked to this account.");
        }

        if (!employee.Active)
        {
            return this.UnauthorizedProblem("Your employee account is inactive.");
        }

        var response = await EstablishSessionAsync(appUser, employee, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Exchanges the refresh cookie for a new access token and a rotated refresh cookie.
    /// Anonymous by design: the caller reaches here precisely because its access token has expired.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var rawToken = Request.Cookies[_jwtOptions.RefreshCookieName];

        var outcome = await _refreshTokenService.RotateAsync(rawToken ?? string.Empty, CallerIp(), cancellationToken);
        if (!outcome.Succeeded || outcome.User is null)
        {
            ClearRefreshCookie();
            return this.UnauthorizedProblem("Your session has expired. Please sign in again.");
        }

        var employee = await LoadEmployeeAsync(outcome.User.Id, cancellationToken);
        if (employee == null || !employee.Active)
        {
            await _refreshTokenService.RevokeAllForUserAsync(
                outcome.User.Id,
                RefreshTokenRevocationReasons.EmployeeDeactivated,
                cancellationToken);

            ClearRefreshCookie();
            return this.UnauthorizedProblem("Your employee account is inactive.");
        }

        SetRefreshCookie(outcome.Token);

        var roles = await _userManager.GetRolesAsync(outcome.User);

        return Ok(BuildAuthResponse(outcome.User, employee, roles));
    }

    /// <summary>
    /// Revokes the whole token family behind the current refresh cookie and clears it.
    /// Anonymous so that signing out still works once the access token has expired.
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var rawToken = Request.Cookies[_jwtOptions.RefreshCookieName];

        if (!string.IsNullOrWhiteSpace(rawToken))
        {
            await _refreshTokenService.RevokeFamilyAsync(
                rawToken,
                RefreshTokenRevocationReasons.Logout,
                cancellationToken);
        }

        ClearRefreshCookie();
        return NoContent();
    }

    /// <summary>The authenticated caller's own profile and roles, without minting a new token.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var appUser = await _userManager.GetUserAsync(User);
        if (appUser == null)
        {
            return this.UnauthorizedProblem("User is not authenticated.");
        }

        var employee = await LoadEmployeeAsync(appUser.Id, cancellationToken);
        if (employee == null || !employee.Active)
        {
            return this.UnauthorizedProblem("Your employee account is inactive.");
        }

        var roles = await _userManager.GetRolesAsync(appUser);

        return Ok(new CurrentUserDto
        {
            Roles = roles.ToArray(),
            Employee = employee.ToDto()
        });
    }

    /// <summary>
    /// Changes the password, then signs every other device out and re-establishes the current
    /// session, so the caller stays logged in here but stolen sessions elsewhere die.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var appUser = await _userManager.GetUserAsync(User);
        if (appUser == null)
        {
            return this.UnauthorizedProblem("User is not authenticated.");
        }

        var employee = await LoadEmployeeAsync(appUser.Id, cancellationToken);
        if (employee == null || !employee.Active)
        {
            return this.UnauthorizedProblem("Your employee account is inactive.");
        }

        var result = await _userManager.ChangePasswordAsync(appUser, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(nameof(dto.NewPassword), error.Description);
            }

            return ValidationProblem(ModelState);
        }

        await _refreshTokenService.RevokeAllForUserAsync(
            appUser.Id,
            RefreshTokenRevocationReasons.PasswordChanged,
            cancellationToken);

        var response = await EstablishSessionAsync(appUser, employee, cancellationToken);
        return Ok(response);
    }

    private async Task<AuthResponseDto> EstablishSessionAsync(
        AppUser appUser,
        Employee employee,
        CancellationToken cancellationToken)
    {
        var refreshToken = await _refreshTokenService.IssueAsync(appUser, CallerIp(), cancellationToken);
        SetRefreshCookie(refreshToken);

        var roles = await _userManager.GetRolesAsync(appUser);

        return BuildAuthResponse(appUser, employee, roles);
    }

    private AuthResponseDto BuildAuthResponse(AppUser appUser, Employee employee, IList<string> roles)
    {
        var accessToken = _tokenService.CreateAccessToken(appUser, roles);

        return new AuthResponseDto
        {
            AccessToken = accessToken.Value,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            Roles = roles.ToArray(),
            Employee = employee.ToDto()
        };
    }

    private Task<Employee?> LoadEmployeeAsync(string identityUserId, CancellationToken cancellationToken) =>
        _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.IdentityUser)
            .FirstOrDefaultAsync(e => e.IdentityUserId == identityUserId, cancellationToken);

    private void SetRefreshCookie(IssuedRefreshToken token) =>
        Response.Cookies.Append(_jwtOptions.RefreshCookieName, token.RawToken, BuildCookieOptions(token.ExpiresAt));

    private void ClearRefreshCookie() =>
        Response.Cookies.Delete(_jwtOptions.RefreshCookieName, BuildCookieOptions(null));

    private CookieOptions BuildCookieOptions(DateTimeOffset? expiresAt) => new()
    {
        HttpOnly = true,

        // Browsers treat localhost as a secure context, so this holds in development too.
        Secure = true,

        SameSite = _jwtOptions.RefreshCookieSameSite,
        Path = _jwtOptions.RefreshCookiePath,
        Expires = expiresAt,
        IsEssential = true
    };

    private string? CallerIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
