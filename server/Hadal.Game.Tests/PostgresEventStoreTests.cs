using Hadal.Game.Events;
using HADAL.Shared.Simulation;
using Npgsql;

namespace Hadal.Game.Tests
{
    /// <summary>Event definitions, scores and alliance totals against a real PostgreSQL (HADAL_TEST_PG); skipped otherwise.</summary>
    public sealed class PostgresEventStoreTests
    {
        private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("HADAL_TEST_PG");

        [Fact]
        public async Task Events_scores_and_alliance_totals_round_trip()
        {
            if (string.IsNullOrEmpty(ConnectionString))
                return; // not configured on this machine

            await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
            var store = new PostgresEventStore(dataSource);
            await store.InitializeAsync(default);
            await store.InitializeAsync(default); // idempotent

            var now = DateTimeOffset.UtcNow;
            var id = "evtest" + Guid.NewGuid().ToString("N")[..8];
            var definition = new EventDefinition
            {
                Id = id, Template = EventTemplates.Points, Title = { ["tr"] = "Deneme" }, StartsAt = now, EndsAt = now.AddDays(1),
                Actions = { new EventActionConfig { Action = EventActions.Gather, Points = 1, Per = 1000 } }
            };
            await store.SaveEventAsync(definition, default);
            definition.Title["en"] = "Trial";
            await store.SaveEventAsync(definition, default); // upsert

            var score = new EventScore { EventId = id, PlayerId = "p1", AllianceId = "a1", Points = 42, UpdatedAt = now, LoginDays = 2, Claimed = { 0, 3 } };
            score.Carry["gather"] = 250;
            await store.SaveScoresAsync(new[] { score }, default);
            score.Points = 43;
            await store.SaveScoresAsync(new[] { score }, default);
            await store.SaveAlliancePointsAsync(new[] { (id, "a1", 99L) }, default);

            var loaded = Assert.Single(await store.LoadEventsAsync(default), e => e.Id == id);
            Assert.Equal("Trial", loaded.Title["en"]);
            Assert.Equal(1000, Assert.Single(loaded.Actions).Per);

            var (scores, alliances) = await store.LoadProgressAsync(new[] { id }, default);
            var back = Assert.Single(scores);
            Assert.Equal((43L, "a1", 2), (back.Points, back.AllianceId, back.LoginDays));
            Assert.Equal(new[] { 0, 3 }, back.Claimed.OrderBy(i => i));
            Assert.Equal(250, back.Carry["gather"]);
            Assert.Equal(99, Assert.Single(alliances).Points);

            // Deleting the event takes its scores with it (this may run against a live database).
            await store.DeleteEventAsync(id, default);
            Assert.Empty((await store.LoadProgressAsync(new[] { id }, default)).Scores);
        }
    }
}
