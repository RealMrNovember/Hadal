using Hadal.Game.Mailbox;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;
using Npgsql;

namespace Hadal.Game.Tests
{
    /// <summary>Mails against a real PostgreSQL (HADAL_TEST_PG); skipped otherwise.</summary>
    public sealed class PostgresMailStoreTests
    {
        private static readonly string? ConnectionString = PostgresTest.ConnectionString;

        [Fact]
        public async Task Mails_round_trip_and_a_claim_is_never_undone()
        {
            if (string.IsNullOrEmpty(ConnectionString))
                return; // not configured on this machine

            await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
            var store = new PostgresMailStore(dataSource);
            await store.InitializeAsync(default);
            await store.InitializeAsync(default); // idempotent

            var now = DateTimeOffset.UtcNow;
            var player = "mailtest" + Guid.NewGuid().ToString("N")[..8];
            var mail = new Mail
            {
                Id = Guid.NewGuid().ToString("N"), PlayerId = player, Kind = MailKinds.Admin, Title = "Hediye", Body = "Gövde",
                Attachments = MailAttachments.From(new Dictionary<ResourceType, long> { [ResourceType.Hadalite] = 50 }, new Dictionary<string, int> { ["vip_100"] = 1 }),
                CreatedAt = now, ExpiresAt = now + MailRules.Lifetime
            };
            await store.InsertAsync(new[] { mail }, default);

            var loaded = Assert.Single(await store.LoadAsync(player, now, default));
            Assert.Equal(("Hediye", 50L, 1), (loaded.Title, loaded.Attachments.Amounts["Hadalite"], loaded.Attachments.Items["vip_100"]));
            Assert.Null(loaded.ClaimedAt);

            loaded.ClaimedAt = now;
            loaded.ReadAt = now;
            await store.UpdateAsync(new[] { loaded }, default);
            loaded.ClaimedAt = null; // a stale write must not clear the claim
            await store.UpdateAsync(new[] { loaded }, default);
            Assert.NotNull(Assert.Single(await store.LoadAsync(player, now, default)).ClaimedAt);

            Assert.Empty(await store.LoadAsync(player, now + MailRules.Lifetime, default));
            loaded.Deleted = true;
            await store.UpdateAsync(new[] { loaded }, default);
            Assert.Empty(await store.LoadAsync(player, now, default));
            Assert.True(await store.PurgeAsync(now, default) >= 1);
        }
    }
}
