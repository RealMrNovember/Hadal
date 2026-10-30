using Hadal.Game.Mailbox;
using HADAL.Shared.DTOs;
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
            var battle = new Mail
            {
                Id = Guid.NewGuid().ToString("N"), PlayerId = player, Kind = MailKinds.Battle, Title = "Rapor",
                Report = new PvpReportDto { Id = "b7", Kind = "defence", Outcome = "lost", Rounds = 2, Defender = new[] { new BattleStackDto { Type = "arc", Tier = 3, Start = 40, Lost = 5 } } },
                CreatedAt = now.AddSeconds(-1), ExpiresAt = now + MailRules.Lifetime
            };
            await store.InsertAsync(new[] { mail, battle }, default);
            var report = (await store.LoadAsync(player, now, default)).Single(m => m.Id == battle.Id).Report!;
            Assert.Equal(("b7", "defence", 2, 35L), (report.Id, report.Kind, report.Rounds, report.Defender[0].Start - report.Defender[0].Lost));
            battle.Deleted = true;
            await store.UpdateAsync(new[] { battle }, default);

            var loaded = Assert.Single(await store.LoadAsync(player, now, default));
            Assert.Null(loaded.Report);
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

        [Fact]
        public async Task Keyed_mails_are_stored_once_even_after_they_are_purged()
        {
            if (string.IsNullOrEmpty(ConnectionString))
                return; // not configured on this machine

            await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
            var store = new PostgresMailStore(dataSource);
            await store.InitializeAsync(default);
            var now = DateTimeOffset.UtcNow;
            var player = "keytest" + Guid.NewGuid().ToString("N")[..8];
            Mail Make() => new()
            {
                Id = Guid.NewGuid().ToString("N"), PlayerId = player, Kind = MailKinds.Event, Title = "loc:mail.event.rank.title|x", Body = "b",
                Attachments = new MailAttachments { Shards = { ["hero_a"] = 5 } }, CreatedAt = now, ExpiresAt = now + MailRules.Lifetime
            };
            var key = "ev:test:" + player + ":rank";

            var first = await store.InsertOnceAsync(new[] { (key, Make()), (key + "2", Make()) }, default);
            Assert.Equal(new[] { key, key + "2" }.OrderBy(k => k), first.OrderBy(k => k));
            Assert.Empty(await store.InsertOnceAsync(new[] { (key, Make()) }, default));
            var stored = await store.LoadAsync(player, now, default);
            Assert.Equal(2, stored.Count);
            Assert.Equal(5, stored[0].Attachments.Shards["hero_a"]);

            // Claimed, deleted and purged: the key still holds.
            foreach (var m in stored)
                m.Deleted = true;
            await store.UpdateAsync(stored, default);
            await store.PurgeAsync(now, default);
            Assert.Empty(await store.InsertOnceAsync(new[] { (key, Make()) }, default));
            Assert.Empty(await store.LoadAsync(player, now, default));
        }
    }
}
