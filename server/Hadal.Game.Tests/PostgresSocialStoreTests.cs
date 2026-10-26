using Hadal.Game.Social;
using Npgsql;

namespace Hadal.Game.Tests
{
    /// <summary>Alliances, members and chat against a real PostgreSQL (HADAL_TEST_PG); skipped otherwise.</summary>
    public sealed class PostgresSocialStoreTests
    {
        private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("HADAL_TEST_PG");

        [Fact]
        public async Task Alliances_members_and_chat_round_trip()
        {
            if (string.IsNullOrEmpty(ConnectionString))
                return; // not configured on this machine

            await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
            var store = new PostgresSocialStore(dataSource);
            await store.InitializeAsync(default);
            await store.InitializeAsync(default); // idempotent

            var now = DateTimeOffset.UtcNow;
            var suffix = Guid.NewGuid().ToString("N")[..6];
            var alliance = new AllianceRecord("a" + suffix, "Test " + suffix, suffix[..4].ToUpperInvariant(), "desc", "p1" + suffix, now);
            await store.SaveAllianceAsync(alliance, default);
            await store.SaveMemberAsync(new MemberRecord("p1" + suffix, alliance.Id, "One", 100, now), default);
            await store.SaveMemberAsync(new MemberRecord("p2" + suffix, alliance.Id, "Two", 50, now), default);
            await store.SaveMemberAsync(new MemberRecord("p2" + suffix, alliance.Id, "Two renamed", 60, now), default); // upsert
            await store.DeleteMemberAsync("p1" + suffix, default);

            var (alliances, members) = await store.LoadAlliancesAsync(default);
            Assert.Contains(alliances, a => a.Id == alliance.Id && a.Tag == alliance.Tag);
            var member = Assert.Single(members, m => m.AllianceId == alliance.Id);
            Assert.Equal(("Two renamed", 60L), (member.Name, member.Power));

            var channel = "alliance:" + alliance.Id;
            var baseId = now.ToUnixTimeMilliseconds() * 1000;
            for (var i = 0; i < 5; i++)
                await store.AddChatAsync(new ChatRecord(baseId + i, channel, "p2" + suffix, "Two", alliance.Tag, "line " + i, now, false), default);
            await store.DeleteChatAsync(baseId + 4, default);

            var recent = (await store.LoadRecentChatAsync(3, default)).Where(c => c.Channel == channel).ToList();
            Assert.Equal(new[] { "line 1", "line 2", "line 3" }, recent.Select(c => c.Text));

            // Deleting the alliance takes its members with it. Clean the lines too: this may run against a live database.
            for (var i = 0; i < 5; i++)
                await store.DeleteChatAsync(baseId + i, default);
            await store.DeleteAllianceAsync(alliance.Id, default);
            (alliances, members) = await store.LoadAlliancesAsync(default);
            Assert.DoesNotContain(alliances, a => a.Id == alliance.Id);
            Assert.DoesNotContain(members, m => m.AllianceId == alliance.Id);
        }
    }
}
