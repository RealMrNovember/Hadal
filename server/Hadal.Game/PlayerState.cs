using HADAL.Shared.Enums;

namespace Hadal.Game
{
    public sealed class PlacedBuilding
    {
        public string BuildingId { get; init; } = string.Empty;
        public string DefinitionId { get; init; } = string.Empty;
        public int Ring { get; init; }
        public int Sector { get; init; }
        /// <summary>Working level: 0 while first under construction, otherwise the level that produces.</summary>
        public int Level { get; set; } = 1;
        public int RotationSteps { get; init; }

        /// <summary>When the running construction (build or upgrade to Level + 1) completes; null = idle.</summary>
        public DateTimeOffset? ConstructionEndsAt { get; set; }

        /// <summary>Full duration of the running construction (progress display).</summary>
        public int ConstructionSeconds { get; set; }

        public bool IsUnderConstruction => ConstructionEndsAt.HasValue;

        // Building functions (docs/24_Building_Functions.md); persisted in PlayerExtras.
        public DateTimeOffset? BoostEndsAt { get; set; }
        public string MissionId { get; set; } = string.Empty;
        public DateTimeOffset? MissionEndsAt { get; set; }
        public int MissionSeconds { get; set; }
        public int MissionSeed { get; set; }
        public DateTimeOffset? OverdriveEndsAt { get; set; }
        public DateTimeOffset? OverdriveReadyAt { get; set; }
    }

    /// <summary>A research level being worked on (docs/24).</summary>
    /// <summary>A training batch in a training yard (docs/rehber 07 §2.4).</summary>
    public sealed class TrainingState
    {
        public string BuildingId { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public int Tier { get; init; }
        public int Count { get; init; }
        public DateTimeOffset StartedAt { get; init; }
        public DateTimeOffset EndsAt { get; set; }

        /// <summary>What was paid, refunded in full on cancel.</summary>
        public Dictionary<ResourceType, long> Cost { get; init; } = new();
    }

    /// <summary>The Med Bay's healing batch (docs/rehber 07 §2.7).</summary>
    public sealed class HealState
    {
        public Dictionary<string, long> Troops { get; init; } = new(StringComparer.Ordinal);
        public DateTimeOffset StartedAt { get; init; }
        public DateTimeOffset EndsAt { get; set; }
        public Dictionary<ResourceType, long> Cost { get; init; } = new();
    }

    public sealed class ActiveResearch
    {
        public string NodeId { get; init; } = string.Empty;
        public int TargetLevel { get; init; }
        public DateTimeOffset EndsAt { get; set; }
        public int Seconds { get; init; }
    }

    /// <summary>An owned hero (docs/25).</summary>
    public sealed class OwnedHeroState
    {
        public string HeroId { get; init; } = string.Empty;
        public int Level { get; set; } = 1;
        public int Stars { get; set; } = 1;
        public int Shards { get; set; }

        /// <summary>Command crew slot, -1 when off the crew.</summary>
        public int Slot { get; set; } = -1;
    }

    /// <summary>
    /// Authoritative per-player state. Mutated only by <see cref="PlayerSession"/> under its lock.
    /// </summary>
    public sealed class PlayerState
    {
        public Guid PlayerId { get; init; }
        public string IdentityKey { get; init; } = string.Empty;

        /// <summary>Simulation ticks applied so far; replicated as ServerTick for stale-frame rejection.</summary>
        public ulong ServerTick { get; set; }

        /// <summary>Wall-clock instant the economy was last advanced to (server clock only).</summary>
        public DateTimeOffset LastSimulatedAt { get; set; }

        public int UnlockedRings { get; set; }

        public Dictionary<ResourceType, long> Wallet { get; } = new();

        public List<PlacedBuilding> Buildings { get; } = new();

        /// <summary>Buildings not yet written to the database.</summary>
        public List<PlacedBuilding> UnsavedBuildings { get; } = new();

        /// <summary>Removed buildings not yet deleted from the database.</summary>
        public List<string> RemovedBuildingIds { get; } = new();

        /// <summary>Finished research levels per node (docs/24).</summary>
        public Dictionary<string, int> ResearchLevels { get; } = new(StringComparer.Ordinal);

        public List<ActiveResearch> ActiveResearch { get; } = new();

        /// <summary>Owned heroes (docs/25).</summary>
        public List<OwnedHeroState> Heroes { get; } = new();

        /// <summary>Cryo-Pod pulls since the last guaranteed-rarity hero.</summary>
        public int SincePity { get; set; }

        /// <summary>When the next free Cryo-Pod pull is ready; null = now.</summary>
        public DateTimeOffset? FreePullAt { get; set; }

        // Quests and bag (docs/27).
        public int ChapterIndex { get; set; }
        public Dictionary<string, long> QuestCounters { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, long> DailyCounters { get; } = new(StringComparer.Ordinal);
        public int DailyDay { get; set; }
        public HashSet<string> ClaimedDaily { get; } = new(StringComparer.Ordinal);
        public int DailyPoints { get; set; }
        public HashSet<int> ClaimedChests { get; } = new();
        public Dictionary<string, int> Items { get; } = new(StringComparer.Ordinal);

        /// <summary>Hero cards of heroes not owned yet; an owned hero keeps its shards on <see cref="OwnedHeroState"/>.</summary>
        public Dictionary<string, int> HeroCards { get; } = new(StringComparer.Ordinal);

        /// <summary>Gathering marches out on the map (docs/rehber 09 §4).</summary>
        public List<MarchState> Marches { get; } = new();

        /// <summary>Troops at home by "type:tier".</summary>
        public Dictionary<string, long> Troops { get; } = new(StringComparer.Ordinal);

        /// <summary>Running training batches, one per training yard.</summary>
        public List<TrainingState> Training { get; } = new();

        /// <summary>Wounded troops in the Med Bays by "type:tier".</summary>
        public Dictionary<string, long> Wounded { get; } = new(StringComparer.Ordinal);

        /// <summary>The healing batch; null when the Med Bay is idle.</summary>
        public HealState? Healing { get; set; }

        /// <summary>Chosen avatar (a hero portrait id); empty shows the initial.</summary>
        public string AvatarId { get; set; } = string.Empty;

        /// <summary>Depth Map stages cleared in order (docs/rehber 08 §5).</summary>
        public int ExpeditionCleared { get; set; }

        /// <summary>Fractions of a unit (thousandths) carried between economy steps; not persisted.</summary>
        public Dictionary<ResourceType, long> CarryMilli { get; } = new();

        /// <summary>Research and building-function state for the save (PlayerExtras), set by the save snapshot.</summary>
        public string ExtrasJson { get; set; } = string.Empty;

        public bool IsDirty { get; set; }
    }
}
