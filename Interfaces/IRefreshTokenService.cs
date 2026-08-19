using MIC.risk.Models;

namespace MIC.risk.Interfaces;

/// <summary>The raw token to hand to the client, plus when its cookie should expire.</summary>
public readonly record struct IssuedRefreshToken(string RawToken, DateTimeOffset ExpiresAt);

/// <summary>
/// Result of exchanging a refresh token. Failures are deliberately coarse to the caller —
/// the client is told only that it must log in again; the specific reason is logged server-side.
/// </summary>
public sealed class RefreshOutcome
{
    public bool Succeeded { get; private init; }
    public string? FailureReason { get; private init; }
    public AppUser? User { get; private init; }
    public IssuedRefreshToken Token { get; private init; }

    public static RefreshOutcome Fail(string reason) =>
        new() { Succeeded = false, FailureReason = reason };

    public static RefreshOutcome Success(AppUser user, IssuedRefreshToken token) =>
        new() { Succeeded = true, User = user, Token = token };
}

public interface IRefreshTokenService
{
    /// <summary>Starts a new token family. Called on login and after a password change.</summary>
    Task<IssuedRefreshToken> IssueAsync(AppUser user, string? createdByIp, CancellationToken cancellationToken = default);

    /// <summary>Validates a raw token and exchanges it for its successor in the same family.</summary>
    Task<RefreshOutcome> RotateAsync(string rawToken, string? createdByIp, CancellationToken cancellationToken = default);

    /// <summary>Revokes the whole family the given raw token belongs to. Safe to call with an unknown token.</summary>
    Task RevokeFamilyAsync(string rawToken, string reason, CancellationToken cancellationToken = default);

    /// <summary>Revokes every live token for an identity user, across all their devices.</summary>
    Task RevokeAllForUserAsync(string userId, string reason, CancellationToken cancellationToken = default);

    /// <summary>Revokes every live token for an employee profile. No-op if the employee has no account.</summary>
    Task RevokeAllForEmployeeAsync(long employeeId, string reason, CancellationToken cancellationToken = default);
}
