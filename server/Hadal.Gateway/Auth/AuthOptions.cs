namespace Hadal.Gateway.Auth
{
    /// <summary>Bound from the "Auth" configuration section. The secret comes from .env, never from git.</summary>
    public sealed class AuthOptions
    {
        public const string Section = "Auth";

        /// <summary>HS256 signing key, at least 32 characters.</summary>
        public string JwtSecret { get; set; } = string.Empty;

        public string Issuer { get; set; } = "hadal-auth";

        public string Audience { get; set; } = "hadal-gateway";

        public int AccessTokenMinutes { get; set; } = 60;

        public int RefreshTokenDays { get; set; } = 30;

        /// <summary>Guest account creations allowed per client IP per hour.</summary>
        public int GuestLoginsPerHour { get; set; } = 20;
    }

    /// <summary>Bound from "Version": served at /manifest/version.json for the client's patch check.</summary>
    public sealed class VersionOptions
    {
        public const string Section = "Version";

        public string MinClientVersion { get; set; } = "0.0.0";
        public string LatestClientVersion { get; set; } = "0.0.0";
        public bool Maintenance { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
