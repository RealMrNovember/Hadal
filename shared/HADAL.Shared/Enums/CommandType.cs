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
        [ProtoEnum] LevelUpHero = 14,
        [ProtoEnum] PromoteHero = 15,
        [ProtoEnum] AssignHero = 16,
        [ProtoEnum] SendChat = 17,
        [ProtoEnum] CreateAlliance = 18,
        [ProtoEnum] JoinAlliance = 19,
        [ProtoEnum] LeaveAlliance = 20,
        [ProtoEnum] BrowseAlliances = 21,
        [ProtoEnum] KickMember = 22,
        [ProtoEnum] RequestHelp = 23,
        [ProtoEnum] HelpAll = 24,
        [ProtoEnum] ClaimQuest = 25,
        [ProtoEnum] ClaimDailyChest = 26,
        [ProtoEnum] OpenItem = 27,
        [ProtoEnum] RecruitHero = 28,
    }
}
