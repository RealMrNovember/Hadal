using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Claims a reached points stage (or a login day) of an event.</summary>
    [ProtoContract]
    public sealed class ClaimEventStageCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string EventId { get; set; } = string.Empty;
        [ProtoMember(4)] public int Index { get; set; }
    }

    /// <summary>Claims the rank reward of an ended event.</summary>
    [ProtoContract]
    public sealed class ClaimEventRankCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string EventId { get; set; } = string.Empty;
    }

    /// <summary>Asks for an event's ranking (answered with an <c>EventRankingDto</c>).</summary>
    [ProtoContract]
    public sealed class ViewEventRankingCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string EventId { get; set; } = string.Empty;
    }
}
