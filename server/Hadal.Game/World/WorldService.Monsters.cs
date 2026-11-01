using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;

namespace Hadal.Game.World
{
    /// <summary>A monster swarm on the map (docs/rehber 09 §5.2). Kept in memory and laid out again on start.</summary>
    public sealed class WorldMonster
    {
        public string Id { get; init; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Level { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        /// <summary>A team is on its way until then.</summary>
        public DateTimeOffset? EngagedUntil { get; set; }
        public string EngagedBy { get; set; } = string.Empty;

        /// <summary>Beaten at this time: gone, and back elsewhere a while later.</summary>
        public DateTimeOffset? DefeatedAt { get; set; }

        /// <summary>An event boss (MonsterKind.EventOnly): the event that holds it and when it leaves (WorldService.Bosses).</summary>
        public string EventId { get; set; } = string.Empty;
        public DateTimeOffset EndsAt { get; set; }

        /// <summary>A boss's shared body: what is left of it and what it started with (MonsterRules.Count).</summary>
        public long HpLeft { get; set; }
        public long HpMax { get; set; }

        /// <summary>A boss's damage by player, and the hits still on their way (taken back on a recall).</summary>
        public Dictionary<string, long> Damage { get; } = new(StringComparer.Ordinal);
        public List<(string PlayerId, string MarchId, long Damage, DateTimeOffset ArrivesAt)> Pending { get; } = new();

        public bool IsBoss => EventId.Length > 0;

        /// <summary>A Lure Trap Gulper: the only alliance that sees and hits it, and its trap's extra damage on every hit.</summary>
        public string AllianceId { get; set; } = string.Empty;
        public double AttackBonus { get; set; }

        /// <summary>A boss's place in its event's list, who brought it down, and whether its outcome went to the event.</summary>
        public int BossIndex { get; set; }
        public string KillerId { get; set; } = string.Empty;
        public bool Finished { get; set; }
    }

    public sealed partial class WorldService
    {
        private readonly Dictionary<string, WorldMonster> _monsters = new(StringComparer.Ordinal);
        private readonly Dictionary<(int, int), WorldMonster> _monsterAt = new();
        private readonly Random _monsterRandom = new(11);
        private int _nextMonsterId;

        /// <summary>A beaten swarm comes back elsewhere in its belt after this long.</summary>
        private static readonly TimeSpan MonsterRespawn = TimeSpan.FromMinutes(10);

        /// <summary>Lays the swarms out by belt (Per100Tiles of each belt's tiles). Caller holds the lock.</summary>
        private void SeedMonsters()
        {
            if (_monsters.Count > 0)
                return;
            var world = Catalog.World;
            var size = Math.Max(10, world.size);
            foreach (var belt in world.belts)
            {
                var tiles = 0;
                for (var x = 0; x < size; x++)
                    for (var y = 0; y < size; y++)
                        if (GatherRules.BeltOf(world, x, y) == belt)
                            tiles++;
                for (var i = 0; i < tiles * MonsterRules.Per100Tiles / 100; i++)
                    PlaceMonster(new WorldMonster { Id = "c" + _nextMonsterId++ }, belt);
            }
        }

        /// <summary>Puts a swarm of a kind the belt's levels allow on a free tile of the belt. Caller holds the lock.</summary>
        private void PlaceMonster(WorldMonster monster, BeltData belt)
        {
            var (min, max) = MonsterRules.LevelsInBelt(belt.id);
            var kinds = MonsterRules.KindsInBelt(belt.id);
            if (kinds.Length == 0)
                return;
            var kind = MonsterRules.Pick(kinds, _monsterRandom.Next);
            var level = _monsterRandom.Next(Math.Max(min, kind.MinLevel), Math.Min(max, kind.MaxLevel) + 1);

            var world = Catalog.World;
            var size = Math.Max(10, world.size);
            var center = GatherRules.Center(world);
            for (var attempt = 0; attempt < 200; attempt++)
            {
                var d = _monsterRandom.Next(belt.inner, belt.outer + 1);
                var (x, y) = RingTile(center, d, _monsterRandom);
                if (x < 0 || y < 0 || x >= size || y >= size || _taken.Contains((x, y)) || _strongholdAt.ContainsKey((x, y)) || _structureAt.ContainsKey((x, y)) || _nodeAt.ContainsKey((x, y)) || _monsterAt.ContainsKey((x, y)))
                    continue;
                if (_monsters.ContainsKey(monster.Id))
                    _monsterAt.Remove((monster.X, monster.Y));
                monster.Type = kind.Id;
                monster.Level = level;
                monster.X = x;
                monster.Y = y;
                monster.EngagedUntil = null;
                monster.EngagedBy = string.Empty;
                monster.DefeatedAt = null;
                _monsters[monster.Id] = monster;
                _monsterAt[(x, y)] = monster;
                return;
            }
        }

        /// <summary>Frees swarms whose fight is over and brings beaten ones back elsewhere. Caller holds the lock.</summary>
        private void MaintainMonsters(DateTimeOffset now)
        {
            var world = Catalog.World;
            SyncBosses(now);
            foreach (var monster in _monsters.Values.ToList())
            {
                if (monster.IsBoss)
                    continue;
                if (monster.EngagedUntil <= now)
                {
                    monster.EngagedUntil = null;
                    monster.EngagedBy = string.Empty;
                }

                if (monster.DefeatedAt is { } beaten && beaten + MonsterRespawn <= now)
                {
                    var belt = GatherRules.BeltOf(world, monster.X, monster.Y);
                    if (belt != null)
                        PlaceMonster(monster, belt);
                }
            }
        }

        private static bool Gone(WorldMonster m, DateTimeOffset now) => m.DefeatedAt is { } beaten && beaten <= now;

        private WorldMonsterDto[] MonstersInView(int cx, int cy, int radius, DateTimeOffset now, string viewer = "")
        {
            var troops = Catalog.Troops;
            var mine = viewer.Length > 0 ? _social.AllianceIdOf(viewer) : string.Empty;
            return _monsters.Values
                .Where(m => WorldRules.InView(m.X, m.Y, cx, cy, radius) && !Gone(m, now) && (m.AllianceId.Length == 0 || m.AllianceId == mine))
                .Select(m => new WorldMonsterDto
                {
                    Id = m.Id, Type = m.Type, Level = m.Level, X = m.X, Y = m.Y, Busy = m.EngagedUntil > now,
                    Power = MonsterRules.Kind(m.Type) is { } kind ? MonsterRules.Power(troops, kind, m.Level) : MonsterRules.Power(troops, m.Level),
                    EndsAtMs = m.IsBoss ? m.EndsAt.ToUnixTimeMilliseconds() : 0,
                    HpLeft = m.IsBoss ? Math.Max(0, m.HpLeft) : 0,
                    HpMax = m.IsBoss ? m.HpMax : 0
                }).Select(dto => WithBoard(dto, viewer)).ToArray();
        }

        private void HandleAttack(PlayerSession session, MarchAttackCommand command, DateTimeOffset now)
        {
            lock (_gate)
            {
                MaintainMonsters(now);
                var me = Upsert(session);
                var reason = !_monsters.TryGetValue(command.MonsterId ?? string.Empty, out var monster) || Gone(monster, now) ? AttackRejectReason.UnknownMonster
                    : monster.AllianceId.Length > 0 && _social.AllianceIdOf(session.PublicId) != monster.AllianceId ? AttackRejectReason.NotYourTrap
                    : monster.IsBoss ? (monster.HpLeft > 0 ? AttackRejectReason.None : AttackRejectReason.MonsterBusy)
                    : monster.EngagedUntil > now ? AttackRejectReason.MonsterBusy
                    : AttackRejectReason.None;
                if (reason != AttackRejectReason.None)
                {
                    session.Acknowledge(command.CommandId, command.ClientSequence, reason.ToString(), now);
                    return;
                }

                var (march, _, _) = session.TryStartAttack(new MonsterOffer(monster!.Id, monster.Type, monster.Level, monster.X, monster.Y, monster.IsBoss ? monster.HpLeft : 0,
                        monster.AllianceId.Length > 0, monster.AttackBonus),
                    me.X, me.Y, command.CommandId, command.ClientSequence, now, command.HeroIds ?? Array.Empty<string>());
                if (march == null)
                    return;
                if (monster.IsBoss)
                {
                    HitBoss(monster, session.PublicId, march);
                    return;
                }

                monster.EngagedUntil = march.ArrivesAt;
                monster.EngagedBy = session.PublicId;
                if (march.Battle?.Won == true)
                    monster.DefeatedAt = march.ArrivesAt;
            }
        }

        /// <summary>A team recalled on its way leaves the swarm alone. Caller holds the lock.</summary>
        private void ReleaseMonster(string monsterId, string playerId)
        {
            if (_monsters.TryGetValue(monsterId, out var boss) && boss.IsBoss)
            {
                ReleaseBossHits(boss, playerId);
                return;
            }

            if (!_monsters.TryGetValue(monsterId, out var monster) || monster.EngagedBy != playerId)
                return;
            monster.EngagedUntil = null;
            monster.EngagedBy = string.Empty;
            monster.DefeatedAt = null;
        }

        internal List<WorldMonster> MonstersForTests()
        {
            lock (_gate)
                return _monsters.Values.ToList();
        }
    }
}
