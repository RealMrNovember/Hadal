using System;
using Hadal.Data.Models;

namespace Hadal.Data.Models
{
    [Serializable]
    public sealed class SaveGameData
    {
        public const int CurrentVersion = 2;

        public int version = CurrentVersion;
        public long savedAtUtcTicks;
        public int unlockedGridRings = 4;
        public ResourceSaveEntry[] resources = Array.Empty<ResourceSaveEntry>();
        public BuildingSaveEntry[] buildings = Array.Empty<BuildingSaveEntry>();
        public PolarGridSlotSaveEntry[] polarGridSlots = Array.Empty<PolarGridSlotSaveEntry>();
        public GridCellSaveEntry[] gridCells = Array.Empty<GridCellSaveEntry>();
        public HeroSaveEntry[] heroes = Array.Empty<HeroSaveEntry>();
        public string currentDepthZone;
    }

    [Serializable]
    public struct PolarGridSlotSaveEntry
    {
        public int ring;
        public int sector;
        public int slotType;
        public bool isOccupied;
        public string buildingInstanceId;
        public float buildingRotation;
        public float slotAngle;
        public bool isUnlocked;
        public string preferredBuildingId;
    }

    [Serializable]
    public struct ResourceSaveEntry
    {
        public int resourceType;
        public long amount;
    }

    [Serializable]
    public struct BuildingSaveEntry
    {
        public string instanceKey;
        public string definitionId;
        public int level;
        public int cellRing;
        public int cellSector;
        public float rotation;
        public int cellX;
        public int cellY;

        /// <summary>Time.realtimeSinceStartup when the running construction ends; 0 = idle.</summary>
        public float constructionEndsAt;

        /// <summary>Full duration of the running construction in seconds (progress bars).</summary>
        public float constructionSeconds;

        public bool IsUnderConstruction => constructionEndsAt > 0f;

        // Building functions (docs/24); Time.realtimeSinceStartup ends, 0 = not running.
        public float boostEndsAt;
        public string missionId;
        public float missionEndsAt;
        public float missionSeconds;
        public float overdriveEndsAt;
        public float overdriveReadyAt;
    }

    [Serializable]
    public struct GridCellSaveEntry
    {
        public int x;
        public int y;
        public int ring;
        public int sector;
        public bool isOccupied;
        public string buildingId;
        public float rotation;
    }

    [Serializable]
    public struct HeroSaveEntry
    {
        public string instanceId;
        public string definitionId;
        public int level;
        public int experience;
        public int faction;
    }
}
