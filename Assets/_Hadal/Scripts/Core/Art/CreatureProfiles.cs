using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hadal.Core.Art
{
    /// <summary>How a creature moves: drives both its <c>Hadal/Creature</c> material and <see cref="CreatureMotion"/>.</summary>
    public enum CreatureGait
    {
        /// <summary>Fish: tail sway and body wave in the shader, a slow loop with a bob, turning toward its heading.</summary>
        Swim,

        /// <summary>Floor walkers: a step wobble and a short shuffle; claws snap in the shader.</summary>
        Crawl,

        /// <summary>Jellies and the kraken: bell pulse and tentacle wave in the shader, a slow rise and fall.</summary>
        Drift
    }

    /// <summary>
    /// One world-map creature (art/gpu/creatures.json): its look on the map and its procedural motion. Shader numbers that
    /// depend on the mesh (tentacle start, claw corners) are shares of the mesh bounds, so a regenerated model keeps working.
    /// </summary>
    [Serializable]
    public sealed class CreatureProfile
    {
        public string Id = string.Empty;
        public CreatureGait Gait;

        /// <summary>How tall it stands on the map (metres; a map tile is 8 m).</summary>
        public float MapHeight = 5f;

        public float SwimAmp, SwimFreq = 4f, SwimWaves = 0.8f;

        /// <summary>Tentacles start this far up the mesh (0 = bottom, 1 = top); their wave in metres before map scaling.</summary>
        public float TentacleTop = -1f, TentacleAmp;

        public float PulseAmp;

        /// <summary>Claws: snap height in metres, from this share of the half length forward and this share of the half width out.</summary>
        public float ClawAmp, ClawFront = 0.3f, ClawSide = 0.35f;

        public Color GlowColor = new(0.45f, 1f, 1f);
        public float Glow = 1.2f, GlowThreshold = 0.55f;

        /// <summary>Angler lures: where the bulb hangs as shares of the mesh bounds (x, y, z in 0..1), and its colour.</summary>
        public bool HasLure;
        public Vector3 Lure;
        public Color LureColor = new(0.4f, 1f, 1f);
        public float LureSize = 0.12f;
    }

    /// <summary>Every map creature and which creature stands for which monster kind (MonsterRules).</summary>
    public static class CreatureProfiles
    {
        public static readonly CreatureProfile[] All =
        {
            new()
            {
                Id = "creature_abyss_angler", Gait = CreatureGait.Swim, MapHeight = 4.2f, SwimAmp = 0.09f, SwimFreq = 4.2f,
                GlowColor = new Color(0.8f, 0.45f, 1f), Glow = 1.6f, GlowThreshold = 0.5f,
                HasLure = true, Lure = new Vector3(0.5f, 0.72f, 1.04f), LureColor = new Color(0.45f, 1f, 1f), LureSize = 0.1f
            },
            new()
            {
                Id = "creature_lantern_maw", Gait = CreatureGait.Swim, MapHeight = 6f, SwimAmp = 0.07f, SwimFreq = 3.2f,
                GlowColor = new Color(1f, 0.7f, 0.3f), Glow = 1.8f, GlowThreshold = 0.5f,
                HasLure = true, Lure = new Vector3(0.5f, 0.98f, 0.96f), LureColor = new Color(1f, 0.72f, 0.25f), LureSize = 0.12f
            },
            new()
            {
                Id = "creature_armored_crab", Gait = CreatureGait.Crawl, MapHeight = 4.4f, ClawAmp = 0.12f, ClawFront = 0.25f, ClawSide = 0.3f,
                GlowColor = new Color(0.75f, 0.4f, 1f), Glow = 1.5f, GlowThreshold = 0.45f
            },
            new()
            {
                Id = "creature_glow_angelfish", Gait = CreatureGait.Swim, MapHeight = 1.8f, SwimAmp = 0.12f, SwimFreq = 6f, SwimWaves = 0.6f,
                GlowColor = new Color(0.4f, 1f, 1f), Glow = 2.2f, GlowThreshold = 0.45f
            },
            new()
            {
                Id = "creature_drift_jelly", Gait = CreatureGait.Drift, MapHeight = 3f, TentacleTop = 0.55f, TentacleAmp = 0.08f, PulseAmp = 0.09f,
                GlowColor = new Color(0.7f, 0.5f, 1f), Glow = 2.4f, GlowThreshold = 0.4f
            },
            new()
            {
                Id = "creature_chasm_maw", Gait = CreatureGait.Swim, MapHeight = 11f, SwimAmp = 0.05f, SwimFreq = 2f, SwimWaves = 0.6f,
                GlowColor = new Color(0.4f, 1f, 1f), Glow = 1.8f, GlowThreshold = 0.5f,
                HasLure = true, Lure = new Vector3(0.5f, 0.88f, 0.97f), LureColor = new Color(0.45f, 1f, 0.9f), LureSize = 0.07f
            },
            new()
            {
                Id = "creature_depth_kraken", Gait = CreatureGait.Drift, MapHeight = 15f, TentacleTop = 0.6f, TentacleAmp = 0.22f, PulseAmp = 0.04f,
                GlowColor = new Color(0.4f, 1f, 1f), Glow = 2f, GlowThreshold = 0.45f
            }
        };

        private static readonly Dictionary<string, CreatureProfile> ById = new(StringComparer.Ordinal);

        public static CreatureProfile Get(string id)
        {
            if (ById.Count == 0)
            {
                foreach (var p in All)
                    ById[p.Id] = p;
            }

            return id != null && ById.TryGetValue(id, out var profile) ? profile : null;
        }

        /// <summary>The creature model a monster kind (MonsterRules id) is drawn with.</summary>
        public static string ForMonster(string monsterType) => monsterType switch
        {
            "abyss_angler" => "creature_abyss_angler",
            "lantern_maw" => "creature_lantern_maw",
            "armored_crab" => "creature_armored_crab",
            "chasm_maw" => "creature_chasm_maw",
            "depth_kraken" => "creature_depth_kraken",
            "siren_swarm" => "creature_siren",
            _ => "creature_crawler"
        };

        /// <summary>Bodies in a monster's group on the map: a school of anglers, a pair of maws, one crab, one boss.</summary>
        public static int GroupSize(string monsterType) => monsterType switch
        {
            "lantern_maw" => 2,
            "armored_crab" or "chasm_maw" or "depth_kraken" => 1,
            _ => 3
        };
    }
}
