using Microsoft.EntityFrameworkCore;
using MIC.risk.Data; // Adjust DbContext namespace as per your project setup
using MIC.risk.DTOs;
using MIC.risk.Interfaces;
using MIC.risk.Mappers;
using MIC.risk.Models;
using MIC.risk.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace MIC.risk.Services;

public class EmployeeService : IEmployeeService
{
    private readonly ApplicationDBContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly IRefreshTokenService _refreshTokenService;

    public EmployeeService(
        ApplicationDBContext context,
        UserManager<AppUser> userManager,
        IRefreshTokenService refreshTokenService)
    {
        _context = context;
        _userManager = userManager;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<EmployeeResponseDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.IdentityUser)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        return employee?.ToDto();
    }

    public async Task<EmployeeResponseDto?> GetByIdentityUserIdAsync(string identityUserId, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.IdentityUser)
            .FirstOrDefaultAsync(e => e.IdentityUserId == identityUserId, cancellationToken);

        return employee?.ToDto();
    }

    public async Task<IEnumerable<EmployeeResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var employees = await _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.IdentityUser)
            .ToListAsync(cancellationToken);

        return employees.Select(e => e.ToDto());
    }

    public async Task<EmployeeResponseDto> CreateAsync(CreateEmployeeRequestDto dto, CancellationToken cancellationToken = default)
    {
        // 1. Validate Department exists
        var deptExists = await _context.Departments.AnyAsync(d => d.Id == dto.DeptId, cancellationToken);
        if (!deptExists)
        {
            throw new InvalidOperationException($"Department with ID {dto.DeptId} does not exist.");
        }

        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException($"An account with email '{dto.Email}' already exists.");
        }

        var appUser = new AppUser
        {
            UserName = dto.Email,
            Email = dto.Email
        };

        var createUserResult = await _userManager.CreateAsync(appUser, dto.Password);
        if (!createUserResult.Succeeded)
        {
            var errorMessage = string.Join("; ", createUserResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Unable to create identity user: {errorMessage}");
        }

        if (!string.IsNullOrWhiteSpace(dto.Role))
        {
            var roleResult = await _userManager.AddToRoleAsync(appUser, dto.Role);
            if (!roleResult.Succeeded)
            {
                var errorMessage = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Unable to assign role '{dto.Role}': {errorMessage}");
            }
        }

        var entity = dto.ToEntity(appUser.Id);

        _context.Employees.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        // Fetch re-populated entity with references for correct DTO projection
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<EmployeeResponseDto?> UpdateAsync(long id, UpdateEmployeeRequestDto dto, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (employee == null) return null;

        var deptExists = await _context.Departments.AnyAsync(d => d.Id == dto.DeptId, cancellationToken);
        if (!deptExists)
        {
            throw new InvalidOperationException($"Department with ID {dto.DeptId} does not exist.");
        }

        var wasActive = employee.Active;

        employee.Name = dto.Name;
        employee.DeptId = dto.DeptId;
        employee.Active = dto.Active;

        await _context.SaveChangesAsync(cancellationToken);

        if (wasActive && !dto.Active)
        {
            await RevokeSessionsAsync(employee, cancellationToken);
        }

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> ToggleActiveStatusAsync(long id, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (employee == null) return false;

        employee.Active = !employee.Active;
        await _context.SaveChangesAsync(cancellationToken);

        if (!employee.Active)
        {
            await RevokeSessionsAsync(employee, cancellationToken);
        }

        return true;
    }

    public async Task<bool> ResetPasswordAsync(long id, string newPassword, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (employee == null) return false;

        var appUser = await _userManager.FindByIdAsync(employee.IdentityUserId);
        if (appUser == null) return false;

        // Go through a reset token rather than RemovePassword/AddPassword, so Identity applies
        // its own password policy and stamps the security stamp on the way through.
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(appUser);
        var result = await _userManager.ResetPasswordAsync(appUser, resetToken, newPassword);

        if (!result.Succeeded)
        {
            var errorMessage = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException(errorMessage);
        }

        // Whoever knew the old password must not keep a live session.
        await _refreshTokenService.RevokeAllForUserAsync(
            employee.IdentityUserId,
            RefreshTokenRevocationReasons.PasswordChanged,
            cancellationToken);

        return true;
    }

    /// <summary>
    /// Deactivating an employee has to kill their refresh tokens as well as their access.
    /// The bearer pipeline already blocks their requests, but without this they would keep a
    /// live refresh token and could resume the moment the account were reactivated.
    /// </summary>
    private Task RevokeSessionsAsync(Employee employee, CancellationToken cancellationToken) =>
        _refreshTokenService.RevokeAllForUserAsync(
            employee.IdentityUserId,
            RefreshTokenRevocationReasons.EmployeeDeactivated,
            cancellationToken);

    public async Task EnsureActiveByIdentityUserIdAsync(string identityUserId, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.IdentityUserId == identityUserId, cancellationToken);

        if (employee == null)
        {
            throw new InvalidOperationException("No employee profile linked to the current user.");
        }

        if (!employee.Active)
        {
            throw new UnauthorizedAccessException("Your employee account is inactive.");
        }
    }

    public async Task EnsureActiveByIdAsync(long empId, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == empId, cancellationToken);

        if (employee == null)
        {
            throw new InvalidOperationException($"Employee with ID {empId} does not exist.");
        }

        if (!employee.Active)
        {
            throw new UnauthorizedAccessException("The employee account is inactive.");
        }
    }
}