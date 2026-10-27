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

        /// <summary>
        /// Highest catalog design version a release may publish by itself (0 = any). A newer catalog file is then
        /// held back and the stored one keeps running, so code can ship before its balance goes live
        /// (env <c>Game__PublishCatalogUpTo</c>).
        /// </summary>
        public int PublishCatalogUpTo { get; set; }
    }
}
