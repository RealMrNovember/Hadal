using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Catalog;

namespace HADAL.Shared.Simulation
{
    /// <summary>What a pre-battle estimate says (docs/33 §6).</summary>
    public enum CombatOutlook
    {
        /// <summary>Nothing known about the other side (no scout report).</summary>
        Unknown = 0,
        Win = 1,

        /// <summary>A win that costs more than half the team, or a loss by a narrow margin.</summary>
        Hard = 2,
        Lose = 3
    }

    /// <summary>
    /// A pre-battle estimate (docs/33 §6): the same <see cref="CombatRules"/> the server uses, run without a seed on what the
    /// player can see (their own team, a scouted or visible enemy). The real battle uses the army as it is at the arrival
    /// instant, so this is an estimate, never a promise.
    /// </summary>
    public static class CombatEstimate
    {
        /// <summary>A win where the attacker loses more than this share of the team counts as hard.</summary>
        public const double HardLossShare = 0.5;

        public static CombatOutlook Of(TroopCatalogData troops, IReadOnlyDictionary<string, long> mine, IReadOnlyList<int> myHeroPowers, BattleSide? enemy)
        {
            if (enemy == null || enemy.Troops.Values.Sum() <= 0)
                return enemy == null ? CombatOutlook.Unknown : CombatOutlook.Win;
            var side = new BattleSide();
            foreach (var pair in mine.Where(p => p.Value > 0))
                side.Troops[pair.Key] = pair.Value;
            if (side.Troops.Count == 0)
                return CombatOutlook.Lose;
            side.AtkPercent = side.DefPercent = side.HpPercent = CombatRules.HeroBonus(myHeroPowers ?? Array.Empty<int>());
            var result = CombatRules.Resolve(troops, side, enemy);
            var start = side.Troops.Values.Sum();
            var lost = result.AttackerLost.Values.Sum();
            if (result.AttackerWon)
                return lost > HardLossShare * start ? CombatOutlook.Hard : CombatOutlook.Win;
            // A loss where the enemy was left nearly broken is still worth calling hard.
            var enemyStart = enemy.Troops.Values.Sum();
            var enemyLost = result.DefenderLost.Values.Sum();
            return enemyStart > 0 && enemyLost > 0.6 * enemyStart ? CombatOutlook.Hard : CombatOutlook.Lose;
        }

        /// <summary>An enemy army from troop stacks ("type:tier" → count) with a hero bonus percent.</summary>
        public static BattleSide Army(IEnumerable<KeyValuePair<string, long>> stacks, double bonusPercent = 0)
        {
            var side = new BattleSide { AtkPercent = bonusPercent, DefPercent = bonusPercent, HpPercent = bonusPercent };
            foreach (var pair in stacks.Where(p => p.Value > 0))
                side.Troops[pair.Key] = (side.Troops.TryGetValue(pair.Key, out var n) ? n : 0) + pair.Value;
            return side;
        }
    }
}
