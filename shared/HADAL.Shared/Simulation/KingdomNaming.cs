namespace HADAL.Shared.Simulation
{
    /// <summary>Kingdom ids are "w{number}" (docs/39): "w1" is Kingdom #1, the world from before kingdoms.</summary>
    public static class KingdomNaming
    {
        public static string IdOf(int number) => "w" + number;

        /// <summary>The kingdom number of an id; 1 for an empty or unknown id (the old single world).</summary>
        public static int NumberOf(string worldId) =>
            !string.IsNullOrEmpty(worldId) && worldId.Length > 1 && worldId[0] == 'w' && int.TryParse(worldId.Substring(1), out var n) && n > 0 ? n : 1;
    }
}
