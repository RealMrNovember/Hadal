using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Starts upgrading a placed building to its next level (docs/23_Base_Management.md).</summary>
    [ProtoContract]
    public sealed class UpgradeBuildingCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }

        /// <summary>The building's EntityId (BuildingStateDto.EntityId).</summary>
        [ProtoMember(3)] public string BuildingEntityId { get; set; } = string.Empty;
    }
}
