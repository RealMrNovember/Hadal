using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>A player's profile card (entity <c>EntityIds.Profile</c>; the own one also in the snapshot).</summary>
    [ProtoContract]
    public sealed class PlayerProfileDto
    {
        [ProtoMember(1)] public string PlayerId { get; set; } = string.Empty;
        [ProtoMember(2)] public string Name { get; set; } = string.Empty;
        [ProtoMember(3)] public string AvatarId { get; set; } = string.Empty;
        [ProtoMember(4)] public int Level { get; set; }
        [ProtoMember(5)] public long Power { get; set; }
        [ProtoMember(6)] public string AllianceTag { get; set; } = string.Empty;
        [ProtoMember(7)] public string AllianceName { get; set; } = string.Empty;
        [ProtoMember(8)] public int X { get; set; }
        [ProtoMember(9)] public int Y { get; set; }
        [ProtoMember(10)] public bool Online { get; set; }

        /// <summary>The player's kingdom number (docs/39); 0 = not known.</summary>
        [ProtoMember(11)] public int Kingdom { get; set; }
    }
}
