using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>
    /// Moves a placed building to another slot of the base, free and keeping its level, its running
    /// construction and its assignments. An occupied target swaps the two buildings when each fits the
    /// other's slot (PlacementRules.ValidateMove).
    /// </summary>
    [ProtoContract]
    public sealed class MoveBuildingCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }

        /// <summary>The building's EntityId (BuildingStateDto.EntityId).</summary>
        [ProtoMember(3)] public string BuildingEntityId { get; set; } = string.Empty;

        [ProtoMember(4)] public int RingIndex { get; set; }
        [ProtoMember(5)] public int SectorIndex { get; set; }
    }
}
