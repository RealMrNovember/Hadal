using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Founds an alliance; the founder becomes its leader (docs/25).</summary>
    [ProtoContract]
    public sealed class CreateAllianceCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }

        [ProtoMember(3)] public string Name { get; set; } = string.Empty;

        [ProtoMember(4)] public string Tag { get; set; } = string.Empty;

        [ProtoMember(5)] public string Description { get; set; } = string.Empty;
    }
}
