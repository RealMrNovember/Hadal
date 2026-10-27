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

        private readonly int _publishUpTo;

        public CatalogBootstrap(IAdminStore store, GameWorld world, LiveCatalog live, ILogger<CatalogBootstrap> logger, IOptions<GameOptions> options)
        {
            _publishUpTo = options.Value.PublishCatalogUpTo;
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

            // A release whose catalog file is a newer design ("version" field) than the panel's last
            // edit supersedes it; it is stored as a new version so the panel history shows it.
            if (FileSupersedes(_live.Json, latest.Json) && _publishUpTo > 0 && DesignVersion(_live.Json) > _publishUpTo)
            {
                _logger.LogInformation("Catalog file v{Design} is held back (Game:PublishCatalogUpTo = {Limit}); stored catalog v{Stored} keeps running.",
                    DesignVersion(_live.Json), _publishUpTo, latest.Version);
            }
            else if (FileSupersedes(_live.Json, latest.Json))
            {
                var version = await _store.AddCatalogAsync(_live.Json, "release", $"catalog file v{DesignVersion(_live.Json)}", cancellationToken);
                _world.ReplaceCatalog(CatalogLoader.Parse(_live.Json), version);
                _live.Set(version, _live.Json);
                await _store.AuditAsync("release", "catalog.publish", $"v{version}", "newer catalog file in the release", "server", cancellationToken);
                _logger.LogInformation("Catalog file v{Design} supersedes stored catalog v{Stored}; published as v{Version}.",
                    DesignVersion(_live.Json), latest.Version, version);
                return;
            }

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

        /// <summary>True when the file's design version is newer than the stored catalog's.</summary>
        public static bool FileSupersedes(string fileJson, string storedJson) => DesignVersion(fileJson) > DesignVersion(storedJson);

        /// <summary>The catalog's own top-level "version" (bumped with each balance design); 0 if missing.</summary>
        public static int DesignVersion(string json)
        {
            try
            {
                using var document = System.Text.Json.JsonDocument.Parse(json);
                return document.RootElement.TryGetProperty("version", out var v) && v.TryGetInt32(out var n) ? n : 0;
            }
            catch (System.Text.Json.JsonException)
            {
                return 0;
            }
        }
    }
}
