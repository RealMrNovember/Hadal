namespace HADAL.Shared.Simulation
{
    /// <summary>
    /// The alliance's own rankings (docs/31 §2): members by power, by troops they defeated in PvP, and by research
    /// points they donated.
    /// </summary>
    public static class AllianceRankingKinds
    {
        public const string Power = "power";
        public const string Kills = "kills";
        public const string Donations = "donations";

        public static bool IsKind(string? kind) => kind == Power || kind == Kills || kind == Donations;
    }
}
