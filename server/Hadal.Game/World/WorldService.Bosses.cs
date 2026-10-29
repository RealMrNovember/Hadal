using Microsoft.Extensions.Logging;
using Hadal.Game.Events;
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

        public static string BossId(string eventId, int index) => $"e:{eventId}:{index}";

        /// <summary>Adds the bosses of events that went live and removes those of events that ended. Caller holds the lock.</summary>
        private void SyncBosses(DateTimeOffset now)
        {
            var wanted = EventBosses?.Invoke() ?? Array.Empty<EventBossSpawn>();
            var live = new HashSet<string>(StringComparer.Ordinal);
            foreach (var spawn in wanted)
            {
                if (spawn.EndsAt <= now || MonsterRules.Kind(spawn.Kind) is not { EventOnly: true } kind)
                    continue;
                var id = BossId(spawn.EventId, spawn.Index);
                live.Add(id);
                if (_monsters.TryGetValue(id, out var known))
                {
                    known.EndsAt = spawn.EndsAt; // an extended event keeps its boss longer
                    continue;
                }

                if (FreeTileNear(spawn.X, spawn.Y) is not { } tile)
                    continue;
                var level = Math.Clamp(spawn.Level, kind.MinLevel, kind.MaxLevel);
                var boss = new WorldMonster
                {
                    Id = id, Type = kind.Id, Level = level, X = tile.X, Y = tile.Y, EventId = spawn.EventId, EndsAt = spawn.EndsAt,
                    HpMax = MonsterRules.Count(kind, level), HpLeft = MonsterRules.Count(kind, level)
                };
                _monsters[id] = boss;
                _monsterAt[tile] = boss;
                _logger.LogInformation("Event boss {Kind} level {Level} is at {X}:{Y} until {EndsAt}.", kind.Id, level, tile.X, tile.Y, spawn.EndsAt);
            }

            foreach (var boss in _monsters.Values.Where(m => m.IsBoss && (!live.Contains(m.Id) || m.EndsAt <= now)).ToList())
            {
                _monsters.Remove(boss.Id);
                if (_monsterAt.TryGetValue((boss.X, boss.Y), out var at) && at == boss)
                    _monsterAt.Remove((boss.X, boss.Y));
            }
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
            if (boss.HpLeft <= 0 || march.Battle?.Won == true)
            {
                boss.HpLeft = 0;
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
            }

            if (boss.HpLeft > 0)
                boss.DefeatedAt = null;
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
