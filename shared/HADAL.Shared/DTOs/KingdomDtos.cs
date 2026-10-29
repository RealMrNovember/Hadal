using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>One kingdom (docs/39): a world of its own, "Krallık #Number".</summary>
    [ProtoContract]
    public sealed class KingdomDto
    {
        /// <summary>"w{number}".</summary>
        [ProtoMember(1)] public string Id { get; set; } = string.Empty;
        [ProtoMember(2)] public int Number { get; set; }
        [ProtoMember(3)] public string Name { get; set; } = string.Empty;
        [ProtoMember(4)] public long OpenedAtMs { get; set; }

        /// <summary>"open", "full" or "closed".</summary>
        [ProtoMember(5)] public string Status { get; set; } = string.Empty;
        [ProtoMember(6)] public int Players { get; set; }

        /// <summary>The viewer's own kingdom.</summary>
        [ProtoMember(7)] public bool Mine { get; set; }
    }

    /// <summary>Every kingdom, oldest first (entity <c>EntityIds.Kingdoms</c>).</summary>
    [ProtoContract]
    public sealed class KingdomListDto
    {
        [ProtoMember(1)] public KingdomDto[] Kingdoms { get; set; } = System.Array.Empty<KingdomDto>();
    }
}
