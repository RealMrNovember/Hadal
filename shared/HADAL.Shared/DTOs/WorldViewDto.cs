using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>Bases around a map tile (entity <c>EntityIds.WorldView</c>, docs/28).</summary>
    [ProtoContract]
    public sealed class WorldViewDto
    {
        [ProtoMember(1)] public int CenterX { get; set; }
        [ProtoMember(2)] public int CenterY { get; set; }
        [ProtoMember(3)] public int Radius { get; set; }
        [ProtoMember(4)] public int MapSize { get; set; }

        /// <summary>The viewer's own base tile.</summary>
        [ProtoMember(5)] public int MyX { get; set; }
        [ProtoMember(6)] public int MyY { get; set; }

        [ProtoMember(7)] public WorldBaseDto[] Bases { get; set; } = System.Array.Empty<WorldBaseDto>();

        /// <summary>Resource nodes in view (docs/rehber 09 §4).</summary>
        [ProtoMember(8)] public WorldNodeDto[] Nodes { get; set; } = System.Array.Empty<WorldNodeDto>();

        /// <summary>Monsters in view (docs/rehber 09 §5.2).</summary>
        [ProtoMember(9)] public WorldMonsterDto[] Monsters { get; set; } = System.Array.Empty<WorldMonsterDto>();

        /// <summary>Every team crossing the view, the viewer's own included (docs/28): all players see them live.</summary>
        [ProtoMember(10)] public WorldMarchDto[] Marches { get; set; } = System.Array.Empty<WorldMarchDto>();
    }

    /// <summary>A team on the map as others see it: whose, from where to where, and when.</summary>
    [ProtoContract]
    public sealed class WorldMarchDto
    {
        [ProtoMember(1)] public string Id { get; set; } = string.Empty;
        [ProtoMember(2)] public string OwnerId { get; set; } = string.Empty;
        [ProtoMember(3)] public string OwnerName { get; set; } = string.Empty;
        [ProtoMember(4)] public string AllianceTag { get; set; } = string.Empty;

        /// <summary>0 = the viewer's own, 1 = an alliance mate's, 2 = anyone else's.</summary>
        [ProtoMember(5)] public int Relation { get; set; }
        [ProtoMember(6)] public int FromX { get; set; }
        [ProtoMember(7)] public int FromY { get; set; }
        [ProtoMember(8)] public int ToX { get; set; }
        [ProtoMember(9)] public int ToY { get; set; }
        [ProtoMember(10)] public long StartedAtMs { get; set; }
        [ProtoMember(11)] public long ArrivesAtMs { get; set; }
        [ProtoMember(12)] public long GatheredAtMs { get; set; }
        [ProtoMember(13)] public long HomeAtMs { get; set; }

        /// <summary>"gather" or "attack".</summary>
        [ProtoMember(14)] public string Kind { get; set; } = string.Empty;
    }

    [ProtoContract]
    public sealed class WorldMonsterDto
    {
        [ProtoMember(1)] public string Id { get; set; } = string.Empty;
        [ProtoMember(2)] public string Type { get; set; } = string.Empty;
        [ProtoMember(3)] public int Level { get; set; }
        [ProtoMember(4)] public int X { get; set; }
        [ProtoMember(5)] public int Y { get; set; }

        /// <summary>A team is on its way to it.</summary>
        [ProtoMember(6)] public bool Busy { get; set; }
        [ProtoMember(7)] public long Power { get; set; }
    }

    /// <summary>A resource node on the map.</summary>
    [ProtoContract]
    public sealed class WorldNodeDto
    {
        [ProtoMember(1)] public string Id { get; set; } = string.Empty;

        /// <summary>Node kind from the catalog (<c>world.nodeTypes</c>), e.g. "enkaz_alani".</summary>
        [ProtoMember(2)] public string Type { get; set; } = string.Empty;
        [ProtoMember(3)] public int Level { get; set; }
        [ProtoMember(4)] public int X { get; set; }
        [ProtoMember(5)] public int Y { get; set; }

        /// <summary>Load left and load when fresh.</summary>
        [ProtoMember(6)] public long Remaining { get; set; }
        [ProtoMember(7)] public long Capacity { get; set; }

        /// <summary>A march (anyone's) is working it.</summary>
        [ProtoMember(8)] public bool Busy { get; set; }

        /// <summary>The viewer's own march is working it.</summary>
        [ProtoMember(9)] public bool Mine { get; set; }

        /// <summary>When a fading node (whale fall) disappears, Unix ms; 0 = never.</summary>
        [ProtoMember(10)] public long FadesAtMs { get; set; }
    }

    [ProtoContract]
    public sealed class WorldBaseDto
    {
        [ProtoMember(1)] public string PlayerId { get; set; } = string.Empty;
        [ProtoMember(2)] public string Name { get; set; } = string.Empty;
        [ProtoMember(3)] public string AllianceTag { get; set; } = string.Empty;
        [ProtoMember(4)] public int X { get; set; }
        [ProtoMember(5)] public int Y { get; set; }

        /// <summary>Core Reactor level.</summary>
        [ProtoMember(6)] public int Level { get; set; }
        [ProtoMember(7)] public long Power { get; set; }
        [ProtoMember(8)] public bool Online { get; set; }
        [ProtoMember(9)] public string AvatarId { get; set; } = string.Empty;
    }
}
