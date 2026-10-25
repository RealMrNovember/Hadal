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
    }
}
