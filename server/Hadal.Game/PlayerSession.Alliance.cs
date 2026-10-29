using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;

namespace Hadal.Game
{
    /// <summary>What the alliance economy (docs/31: shop, gifts) needs from a session: items into the bag with a report.</summary>
    public sealed partial class PlayerSession
    {
        private IReadOnlyDictionary<string, int>? _allianceResearch;

        /// <summary>
        /// The alliance's research levels (AllianceResearchRules), null without an alliance; the bonuses join the
        /// modifiers at once. <paramref name="push"/> false before the login snapshot, which carries them already.
        /// </summary>
        internal void SetAllianceResearch(IReadOnlyDictionary<string, int>? levels, DateTimeOffset now, bool push = true)
        {
            lock (_gate)
            {
                if (push)
                    AdvanceTo(now); // production so far at the old rates
                _allianceResearch = levels;
                RefreshModifiers();
                if (push)
                    SendDelta(now, new List<EntityChangeDto> { ProductionChange(now), WalletChange() });
            }
        }

        /// <summary>Re-sums the modifiers after a test set research levels directly.</summary>
        internal void RefreshModifiersForTests()
        {
            lock (_gate)
                RefreshModifiers();
        }

        internal PlayerModifiers ModifiersForTests
        {
            get { lock (_gate) return _modifiers; }
        }

        /// <summary>An alliance shop purchase: the items go to the bag and the purchase is answered with a reward report.</summary>
        internal void GrantShopItem(string commandId, ulong sequence, string itemId, int count, DateTimeOffset now)
        {
            lock (_gate)
            {
                AdvanceTo(now);
                GrantAndReport(commandId, sequence, new List<ResolvedReward> { new("item", itemId, count) }, now);
            }
        }

        /// <summary>Collected alliance gifts: their chests go to the bag, answered with a reward report.</summary>
        internal void GrantGiftChests(string commandId, ulong sequence, IReadOnlyDictionary<string, int> chests, DateTimeOffset now)
        {
            lock (_gate)
            {
                AdvanceTo(now);
                GrantAndReport(commandId, sequence, chests.Select(p => new ResolvedReward("item", p.Key, p.Value)).ToList(), now);
            }
        }
    }
}
