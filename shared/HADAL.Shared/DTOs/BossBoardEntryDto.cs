using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>One line of an event boss's damage board: a member of the viewer's alliance and their rank in it.</summary>
    [ProtoContract]
    public sealed class BossBoardEntryDto
    {
        [ProtoMember(1)] public string PlayerId { get; set; } = string.Empty;
        [ProtoMember(2)] public string Name { get; set; } = string.Empty;
        [ProtoMember(3)] public long Damage { get; set; }

        /// <summary>1 = most damage in the alliance; 0 = no alliance (the viewer's own line only).</summary>
        [ProtoMember(4)] public int Rank { get; set; }
    }
}
