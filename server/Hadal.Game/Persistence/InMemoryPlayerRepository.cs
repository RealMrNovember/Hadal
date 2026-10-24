using System.Collections.Concurrent;

namespace Hadal.Game.Persistence
{
    /// <summary>Tests and local runs without PostgreSQL. Stores deep copies, like a real database would.</summary>
    public sealed class InMemoryPlayerRepository : IPlayerRepository
    {
        private readonly ConcurrentDictionary<string, PlayerState> _byDevice = new(StringComparer.Ordinal);

        public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<PlayerState?> LoadByDeviceAsync(string deviceId, CancellationToken cancellationToken) =>
            Task.FromResult(_byDevice.TryGetValue(deviceId, out var stored) ? Copy(stored) : null);

        public Task CreateAsync(PlayerState player, CancellationToken cancellationToken)
        {
            if (!_byDevice.TryAdd(player.DeviceId, Copy(player)))
                throw new InvalidOperationException($"Device '{player.DeviceId}' already has a player.");

            player.UnsavedBuildings.Clear();
            return Task.CompletedTask;
        }

        public Task SaveAsync(PlayerState player, CancellationToken cancellationToken)
        {
            // Same contract as the SQL store: scalar fields and wallet are overwritten, buildings are
            // appended from UnsavedBuildings. The save snapshot does not carry the full building list.
            if (!_byDevice.TryGetValue(player.DeviceId, out var stored))
                throw new InvalidOperationException($"Device '{player.DeviceId}' has no player to save.");

            var updated = Copy(stored);
            updated.ServerTick = player.ServerTick;
            updated.LastSimulatedAt = player.LastSimulatedAt;
            updated.UnlockedRings = player.UnlockedRings;
            updated.Wallet.Clear();
            foreach (var pair in player.Wallet)
                updated.Wallet[pair.Key] = pair.Value;

            foreach (var building in player.UnsavedBuildings)
            {
                updated.Buildings.RemoveAll(b => b.BuildingId == building.BuildingId);
                updated.Buildings.Add(building);
            }

            _byDevice[player.DeviceId] = Copy(updated);
            player.UnsavedBuildings.Clear();
            return Task.CompletedTask;
        }

        private static PlayerState Copy(PlayerState source)
        {
            var copy = new PlayerState
            {
                PlayerId = source.PlayerId,
                DeviceId = source.DeviceId,
                ServerTick = source.ServerTick,
                LastSimulatedAt = source.LastSimulatedAt,
                UnlockedRings = source.UnlockedRings
            };

            foreach (var pair in source.Wallet)
                copy.Wallet[pair.Key] = pair.Value;

            foreach (var building in source.Buildings)
            {
                copy.Buildings.Add(new PlacedBuilding
                {
                    BuildingId = building.BuildingId,
                    DefinitionId = building.DefinitionId,
                    Ring = building.Ring,
                    Sector = building.Sector,
                    Level = building.Level,
                    RotationSteps = building.RotationSteps
                });
            }

            return copy;
        }
    }
}
