using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Spends shards to add a star to a hero (docs/25).</summary>
    [ProtoContract]
    public sealed class PromoteHeroCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }

        [ProtoMember(3)] public string HeroId { get; set; } = string.Empty;
    }
}
