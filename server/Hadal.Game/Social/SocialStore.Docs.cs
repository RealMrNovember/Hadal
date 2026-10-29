using System.Collections.Concurrent;
using Npgsql;

namespace Hadal.Game.Social
{
    /// <summary>
    /// Alliance documents (docs/31): logs, coins, shop stock, gifts, research — each one JSON document under a key such
    /// as "a:&lt;allianceId&gt;:log" or "p:&lt;playerId&gt;:coins", so new alliance features need no migration.
    /// </summary>
    public sealed partial class InMemorySocialStore
    {
        private readonly ConcurrentDictionary<string, string> _docs = new(StringComparer.Ordinal);

        public Task<List<(string Key, string Json)>> LoadDocsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_docs.Select(p => (p.Key, p.Value)).ToList());

        public Task SaveDocAsync(string key, string json, CancellationToken cancellationToken)
        {
            _docs[key] = json;
            return Task.CompletedTask;
        }

        public Task DeleteDocsAsync(string prefix, CancellationToken cancellationToken)
        {
            foreach (var key in _docs.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList())
                _docs.TryRemove(key, out _);
            return Task.CompletedTask;
        }
    }

    public sealed partial class PostgresSocialStore
    {
        private const string DocsSchema = @"

CREATE TABLE IF NOT EXISTS hadal.alliance_docs (
    doc_key     text        PRIMARY KEY,
    data        jsonb       NOT NULL,
    updated_at  timestamptz NOT NULL DEFAULT now()
);";

        public async Task<List<(string Key, string Json)>> LoadDocsAsync(CancellationToken cancellationToken)
        {
            var list = new List<(string, string)>();
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand("SELECT doc_key, data::text FROM hadal.alliance_docs", connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                list.Add((reader.GetString(0), reader.GetString(1)));
            return list;
        }

        public Task SaveDocAsync(string key, string json, CancellationToken cancellationToken) => ExecuteAsync(@"
INSERT INTO hadal.alliance_docs (doc_key, data, updated_at) VALUES (@k, @d::jsonb, now())
ON CONFLICT (doc_key) DO UPDATE SET data = EXCLUDED.data, updated_at = now()", cancellationToken, ("k", key), ("d", json));

        public Task DeleteDocsAsync(string prefix, CancellationToken cancellationToken) =>
            ExecuteAsync("DELETE FROM hadal.alliance_docs WHERE starts_with(doc_key, @p)", cancellationToken, ("p", prefix));
    }
}
