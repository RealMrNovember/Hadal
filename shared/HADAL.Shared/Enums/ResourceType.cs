using ProtoBuf;

namespace HADAL.Shared.Enums
{
    [ProtoContract]
    public enum ResourceType
    {
        [ProtoEnum] Unknown = 0,
        [ProtoEnum] Oxygen = 1,
        [ProtoEnum] Food = 2,
        [ProtoEnum] Biomass = 3,
        [ProtoEnum] Research = 4,
        [ProtoEnum] Alloy = 5,
    }
}
