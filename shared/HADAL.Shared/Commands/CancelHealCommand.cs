using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Stops the healing batch; the troops go back to the wounded and the price comes back.</summary>
    [ProtoContract]
    public sealed class CancelHealCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
    }
}
