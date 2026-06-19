using System.Collections.Generic;
using HADAL.Shared.Enums;
using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    [ProtoContract]
    public sealed class ResourceStateDto
    {
        [ProtoMember(1)] public Dictionary<ResourceType, long> Amounts { get; set; } = new();
    }
}
