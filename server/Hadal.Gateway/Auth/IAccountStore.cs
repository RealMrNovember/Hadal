using System.Collections.Concurrent;

namespace Hadal.Gateway.Auth
{
    /// <summary>Accounts and refresh tokens. Refresh tokens are stored only as SHA-256 hashes.</summary>
    public interface IAccountStore
    {
        Task InitializeAsync(CancellationToken cancellationToken);

        Task<Guid> CreateGuestAsync(string deviceId, string clientVersion, CancellationToken cancellationToken);

        Task StoreRefreshTokenAsync(byte[] tokenHash, Guid accountId, DateTimeOffset expiresAt, CancellationToken cancellationToken);

        /// <summary>
        /// Atomically deletes and returns the owner of a refresh token (single use — rotation).
        /// Null when unknown or expired.
        /// </summary>
        Task<Guid?> ConsumeRefreshTokenAsync(byte[] tokenHash, DateTimeOffset now, CancellationToken cancellationToken);
    }

    public sealed class InMemoryAccountStore : IAccountStore
    {
        private readonly ConcurrentDictionary<Guid, DateTimeOffset> _accounts = new();
        private readonly ConcurrentDictionary<string, (Guid Account, DateTimeOffset Expires)> _tokens = new();

        public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Guid> CreateGuestAsync(string deviceId, string clientVersion, CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid();
            _accounts[id] = DateTimeOffset.UtcNow;
            return Task.FromResult(id);
        }

        public Task StoreRefreshTokenAsync(byte[] tokenHash, Guid accountId, DateTimeOffset expiresAt, CancellationToken cancellationToken)
        {
            _tokens[Convert.ToHexString(tokenHash)] = (accountId, expiresAt);
            return Task.CompletedTask;
        }

        public Task<Guid?> ConsumeRefreshTokenAsync(byte[] tokenHash, DateTimeOffset now, CancellationToken cancellationToken)
        {
            if (_tokens.TryRemove(Convert.ToHexString(tokenHash), out var entry) && entry.Expires > now)
                return Task.FromResult<Guid?>(entry.Account);

            return Task.FromResult<Guid?>(null);
        }
    }
}
