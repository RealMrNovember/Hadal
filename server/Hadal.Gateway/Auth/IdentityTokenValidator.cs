using HADAL.Shared.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Hadal.Gateway.Auth
{
    public enum IdentityStatus
    {
        Valid,
        UnknownProvider,
        NotConfigured,
        Invalid
    }

    public readonly record struct IdentityResult(IdentityStatus Status, string Subject);

    /// <summary>Verifies Sign in with Google / Sign in with Apple ID tokens; the subject is the stable user id.</summary>
    public interface IIdentityTokenValidator
    {
        Task<IdentityResult> ValidateAsync(string provider, string idToken, CancellationToken cancellationToken);
    }

    /// <summary>
    /// OpenID Connect validation against each provider's published signing keys (JWKS, cached and
    /// refreshed by <see cref="ConfigurationManager{T}"/>). A provider is disabled until its client
    /// ids are configured (Auth:GoogleClientIds / Auth:AppleClientIds), so nothing is accepted by default.
    /// </summary>
    public sealed class OidcIdentityTokenValidator : IIdentityTokenValidator
    {
        private sealed record Provider(string[] Issuers, string[] Audiences, ConfigurationManager<OpenIdConnectConfiguration> Metadata);

        private readonly Dictionary<string, Provider> _providers = new(StringComparer.Ordinal);
        private readonly JsonWebTokenHandler _handler = new();

        public OidcIdentityTokenValidator(IOptions<AuthOptions> options)
        {
            Add(AccountProviders.Google, options.Value.GoogleClientIds,
                "https://accounts.google.com/.well-known/openid-configuration",
                "https://accounts.google.com", "accounts.google.com");
            Add(AccountProviders.Apple, options.Value.AppleClientIds,
                "https://appleid.apple.com/.well-known/openid-configuration",
                "https://appleid.apple.com");
        }

        private void Add(string name, string clientIds, string metadataUrl, params string[] issuers)
        {
            var audiences = clientIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (audiences.Length == 0)
                return;

            var metadata = new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataUrl, new OpenIdConnectConfigurationRetriever(), new HttpDocumentRetriever { RequireHttps = true });
            _providers[name] = new Provider(issuers, audiences, metadata);
        }

        public async Task<IdentityResult> ValidateAsync(string provider, string idToken, CancellationToken cancellationToken)
        {
            if (provider != AccountProviders.Google && provider != AccountProviders.Apple)
                return new IdentityResult(IdentityStatus.UnknownProvider, string.Empty);

            if (!_providers.TryGetValue(provider, out var config))
                return new IdentityResult(IdentityStatus.NotConfigured, string.Empty);

            if (string.IsNullOrWhiteSpace(idToken) || idToken.Length > 8192)
                return new IdentityResult(IdentityStatus.Invalid, string.Empty);

            var metadata = await config.Metadata.GetConfigurationAsync(cancellationToken);
            var result = await _handler.ValidateTokenAsync(idToken, new TokenValidationParameters
            {
                ValidIssuers = config.Issuers,
                ValidAudiences = config.Audiences,
                IssuerSigningKeys = metadata.SigningKeys,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2)
            });

            return result.IsValid && result.Claims.TryGetValue(JwtRegisteredClaimNames.Sub, out var sub) && sub is string subject && subject.Length > 0
                ? new IdentityResult(IdentityStatus.Valid, subject)
                : new IdentityResult(IdentityStatus.Invalid, string.Empty);
        }
    }
}
