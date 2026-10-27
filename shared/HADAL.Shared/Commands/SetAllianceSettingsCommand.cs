using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>The leader sets who may join (<c>AllianceJoinMode</c>) and the description.</summary>
    [ProtoContract]
    public sealed class SetAllianceSettingsCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public int JoinMode { get; set; }
        [ProtoMember(4)] public string Description { get; set; } = string.Empty;

        /// <summary>New name, tag and banner; empty keeps the current one.</summary>
        [ProtoMember(5)] public string Name { get; set; } = string.Empty;
        [ProtoMember(6)] public string Tag { get; set; } = string.Empty;
        [ProtoMember(7)] public string Emblem { get; set; } = string.Empty;
    }
}
