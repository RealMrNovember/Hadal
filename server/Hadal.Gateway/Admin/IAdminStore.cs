using System.Collections.Concurrent;
using Hadal.Gateway.Auth;
using HADAL.Shared.Enums;

namespace Hadal.Gateway.Admin
{
    /// <summary>Everything the Overseer reads and writes besides live sessions (those go through GameWorld).</summary>
    public interface IAdminStore
    {
        Task InitializeAsync(CancellationToken cancellationToken);

        // Admins and audit
        Task<int> CountAdminsAsync(CancellationToken cancellationToken);
        Task<AdminUser?> FindAdminAsync(string username, CancellationToken cancellationToken);
        Task<IReadOnlyList<AdminUser>> ListAdminsAsync(CancellationToken cancellationToken);
        Task<bool> CreateAdminAsync(AdminUser admin, CancellationToken cancellationToken);
        Task SetAdminDisabledAsync(Guid id, bool disabled, CancellationToken cancellationToken);
        Task TouchLoginAsync(Guid id, DateTimeOffset at, CancellationToken cancellationToken);
        Task AuditAsync(string admin, string action, string target, string details, string ip, CancellationToken cancellationToken);
        Task<IReadOnlyList<AuditEntry>> ListAuditAsync(int limit, CancellationToken cancellationToken);

        // Coupons
        Task<bool> CreateCouponAsync(Coupon coupon, CancellationToken cancellationToken);
        Task<IReadOnlyList<Coupon>> ListCouponsAsync(CancellationToken cancellationToken);
        Task SetCouponActiveAsync(string code, bool active, CancellationToken cancellationToken);

        /// <summary>Atomically checks and records a redemption; the caller grants the rewards.</summary>
        Task<(RedeemResult Result, Coupon? Coupon)> RedeemAsync(string code, Guid accountId, DateTimeOffset now, CancellationToken cancellationToken);

        /// <summary>Undoes a recorded redemption whose grant could not be applied.</summary>
        Task UndoRedemptionAsync(string code, Guid accountId, CancellationToken cancellationToken);

        // Live ops and balance
        Task<LiveOpsSettings> GetLiveOpsAsync(CancellationToken cancellationToken);
        Task SetLiveOpsAsync(LiveOpsSettings settings, CancellationToken cancellationToken);
        Task<CatalogVersion?> GetLatestCatalogAsync(CancellationToken cancellationToken);
        Task<CatalogVersion?> GetCatalogAsync(int version, CancellationToken cancellationToken);
        Task<int> AddCatalogAsync(string json, string createdBy, string note, CancellationToken cancellationToken);
        Task<IReadOnlyList<CatalogVersion>> ListCatalogsAsync(int limit, CancellationToken cancellationToken);

        // Players (accounts)
        Task<IReadOnlyList<PlayerSummary>> SearchPlayersAsync(string query, int limit, CancellationToken cancellationToken);
        Task<PlayerSummary?> GetPlayerAsync(Guid accountId, CancellationToken cancellationToken);
        Task<(int Total, int Last24h)> CountPlayersAsync(DateTimeOffset now, CancellationToken cancellationToken);
        Task SetBanAsync(Guid accountId, DateTimeOffset? until, string reason, CancellationToken cancellationToken);

        /// <summary>Ban in force at <paramref name="now"/>, or null.</summary>
        Task<(DateTimeOffset Until, string Reason)?> GetActiveBanAsync(Guid accountId, DateTimeOffset now, CancellationToken cancellationToken);
    }

    /// <summary>Tests and local runs. Player data comes from the in-memory account store.</summary>
    public sealed class InMemoryAdminStore : IAdminStore
    {
        private readonly InMemoryAccountStore? _accounts;
        private readonly ConcurrentDictionary<string, AdminUser> _admins = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<AuditEntry> _audit = new();
        private readonly ConcurrentDictionary<string, Coupon> _coupons = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<(string, Guid), bool> _redemptions = new();
        private readonly ConcurrentDictionary<Guid, (DateTimeOffset? Until, string Reason)> _bans = new();
        private readonly List<CatalogVersion> _catalogs = new();
        private readonly object _gate = new();
        private LiveOpsSettings _liveOps = new();

        public InMemoryAdminStore(IAccountStore accounts)
        {
            _accounts = accounts as InMemoryAccountStore;
        }

        public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<int> CountAdminsAsync(CancellationToken cancellationToken) => Task.FromResult(_admins.Count);

        public Task<AdminUser?> FindAdminAsync(string username, CancellationToken cancellationToken) =>
            Task.FromResult(_admins.TryGetValue(username, out var admin) ? admin : null);

        public Task<IReadOnlyList<AdminUser>> ListAdminsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AdminUser>>(_admins.Values.OrderBy(a => a.Username).ToList());

        public Task<bool> CreateAdminAsync(AdminUser admin, CancellationToken cancellationToken) =>
            Task.FromResult(_admins.TryAdd(admin.Username, admin));

        public Task SetAdminDisabledAsync(Guid id, bool disabled, CancellationToken cancellationToken)
        {
            foreach (var pair in _admins.Where(a => a.Value.Id == id).ToList())
                _admins[pair.Key] = pair.Value with { Disabled = disabled };
            return Task.CompletedTask;
        }

        public Task TouchLoginAsync(Guid id, DateTimeOffset at, CancellationToken cancellationToken)
        {
            foreach (var pair in _admins.Where(a => a.Value.Id == id).ToList())
                _admins[pair.Key] = pair.Value with { LastLoginAt = at };
            return Task.CompletedTask;
        }

        public Task AuditAsync(string admin, string action, string target, string details, string ip, CancellationToken cancellationToken)
        {
            lock (_gate)
                _audit.Add(new AuditEntry(_audit.Count + 1, DateTimeOffset.UtcNow, admin, action, target, details, ip));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AuditEntry>> ListAuditAsync(int limit, CancellationToken cancellationToken)
        {
            lock (_gate)
                return Task.FromResult<IReadOnlyList<AuditEntry>>(_audit.AsEnumerable().Reverse().Take(limit).ToList());
        }

        public Task<bool> CreateCouponAsync(Coupon coupon, CancellationToken cancellationToken) =>
            Task.FromResult(_coupons.TryAdd(coupon.Code, coupon));

        public Task<IReadOnlyList<Coupon>> ListCouponsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Coupon>>(_coupons.Values.OrderByDescending(c => c.CreatedAt).ToList());

        public Task SetCouponActiveAsync(string code, bool active, CancellationToken cancellationToken)
        {
            if (_coupons.TryGetValue(code, out var coupon))
                _coupons[code] = coupon with { Active = active };
            return Task.CompletedTask;
        }

        public Task<(RedeemResult Result, Coupon? Coupon)> RedeemAsync(string code, Guid accountId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                if (!_coupons.TryGetValue(code, out var coupon))
                    return Task.FromResult<(RedeemResult, Coupon?)>((RedeemResult.Unknown, null));
                // "You already used it" beats "it ran out" for the player who used the last one.
                if (_redemptions.ContainsKey((code, accountId)))
                    return Task.FromResult<(RedeemResult, Coupon?)>((RedeemResult.AlreadyRedeemed, coupon));
                var verdict = Check(coupon, accountId, now);
                if (verdict != RedeemResult.Redeemed)
                    return Task.FromResult<(RedeemResult, Coupon?)>((verdict, coupon));
                if (!_redemptions.TryAdd((code, accountId), true))
                    return Task.FromResult<(RedeemResult, Coupon?)>((RedeemResult.AlreadyRedeemed, coupon));

                coupon = coupon with { Redeemed = coupon.Redeemed + 1 };
                _coupons[code] = coupon;
                return Task.FromResult<(RedeemResult, Coupon?)>((RedeemResult.Redeemed, coupon));
            }
        }

        public Task UndoRedemptionAsync(string code, Guid accountId, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                if (_redemptions.TryRemove((code, accountId), out _) && _coupons.TryGetValue(code, out var coupon))
                    _coupons[code] = coupon with { Redeemed = Math.Max(0, coupon.Redeemed - 1) };
            }

            return Task.CompletedTask;
        }

        /// <summary>Rules in the order a player should hear about them.</summary>
        internal static RedeemResult Check(Coupon coupon, Guid accountId, DateTimeOffset now)
        {
            if (coupon.TargetAccountId.HasValue && coupon.TargetAccountId.Value != accountId)
                return RedeemResult.NotForThisAccount;
            if (!coupon.Active)
                return RedeemResult.Inactive;
            if (coupon.StartsAt.HasValue && coupon.StartsAt.Value > now)
                return RedeemResult.NotStarted;
            if (coupon.ExpiresAt.HasValue && coupon.ExpiresAt.Value <= now)
                return RedeemResult.Expired;
            if (coupon.MaxRedemptions > 0 && coupon.Redeemed >= coupon.MaxRedemptions)
                return RedeemResult.Exhausted;
            return RedeemResult.Redeemed;
        }

        public Task<LiveOpsSettings> GetLiveOpsAsync(CancellationToken cancellationToken) => Task.FromResult(_liveOps);

        public Task SetLiveOpsAsync(LiveOpsSettings settings, CancellationToken cancellationToken)
        {
            _liveOps = settings;
            return Task.CompletedTask;
        }

        public Task<CatalogVersion?> GetLatestCatalogAsync(CancellationToken cancellationToken)
        {
            lock (_gate)
                return Task.FromResult(_catalogs.LastOrDefault());
        }

        public Task<CatalogVersion?> GetCatalogAsync(int version, CancellationToken cancellationToken)
        {
            lock (_gate)
                return Task.FromResult(_catalogs.FirstOrDefault(c => c.Version == version));
        }

        public Task<int> AddCatalogAsync(string json, string createdBy, string note, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                var version = _catalogs.Count + 1;
                _catalogs.Add(new CatalogVersion(version, json, createdBy, DateTimeOffset.UtcNow, note));
                return Task.FromResult(version);
            }
        }

        public Task<IReadOnlyList<CatalogVersion>> ListCatalogsAsync(int limit, CancellationToken cancellationToken)
        {
            lock (_gate)
                return Task.FromResult<IReadOnlyList<CatalogVersion>>(_catalogs.AsEnumerable().Reverse().Take(limit).ToList());
        }

        public async Task<IReadOnlyList<PlayerSummary>> SearchPlayersAsync(string query, int limit, CancellationToken cancellationToken)
        {
            var result = new List<PlayerSummary>();
            if (_accounts == null)
                return result;

            foreach (var id in _accounts.AccountIds)
            {
                var player = await GetPlayerAsync(id, cancellationToken);
                if (player == null)
                    continue;
                if (string.IsNullOrEmpty(query) || player.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || id.ToString("N").StartsWith(query, StringComparison.OrdinalIgnoreCase))
                    result.Add(player);
            }

            return result.OrderByDescending(p => p.LastLoginAt).Take(limit).ToList();
        }

        public async Task<PlayerSummary?> GetPlayerAsync(Guid accountId, CancellationToken cancellationToken)
        {
            var profile = _accounts == null ? null : await _accounts.GetProfileAsync(accountId, cancellationToken);
            if (profile == null)
                return null;

            _bans.TryGetValue(accountId, out var ban);
            return new PlayerSummary(accountId, profile.DisplayName, profile.CreatedAt, profile.CreatedAt, ban.Until, ban.Reason ?? string.Empty, profile.Providers);
        }

        public Task<(int Total, int Last24h)> CountPlayersAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            var total = _accounts?.AccountIds.Count ?? 0;
            return Task.FromResult((total, total));
        }

        public Task SetBanAsync(Guid accountId, DateTimeOffset? until, string reason, CancellationToken cancellationToken)
        {
            if (until.HasValue)
                _bans[accountId] = (until, reason);
            else
                _bans.TryRemove(accountId, out _);
            return Task.CompletedTask;
        }

        public Task<(DateTimeOffset Until, string Reason)?> GetActiveBanAsync(Guid accountId, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult<(DateTimeOffset, string)?>(_bans.TryGetValue(accountId, out var ban) && ban.Until > now ? (ban.Until!.Value, ban.Reason) : null);
    }
}
