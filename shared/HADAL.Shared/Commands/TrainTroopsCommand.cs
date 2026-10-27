using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Trains a batch of one troop type and tier in a training building (docs/rehber 07 §2.4).</summary>
    [ProtoContract]
    public sealed class TrainTroopsCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }

        /// <summary>The placed training building (instance id).</summary>
        [ProtoMember(3)] public string BuildingId { get; set; } = string.Empty;

        [ProtoMember(4)] public string TroopType { get; set; } = string.Empty;
        [ProtoMember(5)] public int Tier { get; set; }
        [ProtoMember(6)] public int Count { get; set; }
    }
}
