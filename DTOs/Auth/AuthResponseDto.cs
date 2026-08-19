using MIC.risk.DTOs;

namespace MIC.risk.DTOs.Auth;

/// <summary>
/// Everything the client needs to run an authenticated session, returned by login, refresh
/// and change-password alike so the SPA has one code path for establishing a session.
/// The refresh token is deliberately absent: it travels only in an HttpOnly cookie.
/// </summary>
public class AuthResponseDto
{
    public required string AccessToken { get; set; }

    public required DateTimeOffset AccessTokenExpiresAt { get; set; }

    public required IReadOnlyList<string> Roles { get; set; }

    /// <summary>
    /// The caller's own employee profile. Its <c>id</c> is the value the API expects in the
    /// <c>empId</c> / <c>uploadedByEmpId</c> fields of request bodies, so the client never has
    /// to enumerate employees to discover who it is.
    /// </summary>
    public required EmployeeResponseDto Employee { get; set; }
}

/// <summary>The authenticated caller, without minting anything new.</summary>
public class CurrentUserDto
{
    public required IReadOnlyList<string> Roles { get; set; }

    public required EmployeeResponseDto Employee { get; set; }
}
