using HADAL.Shared.Enums;

namespace Hadal.Game
{
    public sealed class PlacedBuilding
    {
        public string BuildingId { get; init; } = string.Empty;
        public string DefinitionId { get; init; } = string.Empty;
        public int Ring { get; init; }
        public int Sector { get; init; }
        public int Level { get; set; } = 1;
        public int RotationSteps { get; init; }
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

        public bool IsDirty { get; set; }
    }
}
