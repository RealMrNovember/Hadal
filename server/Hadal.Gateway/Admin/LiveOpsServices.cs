using System.Security.Cryptography;
using Hadal.Game;
using HADAL.Shared.Auth;
using HADAL.Shared.Enums;

namespace Hadal.Gateway.Admin
{
    /// <summary>Maintenance / announcement switches, cached for the version manifest (read on every client start).</summary>
    public sealed class LiveOpsCache
    {
        private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(5);

        private readonly IAdminStore _store;
        private readonly IClock _clock;
        private (LiveOpsSettings Value, DateTimeOffset LoadedAt)? _cached;

        public LiveOpsCache(IAdminStore store, IClock clock)
        {
            _store = store;
            _clock = clock;
        }

        public async Task<LiveOpsSettings> GetAsync(CancellationToken cancellationToken)
        {
            var cached = _cached;
            if (cached.HasValue && _clock.UtcNow - cached.Value.LoadedAt < Ttl)
                return cached.Value.Value;

            var value = await _store.GetLiveOpsAsync(cancellationToken);
            _cached = (value, _clock.UtcNow);
            return value;
        }

        public async Task SetAsync(LiveOpsSettings settings, CancellationToken cancellationToken)
        {
            await _store.SetLiveOpsAsync(settings, cancellationToken);
            _cached = (settings, _clock.UtcNow);
        }
    }

    /// <summary>Gift codes: atomic redemption in the store, then the rewards go through GameWorld (live or offline).</summary>
    public sealed class CouponService
    {
        private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

        private readonly IAdminStore _store;
        private readonly GameWorld _world;
        private readonly IClock _clock;

        public CouponService(IAdminStore store, GameWorld world, IClock clock)
        {
            _store = store;
            _world = world;
            _clock = clock;
        }

        public static string NewCode()
        {
            Span<char> code = stackalloc char[10];
            for (var i = 0; i < code.Length; i++)
                code[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
            return new string(code);
        }

        public static string Normalise(string code) =>
            (code ?? string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty).Trim().ToUpperInvariant();

        public async Task<(string Error, Coupon? Coupon)> RedeemAsync(string code, Guid accountId, CancellationToken cancellationToken)
        {
            code = Normalise(code);
            if (code.Length is 0 or > 32)
                return (AccountErrors.CouponUnknown, null);

            var (result, coupon) = await _store.RedeemAsync(code, accountId, _clock.UtcNow, cancellationToken);
            switch (result)
            {
                case RedeemResult.Unknown: return (AccountErrors.CouponUnknown, null);
                case RedeemResult.Inactive: return (AccountErrors.CouponInactive, null);
                case RedeemResult.Expired: return (AccountErrors.CouponExpired, null);
                case RedeemResult.Exhausted: return (AccountErrors.CouponExhausted, null);
                case RedeemResult.AlreadyRedeemed: return (AccountErrors.CouponAlreadyRedeemed, null);
                case RedeemResult.NotStarted: return (AccountErrors.CouponNotStarted, null);
                case RedeemResult.NotForThisAccount: return (AccountErrors.CouponNotForYou, null);
            }

            // Coupon gifts land in the bag as packages of exactly their amount.
            if (!await _world.GrantAsync(accountId.ToString("N"), coupon!.Rewards, cancellationToken, asPackages: true))
            {
                // The account never entered the game yet: give the code back rather than lose it.
                await _store.UndoRedemptionAsync(code, accountId, cancellationToken);
                return ("PlayerNotStarted", null);
            }

            return (string.Empty, coupon);
        }

        public static CouponRedeemResponse ToResponse(Coupon coupon) => new()
        {
            code = coupon.Code,
            rewards = coupon.Rewards.Select(r => new CouponRewardEntry { resource = r.Key.ToString(), amount = r.Value }).ToArray()
        };
    }
}
