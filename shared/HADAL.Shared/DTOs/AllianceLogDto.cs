using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>One line of an alliance log: what happened (a type the client words) with its arguments.</summary>
    [ProtoContract]
    public sealed class AllianceLogEntryDto
    {
        /// <summary>Alliance log: "joined", "left", "kicked", "rank", "leader", "research". Battle log: see AllianceLogKinds.</summary>
        [ProtoMember(1)] public string Type { get; set; } = string.Empty;
        [ProtoMember(2)] public string[] Args { get; set; } = System.Array.Empty<string>();
        [ProtoMember(3)] public long AtMs { get; set; }
    }

    /// <summary>The alliance's log or battle log, newest first (docs/31 §2).</summary>
    [ProtoContract]
    public sealed class AllianceLogDto
    {
        [ProtoMember(1)] public string Kind { get; set; } = string.Empty;
        [ProtoMember(2)] public AllianceLogEntryDto[] Entries { get; set; } = System.Array.Empty<AllianceLogEntryDto>();
    }
}
