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

        /// <summary>Alliance Duel daily chests: the day (1-based) the chest belongs to; 0 = today (or any other template).</summary>
        [ProtoMember(5)] public int Day { get; set; }
    }

    /// <summary>
    /// A Mobilization task move (docs/29 §4.5): take the card in pool <see cref="Slot"/>, drop the held task, or refresh
    /// it (MobilizationActions).
    /// </summary>
    [ProtoContract]
    public sealed class MobilizationTaskCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string EventId { get; set; } = string.Empty;

        /// <summary>MobilizationActions.Take, Drop or Refresh.</summary>
        [ProtoMember(4)] public string Action { get; set; } = string.Empty;
        [ProtoMember(5)] public int Slot { get; set; }
    }

    public static class MobilizationActions
    {
        public const string Take = "take";
        public const string Drop = "drop";
        public const string Refresh = "refresh";

        /// <summary>
        /// No move: the server answers with the player's event list. An open Mobilization or Duel page sends it on opening
        /// and every so often, since mates' takes and the rival's points change without the player doing anything.
        /// </summary>
        public const string View = "view";
    }

    /// <summary>
    /// A Trench Conquest move (docs/29 §4.6): register the player's team into lane <see cref="Lane"/> (again: a fresh
    /// snapshot), move it to another lane, or just ask for the view (TrenchActions).
    /// </summary>
    [ProtoContract]
    public sealed class TrenchCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string EventId { get; set; } = string.Empty;

        /// <summary>TrenchActions.Register, Move, Withdraw or View.</summary>
        [ProtoMember(4)] public string Action { get; set; } = string.Empty;

        /// <summary>The lane, 0 to TrenchRules.Lanes - 1.</summary>
        [ProtoMember(5)] public int Lane { get; set; }

        /// <summary>The heroes to register (empty: the strongest, filled like a march).</summary>
        [ProtoMember(6)] public string[] HeroIds { get; set; } = System.Array.Empty<string>();
    }

    public static class TrenchActions
    {
        public const string Register = "register";
        public const string Move = "move";
        public const string Withdraw = "withdraw";
        public const string View = "view";
    }

    /// <summary>
    /// A Lure Trap move (docs/29 §4.1, LureTrapRules): the leader or a war-duty officer schedules trap
    /// <see cref="Trap"/> (a start time and a difficulty) or calls a scheduled one off before it starts; anyone may view.
    /// </summary>
    [ProtoContract]
    public sealed class LureTrapCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string EventId { get; set; } = string.Empty;

        /// <summary>LureTrapActions.Schedule, Cancel or View.</summary>
        [ProtoMember(4)] public string Action { get; set; } = string.Empty;

        /// <summary>The trap, 0 to LureTrapRules.Traps - 1.</summary>
        [ProtoMember(5)] public int Trap { get; set; }

        /// <summary>Schedule: when the Gulper rises (Unix ms) and how hard it is (1 to LureTrapRules.MaxDifficulty).</summary>
        [ProtoMember(6)] public long StartsAtMs { get; set; }
        [ProtoMember(7)] public int Difficulty { get; set; }
    }

    public static class LureTrapActions
    {
        public const string Schedule = "schedule";
        public const string Cancel = "cancel";
        public const string View = "view";
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

    /// <summary>
    /// Asks for the event calendar: the coming week's events, the recurring ones included before they are created
    /// (answered with an <c>EventCalendarDto</c>). Texts come in <see cref="Lang"/> only, to keep the answer small.
    /// </summary>
    [ProtoContract]
    public sealed class ViewEventCalendarCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string Lang { get; set; } = string.Empty;
    }
}
