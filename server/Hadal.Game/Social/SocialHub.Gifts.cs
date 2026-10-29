using System.Text.Json;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Protocol;
using HADAL.Shared.Simulation;

namespace Hadal.Game.Social
{
    /// <summary>
    /// Alliance gifts (docs/31 step 5, AllianceGiftRules): a member's monster win, Hadalite purchase or event rank drops a
    /// gift for every member of the moment; each collects it once for a chest (by the alliance's gift level), alliance
    /// coins and gift points. Kept per alliance as the "a:&lt;id&gt;:gifts" alliance document.
    /// </summary>
    public sealed partial class SocialHub
    {
        private sealed class Gift
        {
            public string Id { get; set; } = string.Empty;
            public string Source { get; set; } = string.Empty;
            public string By { get; set; } = string.Empty;
            public long Arg { get; set; }
            public DateTimeOffset ExpiresAt { get; set; }
            public List<string> Members { get; set; } = new();
            public List<string> Collected { get; set; } = new();
        }

        private sealed class GiftPool
        {
            public long Points { get; set; }
            public List<Gift> Gifts { get; set; } = new();
        }

        private readonly Dictionary<string, GiftPool> _gifts = new(StringComparer.Ordinal);

        private static string GiftKey(string allianceId) => $"a:{allianceId}:gifts";

        private GiftPool PoolOf(string allianceId) =>
            _gifts.TryGetValue(allianceId, out var pool) ? pool : _gifts[allianceId] = new GiftPool();

        private void SavePool(string allianceId, GiftPool pool)
        {
            var json = JsonSerializer.Serialize(pool);
            Write(ct => _store.SaveDocAsync(GiftKey(allianceId), json, ct));
        }

        /// <summary>
        /// A gift for the player's alliance (AllianceGiftRules source; <paramref name="arg"/> the monster's level or the
        /// Hadalite bought). Nothing happens outside an alliance. Safe from the world, events and the game loop.
        /// </summary>
        public void DropGift(string playerId, string source, long arg, DateTimeOffset now)
        {
            if (!AllianceGiftRules.IsSource(source))
                return;
            lock (_gate)
            {
                var alliance = AllianceOf(playerId);
                if (alliance == null)
                    return;
                var pool = PoolOf(alliance.Record.Id);
                pool.Gifts.RemoveAll(g => g.ExpiresAt <= now);
                pool.Gifts.Add(new Gift
                {
                    Id = "g" + Guid.NewGuid().ToString("N"),
                    Source = source,
                    By = NameOf(playerId) is { Length: > 0 } name ? name : playerId,
                    Arg = arg,
                    ExpiresAt = now + AllianceGiftRules.Lifetime,
                    Members = alliance.Members.Select(m => m.PlayerId).ToList()
                });
                if (pool.Gifts.Count > AllianceGiftRules.Keep)
                    pool.Gifts.RemoveRange(0, pool.Gifts.Count - AllianceGiftRules.Keep);
                SavePool(alliance.Record.Id, pool);
                PushGifts(alliance, now);
            }
        }

        /// <summary>Gifts from what a session did since the last tick: one per monster defeated (its level).</summary>
        public void GiftsFrom(PlayerSession session, List<(string Action, long Amount)> actions, DateTimeOffset now)
        {
            foreach (var (action, amount) in actions)
            {
                if (action == EventActions.MonsterLevels)
                    DropGift(session.PublicId, AllianceGiftRules.Monster, amount, now);
            }
        }

        private SocialRejectReason Collect(PlayerSession session, CollectAllianceGiftCommand command, DateTimeOffset now)
        {
            var alliance = AllianceOf(session.PublicId);
            if (alliance == null)
                return SocialRejectReason.NotInAlliance;
            var pool = PoolOf(alliance.Record.Id);
            var waiting = pool.Gifts.Where(g => g.ExpiresAt > now && g.Members.Contains(session.PublicId) && !g.Collected.Contains(session.PublicId)
                                                && (string.IsNullOrEmpty(command.GiftId) || g.Id == command.GiftId)).ToList();
            if (waiting.Count == 0)
                return SocialRejectReason.NothingToCollect;

            // Each chest is the one of the level the alliance had when it was opened.
            var chests = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var gift in waiting)
            {
                var chest = AllianceGiftRules.Chest(AllianceGiftRules.Level(pool.Points));
                chests[chest] = (chests.TryGetValue(chest, out var n) ? n : 0) + 1;
                gift.Collected.Add(session.PublicId);
                pool.Points += AllianceGiftRules.Points(gift.Source, gift.Arg);
            }

            SavePool(alliance.Record.Id, pool);
            AddCoins(session.PublicId, AllianceGiftRules.CoinsPerGift * waiting.Count, now);
            PushGifts(alliance, now);
            session.GrantGiftChests(command.CommandId, command.ClientSequence, chests, now);
            return SocialRejectReason.None;
        }

        private void PushGifts(Alliance alliance, DateTimeOffset now)
        {
            foreach (var member in alliance.Members)
            {
                if (_online.TryGetValue(member.PlayerId, out var session))
                    session.Push(new List<EntityChangeDto> { GiftsChange(member.PlayerId, now) }, now);
            }
        }

        private EntityChangeDto GiftsChange(string playerId, DateTimeOffset now)
        {
            var alliance = AllianceOf(playerId);
            var pool = alliance == null ? new GiftPool() : PoolOf(alliance.Record.Id);
            var level = AllianceGiftRules.Level(pool.Points);
            return new EntityChangeDto
            {
                EntityId = EntityIds.AllianceGifts,
                Kind = EntityChangeKind.Update,
                Payload = _serializer.Serialize(new AllianceGiftsDto
                {
                    Level = level,
                    Points = pool.Points,
                    NextPoints = level < AllianceGiftRules.MaxLevel ? AllianceGiftRules.LevelPoints[level + 1] : 0,
                    Chest = AllianceGiftRules.Chest(level),
                    Gifts = Enumerable.Reverse(pool.Gifts).Where(g => g.ExpiresAt > now && g.Members.Contains(playerId)).Select(g => new AllianceGiftDto
                    {
                        Id = g.Id, Source = g.Source, ByName = g.By, Arg = g.Arg, ExpiresAtMs = g.ExpiresAt.ToUnixTimeMilliseconds(),
                        Points = AllianceGiftRules.Points(g.Source, g.Arg), Collected = g.Collected.Contains(playerId)
                    }).ToArray()
                })
            };
        }

        private void LoadGifts(IEnumerable<(string Key, string Json)> docs)
        {
            foreach (var (key, json) in docs)
            {
                if (!key.StartsWith("a:", StringComparison.Ordinal) || !key.EndsWith(":gifts", StringComparison.Ordinal))
                    continue;
                try
                {
                    _gifts[key.Substring(2, key.Length - 2 - ":gifts".Length)] = JsonSerializer.Deserialize<GiftPool>(json) ?? new GiftPool();
                }
                catch (JsonException)
                {
                }
            }
        }

        /// <summary>An alliance's gift points (tests and the Overseer).</summary>
        internal long GiftPointsOf(string allianceId)
        {
            lock (_gate)
                return PoolOf(allianceId).Points;
        }
    }
}
