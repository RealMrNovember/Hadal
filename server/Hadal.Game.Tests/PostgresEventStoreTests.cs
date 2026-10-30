using Hadal.Game.Events;
using HADAL.Shared.Simulation;
using Npgsql;

namespace Hadal.Game.Tests
{
    /// <summary>Event definitions, scores and alliance totals against a real PostgreSQL (HADAL_TEST_PG); skipped otherwise.</summary>
    public sealed class PostgresEventStoreTests
    {
        private static readonly string? ConnectionString = PostgresTest.ConnectionString;

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

            // An alliance's Mobilization pool and duel day points, and a player's held task and day points.
            var state = new EventAllianceState { Pool = { new MobilizationCard { Kind = "gather", Tier = 2, Target = 300_000, Points = 200 }, null } };
            state.Draws.AddRange(new[] { 1, 2 });
            state.ReadyAt.AddRange(new[] { now, now.AddMinutes(5) });
            state.DayPoints[3] = 777;
            await store.SaveAllianceStatesAsync(new[] { (id, "a1", state.ToJson()) }, default);
            state.DayPoints[4] = 5;
            await store.SaveAllianceStatesAsync(new[] { (id, "a1", state.ToJson()) }, default); // upsert
            var back2 = EventAllianceState.FromJson(Assert.Single(await store.LoadAllianceStatesAsync(new[] { id }, default)).Json);
            Assert.Equal(("gather", 2, 777L, 5L), (back2.Pool[0]!.Kind, back2.Pool[0]!.Tier, back2.DayPoints[3], back2.DayPoints[4]));
            Assert.Null(back2.Pool[1]);
            score.Task = new MobilizationCard { Kind = "train", Tier = 4, Target = 300, Points = 560 };
            score.TaskProgress = 12;
            score.DayPoints[1] = 44;
            await store.SaveScoresAsync(new[] { score }, default);
            var held = Assert.Single((await store.LoadProgressAsync(new[] { id }, default)).Scores);
            Assert.Equal(("train", 12L, 44L), (held.Task!.Kind, held.TaskProgress, held.DayPoints[1]));

            // Deleting the event takes its scores and states with it (this may run against a live database).
            await store.DeleteEventAsync(id, default);
            Assert.Empty((await store.LoadProgressAsync(new[] { id }, default)).Scores);
            Assert.Empty(await store.LoadAllianceStatesAsync(new[] { id }, default));
        }

        [Fact]
        public async Task Schedules_and_the_occurrence_ledger_round_trip()
        {
            if (string.IsNullOrEmpty(ConnectionString))
                return; // not configured on this machine

            await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
            var store = new PostgresEventStore(dataSource);
            await store.InitializeAsync(default);

            var now = DateTimeOffset.UtcNow;
            var schedule = DefaultSchedules.All(now)[0];
            schedule.Id = "test-" + Guid.NewGuid().ToString("N")[..8];
            await store.SaveScheduleAsync(schedule, default);
            schedule.Enabled = false;
            await store.SaveScheduleAsync(schedule, default); // upsert
            var loaded = Assert.Single(await store.LoadSchedulesAsync(default), s => s.Id == schedule.Id);
            Assert.False(loaded.Enabled);
            Assert.Equal(schedule.Blueprint.Phases.Count, loaded.Blueprint.Phases.Count);
            Assert.Equal("Derinlik Yarışı", loaded.Blueprint.Title["tr"]);

            // The ledger keeps an id once; old entries are not loaded, markers always are.
            var recent = schedule.Id + ":" + now.ToString("yyyy-MM-dd") + ":w1";
            var old = schedule.Id + ":2020-01-01:w1";
            var marker = "seed:" + schedule.Id;
            await store.SaveOccurrencesAsync(new[] { (recent, schedule.Id, now), (old, schedule.Id, now.AddYears(-6)), (marker, string.Empty, now.AddYears(-6)) }, default);
            await store.SaveOccurrencesAsync(new[] { (recent, schedule.Id, now) }, default); // a second write changes nothing
            var ledger = await store.LoadOccurrencesAsync(now.AddDays(-14), default);
            Assert.Contains(recent, ledger);
            Assert.Contains(marker, ledger);
            Assert.DoesNotContain(old, ledger);

            await store.DeleteScheduleAsync(schedule.Id, default);
            Assert.DoesNotContain(await store.LoadSchedulesAsync(default), s => s.Id == schedule.Id);
        }
    }
}
