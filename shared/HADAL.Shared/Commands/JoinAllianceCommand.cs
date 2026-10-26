using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Joins an open alliance (docs/25).</summary>
    [ProtoContract]
    public sealed class JoinAllianceCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }

        [ProtoMember(3)] public string AllianceId { get; set; } = string.Empty;
    }
}
