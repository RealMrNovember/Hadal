using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Collects a finished chapter or daily task's reward (docs/27).</summary>
    [ProtoContract]
    public sealed class ClaimQuestCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string QuestId { get; set; } = string.Empty;
    }
}
