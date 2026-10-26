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
    }
}
