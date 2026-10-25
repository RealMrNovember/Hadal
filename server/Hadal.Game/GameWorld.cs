using System.Collections.Concurrent;
using Hadal.Game.Persistence;
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
        private readonly GameCatalog _catalog;
        private readonly ProductionCatalog _production;
        private readonly IReadOnlyDictionary<ResourceType, long> _capacities;
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
        {
            _catalog = catalog;
            _production = catalog.ToProductionCatalog();
            _capacities = catalog.Capacities();
            _repository = repository;
            _clock = clock;
            _serializer = serializer;
            _logger = logger;
        }

        public int SessionCount => _sessions.Count;

        public async Task<PlayerSession> ConnectAsync(string identityKey, IClientConnection connection, CancellationToken cancellationToken)
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

                    session = new PlayerSession(state, _catalog, _production, _capacities, _serializer);
                    _sessions[identityKey] = session;
                }

                var previous = session.Attach(connection, _clock.UtcNow);
                if (previous != null && !ReferenceEquals(previous, connection))
                {
                    _logger.LogInformation("Player {PlayerId} reconnected; closing previous connection.", session.PlayerId);
                    previous.Close("replaced by a newer connection");
                }

                return session;
            }
            finally
            {
                _connectGate.Release();
            }
        }

        public void HandleCommand(PlayerSession session, CommandEnvelope envelope) =>
            session.HandleCommand(envelope, _clock.UtcNow);

        /// <summary>Detaches, saves and unloads the session if this connection still owns it.</summary>
        public async Task DisconnectAsync(PlayerSession session, IClientConnection connection, CancellationToken cancellationToken)
        {
            if (!session.Detach(connection, _clock.UtcNow))
                return;

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
