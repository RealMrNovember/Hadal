namespace Hadal.Game
{
    /// <summary>Bound from the "Game" configuration section.</summary>
    public sealed class GameOptions
    {
        public const string Section = "Game";

        /// <summary>Path to shared/catalog/catalog.json (copied into the image at build time).</summary>
        public string CatalogPath { get; set; } = "catalog/catalog.json";

        /// <summary>How often dirty player state is flushed to the database.</summary>
        public int SaveIntervalSeconds { get; set; } = 10;
    }
}
