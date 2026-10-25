using Hadal.Game;
using Microsoft.Extensions.Options;

namespace Hadal.Gateway.Admin
{
    /// <summary>
    /// The catalog JSON the game currently runs (version 0 = the file baked into the image,
    /// 1.. = Overseer edits stored in the database). Served to clients at /api/v1/catalog.
    /// </summary>
    public sealed class LiveCatalog
    {
        private sealed record State(int Version, string Json);

        private State _current;

        public LiveCatalog(IOptions<GameOptions> options)
        {
            _current = new State(0, File.Exists(options.Value.CatalogPath) ? File.ReadAllText(options.Value.CatalogPath) : "{}");
        }

        public int Version => Volatile.Read(ref _current).Version;

        public string Json => Volatile.Read(ref _current).Json;

        public void Set(int version, string json) => Volatile.Write(ref _current, new State(version, json));
    }

    /// <summary>
    /// After the stores exist, switches the world to the newest catalog version in the database
    /// (if any), so balance edits survive restarts and deploys.
    /// </summary>
    public sealed class CatalogBootstrap : IHostedService
    {
        private readonly IAdminStore _store;
        private readonly GameWorld _world;
        private readonly LiveCatalog _live;
        private readonly ILogger<CatalogBootstrap> _logger;

        public CatalogBootstrap(IAdminStore store, GameWorld world, LiveCatalog live, ILogger<CatalogBootstrap> logger)
        {
            _store = store;
            _world = world;
            _live = live;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _store.InitializeAsync(cancellationToken);
            var latest = await _store.GetLatestCatalogAsync(cancellationToken);
            if (latest == null)
                return;

            try
            {
                _world.ReplaceCatalog(CatalogLoader.Parse(latest.Json), latest.Version);
                _live.Set(latest.Version, latest.Json);
            }
            catch (FormatException exception)
            {
                // A stored version that no longer validates (e.g. after a schema change) must not stop the server.
                _logger.LogError(exception, "Stored catalog version {Version} is invalid; running the built-in catalog.", latest.Version);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
