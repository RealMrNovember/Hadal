using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>The army at home and the training queues (entity <c>EntityIds.Army</c>; also in the snapshot).</summary>
    [ProtoContract]
    public sealed class ArmyDto
    {
        [ProtoMember(1)] public TroopStackDto[] Troops { get; set; } = System.Array.Empty<TroopStackDto>();
        [ProtoMember(2)] public TrainingDto[] Training { get; set; } = System.Array.Empty<TrainingDto>();

        /// <summary>How many troops the base can keep (Military Domes).</summary>
        [ProtoMember(3)] public long Capacity { get; set; }

        /// <summary>Upkeep an hour (docs/rehber 07 §2.5).</summary>
        [ProtoMember(4)] public double UpkeepFood { get; set; }
        [ProtoMember(5)] public double UpkeepOxygen { get; set; }
    }

    [ProtoContract]
    public sealed class TroopStackDto
    {
        [ProtoMember(1)] public string Type { get; set; } = string.Empty;
        [ProtoMember(2)] public int Tier { get; set; }
        [ProtoMember(3)] public long Count { get; set; }
    }

    [ProtoContract]
    public sealed class TrainingDto
    {
        [ProtoMember(1)] public string BuildingId { get; set; } = string.Empty;
        [ProtoMember(2)] public string Type { get; set; } = string.Empty;
        [ProtoMember(3)] public int Tier { get; set; }
        [ProtoMember(4)] public int Count { get; set; }
        [ProtoMember(5)] public long StartedAtMs { get; set; }
        [ProtoMember(6)] public long EndsAtMs { get; set; }
    }
}
