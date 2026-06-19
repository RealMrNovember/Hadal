using ProtoBuf;

namespace HADAL.Shared.Enums
{
    [ProtoContract]
    public enum GachaType
    {
        [ProtoEnum] Unknown = 0,
        [ProtoEnum] CryoPodSalvage = 1,
        [ProtoEnum] DeepSeaSonarPing = 2,
    }
}
