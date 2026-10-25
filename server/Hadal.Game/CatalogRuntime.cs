using HADAL.Shared.Catalog;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;

namespace Hadal.Game
{
    /// <summary>
    /// The catalog every session plays by, swappable at runtime (balance edits from the Overseer
    /// panel apply without a restart). Readers take one <see cref="Snapshot"/> per operation so a
    /// swap never mixes old and new rules inside a calculation.
    /// </summary>
    public sealed class CatalogRuntime
    {
        public sealed record Snapshot(GameCatalog Catalog, ProductionCatalog Production,
            IReadOnlyDictionary<ResourceType, long> Capacities, int Version);

        private Snapshot _current;

        public CatalogRuntime(GameCatalog catalog, int version = 0)
        {
            _current = Build(catalog, version);
        }

        public Snapshot Current => Volatile.Read(ref _current);

        /// <summary>Use <see cref="GameWorld.ReplaceCatalog"/> so live sessions settle first.</summary>
        internal void Replace(GameCatalog catalog, int version) => Volatile.Write(ref _current, Build(catalog, version));

        private static Snapshot Build(GameCatalog catalog, int version) =>
            new(catalog, catalog.ToProductionCatalog(), catalog.Capacities(), version);
    }
}
