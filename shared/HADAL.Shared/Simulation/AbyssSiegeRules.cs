using System;
using System.Collections.Generic;
using System.Linq;

namespace HADAL.Shared.Simulation
{
    /// <summary>
    /// The Abyss Siege (Derinlik Kuşatması, docs/29 §4.2), shared by the server and the client. The leader or a war-duty
    /// officer sets a start and a difficulty; then <see cref="Waves"/> waves of creatures come, one every
    /// <see cref="WaveSeconds"/> seconds, against the base of every member. Elite waves hit only members who are online;
    /// hub waves hit the Alliance Hub, held by the teams reinforcing it. Shields do not help, nothing is plundered and
    /// nobody dies (the fallen are wounded); a base breached <see cref="BreachLimit"/> times drops out. Kills at your own
    /// base score in full, kills while reinforcing at <see cref="ReinforceShare"/>.
    /// </summary>
    public static class AbyssSiegeRules
    {
        public const int Waves = 20;
        public const int WaveSeconds = 120;
        public const int MaxDifficulty = 10;
        public const int BreachLimit = 2;
        public const double ReinforceShare = 0.5;
        public const int MinLeadMinutes = 10;
        public const int DefaultMinReactor = 10;
        public const int DefaultMinMembers = 5;

        /// <summary>Elite waves are half again as large, and strike harder.</summary>
        private const double EliteScale = 1.5;

        /// <summary>A hub wave is three bases' worth of creatures.</summary>
        private const double HubScale = 3;

        public static bool IsHubWave(int wave) => wave == 10 || wave == 20;

        public static bool IsEliteWave(int wave) => wave == 7 || wave == 14 || wave == 17;

        public static bool IsDifficulty(int difficulty) => difficulty >= 1 && difficulty <= MaxDifficulty;

        /// <summary>How long the whole siege runs, first wave to last.</summary>
        public static long LengthMs => (Waves - 1) * WaveSeconds * 1000L;

        /// <summary>When wave <paramref name="wave"/> (1-based) strikes.</summary>
        public static long WaveAtMs(long startsAtMs, int wave) => startsAtMs + (wave - 1) * WaveSeconds * 1000L;

        /// <summary>A start must leave members time to prepare, and every wave must fall inside the event.</summary>
        public static bool CanStart(long startMs, long nowMs, long eventEndsMs) =>
            startMs >= nowMs + MinLeadMinutes * 60_000L && startMs + LengthMs < eventEndsMs;

        /// <summary>The creatures' level: two per difficulty step, one more every third wave, within the map's range.</summary>
        public static int Level(int difficulty, int wave) => Math.Clamp(2 * difficulty + wave / 3 + (IsEliteWave(wave) ? 3 : 0), 1, 35);

        /// <summary>The deeper the level, the worse the creature (all roaming kinds, MonsterRules).</summary>
        public static MonsterKind Kind(int level)
        {
            var id = level >= 20 ? "lantern_maw" : level >= 10 ? "abyss_angler" : "abyss_crawler";
            return MonsterRules.Kind(id)!;
        }

        /// <summary>The wave against one base (or, on a hub wave, the hub).</summary>
        public static BattleSide Army(int difficulty, int wave)
        {
            var level = Level(difficulty, wave);
            var side = MonsterRules.Army(Kind(level), level);
            var scale = IsHubWave(wave) ? HubScale : IsEliteWave(wave) ? EliteScale : 1;
            foreach (var key in side.Troops.Keys.ToList())
                side.Troops[key] *= scale;
            return side;
        }

        /// <summary>Whether a member's base still takes waves.</summary>
        public static bool DroppedOut(int breaches) => breaches >= BreachLimit;
    }
}
