using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>One alliance gift as a member sees it (docs/31 §2).</summary>
    [ProtoContract]
    public sealed class AllianceGiftDto
    {
        [ProtoMember(1)] public string Id { get; set; } = string.Empty;

        /// <summary>AllianceGiftRules: "monster", "purchase" or "event".</summary>
        [ProtoMember(2)] public string Source { get; set; } = string.Empty;
        [ProtoMember(3)] public string ByName { get; set; } = string.Empty;

        /// <summary>The monster's level or the Hadalite bought.</summary>
        [ProtoMember(4)] public long Arg { get; set; }
        [ProtoMember(5)] public long ExpiresAtMs { get; set; }
        [ProtoMember(6)] public long Points { get; set; }
        [ProtoMember(7)] public bool Collected { get; set; }
    }

    /// <summary>The alliance's gift level and this member's gifts, newest first.</summary>
    [ProtoContract]
    public sealed class AllianceGiftsDto
    {
        [ProtoMember(1)] public int Level { get; set; }
        [ProtoMember(2)] public long Points { get; set; }

        /// <summary>Points the next level needs (0 at the top).</summary>
        [ProtoMember(3)] public long NextPoints { get; set; }

        /// <summary>The chest a gift holds at this level (an item id).</summary>
        [ProtoMember(4)] public string Chest { get; set; } = string.Empty;
        [ProtoMember(5)] public AllianceGiftDto[] Gifts { get; set; } = System.Array.Empty<AllianceGiftDto>();
    }
}
