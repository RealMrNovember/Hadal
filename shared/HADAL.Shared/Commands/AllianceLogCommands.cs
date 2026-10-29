using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Asks for the alliance log or the battle log (answered with an <c>AllianceLogDto</c>, docs/31 §2).</summary>
    [ProtoContract]
    public sealed class ViewAllianceLogCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }

        /// <summary>AllianceLogKinds: "log" (joins, leaves, ranks, research) or "battle".</summary>
        [ProtoMember(3)] public string Kind { get; set; } = string.Empty;
    }
}
