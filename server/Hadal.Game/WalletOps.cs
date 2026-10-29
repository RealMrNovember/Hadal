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
                var next = Math.Max(0L, Sum(have, pair.Value));
                if (capacities.TryGetValue(pair.Key, out var cap) && cap > 0)
                    next = Math.Min(next, Math.Max(cap, have)); // never confiscate what is already above the cap
                wallet[pair.Key] = next;
            }
        }

        /// <summary>
        /// Adds an amount past any cap (grants, capsules, refunds, purchases): never below zero and never wrapping
        /// around on overflow, which would turn a huge balance into nothing.
        /// </summary>
        public static void Credit(IDictionary<ResourceType, long> wallet, ResourceType type, long amount)
        {
            var have = wallet.TryGetValue(type, out var current) ? current : 0L;
            wallet[type] = Math.Max(0L, Sum(have, amount));
        }

        /// <summary>a + b, saturating at long.MinValue and long.MaxValue instead of wrapping.</summary>
        public static long Sum(long a, long b)
        {
            var sum = unchecked(a + b);
            // Overflow happened when both inputs share a sign and the result does not.
            if (((a ^ sum) & (b ^ sum)) < 0)
                return a < 0 ? long.MinValue : long.MaxValue;
            return sum;
        }

        /// <summary>a * b for non-negative counts, saturating at long.MaxValue.</summary>
        public static long Product(long a, long b)
        {
            if (a <= 0 || b <= 0)
                return 0;
            return a > long.MaxValue / b ? long.MaxValue : a * b;
        }
    }
}
