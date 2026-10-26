using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Puts a hero on a command crew slot, or takes it off with slot -1 (docs/25).</summary>
    [ProtoContract]
    public sealed class AssignHeroCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }

        [ProtoMember(3)] public string HeroId { get; set; } = string.Empty;

        [ProtoMember(4)] public int Slot { get; set; }
    }
}
