using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Trains a hero one level (docs/25).</summary>
    [ProtoContract]
    public sealed class LevelUpHeroCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }

        /// <summary>The catalog hero id.</summary>
        [ProtoMember(3)] public string HeroId { get; set; } = string.Empty;
    }
}
