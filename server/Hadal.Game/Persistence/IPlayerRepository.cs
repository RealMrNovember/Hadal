namespace Hadal.Game.Persistence
{
    public interface IPlayerRepository
    {
        /// <summary>Creates schema objects if they do not exist. Idempotent.</summary>
        Task InitializeAsync(CancellationToken cancellationToken);

        Task<PlayerState?> LoadByIdentityAsync(string identityKey, CancellationToken cancellationToken);

        /// <summary>Inserts a brand-new player. Fails if the identity already has one.</summary>
        Task CreateAsync(PlayerState player, CancellationToken cancellationToken);

        /// <summary>Persists wallet, tick, clock and any buildings in <see cref="PlayerState.UnsavedBuildings"/>.</summary>
        Task SaveAsync(PlayerState player, CancellationToken cancellationToken);

        /// <summary>Removes the player of an identity with its buildings; returns its player id, or null if there was none.</summary>
        Task<Guid?> DeleteAsync(string identityKey, CancellationToken cancellationToken);

        /// <summary>Players whose identity has no account (left by load and acceptance tests).</summary>
        Task<IReadOnlyList<(string IdentityKey, Guid PlayerId, DateTimeOffset CreatedAt)>> ListWithoutAccountAsync(CancellationToken cancellationToken);

        /// <summary>Every player's id (mail to everyone).</summary>
        Task<IReadOnlyList<Guid>> ListPlayerIdsAsync(CancellationToken cancellationToken);
    }
}
