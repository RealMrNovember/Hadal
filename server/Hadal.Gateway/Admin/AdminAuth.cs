using System.Security.Cryptography;
using System.Text;
using Hadal.Game;
using Hadal.Gateway.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Hadal.Gateway.Admin
{
    public sealed record AdminPrincipal(Guid Id, string Username, string Role)
    {
        public const string ItemKey = "hadal.admin";

        /// <summary>No valid session; the RequireAdmin filter turns it into 401.</summary>
        public static readonly AdminPrincipal Anonymous = new(Guid.Empty, string.Empty, string.Empty);

        public bool IsAnonymous => Id == Guid.Empty;

        /// <summary>
        /// Minimal-API binding (runs before endpoint filters): authenticates the Bearer token once
        /// per request. Never null, so a missing session is a 401 from the filter, not a 400.
        /// </summary>
        public static async ValueTask<AdminPrincipal?> BindAsync(HttpContext http)
        {
            if (http.Items.TryGetValue(ItemKey, out var cached) && cached is AdminPrincipal known)
                return known;

            var me = await http.RequestServices.GetRequiredService<AdminAuth>().AuthenticateAsync(http, http.RequestAborted) ?? Anonymous;
            http.Items[ItemKey] = me;
            return me;
        }
    }

    /// <summary>
    /// Overseer sign-in: password (PBKDF2) + TOTP, then a short-lived HS256 token the panel keeps in
    /// sessionStorage and sends as a Bearer header (no cookies, so no CSRF surface). Separate key
    /// and audience from player tokens: a player token can never open the panel.
    /// </summary>
    public sealed class AdminAuth
    {
        public const string ProxyHeader = "X-Overseer-Key";
        public const string ClientIpHeader = "X-Overseer-Client-IP";
        private const string Audience = "hadal-overseer";

        private readonly AdminOptions _options;
        private readonly IAdminStore _store;
        private readonly IClock _clock;
        private readonly JsonWebTokenHandler _handler = new();
        private readonly SymmetricSecurityKey? _key;
        private readonly byte[] _proxyKey;

        public AdminAuth(IOptions<AdminOptions> options, IAdminStore store, IClock clock)
        {
            _options = options.Value;
            _store = store;
            _clock = clock;
            _key = _options.Enabled ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.JwtSecret)) : null;
            _proxyKey = Encoding.UTF8.GetBytes(_options.ProxyKey);
        }

        public bool Enabled => _options.Enabled;

        /// <summary>True when the request came through the fronting proxy (hadal.cicibyte.com/admin).</summary>
        public bool IsFromProxy(HttpContext http)
        {
            if (!_options.Enabled)
                return false;

            var header = http.Request.Headers[ProxyHeader].ToString();
            return header.Length > 0 && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(header), _proxyKey);
        }

        /// <summary>The browser's IP as seen by the proxy (only trusted after <see cref="IsFromProxy"/>).</summary>
        public static string ClientIp(HttpContext http)
        {
            var ip = http.Request.Headers[ClientIpHeader].ToString();
            return ip.Length > 0 ? ip : http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        public async Task<(string? Token, AdminPrincipal? Principal)> LoginAsync(string username, string password, string code, CancellationToken cancellationToken)
        {
            var admin = await _store.FindAdminAsync(username ?? string.Empty, cancellationToken);

            // Always run the password hash, even for unknown users, so timing does not reveal accounts.
            var passwordOk = PasswordHasher.Verify(password ?? string.Empty, admin?.PasswordHash ?? DummyHash);
            if (admin == null || admin.Disabled || !passwordOk || !Totp.Verify(admin.TotpSecret, code ?? string.Empty, _clock.UtcNow))
                return (null, null);

            await _store.TouchLoginAsync(admin.Id, _clock.UtcNow, cancellationToken);
            var principal = new AdminPrincipal(admin.Id, admin.Username, admin.Role);
            return (Issue(principal), principal);
        }

        private static readonly string DummyHash = PasswordHasher.Hash(Guid.NewGuid().ToString("N"));

        public string Issue(AdminPrincipal principal) => _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "hadal-overseer",
            Audience = Audience,
            IssuedAt = _clock.UtcNow.UtcDateTime,
            NotBefore = _clock.UtcNow.UtcDateTime,
            Expires = _clock.UtcNow.AddHours(_options.SessionHours).UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = principal.Id.ToString("N"),
                ["name"] = principal.Username,
                ["role"] = principal.Role
            },
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256)
        });

        public int SessionSeconds => _options.SessionHours * 3600;

        /// <summary>Validates the Bearer token and re-checks the admin is still enabled with that role.</summary>
        public async Task<AdminPrincipal?> AuthenticateAsync(HttpContext http, CancellationToken cancellationToken)
        {
            if (_key == null)
                return null;

            var header = http.Request.Headers.Authorization.ToString();
            if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return null;

            var result = await _handler.ValidateTokenAsync(header[7..].Trim(), new TokenValidationParameters
            {
                ValidIssuer = "hadal-overseer",
                ValidAudience = Audience,
                IssuerSigningKey = _key,
                ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                ClockSkew = TimeSpan.FromSeconds(30),
                LifetimeValidator = (_, expires, _, _) => expires != null && expires.Value > _clock.UtcNow.UtcDateTime
            });
            if (!result.IsValid || !result.Claims.TryGetValue("name", out var name) || name is not string username)
                return null;

            var admin = await _store.FindAdminAsync(username, cancellationToken);
            return admin == null || admin.Disabled ? null : new AdminPrincipal(admin.Id, admin.Username, admin.Role);
        }

        /// <summary>Creates an admin with a random password and TOTP secret; both are shown once.</summary>
        public static (AdminUser Admin, string Password) NewAdmin(string username, string role)
        {
            var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(18)).Replace('+', 'x').Replace('/', 'y');
            var admin = new AdminUser(Guid.NewGuid(), username, PasswordHasher.Hash(password), Totp.NewSecret(), role, false,
                DateTimeOffset.UtcNow, null);
            return (admin, password);
        }
    }
}
