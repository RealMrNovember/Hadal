using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Turns collected hero cards (shards) into the hero (docs/27).</summary>
    [ProtoContract]
    public sealed class RecruitHeroCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string HeroId { get; set; } = string.Empty;
    }
}
