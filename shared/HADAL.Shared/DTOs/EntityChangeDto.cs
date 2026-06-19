using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    [ProtoContract]
    public enum EntityChangeKind
    {
        [ProtoEnum] Unknown = 0,
        [ProtoEnum] Create = 1,
        [ProtoEnum] Update = 2,
        [ProtoEnum] Delete = 3,
    }

    [ProtoContract]
    public sealed class EntityChangeDto
    {
        [ProtoMember(1)] public string EntityId { get; set; } = string.Empty;
        [ProtoMember(2)] public EntityChangeKind Kind { get; set; }
        [ProtoMember(3)] public byte[] Payload { get; set; } = System.Array.Empty<byte>();
    }
}
