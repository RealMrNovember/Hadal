using System.Collections.Generic;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using Hadal.Core.Catalog;

namespace Hadal.UI
{
    /// <summary>
    /// The attack setup against a monster or an event boss: the two powers, the Sonar cost, and a preview of the fight
    /// (MonsterBattle on the shared CombatEstimate, the same deterministic rules the server fights with): Wins / Hard /
    /// Loses, and for a boss the damage this team would take off its shared body, with a warning when it would be none.
    /// The server still resolves the real battle when the team sets out.
    /// </summary>
    public sealed partial class WorldMapView
    {
        private string MonsterSetupText(WorldMonsterDto monster, IReadOnlyDictionary<string, long> aboard, IReadOnlyList<int> heroPowers, int trip, out bool sonarShort)
        {
            var troops = ClientCatalog.Catalog.Troops;
            var kind = MonsterRules.Kind(monster.Type);
            var cost = kind?.Sonar ?? 40;
            sonarShort = SonarNow() < cost;
            var mine = TroopRules.Power(troops, aboard);
            var text = $"{Loc.F("march.vs", mine.ToString("N0"), monster.Power.ToString("N0"))}\n" +
                       $"<color={(sonarShort ? "#FF8A80" : "#8CD9FF")}>{Loc.T("world.sonar")} {cost} ({SonarNow()})</color>   {Loc.F("world.monster.trip", Loc.Duration(trip))}";
            if (kind == null)
                return text;

            var boss = kind.EventOnly;
            var bodyLeft = boss ? monster.HpLeft : 0;
            var outlook = MonsterBattle.Outlook(troops, aboard, heroPowers, kind, monster.Level, bodyLeft);
            var line = $"<color={OutlookColor(outlook)}>{Loc.T("pvp.estimate." + outlook.ToString().ToLowerInvariant())}</color>";
            if (boss)
            {
                var damage = MonsterBattle.Damage(troops, aboard, heroPowers, kind, monster.Level, bodyLeft);
                line += damage > 0
                    ? $"   {Loc.F("world.estimate.damage", damage.ToString("N0"))}"
                    : $"   <color=#FF8A80>{Loc.T("world.estimate.nodamage")}</color>";
            }

            return text + "\n" + line;
        }

        private static string OutlookColor(CombatOutlook outlook) => outlook switch
        {
            CombatOutlook.Win => "#5BE37A",
            CombatOutlook.Hard => "#FFB347",
            CombatOutlook.Lose => "#FF8A80",
            _ => "#8CA3AD"
        };
    }
}
