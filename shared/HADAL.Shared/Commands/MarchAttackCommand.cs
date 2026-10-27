using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Sends a team to attack a monster on the map (docs/rehber 09 §5, 07 §4).</summary>
    [ProtoContract]
    public sealed class MarchAttackCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string MonsterId { get; set; } = string.Empty;

        /// <summary>The team; empty = the strongest free heroes.</summary>
        [ProtoMember(4)] public string[] HeroIds { get; set; } = System.Array.Empty<string>();
    }
}
