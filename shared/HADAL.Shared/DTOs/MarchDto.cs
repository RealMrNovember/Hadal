using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>The player's gathering marches (entity <c>EntityIds.Marches</c>, docs/rehber 09 §4).</summary>
    [ProtoContract]
    public sealed class MarchListDto
    {
        [ProtoMember(1)] public MarchDto[] Marches { get; set; } = System.Array.Empty<MarchDto>();

        /// <summary>Marches the player may have out at once.</summary>
        [ProtoMember(2)] public int Queues { get; set; }

        /// <summary>Load one march carries.</summary>
        [ProtoMember(3)] public long Load { get; set; }

        /// <summary>Core Reactor level that opens the next queue; 0 when all are open.</summary>
        [ProtoMember(4)] public int NextQueueReactor { get; set; }

        /// <summary>Server clock when this was sent, Unix ms (the client lines its timers up with it).</summary>
        [ProtoMember(5)] public long ServerNowMs { get; set; }
    }

    public enum MarchPhase
    {
        Outbound = 0,
        Gathering = 1,
        Returning = 2
    }

    [ProtoContract]
    public sealed class MarchDto
    {
        [ProtoMember(1)] public string Id { get; set; } = string.Empty;
        [ProtoMember(2)] public string NodeId { get; set; } = string.Empty;
        [ProtoMember(3)] public string NodeType { get; set; } = string.Empty;
        [ProtoMember(4)] public int NodeLevel { get; set; }
        [ProtoMember(5)] public int X { get; set; }
        [ProtoMember(6)] public int Y { get; set; }
        [ProtoMember(7)] public MarchPhase Phase { get; set; }

        /// <summary>Phase boundaries, Unix ms: left home, reached the node, done gathering, back home.</summary>
        [ProtoMember(8)] public long StartedAtMs { get; set; }
        [ProtoMember(9)] public long ArrivesAtMs { get; set; }
        [ProtoMember(10)] public long GatheredAtMs { get; set; }
        [ProtoMember(11)] public long HomeAtMs { get; set; }

        /// <summary>What the march brings home (planned; shrinks if recalled early).</summary>
        [ProtoMember(12)] public ResourceAmountEntry[] Cargo { get; set; } = System.Array.Empty<ResourceAmountEntry>();

        /// <summary>The hero leading the march and the divers aboard (troops stand in until Paket D).</summary>
        [ProtoMember(13)] public string HeroId { get; set; } = string.Empty;
        [ProtoMember(14)] public int Crew { get; set; }

        /// <summary>Where the march set out from (the base tile).</summary>
        [ProtoMember(15)] public int FromX { get; set; }
        [ProtoMember(16)] public int FromY { get; set; }

        /// <summary>The team's heroes and how many troops each leads (same order).</summary>
        [ProtoMember(17)] public string[] HeroIds { get; set; } = System.Array.Empty<string>();
        [ProtoMember(18)] public long[] HeroTroops { get; set; } = System.Array.Empty<long>();

        /// <summary>The troops aboard by type and tier.</summary>
        [ProtoMember(19)] public TroopStackDto[] Troops { get; set; } = System.Array.Empty<TroopStackDto>();
    }
}
