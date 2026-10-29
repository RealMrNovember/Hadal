using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Enums;

namespace HADAL.Shared.Simulation
{
    /// <summary>
    /// Player-versus-player rules (docs/30): shields, beginner protection, plunder, casualties, rallies and
    /// reinforcements. The server resolves every battle at the march's arrival instant.
    /// </summary>
    public static class PvpRules
    {
        public const string Attack = "attack";
        public const string Scout = "scout";
        public const string Reinforce = "reinforce";
        public const string Rally = "rally";
        public const string Join = "join";

        public const string TargetBase = "base";
        public const string TargetTeam = "team";
        public const string TargetStronghold = "stronghold";

        /// <summary>An alliance building or beacon (docs/32 §2.3): reinforce builds or guards it, attack wears it down.</summary>
        public const string TargetStructure = "structure";

        public static bool IsMode(string? mode) => mode is Attack or Scout or Reinforce or Rally or Join;

        public static bool IsTargetKind(string? kind) => kind is TargetBase or TargetTeam or TargetStronghold or TargetStructure;

        // --- Shields (§3) ------------------------------------------------------------------------

        public const string Shield8h = "shield_8h";
        public const string Shield24h = "shield_24h";
        public const string Shield48h = "shield_48h";
        public const string Shield7d = "shield_7d";

        public static readonly string[] ShieldItems = { Shield8h, Shield24h, Shield48h, Shield7d };

        public static bool IsShield(string? id) => id is Shield8h or Shield24h or Shield48h or Shield7d;

        public static TimeSpan ShieldDuration(string id) => id switch
        {
            Shield8h => TimeSpan.FromHours(8),
            Shield24h => TimeSpan.FromHours(24),
            Shield48h => TimeSpan.FromHours(48),
            Shield7d => TimeSpan.FromDays(7),
            _ => TimeSpan.Zero
        };

        /// <summary>Hadalite price of a shield when the bag has none.</summary>
        public static long ShieldHadalite(string id) => id switch
        {
            Shield8h => 300,
            Shield24h => 800,
            Shield48h => 1_500,
            Shield7d => 4_000,
            _ => long.MaxValue
        };

        /// <summary>Below this Core Reactor level a base can neither attack players nor be attacked.</summary>
        public const int BeginnerReactor = 6;

        public static bool IsBeginner(int reactorLevel) => reactorLevel < BeginnerReactor;

        // --- Plunder and casualties (§4.2) -------------------------------------------------------

        public const double PlunderShare = 0.2;
        public const long ProtectedPerReactor = 5_000;

        public static readonly ResourceType[] Lootable = { ResourceType.Titanium, ResourceType.Biomass, ResourceType.Food };

        /// <summary>
        /// What an attacker takes from a base: <see cref="PlunderShare"/> of each lootable resource above the protected
        /// amount, scaled down together so the total fits <paramref name="load"/>.
        /// </summary>
        public static Dictionary<ResourceType, long> Plunder(IReadOnlyDictionary<ResourceType, long> wallet, int reactorLevel, long load)
        {
            var guard = ProtectedPerReactor * Math.Max(1, reactorLevel);
            var want = new Dictionary<ResourceType, long>();
            foreach (var r in Lootable)
            {
                var have = wallet.TryGetValue(r, out var v) ? v : 0;
                var take = (long)Math.Floor(Math.Max(0, have - guard) * PlunderShare);
                if (take > 0)
                    want[r] = take;
            }

            return Fit(want, load);
        }

        /// <summary>Scales <paramref name="amounts"/> down together so they sum to at most <paramref name="load"/>.</summary>
        public static Dictionary<ResourceType, long> Fit(IReadOnlyDictionary<ResourceType, long> amounts, long load)
        {
            var total = amounts.Values.Where(v => v > 0).Sum();
            var result = new Dictionary<ResourceType, long>();
            if (total <= 0 || load <= 0)
                return result;
            var scale = Math.Min(1.0, (double)load / total);
            foreach (var pair in amounts.Where(p => p.Value > 0))
            {
                var v = (long)Math.Floor(pair.Value * scale);
                if (v > 0)
                    result[pair.Key] = v;
            }

            return result;
        }

        /// <summary>Teams outside a base lose half their fallen as dead, half come home wounded.</summary>
        public static (long Dead, long Wounded) SplitOutside(long lost)
        {
            var wounded = lost / 2;
            return (lost - wounded, wounded);
        }

        /// <summary>
        /// Shares a side's losses by "type:tier" among its contributors in proportion to what each brought, rounding so
        /// the shares add up exactly (largest remainders first, ties by contributor order).
        /// </summary>
        public static List<Dictionary<string, long>> ShareLosses(IReadOnlyList<IReadOnlyDictionary<string, long>> contributors,
            IReadOnlyDictionary<string, long> lost)
        {
            var result = contributors.Select(_ => new Dictionary<string, long>(StringComparer.Ordinal)).ToList();
            foreach (var pair in lost.Where(p => p.Value > 0))
            {
                var held = contributors.Select(c => c.TryGetValue(pair.Key, out var n) ? n : 0).ToList();
                var total = held.Sum();
                if (total <= 0)
                    continue;
                var toShare = Math.Min(pair.Value, total);
                var exact = held.Select(h => (double)h * toShare / total).ToList();
                var floors = exact.Select(e => (long)Math.Floor(e)).ToList();
                var left = toShare - floors.Sum();
                foreach (var i in Enumerable.Range(0, held.Count).OrderByDescending(i => exact[i] - floors[i]).ThenBy(i => i))
                {
                    if (left <= 0)
                        break;
                    if (floors[i] < held[i])
                    {
                        floors[i]++;
                        left--;
                    }
                }

                for (var i = 0; i < held.Count; i++)
                    if (floors[i] > 0)
                        result[i][pair.Key] = floors[i];
            }

            return result;
        }

        /// <summary>Shares resources by weight (the survivors' load), largest remainders first.</summary>
        public static List<Dictionary<ResourceType, long>> ShareResources(IReadOnlyDictionary<ResourceType, long> amounts, IReadOnlyList<long> weights)
        {
            var result = weights.Select(_ => new Dictionary<ResourceType, long>()).ToList();
            var total = weights.Sum();
            if (total <= 0)
                return result;
            foreach (var pair in amounts.Where(p => p.Value > 0))
            {
                var exact = weights.Select(w => (double)w * pair.Value / total).ToList();
                var floors = exact.Select(e => (long)Math.Floor(e)).ToList();
                var left = pair.Value - floors.Sum();
                foreach (var i in Enumerable.Range(0, weights.Count).OrderByDescending(i => exact[i] - floors[i]).ThenBy(i => i))
                {
                    if (left-- <= 0)
                        break;
                    floors[i]++;
                }

                for (var i = 0; i < weights.Count; i++)
                    if (floors[i] > 0)
                        result[i][pair.Key] = floors[i];
            }

            return result;
        }

        // --- Rallies and reinforcements (§5) ------------------------------------------------------

        public static readonly int[] MusterOptions = { 60, 180, 300, 600 };

        public static bool IsMuster(int seconds) => Array.IndexOf(MusterOptions, seconds) >= 0;

        public const int MaxRallyTeams = 10;
        public const int MaxReinforcements = 5;

        /// <summary>A scout travels at twice the base speed.</summary>
        public const int ScoutSpeed = 200;

        /// <summary>A reinforcing team stays until recalled; its "gathering" ends this far ahead.</summary>
        public static readonly TimeSpan StationTime = TimeSpan.FromDays(3650);

        public const int WarLogSize = 30;
    }
}
