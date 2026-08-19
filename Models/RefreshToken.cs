namespace MIC.risk.Models;

/// <summary>
/// A single link in a refresh-token chain. The raw token value is returned to the client once,
/// in an HttpOnly cookie, and never stored here — only its SHA-256 hash.
/// </summary>
public class RefreshToken
{
    public long Id { get; set; }

    public string UserId { get; set; } = null!;
    public AppUser User { get; set; } = null!;

    /// <summary>Uppercase hex SHA-256 of the raw token.</summary>
    public string TokenHash { get; set; } = null!;

    /// <summary>
    /// Groups every token descended from one login, so that presenting an already-rotated
    /// token can revoke the whole chain rather than just the one link.
    /// </summary>
    public Guid FamilyId { get; set; }

    /// <summary>
    /// Hard cut-off inherited from the first token in the family, regardless of activity.
    /// Null when no absolute cap is configured, which is the default.
    /// </summary>
    public DateTimeOffset? FamilyExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public string? RevokedReason { get; set; }

    public long? ReplacedByTokenId { get; set; }

    public string? CreatedByIp { get; set; }
}

/// <summary>Reasons recorded on <see cref="RefreshToken.RevokedReason"/>.</summary>
public static class RefreshTokenRevocationReasons
{
    /// <summary>Normal rotation: this token was exchanged for its successor.</summary>
    public const string Rotated = "Rotated";

    public const string Logout = "Logout";
    public const string ReuseDetected = "ReuseDetected";
    public const string EmployeeDeactivated = "EmployeeDeactivated";
    public const string PasswordChanged = "PasswordChanged";
    public const string FamilyExpired = "FamilyExpired";
}
