namespace Hadal.Gateway.Admin
{
    /// <summary>
    /// The Overseer (docs/22). Served at https://hadal.cicibyte.com/admin: the teaser host's nginx
    /// (kale) proxies /admin to this gateway and adds <see cref="ProxyKey"/>. Requests without
    /// the key — including /admin on hadal-api directly — get 404, so the panel has one door.
    /// </summary>
    public sealed class AdminOptions
    {
        public const string Section = "Admin";

        /// <summary>Shared secret the fronting nginx sends as X-Overseer-Key. Empty = panel disabled.</summary>
        public string ProxyKey { get; set; } = string.Empty;

        /// <summary>HS256 key for admin session cookies; separate from the player JWT key.</summary>
        public string JwtSecret { get; set; } = string.Empty;

        public int SessionHours { get; set; } = 8;

        /// <summary>Login attempts per client IP per 15 minutes.</summary>
        public int LoginAttemptsPer15Minutes { get; set; } = 10;

        public bool Enabled => ProxyKey.Length >= 32 && JwtSecret.Length >= 32;
    }
}
