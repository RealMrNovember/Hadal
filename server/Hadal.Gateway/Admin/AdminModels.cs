using HADAL.Shared.Enums;

namespace Hadal.Gateway.Admin
{
    /// <summary>owner: everything incl. balance and admins · gm: players, coupons, live ops · viewer: read only.</summary>
    public static class AdminRoles
    {
        public const string Owner = "owner";
        public const string GameMaster = "gm";
        public const string Viewer = "viewer";

        public static bool IsValid(string role) => role is Owner or GameMaster or Viewer;

        public static int Rank(string role) => role switch { Owner => 3, GameMaster => 2, Viewer => 1, _ => 0 };
    }

    public sealed record AdminUser(Guid Id, string Username, string PasswordHash, string TotpSecret, string Role,
        bool Disabled, DateTimeOffset CreatedAt, DateTimeOffset? LastLoginAt);

    public sealed record AuditEntry(long Id, DateTimeOffset At, string Admin, string Action, string Target, string Details, string Ip);

    /// <summary>
    /// A gift code. Every player redeems a code at most once. Optional rules: a validity window
    /// (<see cref="StartsAt"/> .. <see cref="ExpiresAt"/>), a total limit (<see cref="MaxRedemptions"/>,
    /// 0 = unlimited) and a single owner (<see cref="TargetAccountId"/>: compensation, VIP gifts).
    /// </summary>
    public sealed record Coupon(string Code, Dictionary<ResourceType, long> Rewards, int MaxRedemptions, int Redeemed,
        DateTimeOffset? ExpiresAt, bool Active, string CreatedBy, DateTimeOffset CreatedAt, string Note,
        DateTimeOffset? StartsAt = null, Guid? TargetAccountId = null);

    public enum RedeemResult
    {
        Redeemed,
        Unknown,
        Inactive,
        Expired,
        Exhausted,
        AlreadyRedeemed,
        NotStarted,
        NotForThisAccount
    }

    /// <summary>Live-ops switches read by the version manifest and the client title screen.</summary>
    public sealed class LiveOpsSettings
    {
        public bool Maintenance { get; set; }
        public string MaintenanceMessage { get; set; } = string.Empty;
        public string Announcement { get; set; } = string.Empty;

        /// <summary>Newest app build; clients below it are offered the update. Empty = the server's Version config.</summary>
        public string LatestClientVersion { get; set; } = string.Empty;

        /// <summary>Oldest build still allowed to play; older ones must update. Empty = Version config.</summary>
        public string MinClientVersion { get; set; } = string.Empty;

        /// <summary>Where the update is downloaded from (https). Empty = Version config.</summary>
        public string UpdateUrl { get; set; } = string.Empty;
    }

    public sealed record CatalogVersion(int Version, string Json, string CreatedBy, DateTimeOffset CreatedAt, string Note);

    public sealed record PlayerSummary(Guid AccountId, string DisplayName, DateTimeOffset CreatedAt, DateTimeOffset LastLoginAt,
        DateTimeOffset? BannedUntil, string BanReason, IReadOnlyList<string> Providers);
}
