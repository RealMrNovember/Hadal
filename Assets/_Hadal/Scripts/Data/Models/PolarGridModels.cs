using System;
using Hadal.Data.Enums;

namespace Hadal.Data.Models
{
    /// <summary>
    /// Primary grid index for HADAL circular base. Ring 0 = core reactor.
    /// </summary>
    [Serializable]
    public struct PolarGridSlotId : IEquatable<PolarGridSlotId>
    {
        public int Ring;
        public int Sector;

        public PolarGridSlotId(int ring, int sector)
        {
            Ring = ring;
            Sector = sector;
        }

        public bool Equals(PolarGridSlotId other) => Ring == other.Ring && Sector == other.Sector;

        public override bool Equals(object obj) => obj is PolarGridSlotId other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Ring, Sector);

        public override string ToString() => $"R{Ring}:S{Sector}";

        public static bool operator ==(PolarGridSlotId a, PolarGridSlotId b) => a.Equals(b);
        public static bool operator !=(PolarGridSlotId a, PolarGridSlotId b) => !a.Equals(b);
    }

    [Serializable]
    public struct PolarGridSlot
    {
        public PolarGridSlotId Id;
        public GridSlotType SlotType;
        public bool IsOccupied;
        public string BuildingInstanceId;
        public float BuildingRotationDegrees;
        public float SlotAngleDegrees;
        public bool IsUnlocked;
        public string PreferredBuildingId;
    }

    [Serializable]
    public struct PlacementValidationResult
    {
        public bool IsValid;
        public string Reason;

        public static PlacementValidationResult Success() => new() { IsValid = true };
        public static PlacementValidationResult Fail(string reason) => new() { IsValid = false, Reason = reason };
    }

    /// <summary>
    /// Legacy cartesian cell — retained for save migration from v1.
    /// </summary>
    [Serializable]
    public struct GridCell
    {
        public int X;
        public int Y;
        public int Ring;
        public int Sector;
        public bool IsOccupied;
        public string BuildingId;
        public float RotationDegrees;

        public PolarGridSlotId ToPolarId() => new(Ring, Sector >= 0 ? Sector : 0);
    }
}
