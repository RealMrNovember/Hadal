using ProtoBuf;

namespace HADAL.Shared.Commands
{
    [ProtoContract]
    public sealed class PlaceBuildingCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string BuildingDefinitionId { get; set; } = string.Empty;
        [ProtoMember(4)] public int RingIndex { get; set; }
        [ProtoMember(5)] public int SectorIndex { get; set; }
        [ProtoMember(6)] public int RotationSteps { get; set; }
    }
}
