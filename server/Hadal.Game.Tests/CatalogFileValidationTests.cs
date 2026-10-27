using HADAL.Shared.Simulation;

namespace Hadal.Game.Tests
{
    /// <summary>
    /// Checks a catalog file before it is stored as a live version (HADAL_VALIDATE_CATALOG=path); skipped otherwise.
    /// A stored version that does not parse makes the server fall back to the built-in file.
    /// </summary>
    public sealed class CatalogFileValidationTests
    {
        [Fact]
        public void A_catalog_about_to_go_live_parses()
        {
            var path = Environment.GetEnvironmentVariable("HADAL_VALIDATE_CATALOG");
            if (string.IsNullOrEmpty(path))
                return;

            var catalog = CatalogLoader.Parse(File.ReadAllText(path));
            Assert.NotEmpty(catalog.World.belts);
            Assert.True(GatherRules.Load(catalog.Gather, 24) > 0);
        }
    }
}
