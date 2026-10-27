using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Stops a training batch; the price comes back in full (docs/rehber 07 §2.4).</summary>
    [ProtoContract]
    public sealed class CancelTrainingCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string BuildingId { get; set; } = string.Empty;
    }
}
