using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Boosts a production building for a while (docs/24).</summary>
    [ProtoContract]
    public sealed class BoostBuildingCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }

        /// <summary>The building's EntityId.</summary>
        [ProtoMember(3)] public string BuildingEntityId { get; set; } = string.Empty;
    }
}
