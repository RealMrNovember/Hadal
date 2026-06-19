using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    [ProtoContract]
    public sealed class HeroStateDto
    {
        [ProtoMember(1)] public string EntityId { get; set; } = string.Empty;
        [ProtoMember(2)] public string DefinitionId { get; set; } = string.Empty;
        [ProtoMember(3)] public int Level { get; set; }
        [ProtoMember(4)] public int Experience { get; set; }
        [ProtoMember(5)] public int Faction { get; set; }
    }
}
