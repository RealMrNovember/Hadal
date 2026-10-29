using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Donates once to the alliance's focus research (AllianceResearchRules): its resource for research points.</summary>
    [ProtoContract]
    public sealed class DonateAllianceResearchCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
    }

    /// <summary>Officers (R4+) choose which alliance research the members' donations go to.</summary>
    [ProtoContract]
    public sealed class SetResearchFocusCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string NodeId { get; set; } = string.Empty;
    }
}
