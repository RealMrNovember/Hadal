using Microsoft.Extensions.Logging;
using Hadal.Game.Events;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;

namespace Hadal.Game.World
{
    /// <summary>
    /// Event bosses on the map (MonsterKind.EventOnly; docs/29, research §6.2 "alliance boss"): a live event that lists
    /// bosses (EventDefinition.Bosses) puts each on its tile until the event ends. A boss is one shared body: every team
    /// may hit it at once, each hit takes the troops it killed off the body, and the team that brings it down gets the
    /// kill rewards. Damage is scored as <see cref="EventActions.BossDamage"/>, so an alliance contest on that action
    /// ranks alliances by their members' combined damage.
    /// </summary>
    public sealed partial class WorldService
    {
        /// <summary>The bosses of live events (EventService.LiveBosses); read without the event lock.</summary>
        public Func<IReadOnlyList<EventBossSpawn>>? EventBosses { get; set; }

        /// <summary>Each hit (or recalled hit, negative) with the body left: EventService.RecordBossHit stores it write-behind.</summary>
        public Action<string, int, string, long, long>? BossHit { get; set; }

        public static string BossId(string eventId, int index) => $"e:{eventId}:{index}";

        /// <summary>Where each boss on this map stands, readable without the world lock (the Lure Trap page shows it).</summary>
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, (int X, int Y)> _bossAt = new(StringComparer.Ordinal);

        public (int X, int Y)? BossPosition(string bossId) => _bossAt.TryGetValue(bossId, out var at) ? at : null;

        private readonly List<BossOutcome> _bossOutcomes = new();

        /// <summary>
        /// Brings the event bosses up to date and hands over those that died or whose event ended since the last call
        /// (GameWorld pays them through EventService.ClaimBossPrizes, which pays each boss once).
        /// </summary>
        public List<BossOutcome> TakeFinishedBosses(DateTimeOffset now)
        {
            lock (_gate)
            {
                SyncBosses(now);
                var done = _bossOutcomes.ToList();
                _bossOutcomes.Clear();
                return done;
            }
        }

        /// <summary>
        /// The viewer's damage board on a boss: the members of their alliance who hit it, most damage first (top 10, and
        /// the viewer's own line), as ranked for the payout. Without an alliance the viewer sees only their own line.
        /// Caller holds the lock (world, then hub).
        /// </summary>
        private WorldMonsterDto WithBoard(WorldMonsterDto dto, string viewer)
        {
            if (dto.HpMax <= 0 || viewer.Length == 0 || !_monsters.TryGetValue(dto.Id, out var boss))
                return dto;
            var alliance = _social.AllianceIdOf(viewer);
            var cache = new Dictionary<string, string>(StringComparer.Ordinal);
            string AllianceOf(string id) => cache.TryGetValue(id, out var a) ? a : cache[id] = _social.AllianceIdOf(id);
            var board = alliance.Length > 0
                ? EventBossRewards.AllianceBoard(boss.Damage, AllianceOf, alliance)
                : boss.Damage.TryGetValue(viewer, out var own) && own > 0 ? new List<(string, long, int)> { (viewer, own, 0) } : new List<(string, long, int)>();
            var mine = board.FirstOrDefault(e => e.Item1 == viewer);
            dto.MyDamage = boss.Damage.TryGetValue(viewer, out var d) ? d : 0;
            dto.MyRank = mine.Item1 == viewer ? mine.Item3 : 0;
            dto.Board = board.Where(e => e.Item3 <= 10 || e.Item1 == viewer).Select(e => new BossBoardEntryDto
            {
                PlayerId = e.Item1, Name = NameOf(e.Item1) ?? string.Empty, Damage = e.Item2, Rank = e.Item3
            }).ToArray();
            return dto;
        }

        /// <summary>Adds the bosses of events that went live and removes those of events that ended. Caller holds the lock.</summary>
        private void SyncBosses(DateTimeOffset now)
        {
            var wanted = EventBosses?.Invoke() ?? Array.Empty<EventBossSpawn>();
            var live = new HashSet<string>(StringComparer.Ordinal);
            foreach (var spawn in wanted)
            {
                // A Lure Trap Gulper waits for its trap's start.
                if (spawn.EndsAt <= now || spawn.StartsAt > now || MonsterRules.Kind(spawn.Kind) is not { EventOnly: true } kind)
                    continue;
                var id = BossId(spawn.EventId, spawn.Index);
                live.Add(id);
                if (_monsters.TryGetValue(id, out var known))
                {
                    known.EndsAt = spawn.EndsAt; // an extended event keeps its boss longer
                    known.AttackBonus = spawn.AttackBonus; // more bait, a stronger trap
                    continue;
                }

                var (atX, atY) = spawn.X >= 0 ? (spawn.X, spawn.Y) : RallyPointOf(spawn.AllianceId, spawn.Anchor) ?? (-1, -1);
                if (atX < 0 || FreeTileNear(atX, atY) is not { } tile)
                    continue;
                var level = Math.Clamp(spawn.Level, kind.MinLevel, kind.MaxLevel);
                var boss = new WorldMonster
                {
                    Id = id, Type = kind.Id, Level = level, X = tile.X, Y = tile.Y, EventId = spawn.EventId, EndsAt = spawn.EndsAt, BossIndex = spawn.Index,
                    AllianceId = spawn.AllianceId, AttackBonus = spawn.AttackBonus,
                    HpMax = MonsterRules.Count(kind, level), HpLeft = MonsterRules.Count(kind, level)
                };
                // Back after a restart: the body it had left and who hit it; a body already spent pays out at once.
                if (spawn.Left >= 0)
                {
                    boss.HpLeft = Math.Min(boss.HpMax, spawn.Left);
                    foreach (var pair in spawn.Damage ?? new Dictionary<string, long>())
                        boss.Damage[pair.Key] = pair.Value;
                    if (boss.HpLeft <= 0)
                        boss.DefeatedAt = now;
                }

                _monsters[id] = boss;
                _monsterAt[tile] = boss;
                _bossAt[id] = tile;
                _logger.LogInformation("Event boss {Kind} level {Level} is at {X}:{Y} until {EndsAt}.", kind.Id, level, tile.X, tile.Y, spawn.EndsAt);
            }

            // A boss brought down (its killer has arrived) or out of time pays its contributors once (EventBossRewards).
            foreach (var boss in _monsters.Values.Where(m => m.IsBoss && !m.Finished
                         && (Gone(m, now) || !live.Contains(m.Id) || m.EndsAt <= now)).ToList())
            {
                boss.Finished = true;
                var damage = boss.Damage.Where(p => p.Value > 0).ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
                var alliances = damage.Keys.ToDictionary(p => p, p => _social.AllianceIdOf(p), StringComparer.Ordinal);
                _bossOutcomes.Add(new BossOutcome(boss.EventId, boss.BossIndex, boss.Type, boss.Level, Gone(boss, now), boss.KillerId, damage, alliances));
            }

            foreach (var boss in _monsters.Values.Where(m => m.IsBoss && (!live.Contains(m.Id) || m.EndsAt <= now)).ToList())
            {
                _monsters.Remove(boss.Id);
                _bossAt.TryRemove(boss.Id, out _);
                if (_monsterAt.TryGetValue((boss.X, boss.Y), out var at) && at == boss)
                    _monsterAt.Remove((boss.X, boss.Y));
            }
        }

        /// <summary>An alliance's rally point (its hub, else <paramref name="anchor"/>'s base); null when neither is on this map.</summary>
        private (int X, int Y)? RallyPointOf(string allianceId, string anchor)
        {
            if (allianceId.Length > 0 && HubOf(allianceId) is { } hub)
                return (hub.X, hub.Y);
            return anchor.Length > 0 && _bases.TryGetValue(anchor, out var b) ? (b.X, b.Y) : null;
        }

        /// <summary>The asked tile, or the nearest free one within 6 tiles (a base or node may stand on it).</summary>
        private (int X, int Y)? FreeTileNear(int x, int y)
        {
            var size = Math.Max(10, Catalog.World.size);
            bool Free(int tx, int ty) => tx >= 0 && ty >= 0 && tx < size && ty < size && !_taken.Contains((tx, ty))
                                         && !_strongholdAt.ContainsKey((tx, ty)) && !_nodeAt.ContainsKey((tx, ty)) && !_monsterAt.ContainsKey((tx, ty));
            for (var r = 0; r <= 6; r++)
                for (var dx = -r; dx <= r; dx++)
                    for (var dy = -r; dy <= r; dy++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) == r && Free(x + dx, y + dy))
                            return (x + dx, y + dy);
                    }

            return null;
        }

        /// <summary>A team set out against a boss: its kills come off the shared body now (the battle is already
        /// resolved), and the body that reaches zero is gone when the killing team arrives. Caller holds the lock.</summary>
        private void HitBoss(WorldMonster boss, string playerId, MarchState march)
        {
            var damage = march.Battle?.EnemyLost.Values.Sum() ?? 0;
            boss.HpLeft = Math.Max(0, boss.HpLeft - damage);
            boss.Damage[playerId] = (boss.Damage.TryGetValue(playerId, out var had) ? had : 0) + damage;
            boss.Pending.RemoveAll(p => p.ArrivesAt <= march.StartedAt); // fought already: no longer recallable
            boss.Pending.Add((playerId, march.Id, damage, march.ArrivesAt));
            BossHit?.Invoke(boss.EventId, boss.BossIndex, playerId, damage, boss.HpLeft);
            if (boss.HpLeft <= 0 || march.Battle?.Won == true)
            {
                boss.HpLeft = 0;
                boss.KillerId = playerId;
                boss.DefeatedAt = march.ArrivesAt;
            }
        }

        /// <summary>A recalled hit fights nothing: its damage goes back on the body. Caller holds the lock.</summary>
        private void ReleaseBossHits(WorldMonster boss, string playerId)
        {
            var recalled = SessionOf?.Invoke(playerId)?.RecalledAttacks() ?? new HashSet<string>(StringComparer.Ordinal);
            foreach (var hit in boss.Pending.Where(p => p.PlayerId == playerId && recalled.Contains(p.MarchId)).ToList())
            {
                boss.Pending.Remove(hit);
                boss.HpLeft += hit.Damage;
                boss.Damage[playerId] = Math.Max(0, boss.Damage[playerId] - hit.Damage);
                BossHit?.Invoke(boss.EventId, boss.BossIndex, playerId, -hit.Damage, boss.HpLeft);
            }

            if (boss.HpLeft > 0)
            {
                boss.DefeatedAt = null;
                if (boss.KillerId == playerId)
                    boss.KillerId = string.Empty;
            }
        }

        /// <summary>A boss's damage by player, most first (tests, and later an in-game contribution board).</summary>
        internal IReadOnlyList<KeyValuePair<string, long>> BossDamage(string bossId)
        {
            lock (_gate)
                return _monsters.TryGetValue(bossId, out var boss)
                    ? boss.Damage.Where(p => p.Value > 0).OrderByDescending(p => p.Value).ToList()
                    : new List<KeyValuePair<string, long>>();
        }
    }
}
