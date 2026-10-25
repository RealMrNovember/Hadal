using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>The base's open area. Replicated as the singleton entity <c>EntityIds.BaseLayout</c>.</summary>
    [ProtoContract]
    public sealed class BaseLayoutDto
    {
        /// <summary>Outermost open ring index (rings 0..UnlockedRings accept buildings).</summary>
        [ProtoMember(1)] public int UnlockedRings { get; set; }
    }
}
