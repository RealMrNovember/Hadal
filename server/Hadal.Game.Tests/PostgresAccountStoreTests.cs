using Hadal.Gateway.Auth;
using HADAL.Shared.Auth;
using Npgsql;

namespace Hadal.Game.Tests
{
    /// <summary>Account store SQL against a real PostgreSQL (HADAL_TEST_PG); skipped when unset.</summary>
    public sealed class PostgresAccountStoreTests
    {
        private static readonly string? ConnectionString = PostgresTest.ConnectionString;

        [Fact]
        public async Task Profile_Links_AndTransferRotation()
        {
            if (string.IsNullOrEmpty(ConnectionString))
                return; // not configured on this machine

            await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
            var store = new PostgresAccountStore(dataSource);
            await store.InitializeAsync(default);
            await store.InitializeAsync(default); // idempotent, including the column migration

            var a = await store.CreateGuestAsync("pgtest", "0.1.0", default);
            var b = await store.CreateGuestAsync("pgtest", "0.1.0", default);
            var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

            try
            {
                var profile = await store.GetProfileAsync(a, default);
                Assert.Equal(AccountNames.Default(a), profile!.DisplayName);
                Assert.Empty(profile.Providers);

                await store.SetDisplayNameAsync(a, "Kaptan Deniz", default);
                Assert.Equal("Kaptan Deniz", (await store.GetProfileAsync(a, default))!.DisplayName);

                // Transfer: a second code replaces the first.
                Assert.True(await store.LinkAsync(a, AccountProviders.Transfer, "CODEONE" + suffix, "h1", default));
                Assert.True(await store.LinkAsync(a, AccountProviders.Transfer, "CODETWO" + suffix, "h2", default));
                Assert.Null(await store.FindLinkAsync(AccountProviders.Transfer, "CODEONE" + suffix, default));
                Assert.Equal(new AccountLink(a, "h2"), await store.FindLinkAsync(AccountProviders.Transfer, "CODETWO" + suffix, default));

                // Google subject: relinking to the owner is fine, stealing it is refused and changes nothing.
                var subject = "sub-" + suffix;
                Assert.True(await store.LinkAsync(a, AccountProviders.Google, subject, string.Empty, default));
                Assert.True(await store.LinkAsync(a, AccountProviders.Google, subject, string.Empty, default));
                Assert.False(await store.LinkAsync(b, AccountProviders.Google, subject, string.Empty, default));
                Assert.Equal(a, (await store.FindLinkAsync(AccountProviders.Google, subject, default))!.Value.AccountId);

                Assert.Equal(new[] { AccountProviders.Google, AccountProviders.Transfer }, (await store.GetProfileAsync(a, default))!.Providers);
            }
            finally
            {
                await using var cleanup = dataSource.CreateCommand("DELETE FROM hadal.accounts WHERE account_id = ANY(@ids)");
                cleanup.Parameters.AddWithValue("ids", new[] { a, b });
                await cleanup.ExecuteNonQueryAsync();
            }
        }
    }
}
