using System.Collections.Concurrent;
using Hadal.Game.Persistence;
using Hadal.Game.Social;
using Hadal.Game.World;
using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.Enums;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;
using Microsoft.Extensions.Logging;

namespace Hadal.Game
{
    /// <summary>
    /// All live player sessions on this node. Transport-agnostic: the gateway hands it a
    /// connection after the schema handshake and forwards commands.
    /// </summary>
    public sealed class GameWorld
    {
        private readonly CatalogRuntime _runtime;
        private readonly IPlayerRepository _repository;
        private readonly IClock _clock;
        private readonly INetworkSerializer _serializer;
        private readonly ILogger<GameWorld> _logger;

        private readonly ConcurrentDictionary<string, PlayerSession> _sessions = new(StringComparer.Ordinal);
        private readonly SemaphoreSlim _connectGate = new(1, 1);

        public GameWorld(
            GameCatalog catalog,
            IPlayerRepository repository,
            IClock clock,
            INetworkSerializer serializer,
            ILogger<GameWorld> logger)
            : this(new CatalogRuntime(catalog), repository, clock, serializer, logger)
        {
        }

        public GameWorld(
            CatalogRuntime runtime,
            IPlayerRepository repository,
            IClock clock,
            INetworkSerializer serializer,
            ILogger<GameWorld> logger)
            : this(runtime, repository, new InMemorySocialStore(), new InMemoryWorldStore(), clock, serializer, logger)
        {
        }

        public GameWorld(
            CatalogRuntime runtime,
            IPlayerRepository repository,
            ISocialStore socialStore,
            IWorldStore worldStore,
            IClock clock,
            INetworkSerializer serializer,
            ILogger<GameWorld> logger)
        {
            _runtime = runtime;
            _repository = repository;
            _clock = clock;
            _serializer = serializer;
            _logger = logger;
            Social = new SocialHub(socialStore, runtime, serializer, logger);
            Map = new WorldService(worldStore, runtime, serializer, Social, logger, clock);
            Social.NameLookup = Map.NameOf;
        }

        /// <summary>The outer world map (docs/28).</summary>
        public WorldService Map { get; }

        /// <summary>Chat, alliances and alliance help (docs/25).</summary>
        public SocialHub Social { get; }

        public int SessionCount => _sessions.Count;

        private GameCatalog _catalog => _runtime.Current.Catalog;

        public CatalogRuntime.Snapshot Catalog => _runtime.Current;

        /// <summary>Account ids (identity keys) with a live connection.</summary>
        public IReadOnlyCollection<string> OnlineIdentities =>
            _sessions.Where(s => s.Value.Connection != null).Select(s => s.Key).ToList();

        public bool IsOnline(string identityKey) =>
            _sessions.TryGetValue(identityKey, out var session) && session.Connection != null;

        /// <summary>
        /// Swaps the rules every session plays by. Sessions first settle their economy under the
        /// old rules, then pick up the new rates.
        /// </summary>
        public void ReplaceCatalog(GameCatalog catalog, int version)
        {
            var now = _clock.UtcNow;
            foreach (var session in _sessions.Values)
                session.Settle(now);

            _runtime.Replace(catalog, version);

            foreach (var session in _sessions.Values)
                session.ApplyCatalogChange(now);

            _logger.LogInformation("Catalog version {Version} is live for {Sessions} sessions.", version, _sessions.Count);
        }

        /// <summary>
        /// Adds (or with negative amounts removes) resources: live sessions update at once, offline
        /// players are loaded, changed and saved under the connect gate so a login cannot race it.
        /// False when the player does not exist.
        /// </summary>
        public async Task<bool> GrantAsync(string identityKey, IReadOnlyDictionary<ResourceType, long> amounts, CancellationToken cancellationToken)
        {
            await _connectGate.WaitAsync(cancellationToken);
            try
            {
                if (_sessions.TryGetValue(identityKey, out var session))
                {
                    session.Grant(amounts, _clock.UtcNow);
                    return true;
                }

                var state = await _repository.LoadByIdentityAsync(identityKey, cancellationToken);
                if (state == null)
                    return false;

                var reactor = state.Buildings.Where(b => b.DefinitionId == BuildingProgression.ReactorId).Select(b => b.Level).DefaultIfEmpty(0).Max();
                WalletOps.Add(state.Wallet, amounts, _catalog.Capacities(reactor));
                await _repository.SaveAsync(state, cancellationToken);
                return true;
            }
            finally
            {
                _connectGate.Release();
            }
        }

        /// <summary>Live state when the player is loaded, otherwise the stored state; null if unknown.</summary>
        public async Task<PlayerState?> DescribeAsync(string identityKey, CancellationToken cancellationToken) =>
            _sessions.TryGetValue(identityKey, out var session)
                ? session.Describe(_clock.UtcNow)
                : await _repository.LoadByIdentityAsync(identityKey, cancellationToken);

        /// <summary>Closes the player's connection (ban, forced logout). The client may reconnect unless refused.</summary>
        /// <summary>
        /// Removes a player for good (Overseer): closes their session without saving, takes their base off the map,
        /// drops them from their alliance and deletes the player row with its buildings. Returns false when the
        /// identity had no player. The account itself belongs to the gateway.
        /// </summary>
        public async Task<bool> DeletePlayerAsync(string identityKey, CancellationToken cancellationToken)
        {
            var now = _clock.UtcNow;
            Guid? playerId = null;
            if (_sessions.TryRemove(identityKey, out var session))
            {
                session.MarkDeleted();
                playerId = session.PlayerId;
                session.Connection?.Close("account removed");
            }

            var deleted = await _repository.DeleteAsync(identityKey, cancellationToken);
            playerId ??= deleted;
            if (playerId == null)
                return false;

            var publicId = playerId.Value.ToString("N");
            Social.ForgetPlayer(publicId, now);
            await Map.ForgetAsync(publicId, now, cancellationToken);
            _logger.LogInformation("Deleted player {PlayerId} (identity {IdentityKey}).", publicId, identityKey);
            return true;
        }

        public Task<IReadOnlyList<(string IdentityKey, Guid PlayerId, DateTimeOffset CreatedAt)>> PlayersWithoutAccountAsync(CancellationToken cancellationToken) =>
            _repository.ListWithoutAccountAsync(cancellationToken);

        public bool Kick(string identityKey, string reason)
        {
            if (!_sessions.TryGetValue(identityKey, out var session) || session.Connection == null)
                return false;

            session.Connection.Close(reason);
            return true;
        }

        public async Task<PlayerSession> ConnectAsync(string identityKey, IClientConnection connection, CancellationToken cancellationToken,
            string? displayName = null)
        {
            if (string.IsNullOrWhiteSpace(identityKey))
                throw new ArgumentException("Identity key (account id) is required.", nameof(identityKey));

            await _connectGate.WaitAsync(cancellationToken);
            try
            {
                if (!_sessions.TryGetValue(identityKey, out var session))
                {
                    var state = await _repository.LoadByIdentityAsync(identityKey, cancellationToken)
                                ?? await CreatePlayerAsync(identityKey, cancellationToken);

                    session = new PlayerSession(state, _runtime, _serializer);
                    _sessions[identityKey] = session;
                }

                var now = _clock.UtcNow;
                session.DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Diver" : displayName;
                session.ApplyOfflineHelps(Social.TakeOfflineHelps(session.PublicId), now);

                var previous = session.Attach(connection, now);
                if (previous != null && !ReferenceEquals(previous, connection))
                {
                    _logger.LogInformation("Player {PlayerId} reconnected; closing previous connection.", session.PlayerId);
                    previous.Close("replaced by a newer connection");
                }

                Social.OnConnected(session, now);
                Map.OnConnected(session);

                return session;
            }
            finally
            {
                _connectGate.Release();
            }
        }

        public void HandleCommand(PlayerSession session, CommandEnvelope envelope)
        {
            if (SocialHub.Handles(envelope.Type))
                Social.Handle(session, envelope, _clock.UtcNow);
            else if (WorldService.Handles(envelope.Type))
                Map.Handle(session, envelope, _clock.UtcNow);
            else
                session.HandleCommand(envelope, _clock.UtcNow);
        }

        /// <summary>Detaches, saves and unloads the session if this connection still owns it.</summary>
        public async Task DisconnectAsync(PlayerSession session, IClientConnection connection, CancellationToken cancellationToken)
        {
            if (!session.Detach(connection, _clock.UtcNow))
                return;

            Social.OnDisconnected(session, _clock.UtcNow);
            Map.OnDisconnected(session);

            await SaveAsync(session, cancellationToken);

            // Unload only if nobody reattached while we were saving.
            if (session.Connection == null)
                _sessions.TryRemove(new KeyValuePair<string, PlayerSession>(session.IdentityKey, session));
        }

        public void Tick()
        {
            var now = _clock.UtcNow;
            foreach (var session in _sessions.Values)
            {
                try
                {
                    session.Tick(now);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Tick failed for player {PlayerId}.", session.PlayerId);
                }
            }
        }

        public async Task SaveAllAsync(CancellationToken cancellationToken)
        {
            foreach (var session in _sessions.Values)
                await SaveAsync(session, cancellationToken);
        }

        private async Task SaveAsync(PlayerSession session, CancellationToken cancellationToken)
        {
            var snapshot = session.TakeSaveSnapshot();
            if (snapshot == null)
                return;

            try
            {
                await _repository.SaveAsync(snapshot, cancellationToken);
            }
            catch (Exception exception)
            {
                session.RestoreAfterFailedSave(snapshot);
                _logger.LogError(exception, "Saving player {PlayerId} failed; will retry.", session.PlayerId);
            }
        }

        private async Task<PlayerState> CreatePlayerAsync(string identityKey, CancellationToken cancellationToken)
        {
            var state = new PlayerState
            {
                PlayerId = Guid.NewGuid(),
                IdentityKey = identityKey,
                LastSimulatedAt = _clock.UtcNow,
                UnlockedRings = _catalog.Grid.startingUnlockedRings
            };

            foreach (var pair in _catalog.StartingWallet())
                state.Wallet[pair.Key] = pair.Value;

            await _repository.CreateAsync(state, cancellationToken);

            // Every base starts around its reactor: the catalog's preferred building for each
            // ring-0 sector is pre-placed and saved straight away.
            foreach (var starter in _catalog.StarterBuildings())
            {
                var building = new PlacedBuilding
                {
                    BuildingId = "b" + Guid.NewGuid().ToString("N"),
                    DefinitionId = starter.DefinitionId,
                    Ring = starter.Ring,
                    Sector = starter.Sector,
                    Level = 1
                };
                state.Buildings.Add(building);
                state.UnsavedBuildings.Add(building);
            }

            if (state.UnsavedBuildings.Count > 0)
                await _repository.SaveAsync(state, cancellationToken);

            _logger.LogInformation("Created player {PlayerId} for identity {IdentityKey}.", state.PlayerId, identityKey);
            return state;
        }
    }
}
