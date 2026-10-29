using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Catalog;

namespace HADAL.Shared.Simulation
{
    /// <summary>
    /// A PvE battle built one way for both sides (docs/rehber 07 §4): the server resolves the real fight with it, and the
    /// attack setup previews it with <see cref="CombatEstimate"/> on the same <see cref="Enemy"/>. PvE battles are
    /// unseeded and deterministic, so a preview with the same team, heroes and monster is what the server resolves at
    /// that moment; the server stays the authority (a boss's body may shrink before the team leaves).
    /// </summary>
    public static class MonsterBattle
    {
        /// <summary>The team: troops aboard with the heroes' command bonus (CombatRules.HeroBonus) on all three stats.</summary>
        public static BattleSide Team(IReadOnlyDictionary<string, long> aboard, IReadOnlyList<int> heroPowers)
        {
            var side = new BattleSide();
            foreach (var pair in aboard.Where(p => p.Value > 0))
                side.Troops[pair.Key] = pair.Value;
            var bonus = CombatRules.HeroBonus(heroPowers);
            side.AtkPercent = side.DefPercent = side.HpPercent = bonus;
            return side;
        }

        /// <summary>The monster's side; an event boss fields only what earlier hits have left of its body.</summary>
        public static BattleSide Enemy(MonsterKind kind, int level, long bossLeft = 0)
        {
            var enemy = MonsterRules.Army(kind, level);
            if (kind.EventOnly && bossLeft > 0)
                enemy.Troops[TroopRules.Key(kind.CountsAs, MonsterRules.Tier(level))] = Math.Min(MonsterRules.Count(kind, level), bossLeft);
            return enemy;
        }

        public static BattleResult Fight(TroopCatalogData troops, IReadOnlyDictionary<string, long> aboard, IReadOnlyList<int> heroPowers,
            MonsterKind kind, int level, long bossLeft = 0) =>
            CombatRules.Resolve(troops, Team(aboard, heroPowers), Enemy(kind, level, bossLeft));

        /// <summary>The troops this team would destroy: for an event boss, the damage it takes off the shared body.</summary>
        public static long Damage(TroopCatalogData troops, IReadOnlyDictionary<string, long> aboard, IReadOnlyList<int> heroPowers,
            MonsterKind kind, int level, long bossLeft = 0) =>
            aboard.Values.Sum() <= 0 ? 0 : Fight(troops, aboard, heroPowers, kind, level, bossLeft).DefenderLost.Values.Sum();

        /// <summary>Win / hard / lose before sending the team (the shared <see cref="CombatEstimate"/>, as PvP uses).</summary>
        public static CombatOutlook Outlook(TroopCatalogData troops, IReadOnlyDictionary<string, long> aboard, IReadOnlyList<int> heroPowers,
            MonsterKind kind, int level, long bossLeft = 0) =>
            CombatEstimate.Of(troops, aboard, heroPowers, Enemy(kind, level, bossLeft));
    }
}
