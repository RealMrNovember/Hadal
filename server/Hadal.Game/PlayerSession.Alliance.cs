using HADAL.Shared.Simulation;

namespace Hadal.Game
{
    /// <summary>What the alliance economy (docs/31: shop, gifts) needs from a session: items into the bag with a report.</summary>
    public sealed partial class PlayerSession
    {
        /// <summary>An alliance shop purchase: the items go to the bag and the purchase is answered with a reward report.</summary>
        internal void GrantShopItem(string commandId, ulong sequence, string itemId, int count, DateTimeOffset now)
        {
            lock (_gate)
            {
                AdvanceTo(now);
                GrantAndReport(commandId, sequence, new List<ResolvedReward> { new("item", itemId, count) }, now);
            }
        }
    }
}
