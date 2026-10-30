using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>
    /// Opens an alliance poll (diplomacy officers and the leader) or a time-slot vote for an alliance event (war officers
    /// and the leader), docs/29 §5. Checked against AlliancePollRules on the server.
    /// </summary>
    [ProtoContract]
    public sealed class CreateAlliancePollCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }

        /// <summary>AlliancePollRules.KindPoll or KindSlots.</summary>
        [ProtoMember(3)] public string Kind { get; set; } = string.Empty;

        /// <summary>The question; for a time-slot vote an optional note.</summary>
        [ProtoMember(4)] public string Question { get; set; } = string.Empty;

        /// <summary>A poll's answers (2–6).</summary>
        [ProtoMember(5)] public string[] Options { get; set; } = System.Array.Empty<string>();

        /// <summary>A time-slot vote's offered starts in server time (Unix ms, 2–5).</summary>
        [ProtoMember(6)] public long[] SlotsMs { get; set; } = System.Array.Empty<long>();

        /// <summary>How long voting runs (1–72 h); 0 takes the kind's default.</summary>
        [ProtoMember(7)] public int Hours { get; set; }

        [ProtoMember(8)] public bool Multiple { get; set; }
        [ProtoMember(9)] public bool Anonymous { get; set; }

        /// <summary>A time-slot vote's event (AlliancePollRules.Purposes).</summary>
        [ProtoMember(10)] public string Purpose { get; set; } = string.Empty;
    }

    /// <summary>
    /// A member's ballot: the chosen option indexes (one, or several on a multiple-choice poll). Voting again while the
    /// vote is open replaces the earlier ballot.
    /// </summary>
    [ProtoContract]
    public sealed class VoteAlliancePollCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string PollId { get; set; } = string.Empty;
        [ProtoMember(4)] public int[] Choices { get; set; } = System.Array.Empty<int>();
    }

    /// <summary>Closes a vote early: its creator, or whoever may open that kind of vote.</summary>
    [ProtoContract]
    public sealed class CloseAlliancePollCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string PollId { get; set; } = string.Empty;
    }
}
