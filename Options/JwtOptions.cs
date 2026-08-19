namespace MIC.risk.Options;

public class JwtOptions
{
    public const string SectionName = "JWT";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    /// <summary>
    /// Deliberately short. The refresh token, not this, is what keeps an employee signed in;
    /// a short access token is what makes role changes and revocations take effect quickly.
    /// </summary>
    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>
    /// Sliding idle window. An employee who uses the app at least once within this many days
    /// is never asked to log in again, because every refresh issues a fresh window.
    /// </summary>
    public int RefreshTokenDays { get; set; } = 60;

    /// <summary>
    /// Hard cap measured from the original login, regardless of activity.
    /// Zero or negative disables it, which is the default and what "log in once" implies.
    /// </summary>
    public int RefreshTokenAbsoluteDays { get; set; }

    /// <summary>
    /// Tolerance for a token that was rotated moments ago being presented again.
    /// Covers the honest race where two browser tabs refresh at the same instant; without it
    /// that race looks identical to token theft and would sign the employee out.
    /// </summary>
    public int RefreshTokenReuseLeewaySeconds { get; set; } = 30;

    public string RefreshCookieName { get; set; } = "mic_refresh_token";

    /// <summary>
    /// Scoped so the cookie is only ever sent to the endpoints that need it.
    /// </summary>
    public string RefreshCookiePath { get; set; } = "/api/account";

    /// <summary>
    /// Strict is correct when the SPA and API are same-site (including via the Vite dev proxy).
    /// Only relax to None — which also forces HTTPS — if they are ever served from different sites.
    /// </summary>
    public SameSiteMode RefreshCookieSameSite { get; set; } = SameSiteMode.Strict;
}
