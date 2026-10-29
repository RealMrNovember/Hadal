using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Asks the alliance to help a running construction, the Med Bay's healing or a research (docs/25).</summary>
    [ProtoContract]
    public sealed class RequestHelpCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }

        [ProtoMember(3)] public string BuildingEntityId { get; set; } = string.Empty;

        /// <summary>HelpKinds: "build" (also when empty, as older clients send), "heal" or "research".</summary>
        [ProtoMember(4)] public string Kind { get; set; } = string.Empty;

        /// <summary>The research node for "research"; unused otherwise.</summary>
        [ProtoMember(5)] public string Target { get; set; } = string.Empty;
    }
}
