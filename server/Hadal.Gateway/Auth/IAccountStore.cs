using System.Collections.Concurrent;
using HADAL.Shared.Auth;

namespace Hadal.Gateway.Auth
{
    public sealed record AccountProfile(Guid AccountId, string DisplayName, DateTimeOffset CreatedAt, IReadOnlyList<string> Providers);

    /// <summary>An identity linked to an account; <see cref="SecretHash"/> is set only for transfer codes.</summary>
    public readonly record struct AccountLink(Guid AccountId, string SecretHash);

    /// <summary>
    /// Accounts, their linked identities and refresh tokens. Refresh tokens are stored only as
    /// SHA-256 hashes, transfer passwords only as PBKDF2 hashes (<see cref="PasswordHasher"/>).
    /// </summary>
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

        Task<AccountProfile?> GetProfileAsync(Guid accountId, CancellationToken cancellationToken);

        Task SetDisplayNameAsync(Guid accountId, string displayName, CancellationToken cancellationToken);

        /// <summary>Another account already goes by this name (case-insensitive).</summary>
        Task<bool> IsNameTakenAsync(string displayName, Guid exceptAccountId, CancellationToken cancellationToken);

        Task<AccountLink?> FindLinkAsync(string provider, string subject, CancellationToken cancellationToken);

        /// <summary>
        /// Links (provider, subject) to the account. For the transfer provider an account has at
        /// most one code: linking a new one replaces the old. False when the subject already
        /// belongs to a different account.
        /// </summary>
        Task<bool> LinkAsync(Guid accountId, string provider, string subject, string secretHash, CancellationToken cancellationToken);
    }

    public static class AccountNames
    {
        /// <summary>Name for an account that has not chosen one, stable per account ("Diver-4821").</summary>
        public static string Default(Guid accountId) =>
            "Diver-" + (BitConverter.ToUInt32(accountId.ToByteArray(), 0) % 10000).ToString("D4");
    }

    public sealed class InMemoryAccountStore : IAccountStore
    {
        private readonly ConcurrentDictionary<Guid, (DateTimeOffset Created, string Name)> _accounts = new();
        private readonly ConcurrentDictionary<string, (Guid Account, DateTimeOffset Expires)> _tokens = new();
        private readonly ConcurrentDictionary<(string Provider, string Subject), AccountLink> _links = new();
        private readonly object _linkGate = new();

        public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>For the in-memory admin store (Overseer player list in tests and local runs).</summary>
        public IReadOnlyCollection<Guid> AccountIds => _accounts.Keys.ToList();

        /// <summary>Overseer account removal: the account, its tokens and links.</summary>
        public bool Remove(Guid accountId)
        {
            foreach (var token in _tokens.Where(t => t.Value.Account == accountId).ToList())
                _tokens.TryRemove(token.Key, out _);
            lock (_linkGate)
            {
                foreach (var link in _links.Where(l => l.Value.AccountId == accountId).ToList())
                    _links.TryRemove(link.Key, out _);
            }

            return _accounts.TryRemove(accountId, out _);
        }

        public Task<Guid> CreateGuestAsync(string deviceId, string clientVersion, CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid();
            _accounts[id] = (DateTimeOffset.UtcNow, AccountNames.Default(id));
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

        public Task<AccountProfile?> GetProfileAsync(Guid accountId, CancellationToken cancellationToken)
        {
            if (!_accounts.TryGetValue(accountId, out var account))
                return Task.FromResult<AccountProfile?>(null);

            var providers = _links.Where(l => l.Value.AccountId == accountId).Select(l => l.Key.Provider).Distinct().OrderBy(p => p).ToArray();
            return Task.FromResult<AccountProfile?>(new AccountProfile(accountId, account.Name, account.Created, providers));
        }

        public Task<bool> IsNameTakenAsync(string displayName, Guid exceptAccountId, CancellationToken cancellationToken) =>
            Task.FromResult(_accounts.Any(a => a.Key != exceptAccountId && string.Equals(a.Value.Name, displayName, StringComparison.OrdinalIgnoreCase)));

        public Task SetDisplayNameAsync(Guid accountId, string displayName, CancellationToken cancellationToken)
        {
            _accounts.AddOrUpdate(accountId, _ => (DateTimeOffset.UtcNow, displayName), (_, a) => (a.Created, displayName));
            return Task.CompletedTask;
        }

        public Task<AccountLink?> FindLinkAsync(string provider, string subject, CancellationToken cancellationToken) =>
            Task.FromResult(_links.TryGetValue((provider, subject), out var link) ? link : (AccountLink?)null);

        public Task<bool> LinkAsync(Guid accountId, string provider, string subject, string secretHash, CancellationToken cancellationToken)
        {
            lock (_linkGate)
            {
                if (_links.TryGetValue((provider, subject), out var existing) && existing.AccountId != accountId)
                    return Task.FromResult(false);

                if (provider == AccountProviders.Transfer)
                {
                    foreach (var key in _links.Where(l => l.Key.Provider == provider && l.Value.AccountId == accountId).Select(l => l.Key).ToList())
                        _links.TryRemove(key, out _);
                }

                _links[(provider, subject)] = new AccountLink(accountId, secretHash);
                return Task.FromResult(true);
            }
        }
    }
}
