using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>
    /// Polar grid slot — authoritative placement uses radius + angle; ring/sector are derived indices.
    /// </summary>
    [ProtoContract]
    public struct GridSlot
    {
        [ProtoMember(1)] public float Radius { get; set; }
        [ProtoMember(2)] public float AngleDegrees { get; set; }
        [ProtoMember(3)] public int RingIndex { get; set; }
        [ProtoMember(4)] public int SectorIndex { get; set; }

        public static GridSlot Create(float radius, float angleDegrees, int ringIndex, int sectorIndex)
        {
            return new GridSlot
            {
                Radius = radius,
                AngleDegrees = angleDegrees,
                RingIndex = ringIndex,
                SectorIndex = sectorIndex
            };
        }

        public override readonly string ToString() =>
            $"R{RingIndex}:S{SectorIndex} r={Radius:F2} θ={AngleDegrees:F1}°";
    }
}
