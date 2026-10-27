using Hadal.Game.Persistence;
using Npgsql;

namespace Hadal.Game.Social
{
    /// <summary>Alliances, members and chat in schema <c>hadal</c> (docs/25).</summary>
    public sealed class PostgresSocialStore : ISocialStore
    {
        private const string Schema = @"
CREATE SCHEMA IF NOT EXISTS hadal;

CREATE TABLE IF NOT EXISTS hadal.alliances (
    alliance_id  text        PRIMARY KEY,
    name         text        NOT NULL,
    tag          text        NOT NULL,
    description  text        NOT NULL DEFAULT '',
    leader_id    text        NOT NULL,
    created_at   timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX IF NOT EXISTS alliances_name_key ON hadal.alliances (lower(name));
CREATE UNIQUE INDEX IF NOT EXISTS alliances_tag_key ON hadal.alliances (upper(tag));

CREATE TABLE IF NOT EXISTS hadal.alliance_members (
    player_id    text        PRIMARY KEY,
    alliance_id  text        NOT NULL REFERENCES hadal.alliances (alliance_id) ON DELETE CASCADE,
    name         text        NOT NULL,
    power        bigint      NOT NULL DEFAULT 0,
    joined_at    timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS hadal.chat_messages (
    message_id   bigint      PRIMARY KEY,
    channel      text        NOT NULL,
    sender_id    text        NOT NULL,
    sender_name  text        NOT NULL,
    alliance_tag text        NOT NULL DEFAULT '',
    body         text        NOT NULL,
    sent_at      timestamptz NOT NULL,
    is_system    boolean     NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS chat_messages_channel ON hadal.chat_messages (channel, message_id DESC);

ALTER TABLE hadal.alliances ADD COLUMN IF NOT EXISTS join_mode integer NOT NULL DEFAULT 0;
ALTER TABLE hadal.alliance_members ADD COLUMN IF NOT EXISTS rank integer NOT NULL DEFAULT 0;
ALTER TABLE hadal.alliance_members ADD COLUMN IF NOT EXISTS role text NOT NULL DEFAULT '';";

        private readonly NpgsqlDataSource _dataSource;

        public PostgresSocialStore(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public Task InitializeAsync(CancellationToken cancellationToken) =>
            PostgresSchema.ApplyAsync(_dataSource, Schema, cancellationToken);

        public async Task<(List<AllianceRecord> Alliances, List<MemberRecord> Members)> LoadAlliancesAsync(CancellationToken cancellationToken)
        {
            var alliances = new List<AllianceRecord>();
            var members = new List<MemberRecord>();
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

            await using (var command = new NpgsqlCommand("SELECT alliance_id, name, tag, description, leader_id, created_at, join_mode FROM hadal.alliances", connection))
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                    alliances.Add(new AllianceRecord(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                        reader.GetString(4), reader.GetFieldValue<DateTimeOffset>(5), reader.GetInt32(6)));
            }

            await using (var command = new NpgsqlCommand("SELECT player_id, alliance_id, name, power, joined_at, rank, role FROM hadal.alliance_members", connection))
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                    members.Add(new MemberRecord(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetInt64(3),
                        reader.GetFieldValue<DateTimeOffset>(4), reader.GetInt32(5), reader.GetString(6)));
            }

            return (alliances, members);
        }

        public async Task<List<ChatRecord>> LoadRecentChatAsync(int perChannel, CancellationToken cancellationToken)
        {
            var result = new List<ChatRecord>();
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(@"
SELECT message_id, channel, sender_id, sender_name, alliance_tag, body, sent_at, is_system FROM (
    SELECT *, row_number() OVER (PARTITION BY channel ORDER BY message_id DESC) AS n FROM hadal.chat_messages
) recent WHERE n <= @n ORDER BY message_id", connection);
            command.Parameters.AddWithValue("n", perChannel);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                result.Add(new ChatRecord(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                    reader.GetString(4), reader.GetString(5), reader.GetFieldValue<DateTimeOffset>(6), reader.GetBoolean(7)));
            return result;
        }

        public Task SaveAllianceAsync(AllianceRecord a, CancellationToken cancellationToken) => ExecuteAsync(@"
INSERT INTO hadal.alliances (alliance_id, name, tag, description, leader_id, created_at, join_mode) VALUES (@id, @name, @tag, @d, @leader, @at, @mode)
ON CONFLICT (alliance_id) DO UPDATE SET name = EXCLUDED.name, tag = EXCLUDED.tag, description = EXCLUDED.description, leader_id = EXCLUDED.leader_id,
    join_mode = EXCLUDED.join_mode",
            cancellationToken, ("id", a.Id), ("name", a.Name), ("tag", a.Tag), ("d", a.Description), ("leader", a.LeaderId), ("at", a.CreatedAt),
            ("mode", a.JoinMode));

        public Task DeleteAllianceAsync(string allianceId, CancellationToken cancellationToken) =>
            ExecuteAsync("DELETE FROM hadal.alliances WHERE alliance_id = @id", cancellationToken, ("id", allianceId));

        public Task SaveMemberAsync(MemberRecord m, CancellationToken cancellationToken) => ExecuteAsync(@"
INSERT INTO hadal.alliance_members (player_id, alliance_id, name, power, joined_at, rank, role) VALUES (@p, @a, @name, @power, @at, @rank, @role)
ON CONFLICT (player_id) DO UPDATE SET alliance_id = EXCLUDED.alliance_id, name = EXCLUDED.name, power = EXCLUDED.power, joined_at = EXCLUDED.joined_at,
    rank = EXCLUDED.rank, role = EXCLUDED.role",
            cancellationToken, ("p", m.PlayerId), ("a", m.AllianceId), ("name", m.Name), ("power", m.Power), ("at", m.JoinedAt), ("rank", m.Rank), ("role", m.Role));

        public Task DeleteMemberAsync(string playerId, CancellationToken cancellationToken) =>
            ExecuteAsync("DELETE FROM hadal.alliance_members WHERE player_id = @p", cancellationToken, ("p", playerId));

        public Task AddChatAsync(ChatRecord c, CancellationToken cancellationToken) => ExecuteAsync(@"
INSERT INTO hadal.chat_messages (message_id, channel, sender_id, sender_name, alliance_tag, body, sent_at, is_system)
VALUES (@id, @ch, @s, @name, @tag, @body, @at, @sys) ON CONFLICT (message_id) DO NOTHING",
            cancellationToken, ("id", c.Id), ("ch", c.Channel), ("s", c.SenderId), ("name", c.SenderName), ("tag", c.AllianceTag),
            ("body", c.Text), ("at", c.SentAt), ("sys", c.IsSystem));

        public Task DeleteChatAsync(long messageId, CancellationToken cancellationToken) =>
            ExecuteAsync("DELETE FROM hadal.chat_messages WHERE message_id = @id", cancellationToken, ("id", messageId));

        private async Task ExecuteAsync(string sql, CancellationToken cancellationToken, params (string Name, object Value)[] parameters)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            foreach (var (name, value) in parameters)
                command.Parameters.AddWithValue(name, value);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
