using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Finishes a construction whose remaining time is within the free-finish window.</summary>
    [ProtoContract]
    public sealed class FinishConstructionCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string BuildingEntityId { get; set; } = string.Empty;
    }
}
