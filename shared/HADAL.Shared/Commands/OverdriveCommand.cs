using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Runs the Core Reactor in overdrive (docs/24).</summary>
    [ProtoContract]
    public sealed class OverdriveCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }

        /// <summary>The reactor's EntityId.</summary>
        [ProtoMember(3)] public string BuildingEntityId { get; set; } = string.Empty;
    }
}
