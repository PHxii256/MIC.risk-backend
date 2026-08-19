using MIC.risk.Models;

namespace MIC.risk.Interfaces;

/// <summary>A minted access token together with the moment it stops being accepted.</summary>
public readonly record struct AccessToken(string Value, DateTimeOffset ExpiresAt);

public interface ITokenService
{
    AccessToken CreateAccessToken(AppUser user, IEnumerable<string> roles);

    /// <summary>Generates a new cryptographically random raw refresh token.</summary>
    string CreateRefreshToken();

    /// <summary>Hashes a raw refresh token for storage and lookup. Never store the raw value.</summary>
    string HashRefreshToken(string rawToken);
}
