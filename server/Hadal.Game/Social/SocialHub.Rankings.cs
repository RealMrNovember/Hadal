using System.Text.Json;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Protocol;
using HADAL.Shared.Simulation;

namespace Hadal.Game.Social
{
    /// <summary>
    /// The alliance's rankings (docs/31 step 7): power, troops defeated in PvP (counted while in the alliance, kept as
    /// the "a:&lt;id&gt;:kills" alliance document) and research donations. Every member is listed.
    /// </summary>
    public sealed partial class SocialHub
    {
        private readonly Dictionary<string, Dictionary<string, long>> _kills = new(StringComparer.Ordinal);

        private static string KillsKey(string allianceId) => $"a:{allianceId}:kills";

        /// <summary>
        /// Counts troops a player defeated in PvP for their alliance's kill ranking. Safe to call from the world
        /// (lock order world, then social); nothing happens outside an alliance.
        /// </summary>
        public void AddKills(string playerId, long kills, DateTimeOffset now)
        {
            if (kills <= 0 || string.IsNullOrEmpty(playerId))
                return;
            lock (_gate)
            {
                var alliance = AllianceOf(playerId);
                if (alliance == null)
                    return;
                if (!_kills.TryGetValue(alliance.Record.Id, out var table))
                    _kills[alliance.Record.Id] = table = new Dictionary<string, long>(StringComparer.Ordinal);
                table[playerId] = (table.TryGetValue(playerId, out var k) ? k : 0) + kills;
                var json = JsonSerializer.Serialize(table);
                var key = KillsKey(alliance.Record.Id);
                Write(ct => _store.SaveDocAsync(key, json, ct));
            }
        }

        private SocialRejectReason ViewRanking(PlayerSession session, ViewAllianceRankingCommand command, DateTimeOffset now)
        {
            var alliance = AllianceOf(session.PublicId);
            if (alliance == null)
                return SocialRejectReason.NotInAlliance;
            var kind = AllianceRankingKinds.IsKind(command.Kind) ? command.Kind : AllianceRankingKinds.Power;
            var kills = _kills.TryGetValue(alliance.Record.Id, out var k) ? k : new Dictionary<string, long>();
            var donated = TreeOf(alliance.Record.Id).Donated;
            var rows = alliance.Members.Select(m =>
            {
                var online = _online.TryGetValue(m.PlayerId, out var s);
                var value = kind switch
                {
                    AllianceRankingKinds.Kills => kills.TryGetValue(m.PlayerId, out var n) ? n : 0,
                    AllianceRankingKinds.Donations => donated.TryGetValue(m.PlayerId, out var d) ? d : 0,
                    _ => online ? s!.Power : m.Power
                };
                return (m.PlayerId, Name: online ? s!.DisplayName : m.Name, Value: value);
            }).OrderByDescending(r => r.Value).ThenBy(r => r.Name, StringComparer.Ordinal).ToList();

            session.Push(new List<EntityChangeDto>
            {
                new()
                {
                    EntityId = EntityIds.AllianceRanking + kind,
                    Kind = EntityChangeKind.Update,
                    Payload = _serializer.Serialize(new AllianceRankingDto
                    {
                        Kind = kind,
                        Entries = rows.Select((r, i) => new AllianceRankEntryDto
                        {
                            Rank = i + 1, PlayerId = r.PlayerId, Name = r.Name, AvatarId = AvatarOf(r.PlayerId), Value = r.Value
                        }).ToArray()
                    })
                }
            }, now);
            return SocialRejectReason.None;
        }

        private void LoadKills(IEnumerable<(string Key, string Json)> docs)
        {
            foreach (var (key, json) in docs)
            {
                if (!key.StartsWith("a:", StringComparison.Ordinal) || !key.EndsWith(":kills", StringComparison.Ordinal))
                    continue;
                try
                {
                    _kills[key.Substring(2, key.Length - 2 - ":kills".Length)] =
                        JsonSerializer.Deserialize<Dictionary<string, long>>(json) ?? new Dictionary<string, long>();
                }
                catch (JsonException)
                {
                }
            }
        }
    }
}
