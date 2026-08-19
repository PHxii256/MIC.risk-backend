using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MIC.risk.Interfaces;
using MIC.risk.Models;
using MIC.risk.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MIC.risk.Service
{
    public class TokenService : ITokenService
    {
        private const int RefreshTokenByteLength = 32;

        private readonly JwtOptions _options;
        private readonly SymmetricSecurityKey _key;

        public TokenService(IOptions<JwtOptions> options)
        {
            _options = options.Value;

            if (string.IsNullOrWhiteSpace(_options.SigningKey))
            {
                throw new InvalidOperationException("JWT:SigningKey is missing from configuration.");
            }

            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        }

        public AccessToken CreateAccessToken(AppUser user, IEnumerable<string> roles)
        {
            // Trimmed to whole seconds so the value handed to the client matches the token's
            // own `exp` claim exactly, and the client never refreshes a fraction too late.
            var expiresUtc = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);
            expiresUtc = new DateTime(
                expiresUtc.Ticks - (expiresUtc.Ticks % TimeSpan.TicksPerSecond),
                DateTimeKind.Utc);

            var expiresAt = new DateTimeOffset(expiresUtc);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
                new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new(JwtRegisteredClaimNames.GivenName, user.UserName ?? string.Empty)
            };

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiresAt.UtcDateTime,
                SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature),
                Issuer = _options.Issuer,
                Audience = _options.Audience
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return new AccessToken(tokenHandler.WriteToken(token), expiresAt);
        }

        public string CreateRefreshToken() =>
            Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(RefreshTokenByteLength));

        public string HashRefreshToken(string rawToken) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
    }
}
