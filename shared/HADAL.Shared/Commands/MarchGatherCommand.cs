using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Sends a gathering march to a resource node (docs/rehber 09 §4.7).</summary>
    [ProtoContract]
    public sealed class MarchGatherCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string NodeId { get; set; } = string.Empty;

        /// <summary>The hero who leads the march (owned, not on another march); empty = the strongest free one.</summary>
        [ProtoMember(4)] public string HeroId { get; set; } = string.Empty;

        /// <summary>The team: up to five heroes, each leading their command capacity of troops (docs/rehber 19 §1.2).</summary>
        [ProtoMember(5)] public string[] HeroIds { get; set; } = System.Array.Empty<string>();
    }
}
