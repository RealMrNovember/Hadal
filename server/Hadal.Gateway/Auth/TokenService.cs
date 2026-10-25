using System.Security.Cryptography;
using System.Text;
using Hadal.Game;
using HADAL.Shared.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Hadal.Gateway.Auth
{
    public readonly record struct TokenValidation(bool IsValid, Guid AccountId, string Reason);

    /// <summary>
    /// Short-lived JWT access tokens (gateway handshake) plus opaque, single-use, rotating refresh
    /// tokens (stored hashed). A leaked access token is useful for an hour; a refresh token once.
    /// </summary>
    public sealed class TokenService
    {
        private readonly AuthOptions _options;
        private readonly IAccountStore _accounts;
        private readonly IClock _clock;
        private readonly SymmetricSecurityKey _key;
        private readonly JsonWebTokenHandler _handler = new();

        public TokenService(IOptions<AuthOptions> options, IAccountStore accounts, IClock clock)
        {
            _options = options.Value;
            _accounts = accounts;
            _clock = clock;

            if (Encoding.UTF8.GetByteCount(_options.JwtSecret) < 32)
                throw new InvalidOperationException("Auth:JwtSecret must be set and at least 32 characters (HADAL_JWT_SECRET in .env).");

            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.JwtSecret));
        }

        public async Task<AuthResponse> IssueAsync(Guid accountId, CancellationToken cancellationToken)
        {
            var now = _clock.UtcNow;
            var expires = now.AddMinutes(_options.AccessTokenMinutes);

            var accessToken = _handler.CreateToken(new SecurityTokenDescriptor
            {
                Issuer = _options.Issuer,
                Audience = _options.Audience,
                IssuedAt = now.UtcDateTime,
                NotBefore = now.UtcDateTime,
                Expires = expires.UtcDateTime,
                Claims = new Dictionary<string, object> { [JwtRegisteredClaimNames.Sub] = accountId.ToString("N") },
                SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256)
            });

            var refreshToken = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
            await _accounts.StoreRefreshTokenAsync(Hash(refreshToken), accountId, now.AddDays(_options.RefreshTokenDays), cancellationToken);

            return new AuthResponse
            {
                accountId = accountId.ToString("N"),
                accessToken = accessToken,
                expiresIn = _options.AccessTokenMinutes * 60,
                refreshToken = refreshToken
            };
        }

        /// <summary>Consumes the refresh token and issues a new pair. Null if it was unknown, used or expired.</summary>
        public async Task<AuthResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken) || refreshToken.Length > 128)
                return null;

            var account = await _accounts.ConsumeRefreshTokenAsync(Hash(refreshToken), _clock.UtcNow, cancellationToken);
            return account == null ? null : await IssueAsync(account.Value, cancellationToken);
        }

        public async Task<TokenValidation> ValidateAccessTokenAsync(string accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return new TokenValidation(false, Guid.Empty, HandshakeRejectReasons.TokenMissing);

            var result = await _handler.ValidateTokenAsync(accessToken, new TokenValidationParameters
            {
                ValidIssuer = _options.Issuer,
                ValidAudience = _options.Audience,
                IssuerSigningKey = _key,
                ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                ClockSkew = TimeSpan.FromSeconds(30),
                LifetimeValidator = (notBefore, expires, _, _) =>
                    expires != null && expires.Value > _clock.UtcNow.UtcDateTime.AddSeconds(-30)
                    && (notBefore == null || notBefore.Value <= _clock.UtcNow.UtcDateTime.AddSeconds(30))
            });

            if (!result.IsValid)
            {
                var reason = result.Exception is SecurityTokenExpiredException or SecurityTokenInvalidLifetimeException
                    ? HandshakeRejectReasons.TokenExpired
                    : HandshakeRejectReasons.TokenInvalid;
                return new TokenValidation(false, Guid.Empty, reason);
            }

            return result.Claims.TryGetValue(JwtRegisteredClaimNames.Sub, out var sub) && Guid.TryParse(sub as string, out var id)
                ? new TokenValidation(true, id, string.Empty)
                : new TokenValidation(false, Guid.Empty, HandshakeRejectReasons.TokenInvalid);
        }

        private static byte[] Hash(string token) => SHA256.HashData(Encoding.UTF8.GetBytes(token));
    }
}
