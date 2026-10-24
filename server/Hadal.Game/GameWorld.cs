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

        public async Task<PlayerSession> ConnectAsync(string deviceId, IClientConnection connection, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                throw new ArgumentException("Device id is required until Auth API lands (Phase 2).", nameof(deviceId));

            await _connectGate.WaitAsync(cancellationToken);
            try
            {
                if (!_sessions.TryGetValue(deviceId, out var session))
                {
                    var state = await _repository.LoadByDeviceAsync(deviceId, cancellationToken)
                                ?? await CreatePlayerAsync(deviceId, cancellationToken);

                    session = new PlayerSession(state, _catalog, _production, _capacities, _serializer);
                    _sessions[deviceId] = session;
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
                _sessions.TryRemove(new KeyValuePair<string, PlayerSession>(session.DeviceId, session));
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

        private async Task<PlayerState> CreatePlayerAsync(string deviceId, CancellationToken cancellationToken)
        {
            var state = new PlayerState
            {
                PlayerId = Guid.NewGuid(),
                DeviceId = deviceId,
                LastSimulatedAt = _clock.UtcNow,
                UnlockedRings = _catalog.Grid.startingUnlockedRings
            };

            foreach (var pair in _catalog.StartingWallet())
                state.Wallet[pair.Key] = pair.Value;

            await _repository.CreateAsync(state, cancellationToken);
            _logger.LogInformation("Created player {PlayerId} for device {DeviceId}.", state.PlayerId, deviceId);
            return state;
        }
    }
}
