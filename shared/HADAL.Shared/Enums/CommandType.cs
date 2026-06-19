using ProtoBuf;

namespace HADAL.Shared.Enums
{
    [ProtoContract]
    public enum CommandType
    {
        [ProtoEnum] Unknown = 0,
        [ProtoEnum] PlaceBuilding = 1,
        [ProtoEnum] RemoveBuilding = 2,
        [ProtoEnum] UpgradeBuilding = 3,
        [ProtoEnum] StartExpedition = 4,
        [ProtoEnum] ExecuteGacha = 5,
    }
}
