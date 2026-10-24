namespace Hadal.Data.Enums
{
    // ResourceType intentionally lives in HADAL.Shared.Enums, not here.
    // A client-local copy drifted out of sync with the wire ordinals and silently
    // relabelled balances (Shared Oxygen=1 read as client Energy=1). Wire-facing
    // identities belong to the shared assembly — see Forbidden Pattern #3.

    public enum BuildingCategory
    {
        Core,
        Production,
        Military,
        Research,
        Infrastructure,
        Dome
    }

    public enum DomeType
    {
        None,
        Mega,
        Agriculture,
        War,
        Science
    }

    public enum HeroClass
    {
        Commander,
        Scientist,
        Engineer,
        Hunter,
        Medic,
        Pilot
    }

    public enum FactionType
    {
        AtlanticUnion,
        PacificOrder,
        Deepborn,
        ForgottenOnes,
        AbyssCult
    }

    public enum HeroRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    public enum EnemyType
    {
        AbyssCrawler,
        SirenSwarm,
        IronLeviathan,
        TheForgotten,
        TitanOfHadal
    }

    public enum DepthZone
    {
        SafeZone,
        MidAbyss,
        DarkAbyss,
        ForbiddenAbyss,
        HadalTrench,
        TheCore
    }

    public enum ExpeditionBiome
    {
        BioluminescentForest,
        SunkenCity,
        LeviathanDepths,
        OldHumanLab,
        AlienTemple,
        SonarOnly
    }

    public enum PressureTier
    {
        Tier1000m,
        Tier3000m,
        Tier5000m,
        Tier7000m,
        Tier11000m
    }

    public enum SubmarineClass
    {
        Scout,
        Research,
        Combat,
        Leviathan
    }
}
