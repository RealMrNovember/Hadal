using System.Text.Json;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Protocol;
using HADAL.Shared.Simulation;

namespace Hadal.Game.Social
{
    /// <summary>
    /// The alliance log and the battle log (docs/31 §2): the newest 200 lines of each, kept in memory and stored as
    /// alliance documents. Joins, leaves, kicks, rank and leader changes are logged with their chat notice; research
    /// logs itself; battles come from the PvP engine through <see cref="LogBattle"/>.
    /// </summary>
    public sealed partial class SocialHub
    {
        private sealed record LogLine(string Type, string[] Args, DateTimeOffset At);

        private readonly Dictionary<string, List<LogLine>> _logs = new(StringComparer.Ordinal);

        private static string LogKey(string allianceId, string kind) => $"a:{allianceId}:{kind}";

        /// <summary>
        /// Adds a line to an alliance's battle log. Safe to call from the world service (lock order world, then social).
        /// <paramref name="type"/> is an AllianceLogKinds battle type; <paramref name="args"/> are, by convention, the
        /// attacker's name, the target's name and the coordinates (x, y).
        /// </summary>
        public void LogBattle(string allianceId, string type, DateTimeOffset now, params string[] args)
        {
            if (string.IsNullOrEmpty(allianceId) || string.IsNullOrEmpty(type))
                return;
            lock (_gate)
                AppendLog(allianceId, AllianceLogKinds.Battle, type, args ?? Array.Empty<string>(), now);
        }

        /// <summary>A line of the alliance's own log ("research" from the research tree, …). Caller holds the lock.</summary>
        private void LogEvent(string allianceId, string type, string[] args, DateTimeOffset now) =>
            AppendLog(allianceId, AllianceLogKinds.Log, type, args, now);

        /// <summary>Chat notices ("sys.joined|Name") go to the alliance log as well. Caller holds the lock.</summary>
        private void LogNotice(Alliance alliance, string key, string argument, DateTimeOffset now)
        {
            var type = key.StartsWith("sys.", StringComparison.Ordinal) ? key.Substring(4) : key;
            LogEvent(alliance.Record.Id, type, argument.Split('|'), now);
        }

        private void AppendLog(string allianceId, string kind, string type, string[] args, DateTimeOffset now)
        {
            var key = LogKey(allianceId, kind);
            if (!_logs.TryGetValue(key, out var lines))
                _logs[key] = lines = new List<LogLine>();
            lines.Add(new LogLine(type, args, now));
            if (lines.Count > AllianceLogKinds.Keep)
                lines.RemoveRange(0, lines.Count - AllianceLogKinds.Keep);
            var json = JsonSerializer.Serialize(lines);
            Write(ct => _store.SaveDocAsync(key, json, ct));
        }

        private void LoadLogs(IEnumerable<(string Key, string Json)> docs)
        {
            foreach (var (key, json) in docs)
            {
                if (!key.EndsWith(":" + AllianceLogKinds.Log, StringComparison.Ordinal) && !key.EndsWith(":" + AllianceLogKinds.Battle, StringComparison.Ordinal))
                    continue;
                try
                {
                    _logs[key] = JsonSerializer.Deserialize<List<LogLine>>(json) ?? new List<LogLine>();
                }
                catch (JsonException)
                {
                    // A damaged log starts again rather than stopping the server.
                }
            }
        }

        private SocialRejectReason ViewLog(PlayerSession session, ViewAllianceLogCommand command, DateTimeOffset now)
        {
            var alliance = AllianceOf(session.PublicId);
            if (alliance == null)
                return SocialRejectReason.NotInAlliance;
            var kind = AllianceLogKinds.IsKind(command.Kind) ? command.Kind : AllianceLogKinds.Log;
            var lines = _logs.TryGetValue(LogKey(alliance.Record.Id, kind), out var found) ? found : new List<LogLine>();
            session.Push(new List<EntityChangeDto>
            {
                new()
                {
                    EntityId = EntityIds.AllianceLog + kind,
                    Kind = EntityChangeKind.Update,
                    Payload = _serializer.Serialize(new AllianceLogDto
                    {
                        Kind = kind,
                        Entries = Enumerable.Reverse(lines).Select(l => new AllianceLogEntryDto
                        {
                            Type = l.Type, Args = l.Args, AtMs = l.At.ToUnixTimeMilliseconds()
                        }).ToArray()
                    })
                }
            }, now);
            return SocialRejectReason.None;
        }

        /// <summary>Lines of one alliance log, oldest first (tests and the Overseer).</summary>
        internal List<(string Type, string[] Args, DateTimeOffset At)> LogOf(string allianceId, string kind)
        {
            lock (_gate)
                return _logs.TryGetValue(LogKey(allianceId, kind), out var lines)
                    ? lines.Select(l => (l.Type, l.Args, l.At)).ToList()
                    : new List<(string, string[], DateTimeOffset)>();
        }
    }
}
