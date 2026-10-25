using HADAL.Shared.Enums;

namespace Hadal.Game
{
    public static class WalletOps
    {
        /// <summary>Adds amounts (negative allowed) to a wallet, clamped to [0, capacity].</summary>
        public static void Add(IDictionary<ResourceType, long> wallet, IReadOnlyDictionary<ResourceType, long> amounts,
            IReadOnlyDictionary<ResourceType, long> capacities)
        {
            foreach (var pair in amounts)
            {
                var have = wallet.TryGetValue(pair.Key, out var current) ? current : 0L;
                var next = Math.Max(0L, have + pair.Value);
                if (capacities.TryGetValue(pair.Key, out var cap) && cap > 0)
                    next = Math.Min(next, Math.Max(cap, have)); // never confiscate what is already above the cap
                wallet[pair.Key] = next;
            }
        }
    }
}
