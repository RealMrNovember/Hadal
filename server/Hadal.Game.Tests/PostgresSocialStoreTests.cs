using Hadal.Game.Social;
using Npgsql;

namespace Hadal.Game.Tests
{
    /// <summary>Alliances, members and chat against a real PostgreSQL (HADAL_TEST_PG); skipped otherwise.</summary>
    public sealed class PostgresSocialStoreTests
    {
        private static readonly string? ConnectionString = PostgresTest.ConnectionString;
        private static readonly string Emoji = " " + char.ConvertFromUtf32(0x1F642);

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
                await store.AddChatAsync(new ChatRecord(baseId + i, channel, "p2" + suffix, "Two", alliance.Tag, "line " + i + Emoji, now, false), default);
            await store.DeleteChatAsync(baseId + 4, default);

            var recent = (await store.LoadRecentChatAsync(3, default)).Where(c => c.Channel == channel).ToList();
            // Emoji (outside the basic plane) survive UTF-8 storage.
            Assert.Equal(new[] { "line 1" + Emoji, "line 2" + Emoji, "line 3" + Emoji }, recent.Select(c => c.Text));

            // Deleting the alliance takes its members with it. Clean the lines too: this may run against a live database.
            for (var i = 0; i < 5; i++)
                await store.DeleteChatAsync(baseId + i, default);
            await store.DeleteAllianceAsync(alliance.Id, default);
            (alliances, members) = await store.LoadAlliancesAsync(default);
            Assert.DoesNotContain(alliances, a => a.Id == alliance.Id);
            Assert.DoesNotContain(members, m => m.AllianceId == alliance.Id);
        }

        /// <summary>Alliance votes (docs/29 §5): saved, updated in place, deleted one by one or by alliance.</summary>
        [Fact]
        public async Task Alliance_polls_round_trip_and_go_with_their_alliance()
        {
            if (string.IsNullOrEmpty(ConnectionString))
                return; // not configured on this machine

            await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
            var store = new PostgresSocialStore(dataSource);
            await store.InitializeAsync(default);
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var now = DateTimeOffset.UtcNow;
            await store.SavePollAsync("v1" + suffix, "a" + suffix, true, now, "{\"id\":\"v1" + suffix + "\",\"ballots\":{}}", default);
            await store.SavePollAsync("v1" + suffix, "a" + suffix, false, now, "{\"id\":\"v1" + suffix + "\",\"ballots\":{\"p\":[1]}}", default); // upsert
            await store.SavePollAsync("v2" + suffix, "a" + suffix, true, now, "{\"id\":\"v2" + suffix + "\"}", default);
            await store.SavePollAsync("v3" + suffix, "b" + suffix, true, now, "{\"id\":\"v3" + suffix + "\"}", default);

            var mine = (await store.LoadPollsAsync(default)).Where(j => j.Contains(suffix, StringComparison.Ordinal)).ToList();
            Assert.Equal(3, mine.Count);
            Assert.Contains(mine, j => j.Contains("v1" + suffix, StringComparison.Ordinal) && j.Contains("[1]", StringComparison.Ordinal));

            await store.DeletePollAsync("v2" + suffix, default);
            await store.DeletePollsOfAllianceAsync("a" + suffix, default);
            mine = (await store.LoadPollsAsync(default)).Where(j => j.Contains(suffix, StringComparison.Ordinal)).ToList();
            Assert.Single(mine);
            await store.DeletePollsOfAllianceAsync("b" + suffix, default);
        }

        /// <summary>Disband clean-up (roadmap §1): one alliance's documents go, a longer id with the same start stays.</summary>
        [Fact]
        public async Task Deleting_an_alliances_documents_removes_only_that_alliance()
        {
            if (string.IsNullOrEmpty(ConnectionString))
                return; // not configured on this machine

            await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
            var store = new PostgresSocialStore(dataSource);
            await store.InitializeAsync(default);
            var id = "doctest" + Guid.NewGuid().ToString("N")[..8];
            var keys = new[] { $"a:{id}:log", $"a:{id}:research", $"a:{id}x:log", $"p:{id}:coins" };
            try
            {
                foreach (var key in keys)
                    await store.SaveDocAsync(key, "{\"k\":1}", default);
                await store.DeleteDocsAsync($"a:{id}:", default);
                var left = (await store.LoadDocsAsync(default)).Select(d => d.Key).Where(k => k.Contains(id, StringComparison.Ordinal)).OrderBy(k => k).ToList();
                Assert.Equal(new[] { $"a:{id}x:log", $"p:{id}:coins" }, left);
            }
            finally
            {
                foreach (var key in keys)
                    await store.DeleteDocsAsync(key, default);
            }
        }
    }
}
