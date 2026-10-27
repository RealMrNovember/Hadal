using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Catalog;

namespace HADAL.Shared.Simulation
{
    /// <summary>One side of a battle: troops by "type:tier" and the percent bonuses that apply to all of them.</summary>
    public sealed class BattleSide
    {
        public Dictionary<string, double> Troops { get; } = new(StringComparer.Ordinal);
        public double AtkPercent { get; set; }
        public double DefPercent { get; set; }
        public double HpPercent { get; set; }

        /// <summary>Monster traits (docs/rehber 07 §3 enemies): multipliers on the base stats.</summary>
        public double AtkScale { get; set; } = 1;
        public double HpScale { get; set; } = 1;
        public double DefScale { get; set; } = 1;
    }

    public sealed class BattleResult
    {
        public bool AttackerWon { get; set; }
        public int Rounds { get; set; }

        /// <summary>Troops lost by "type:tier", rounded per stack (docs/rehber 07 §4.1 step 5).</summary>
        public Dictionary<string, long> AttackerLost { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, long> DefenderLost { get; } = new(StringComparer.Ordinal);
    }

    /// <summary>
    /// Battle resolution (docs/rehber 07 §4): deterministic, at most 10 rounds; both sides strike at once with the
    /// numbers they had at the start of the round; front rows (armored, drillers) shield the back until they fall
    /// under 10% of their start; a side under 30% of its start routs; otherwise the higher share of power left
    /// wins and a tie goes to the defender. Checked against the guide's worked battles (07 §4.6).
    /// </summary>
    public static class CombatRules
    {
        public const double BaseAtk = 30, BaseDef = 30, BaseHp = 100;
        public const double RoundFactor = 0.4;
        public const int MaxRounds = 10;
        public const double RoutShare = 0.3;
        public const double FrontFallShare = 0.1;
        public const double Advantage = 1.2, Disadvantage = 0.85, DrillerOnTroops = 0.6;

        /// <summary>Who each type beats (the drillers stand outside the triangle).</summary>
        private static readonly Dictionary<string, string> Beats = new(StringComparer.Ordinal)
        {
            ["armored"] = "arc",
            ["harpoon"] = "armored",
            ["arc"] = "harpoon"
        };

        public static bool IsFront(string type) => type is "armored" or "driller";

        /// <summary>Share of a type's damage that goes to the front row (07 §4.3).</summary>
        public static double FrontShare(string type) => type switch
        {
            "harpoon" => 0.85,
            "arc" => 0.5,
            "driller" => 1.0,
            _ => 0.7
        };

        public static double Counter(string attacker, string defender)
        {
            if (attacker == "driller")
                return DrillerOnTroops;
            if (Beats.TryGetValue(attacker, out var prey) && prey == defender)
                return Advantage;
            if (Beats.TryGetValue(defender, out var hunter) && hunter == attacker)
                return Disadvantage;
            return 1;
        }

        /// <summary>A troop's ATK, DEF, HP: base × type multiplier × tier power / 3, rounded (07 §3.1).</summary>
        public static (double Atk, double Def, double Hp) Stats(TroopCatalogData troops, string type, int tier)
        {
            var t = TroopRules.Type(troops, type);
            var power = TroopRules.Tier(troops, tier)?.power ?? 1;
            var scale = power / 3.0;
            return (Math.Round(BaseAtk * (t?.atk ?? 1) * scale), Math.Round(BaseDef * (t?.def ?? 1) * scale), Math.Round(BaseHp * (t?.hp ?? 1) * scale));
        }

        public static BattleResult Resolve(TroopCatalogData troops, BattleSide attacker, BattleSide defender, int maxRounds = MaxRounds)
        {
            var sides = new[] { attacker, defender };
            var counts = sides.Select(s => s.Troops.Where(p => p.Value > 0).ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal)).ToArray();
            var start = counts.Select(c => new Dictionary<string, double>(c, StringComparer.Ordinal)).ToArray();
            var stats = new Dictionary<string, (double Atk, double Def, double Hp)>[2];
            for (var i = 0; i < 2; i++)
            {
                stats[i] = new Dictionary<string, (double, double, double)>(StringComparer.Ordinal);
                foreach (var key in counts[i].Keys)
                {
                    TroopRules.TryParseKey(key, out var type, out var tier);
                    var (atk, def, hp) = Stats(troops, type, tier);
                    var s = sides[i];
                    stats[i][key] = (atk * s.AtkScale * (1 + s.AtkPercent / 100), def * s.DefScale * (1 + s.DefPercent / 100), Math.Max(1, hp * s.HpScale * (1 + s.HpPercent / 100)));
                }
            }

            var routed = new bool[2];
            var round = 0;
            for (round = 1; round <= maxRounds; round++)
            {
                var losses = new[] { new Dictionary<string, double>(StringComparer.Ordinal), new Dictionary<string, double>(StringComparer.Ordinal) };
                for (var i = 0; i < 2; i++)
                {
                    var j = 1 - i;
                    var frontStart = start[j].Where(p => IsFront(TypeOf(p.Key))).Sum(p => p.Value);
                    var front = counts[j].Where(p => p.Value > 0 && IsFront(TypeOf(p.Key))).ToList();
                    var back = counts[j].Where(p => p.Value > 0 && !IsFront(TypeOf(p.Key))).ToList();
                    double frontSum = front.Sum(p => p.Value), backSum = back.Sum(p => p.Value);
                    var fallen = frontStart > 0 && frontSum < FrontFallShare * frontStart;

                    foreach (var pair in counts[i])
                    {
                        if (pair.Value <= 0)
                            continue;
                        var type = TypeOf(pair.Key);
                        var atk = stats[i][pair.Key].Atk;
                        var toFront = FrontShare(type);
                        if (backSum <= 0)
                            toFront = 1;
                        if (frontSum <= 0 || fallen)
                            toFront = 0;
                        Hit(front, frontSum, toFront);
                        Hit(back, backSum, 1 - toFront);

                        void Hit(List<KeyValuePair<string, double>> row, double rowSum, double share)
                        {
                            if (share <= 0 || rowSum <= 0)
                                return;
                            foreach (var target in row)
                            {
                                var (_, def, hp) = stats[j][target.Key];
                                var damage = pair.Value * atk * atk / (atk + def) * Counter(type, TypeOf(target.Key)) * RoundFactor * share * target.Value / rowSum;
                                losses[j][target.Key] = (losses[j].TryGetValue(target.Key, out var l) ? l : 0) + damage / hp;
                            }
                        }
                    }
                }

                for (var i = 0; i < 2; i++)
                {
                    foreach (var key in counts[i].Keys.ToList())
                        counts[i][key] = Math.Max(0, counts[i][key] - (losses[i].TryGetValue(key, out var l) ? l : 0));
                    routed[i] = counts[i].Values.Sum() < RoutShare * start[i].Values.Sum();
                }

                if (routed[0] || routed[1])
                    break;
            }

            var result = new BattleResult { Rounds = Math.Min(round, maxRounds) };
            if (routed[0] != routed[1])
                result.AttackerWon = routed[1];
            else
                result.AttackerWon = PowerLeft(troops, counts[0], start[0]) > PowerLeft(troops, counts[1], start[1]);
            foreach (var key in start[0].Keys)
                result.AttackerLost[key] = (long)Math.Round(start[0][key] - counts[0][key]);
            foreach (var key in start[1].Keys)
                result.DefenderLost[key] = (long)Math.Round(start[1][key] - counts[1][key]);
            return result;
        }

        private static string TypeOf(string key) => TroopRules.TryParseKey(key, out var type, out _) ? type : key;

        private static double PowerLeft(TroopCatalogData troops, Dictionary<string, double> now, Dictionary<string, double> start)
        {
            double Power(Dictionary<string, double> c) => c.Sum(p => TroopRules.TryParseKey(p.Key, out _, out var tier) ? p.Value * (TroopRules.Tier(troops, tier)?.power ?? 1) : 0);
            var total = Power(start);
            return total <= 0 ? 0 : Power(now) / total;
        }

        /// <summary>
        /// The leader's troops get a percent bonus from the team's heroes (05 §3.2): the first hero in full, the others
        /// at 0.4. Our heroes carry one power figure, so the bonus is power / 50 percent (a level-1 rare ≈ +3%).
        /// </summary>
        public static double HeroBonus(IReadOnlyList<int> heroPowers)
        {
            double total = 0;
            for (var i = 0; i < heroPowers.Count; i++)
                total += heroPowers[i] * (i == 0 ? 1 : 0.4);
            return total / 50.0;
        }
    }
}
