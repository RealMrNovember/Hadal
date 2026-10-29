using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Simulation;

namespace Hadal.Game
{
    /// <summary>VIP (VipRules): sign-in points with a streak, the daily VIP chest, VIP point items and purchases.</summary>
    public sealed partial class PlayerSession
    {
        private int VipLevel => VipRules.Level(_state.VipPoints);

        private static long UtcDay(DateTimeOffset now) => now.ToUnixTimeSeconds() / 86_400;

        private VipStateDto VipDto(DateTimeOffset now)
        {
            var day = UtcDay(now);
            var streak = _state.VipLoginDay == day - 1 ? _state.VipStreak + 1 : _state.VipLoginDay == day ? _state.VipStreak : 1;
            return new VipStateDto
            {
                Points = _state.VipPoints,
                Level = VipLevel,
                Streak = _state.VipLoginDay >= day - 1 ? _state.VipStreak : 0,
                LoginReady = _state.VipLoginDay != day,
                LoginPoints = VipRules.LoginPoints(streak),
                ChestReady = VipLevel > 0 && _state.VipChestDay != day,
                ResetInMs = (day + 1) * 86_400_000L - now.ToUnixTimeMilliseconds()
            };
        }

        private EntityChangeDto VipChange(DateTimeOffset now) => new()
        {
            EntityId = EntityIds.Vip,
            Kind = EntityChangeKind.Update,
            Payload = _serializer.Serialize(VipDto(now))
        };

        /// <summary>Adds VIP points; a new level brings its bonuses at once. Caller holds the lock.</summary>
        private void AddVipPoints(long points)
        {
            if (points <= 0)
                return;
            var before = VipLevel;
            _state.VipPoints += points;
            _state.IsDirty = true;
            if (VipLevel != before)
                RefreshModifiers();
        }

        private void ClaimVipLogin(ClaimVipLoginCommand command, DateTimeOffset now)
        {
            var day = UtcDay(now);
            if (_state.VipLoginDay == day)
            {
                Reject(command.CommandId, command.ClientSequence, "AlreadyClaimed", now);
                return;
            }

            _state.VipStreak = _state.VipLoginDay == day - 1 ? _state.VipStreak + 1 : 1;
            _state.VipLoginDay = day;
            var points = VipRules.LoginPoints(_state.VipStreak);
            AddVipPoints(points);
            GrantAndReport(command.CommandId, command.ClientSequence, new List<ResolvedReward> { new("vip", "points", points) }, now, grant: false);
            SendDelta(now, new List<EntityChangeDto> { VipChange(now) });
        }

        private void ClaimVipChest(ClaimVipChestCommand command, DateTimeOffset now)
        {
            var day = UtcDay(now);
            var reason = VipLevel <= 0 ? "VipTooLow" : _state.VipChestDay == day ? "AlreadyClaimed" : null;
            if (reason != null)
            {
                Reject(command.CommandId, command.ClientSequence, reason, now);
                return;
            }

            _state.VipChestDay = day;
            GrantAndReport(command.CommandId, command.ClientSequence, QuestRules.Resolve(_catalog, VipRules.DailyChest(VipLevel), Random.Shared.Next), now);
            SendDelta(now, new List<EntityChangeDto> { VipChange(now) });
        }

        /// <summary>Uses VIP point items from the bag. Caller holds the lock.</summary>
        private void OpenVipItem(OpenItemCommand command, long points, int count, DateTimeOffset now)
        {
            if ((_state.Items.TryGetValue(command.ItemId, out var held) ? held : 0) < count)
            {
                Reject(command.CommandId, command.ClientSequence, QuestRejectReason.NotEnoughItems.ToString(), now);
                return;
            }

            TakeItem(command.ItemId, count);
            AddVipPoints(points * count);
            GrantAndReport(command.CommandId, command.ClientSequence, new List<ResolvedReward> { new("vip", "points", points * count) }, now, grant: false);
            SendDelta(now, new List<EntityChangeDto> { VipChange(now) });
        }

        /// <summary>A shop purchase (or the Overseer crediting one): its Hadalite to the wallet, as many VIP points.</summary>
        internal void CreditPurchase(StorePacks.Pack pack, DateTimeOffset now)
        {
            lock (_gate)
            {
                ApplyPurchase(_state, pack);
                RefreshModifiers();
                _walletDirty = true;
                SendDelta(now, new List<EntityChangeDto> { WalletChange(), VipChange(now), ProductionChange(now) });
            }
        }

        /// <summary>
        /// Pays a base teleport: its bag item, or its Hadalite price when the player has none. Returns a rejection
        /// reason, or null when paid.
        /// </summary>
        internal string? PayTeleport(string mode, DateTimeOffset now)
        {
            lock (_gate)
            {
                var item = TeleportRules.ItemFor(mode);
                if ((_state.Items.TryGetValue(item, out var held) ? held : 0) > 0)
                {
                    TakeItem(item, 1);
                    SendDelta(now, new List<EntityChangeDto> { InventoryChange() });
                    return null;
                }

                var price = TeleportRules.HadaliteFor(mode);
                if ((_state.Wallet.TryGetValue(ResourceType.Hadalite, out var hadalite) ? hadalite : 0) < price)
                    return "NotEnoughHadalite";
                _state.Wallet[ResourceType.Hadalite] = hadalite - price;
                _state.IsDirty = true;
                Score(EventActions.HadaliteSpent, price);
                SendDelta(now, new List<EntityChangeDto> { WalletChange() });
                return null;
            }
        }

        /// <summary>Sets the VIP points outright (the Overseer); the level follows from them.</summary>
        internal void SetVipPoints(long points, DateTimeOffset now)
        {
            lock (_gate)
            {
                _state.VipPoints = Math.Max(0, points);
                _state.IsDirty = true;
                RefreshModifiers();
                SendDelta(now, new List<EntityChangeDto> { VipChange(now), ProductionChange(now) });
            }
        }

        /// <summary>The state change of a purchase, for live and offline players alike.</summary>
        internal static void ApplyPurchase(PlayerState state, StorePacks.Pack pack)
        {
            state.Wallet[ResourceType.Hadalite] = (state.Wallet.TryGetValue(ResourceType.Hadalite, out var h) ? h : 0) + pack.Hadalite + pack.Bonus;
            state.VipPoints += pack.VipPoints;
            state.IsDirty = true;
        }
    }
}
