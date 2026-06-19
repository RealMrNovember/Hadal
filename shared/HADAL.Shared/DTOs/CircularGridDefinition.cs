using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>
    /// Static circular grid topology shared between client and server validation.
    /// </summary>
    [ProtoContract]
    public sealed class CircularGridDefinition
    {
        [ProtoMember(1)] public int RingCount { get; set; }
        [ProtoMember(2)] public int[] SectorsPerRing { get; set; } = System.Array.Empty<int>();
        [ProtoMember(3)] public float[] RingRadii { get; set; } = System.Array.Empty<float>();
        [ProtoMember(4)] public float SlotYOffset { get; set; }
        [ProtoMember(5)] public float RotationSnapDegrees { get; set; } = 60f;
    }
}
