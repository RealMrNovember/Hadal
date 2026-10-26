using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>One owned hero (docs/25).</summary>
    [ProtoContract]
    public sealed class HeroStateDto
    {
        [ProtoMember(1)] public string EntityId { get; set; } = string.Empty;
        [ProtoMember(2)] public string DefinitionId { get; set; } = string.Empty;
        [ProtoMember(3)] public int Level { get; set; }
        [ProtoMember(4)] public int Experience { get; set; }
        [ProtoMember(5)] public int Faction { get; set; }
        [ProtoMember(6)] public int Stars { get; set; }
        [ProtoMember(7)] public int Shards { get; set; }

        /// <summary>Command crew slot, -1 when off the crew.</summary>
        [ProtoMember(8)] public int CrewSlot { get; set; }
    }

    /// <summary>The player's heroes and Cryo-Pod state; singleton entity <c>EntityIds.Heroes</c>.</summary>
    [ProtoContract]
    public sealed class HeroRosterDto
    {
        [ProtoMember(1)] public HeroStateDto[] Heroes { get; set; } = System.Array.Empty<HeroStateDto>();

        /// <summary>Pulls since the last guaranteed-rarity hero.</summary>
        [ProtoMember(2)] public int SincePity { get; set; }

        /// <summary>Time until the free pull; 0 = ready now.</summary>
        [ProtoMember(3)] public long FreePullRemainingMs { get; set; }
    }

    /// <summary>What one Cryo-Pod command produced (entity <c>EntityIds.GachaResult</c> + command id).</summary>
    [ProtoContract]
    public sealed class GachaResultDto
    {
        [ProtoMember(1)] public GachaPullEntry[] Pulls { get; set; } = System.Array.Empty<GachaPullEntry>();
    }

    [ProtoContract]
    public sealed class GachaPullEntry
    {
        [ProtoMember(1)] public string HeroId { get; set; } = string.Empty;

        /// <summary>True for a hero the player did not have; otherwise it became shards.</summary>
        [ProtoMember(2)] public bool IsNew { get; set; }
        [ProtoMember(3)] public int Shards { get; set; }
    }
}
