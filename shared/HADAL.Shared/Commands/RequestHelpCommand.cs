using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Asks the alliance to help a running construction (docs/25).</summary>
    [ProtoContract]
    public sealed class RequestHelpCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }

        [ProtoMember(3)] public string BuildingEntityId { get; set; } = string.Empty;
    }
}
