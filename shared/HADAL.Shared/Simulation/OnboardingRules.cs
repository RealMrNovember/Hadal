using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Catalog;

namespace HADAL.Shared.Simulation
{
    /// <summary>Where a player stands with the first-session guide (docs/34).</summary>
    public enum OnboardingStatus
    {
        /// <summary>Not decided yet: an account from before the guide, decided on its next login.</summary>
        Unknown = 0,
        Active = 1,
        Done = 2,

        /// <summary>An existing account already past the guide's milestone; it never sees the guide.</summary>
        Skipped = 3
    }

    /// <summary>A feature that opens at a reactor level, for the "Coming up" list and the lock badges (docs/34 §3).</summary>
    public readonly struct FeatureUnlock
    {
        public FeatureUnlock(string key, string id, int reactorLevel)
        {
            Key = key;
            Id = id;
            ReactorLevel = reactorLevel;
        }

        /// <summary>"building" (Id = building id), "gather", "pvp" or "ring" (Id = ring number).</summary>
        public string Key { get; }

        public string Id { get; }
        public int ReactorLevel { get; }
    }

    /// <summary>The first-session guide (docs/34): who takes it and what opens next.</summary>
    public static class OnboardingRules
    {
        /// <summary>
        /// An account from before the guide with its reactor at this level or higher has done everything the guide's
        /// base steps teach; it is marked skipped instead of being walked through them.
        /// </summary>
        public const int SkipReactorLevel = 4;

        /// <summary>Or with this many chapter tasks claimed.</summary>
        public const int SkipChapterIndex = 3;

        /// <summary>Funnel step numbers: 0 = the guide started, 1..n = step n claimed.</summary>
        public const int StartStep = 0;

        public static bool ShouldSkip(int reactorLevel, int chapterIndex) =>
            reactorLevel >= SkipReactorLevel || chapterIndex >= SkipChapterIndex;

        /// <summary>The buildings whose first copy is worth announcing (the rest are more of the same).</summary>
        private static readonly string[] Announced =
        {
            "research_center", "submarine_dock", "radar_center", "armor_yard", "military_dome", "shield_generator",
            "med_bay", "alliance_hub", "mega_dome", "agriculture_dome"
        };

        /// <summary>
        /// Everything that opens above <paramref name="reactorLevel"/>, nearest first: key buildings (their first copy),
        /// the first gathering queue, base rings. At most <paramref name="max"/>.
        /// </summary>
        public static List<FeatureUnlock> Upcoming(GameCatalog catalog, int reactorLevel, int max = 5)
        {
            var all = new List<FeatureUnlock>();
            foreach (var id in Announced)
            {
                if (catalog.TryGetBuilding(id, out var building))
                    all.Add(new FeatureUnlock("building", id, building.UnlockFor(0)));
            }

            var queues = catalog.Gather.queueReactors ?? Array.Empty<int>();
            if (queues.Length > 0)
                all.Add(new FeatureUnlock("gather", string.Empty, queues.Min()));

            // Attacking other players (and being attacked) after beginner protection (docs/30).
            all.Add(new FeatureUnlock("pvp", string.Empty, PvpRules.BeginnerReactor));

            for (var ring = 2; ring <= catalog.Grid.maxRings; ring++)
            {
                if (catalog.TryGetRingUnlock(ring, out var unlock))
                    all.Add(new FeatureUnlock("ring", ring.ToString(), unlock.ReactorLevel));
            }

            return all.Where(f => f.ReactorLevel > reactorLevel)
                .OrderBy(f => f.ReactorLevel).ThenBy(f => f.Key switch { "gather" => 0, "pvp" => 1, "building" => 2, _ => 3 })
                .Take(Math.Max(0, max)).ToList();
        }

        /// <summary>Reactor level that opens a building's first copy (0 = open from the start).</summary>
        public static int BuildingUnlock(GameCatalog catalog, string buildingId) =>
            catalog.TryGetBuilding(buildingId, out var building) ? building.UnlockFor(0) : 0;

        /// <summary>Reactor level of the first gathering queue (marches and monsters need one).</summary>
        public static int MarchUnlock(GameCatalog catalog)
        {
            var queues = catalog.Gather.queueReactors ?? Array.Empty<int>();
            return queues.Length == 0 ? 0 : queues.Min();
        }
    }
}
