using MIC.risk.Data;
using MIC.risk.Interfaces;
using MIC.risk.Models;
using MIC.risk.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MIC.risk.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly ApplicationDBContext _context;
    private readonly ITokenService _tokenService;
    private readonly JwtOptions _options;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(
        ApplicationDBContext context,
        ITokenService tokenService,
        IOptions<JwtOptions> options,
        ILogger<RefreshTokenService> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IssuedRefreshToken> IssueAsync(
        AppUser user,
        string? createdByIp,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var familyExpiresAt = _options.RefreshTokenAbsoluteDays > 0
            ? now.AddDays(_options.RefreshTokenAbsoluteDays)
            : (DateTimeOffset?)null;

        var (entity, rawToken) = BuildToken(user.Id, Guid.NewGuid(), familyExpiresAt, createdByIp, now);

        _context.RefreshTokens.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return new IssuedRefreshToken(rawToken, entity.ExpiresAt);
    }

    public async Task<RefreshOutcome> RotateAsync(
        string rawToken,
        string? createdByIp,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return RefreshOutcome.Fail("No refresh token was presented.");
        }

        var now = DateTimeOffset.UtcNow;
        var hash = _tokenService.HashRefreshToken(rawToken);

        var token = await _context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (token is null)
        {
            // Either forged, or from a family that has since been purged.
            return RefreshOutcome.Fail("The refresh token is not recognised.");
        }

        if (token.RevokedAt is not null)
        {
            var withinLeeway =
                token.RevokedReason == RefreshTokenRevocationReasons.Rotated &&
                token.RevokedAt.Value.AddSeconds(_options.RefreshTokenReuseLeewaySeconds) >= now;

            if (!withinLeeway)
            {
                // A token that was already spent is being replayed. Assume theft and burn the chain:
                // the legitimate holder is signed out too, which is the correct trade here.
                _logger.LogWarning(
                    "Refresh token reuse detected for user {UserId}, family {FamilyId}. Revoking the family.",
                    token.UserId,
                    token.FamilyId);

                await RevokeFamilyInternalAsync(
                    token.FamilyId,
                    RefreshTokenRevocationReasons.ReuseDetected,
                    now,
                    cancellationToken);

                return RefreshOutcome.Fail("The refresh token has already been used.");
            }

            // Inside the leeway window this is almost certainly two browser tabs refreshing at
            // the same instant rather than an attack, so issue a successor instead of ending
            // the session. The frontend also serialises refreshes; this is the backstop.
            _logger.LogInformation(
                "Refresh token replayed within the leeway window for user {UserId}; treating it as a concurrent refresh.",
                token.UserId);
        }

        if (token.ExpiresAt <= now)
        {
            return RefreshOutcome.Fail("The refresh token has expired.");
        }

        if (token.FamilyExpiresAt is not null && token.FamilyExpiresAt <= now)
        {
            await RevokeFamilyInternalAsync(
                token.FamilyId,
                RefreshTokenRevocationReasons.FamilyExpired,
                now,
                cancellationToken);

            return RefreshOutcome.Fail("The session has reached its maximum lifetime.");
        }

        // The same check the bearer pipeline runs per request, applied here as well so that a
        // deactivated employee cannot quietly keep a session alive by refreshing in the background.
        var isActiveEmployee = await _context.Employees
            .AsNoTracking()
            .AnyAsync(e => e.IdentityUserId == token.UserId && e.Active, cancellationToken);

        if (!isActiveEmployee)
        {
            await RevokeFamilyInternalAsync(
                token.FamilyId,
                RefreshTokenRevocationReasons.EmployeeDeactivated,
                now,
                cancellationToken);

            return RefreshOutcome.Fail("The employee account is inactive.");
        }

        var (successor, successorRaw) = BuildToken(
            token.UserId,
            token.FamilyId,
            token.FamilyExpiresAt,
            createdByIp,
            now);

        _context.RefreshTokens.Add(successor);

        if (token.RevokedAt is null)
        {
            token.RevokedAt = now;
            token.RevokedReason = RefreshTokenRevocationReasons.Rotated;
        }

        // Saved twice because the successor needs its identity value before it can be pointed at.
        await _context.SaveChangesAsync(cancellationToken);

        if (token.ReplacedByTokenId is null)
        {
            token.ReplacedByTokenId = successor.Id;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return RefreshOutcome.Success(
            token.User,
            new IssuedRefreshToken(successorRaw, successor.ExpiresAt));
    }

    public async Task RevokeFamilyAsync(
        string rawToken,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return;
        }

        var hash = _tokenService.HashRefreshToken(rawToken);

        var familyId = await _context.RefreshTokens
            .AsNoTracking()
            .Where(t => t.TokenHash == hash)
            .Select(t => (Guid?)t.FamilyId)
            .FirstOrDefaultAsync(cancellationToken);

        if (familyId is null)
        {
            return;
        }

        await RevokeFamilyInternalAsync(familyId.Value, reason, DateTimeOffset.UtcNow, cancellationToken);
    }

    public async Task RevokeAllForUserAsync(
        string userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.RevokedAt, now)
                    .SetProperty(t => t.RevokedReason, reason),
                cancellationToken);
    }

    public async Task RevokeAllForEmployeeAsync(
        long employeeId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var identityUserId = await _context.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId)
            .Select(e => e.IdentityUserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            return;
        }

        await RevokeAllForUserAsync(identityUserId, reason, cancellationToken);
    }

    private async Task RevokeFamilyInternalAsync(
        Guid familyId,
        string reason,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await _context.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.RevokedAt, now)
                    .SetProperty(t => t.RevokedReason, reason),
                cancellationToken);
    }

    private (RefreshToken Entity, string RawToken) BuildToken(
        string userId,
        Guid familyId,
        DateTimeOffset? familyExpiresAt,
        string? createdByIp,
        DateTimeOffset now)
    {
        var rawToken = _tokenService.CreateRefreshToken();
        var expiresAt = now.AddDays(_options.RefreshTokenDays);

        // Never let the sliding window outlive the family's absolute cap.
        if (familyExpiresAt is not null && expiresAt > familyExpiresAt)
        {
            expiresAt = familyExpiresAt.Value;
        }

        var entity = new RefreshToken
        {
            UserId = userId,
            TokenHash = _tokenService.HashRefreshToken(rawToken),
            FamilyId = familyId,
            FamilyExpiresAt = familyExpiresAt,
            CreatedAt = now,
            ExpiresAt = expiresAt,
            CreatedByIp = createdByIp
        };

        return (entity, rawToken);
    }
}
