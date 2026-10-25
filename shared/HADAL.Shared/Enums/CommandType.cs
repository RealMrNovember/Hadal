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
        [ProtoEnum] ClientAuth = 6,
        [ProtoEnum] FinishConstruction = 7,
        [ProtoEnum] UnlockRing = 8,
        [ProtoEnum] StartResearch = 9,
        [ProtoEnum] FinishResearch = 10,
        [ProtoEnum] BoostBuilding = 11,
        [ProtoEnum] Overdrive = 12,
        [ProtoEnum] StartSalvage = 13,
    }
}
