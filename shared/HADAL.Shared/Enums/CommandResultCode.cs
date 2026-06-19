using ProtoBuf;

namespace HADAL.Shared.Enums
{
    [ProtoContract]
    public enum CommandResultCode
    {
        [ProtoEnum] Unknown = 0,
        [ProtoEnum] Accepted = 1,
        [ProtoEnum] Rejected = 2,
        [ProtoEnum] SchemaIncompatible = 3,
    }
}
