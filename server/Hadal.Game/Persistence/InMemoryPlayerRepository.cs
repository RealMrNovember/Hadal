using System.Collections.Concurrent;

namespace Hadal.Game.Persistence
{
    /// <summary>Tests and local runs without PostgreSQL. Stores deep copies, like a real database would.</summary>
    public sealed class InMemoryPlayerRepository : IPlayerRepository
    {
        private readonly ConcurrentDictionary<string, PlayerState> _byIdentity = new(StringComparer.Ordinal);

        public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Guid?> DeleteAsync(string identityKey, CancellationToken cancellationToken) =>
            Task.FromResult(_byIdentity.TryRemove(identityKey, out var removed) ? removed.PlayerId : (Guid?)null);

        /// <summary>Without an account store every identity counts as accounted for.</summary>
        public Task<IReadOnlyList<(string IdentityKey, Guid PlayerId, DateTimeOffset CreatedAt)>> ListWithoutAccountAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<(string, Guid, DateTimeOffset)>>(Array.Empty<(string, Guid, DateTimeOffset)>());

        public Task<string?> IdentityOfAsync(Guid playerId, CancellationToken cancellationToken) =>
            Task.FromResult(_byIdentity.Values.FirstOrDefault(p => p.PlayerId == playerId)?.IdentityKey);

        public Task<IReadOnlyList<Guid>> ListPlayerIdsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>(_byIdentity.Values.Select(p => p.PlayerId).ToList());

        public Task<PlayerState?> LoadByIdentityAsync(string identityKey, CancellationToken cancellationToken) =>
            Task.FromResult(_byIdentity.TryGetValue(identityKey, out var stored) ? Loaded(stored) : null);

        // Like the SQL store: research and building functions come back from the extras document.
        private static PlayerState Loaded(PlayerState stored)
        {
            var copy = Copy(stored);
            PlayerExtras.Read(copy.ExtrasJson, copy);
            return copy;
        }

        public Task CreateAsync(PlayerState player, CancellationToken cancellationToken)
        {
            if (!_byIdentity.TryAdd(player.IdentityKey, Copy(player)))
                throw new InvalidOperationException($"Identity '{player.IdentityKey}' already has a player.");

            player.UnsavedBuildings.Clear();
            return Task.CompletedTask;
        }

        public Task SaveAsync(PlayerState player, CancellationToken cancellationToken)
        {
            // Same contract as the SQL store: scalar fields and wallet are overwritten, buildings are
            // appended from UnsavedBuildings. The save snapshot does not carry the full building list.
            if (!_byIdentity.TryGetValue(player.IdentityKey, out var stored))
                throw new InvalidOperationException($"Identity '{player.IdentityKey}' has no player to save.");

            var updated = Copy(stored);
            updated.ServerTick = player.ServerTick;
            updated.LastSimulatedAt = player.LastSimulatedAt;
            updated.UnlockedRings = player.UnlockedRings;
            updated.ExtrasJson = player.ExtrasJson;
            updated.Wallet.Clear();
            foreach (var pair in player.Wallet)
                updated.Wallet[pair.Key] = pair.Value;

            foreach (var removed in player.RemovedBuildingIds)
                updated.Buildings.RemoveAll(b => b.BuildingId == removed);

            foreach (var building in player.UnsavedBuildings)
            {
                updated.Buildings.RemoveAll(b => b.BuildingId == building.BuildingId);
                updated.Buildings.Add(building);
            }

            _byIdentity[player.IdentityKey] = Copy(updated);
            player.UnsavedBuildings.Clear();
            player.RemovedBuildingIds.Clear();
            return Task.CompletedTask;
        }

        private static PlayerState Copy(PlayerState source)
        {
            var copy = new PlayerState
            {
                PlayerId = source.PlayerId,
                IdentityKey = source.IdentityKey,
                ServerTick = source.ServerTick,
                LastSimulatedAt = source.LastSimulatedAt,
                UnlockedRings = source.UnlockedRings,
                ExtrasJson = source.ExtrasJson
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
                    RotationSteps = building.RotationSteps,
                    ConstructionEndsAt = building.ConstructionEndsAt,
                    ConstructionSeconds = building.ConstructionSeconds
                });
            }

            return copy;
        }
    }
}
