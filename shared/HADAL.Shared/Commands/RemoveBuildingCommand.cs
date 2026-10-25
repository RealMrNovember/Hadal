using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>
    /// Removes a placed building for a partial refund, or cancels its first construction for a full
    /// one (docs/23_Base_Management.md, rule 14).
    /// </summary>
    [ProtoContract]
    public sealed class RemoveBuildingCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }

        /// <summary>The building's EntityId (BuildingStateDto.EntityId).</summary>
        [ProtoMember(3)] public string BuildingEntityId { get; set; } = string.Empty;
    }
}
