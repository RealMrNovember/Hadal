using System.Collections.Concurrent;
using Npgsql;

namespace Hadal.Game.Social
{
    /// <summary>
    /// Alliance polls and time-slot votes (docs/29 §5): one row per vote with the whole vote (ballots included) as JSON.
    /// A disband deletes its alliance's rows; rows of alliances that no longer exist are dropped at start-up.
    /// </summary>
    public sealed partial class InMemorySocialStore
    {
        private readonly ConcurrentDictionary<string, (string AllianceId, string Json)> _polls = new(StringComparer.Ordinal);

        public Task<List<string>> LoadPollsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_polls.Values.Select(p => p.Json).ToList());

        public Task SavePollAsync(string pollId, string allianceId, bool open, DateTimeOffset createdAt, string json, CancellationToken cancellationToken)
        {
            _polls[pollId] = (allianceId, json);
            return Task.CompletedTask;
        }

        public Task DeletePollAsync(string pollId, CancellationToken cancellationToken)
        {
            _polls.TryRemove(pollId, out _);
            return Task.CompletedTask;
        }

        public Task DeletePollsOfAllianceAsync(string allianceId, CancellationToken cancellationToken)
        {
            foreach (var id in _polls.Where(p => p.Value.AllianceId == allianceId).Select(p => p.Key).ToList())
                _polls.TryRemove(id, out _);
            return Task.CompletedTask;
        }
    }

    public sealed partial class PostgresSocialStore
    {
        private const string PollsSchema = @"

CREATE TABLE IF NOT EXISTS hadal.alliance_polls (
    poll_id      text        PRIMARY KEY,
    alliance_id  text        NOT NULL,
    is_open      boolean     NOT NULL,
    data         jsonb       NOT NULL,
    created_at   timestamptz NOT NULL,
    updated_at   timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS alliance_polls_alliance ON hadal.alliance_polls (alliance_id, created_at DESC);";

        public async Task<List<string>> LoadPollsAsync(CancellationToken cancellationToken)
        {
            var list = new List<string>();
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand("SELECT data::text FROM hadal.alliance_polls ORDER BY created_at", connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                list.Add(reader.GetString(0));
            return list;
        }

        public Task SavePollAsync(string pollId, string allianceId, bool open, DateTimeOffset createdAt, string json, CancellationToken cancellationToken) => ExecuteAsync(@"
INSERT INTO hadal.alliance_polls (poll_id, alliance_id, is_open, data, created_at, updated_at) VALUES (@id, @a, @o, @d::jsonb, @c, now())
ON CONFLICT (poll_id) DO UPDATE SET is_open = EXCLUDED.is_open, data = EXCLUDED.data, updated_at = now()", cancellationToken,
            ("id", pollId), ("a", allianceId), ("o", open), ("d", json), ("c", createdAt));

        public Task DeletePollAsync(string pollId, CancellationToken cancellationToken) =>
            ExecuteAsync("DELETE FROM hadal.alliance_polls WHERE poll_id = @id", cancellationToken, ("id", pollId));

        public Task DeletePollsOfAllianceAsync(string allianceId, CancellationToken cancellationToken) =>
            ExecuteAsync("DELETE FROM hadal.alliance_polls WHERE alliance_id = @a", cancellationToken, ("a", allianceId));
    }
}
