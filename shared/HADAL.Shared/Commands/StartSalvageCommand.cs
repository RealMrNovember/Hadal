using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Sends a Drone Factory's drones on a salvage run (docs/24).</summary>
    [ProtoContract]
    public sealed class StartSalvageCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }

        /// <summary>The Drone Factory's EntityId.</summary>
        [ProtoMember(3)] public string BuildingEntityId { get; set; } = string.Empty;

        /// <summary>A catalog salvage mission id.</summary>
        [ProtoMember(4)] public string MissionId { get; set; } = string.Empty;
    }
}
