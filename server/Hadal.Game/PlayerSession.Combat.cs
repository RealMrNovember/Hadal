using HADAL.Shared.DTOs;
using HADAL.Shared.Protocol;
using HADAL.Shared.Simulation;

namespace Hadal.Game
{
    /// <summary>A monster as the world hands it to a session that wants to attack it; for an event boss, what is left of its body.</summary>
    public sealed record MonsterOffer(string Id, string Type, int Level, int X, int Y, long BossLeft = 0);

    /// <summary>
    /// Attacks on map monsters (docs/rehber 09 §5, 07 §4): the team pays Sonar Energy, travels, fights on arrival and
    /// comes home. The battle is deterministic and the monster cannot change on the way, so it is resolved when the
    /// team sets out and revealed when it arrives; PvE never kills, the lost come home wounded to the Med Bay.
    /// </summary>
    public sealed partial class PlayerSession
    {
        /// <summary>Sonar Energy now (fills 30 an hour up to 500).</summary>
        private double Sonar(DateTimeOffset now) => MonsterRules.SonarNow(_state.SonarStored, _state.SonarAt, now);

        private void SetSonar(double value, DateTimeOffset now)
        {
            _state.SonarStored = Math.Max(0, value);
            _state.SonarAt = now;
        }

        internal (MarchState? March, MarchRejectReason Reason, AttackRejectReason Attack) TryStartAttack(MonsterOffer monster, int homeX, int homeY,
            string commandId, ulong sequence, DateTimeOffset now, IReadOnlyList<string>? heroIds)
        {
            lock (_gate)
            {
                var changes = AdvanceTo(now);
                if (changes.Count > 0)
                    SendDelta(now, changes);

                var kind = MonsterRules.Kind(monster.Type);
                if (kind == null)
                {
                    Reject(commandId, sequence, AttackRejectReason.UnknownMonster.ToString(), now);
                    return (null, MarchRejectReason.None, AttackRejectReason.UnknownMonster);
                }

                var reason = _state.Marches.Count >= GatherRules.Queues(_catalog.Gather, ReactorLevel()) ? MarchRejectReason.NoFreeQueue : MarchRejectReason.None;
                var (team, shares, aboard, teamReason) = PickTeam(string.Empty, heroIds);
                if (reason == MarchRejectReason.None)
                    reason = teamReason;
                if (reason == MarchRejectReason.None && aboard.Values.Sum() <= 0)
                    reason = MarchRejectReason.NoTroops;
                if (reason != MarchRejectReason.None)
                {
                    Reject(commandId, sequence, reason.ToString(), now);
                    return (null, reason, AttackRejectReason.None);
                }

                var sonar = Sonar(now);
                if (sonar < kind.Sonar)
                {
                    Reject(commandId, sequence, AttackRejectReason.NotEnoughSonar.ToString(), now);
                    return (null, MarchRejectReason.None, AttackRejectReason.NotEnoughSonar);
                }

                // The battle: the team's troops with the heroes' command bonus against the swarm.
                var troops = _catalog.Troops;
                var mine = new BattleSide();
                foreach (var pair in aboard)
                    mine.Troops[pair.Key] = pair.Value;
                var bonus = CombatRules.HeroBonus(team.Select(h => _catalog.TryGetHero(h.HeroId, out var d) ? HeroRules.Power(d, h.Level, h.Stars) : 0).ToList());
                mine.AtkPercent = mine.DefPercent = mine.HpPercent = bonus;
                var enemy = MonsterRules.Army(kind, monster.Level);
                // An event boss is one shared body: a team fights only what earlier hits have left of it.
                if (kind.EventOnly && monster.BossLeft > 0)
                    enemy.Troops[TroopRules.Key(kind.CountsAs, MonsterRules.Tier(monster.Level))] = Math.Min(MonsterRules.Count(kind, monster.Level), monster.BossLeft);
                var result = CombatRules.Resolve(troops, mine, enemy);

                var firstWin = result.AttackerWon && !kind.EventOnly && !_state.MonsterWins.Contains(monster.Level);
                var rewards = result.AttackerWon ? MonsterRules.Rewards(kind, monster.Level, firstWin, Random.Shared.Next)
                    : kind.EventOnly && result.DefenderLost.Values.Sum() > 0 ? MonsterRules.BossHitRewards(monster.Level)
                    : new Dictionary<string, int>();
                // A lost attack gives half the Sonar back (09 §5.1).
                SetSonar(sonar - kind.Sonar * (result.AttackerWon ? 1.0 : 0.5), now);

                var speed = TroopRules.MarchSpeed(troops, aboard.Keys);
                var travel = _modifiers.MarchSeconds((int)Math.Ceiling(GatherRules.TravelSeconds(_catalog.World, homeX, homeY, monster.X, monster.Y) * 100.0 / speed));
                var arrives = now.AddSeconds(travel);
                var march = new MarchState
                {
                    Id = "a" + Guid.NewGuid().ToString("N")[..10],
                    Kind = "attack",
                    NodeId = monster.Id,
                    NodeType = monster.Type,
                    NodeLevel = monster.Level,
                    X = monster.X,
                    Y = monster.Y,
                    FromX = homeX,
                    FromY = homeY,
                    HeroId = team.FirstOrDefault()?.HeroId ?? string.Empty,
                    Crew = (int)Math.Min(int.MaxValue, aboard.Values.Sum()),
                    HeroIds = team.Select(h => h.HeroId).ToList(),
                    HeroTroops = shares.Select(s => s.Values.Sum()).ToList(),
                    Troops = aboard,
                    StartedAt = now,
                    ArrivesAt = arrives,
                    GatheredAt = arrives,
                    HomeAt = arrives.AddSeconds(travel),
                    Battle = new BattleState
                    {
                        Won = result.AttackerWon,
                        Rounds = result.Rounds,
                        Lost = result.AttackerLost.Where(p => p.Value > 0).ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal),
                        EnemyStart = enemy.Troops.ToDictionary(p => p.Key, p => (long)Math.Round(p.Value), StringComparer.Ordinal),
                        EnemyLost = result.DefenderLost.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal),
                        Rewards = rewards,
                        FirstWin = firstWin
                    }
                };
                _state.Marches.Add(march);
                TakeTroops(aboard);
                _state.IsDirty = true;
                Record("attack");
                SendDelta(now, new List<EntityChangeDto> { MarchesChange(now), ArmyChange(now), Accepted(commandId, sequence) });
                return (march, MarchRejectReason.None, AttackRejectReason.None);
            }
        }

        /// <summary>
        /// The team reached the monster: the lost leave the team as wounded (PvE never kills, 07 §2.6), the report goes
        /// out, a first win at this level is remembered. Caller holds the lock (economy step).
        /// </summary>
        private EntityChangeDto Fight(MarchState march)
        {
            var battle = march.Battle!;
            battle.Reported = true;
            foreach (var pair in battle.Lost)
            {
                var lost = Math.Min(pair.Value, march.Troops.TryGetValue(pair.Key, out var aboard) ? aboard : 0);
                march.Troops[pair.Key] = aboard - lost;
                if (march.Troops[pair.Key] <= 0)
                    march.Troops.Remove(pair.Key);
                _state.Wounded[pair.Key] = (_state.Wounded.TryGetValue(pair.Key, out var w) ? w : 0) + lost;
            }

            var boss = MonsterRules.Kind(march.NodeType) is { EventOnly: true };
            if (battle.Won)
            {
                if (!boss)
                    _state.MonsterWins.Add(march.NodeLevel);
                Score(EventActions.MonsterKill, 1);
                Score(EventActions.MonsterLevels, march.NodeLevel);
            }

            if (boss && battle.EnemyLost.Values.Sum() is var damage and > 0)
                Score(EventActions.BossDamage, damage);
            _armyDirty = true;
            _marchesDirty = true;
            _state.IsDirty = true;
            return new EntityChangeDto
            {
                EntityId = EntityIds.BattleReport + march.Id,
                Kind = EntityChangeKind.Update,
                Payload = _serializer.Serialize(Report(march))
            };
        }

        private BattleReportDto Report(MarchState march)
        {
            var battle = march.Battle!;
            var troops = _catalog.Troops;
            BattleStackDto[] Stacks(IReadOnlyDictionary<string, long> start, IReadOnlyDictionary<string, long> lost) =>
                start.Select(p => TroopRules.TryParseKey(p.Key, out var type, out var tier)
                        ? new BattleStackDto { Type = type, Tier = tier, Start = p.Value, Lost = lost.TryGetValue(p.Key, out var l) ? l : 0 }
                        : null)
                    .Where(s => s != null).Select(s => s!).ToArray();
            long Power(IReadOnlyDictionary<string, long> stacks) => TroopRules.Power(troops, stacks);

            // The team's starting troops: those still aboard plus the ones it lost.
            var start = new Dictionary<string, long>(march.Troops, StringComparer.Ordinal);
            foreach (var pair in battle.Lost)
                start[pair.Key] = (start.TryGetValue(pair.Key, out var n) ? n : 0) + pair.Value;
            return new BattleReportDto
            {
                Id = march.Id,
                MonsterType = march.NodeType,
                MonsterLevel = march.NodeLevel,
                X = march.X,
                Y = march.Y,
                Won = battle.Won,
                Rounds = battle.Rounds,
                AtMs = march.ArrivesAt.ToUnixTimeMilliseconds(),
                HeroIds = march.HeroIds.ToArray(),
                Attacker = Stacks(start, battle.Lost),
                Defender = Stacks(battle.EnemyStart, battle.EnemyLost),
                Rewards = battle.Rewards.Select(p => new ItemCountDto { Id = p.Key, Count = p.Value }).ToArray(),
                AttackerPower = Power(start),
                DefenderPower = Power(battle.EnemyStart)
            };
        }

        /// <summary>Attacks on their way home without a battle (recalled): an event boss takes those hits back.</summary>
        internal HashSet<string> RecalledAttacks()
        {
            lock (_gate)
                return _state.Marches.Where(m => m.Kind == "attack" && m.Battle == null).Select(m => m.Id).ToHashSet(StringComparer.Ordinal);
        }

        /// <summary>A team recalled before it arrives fights nothing and gets its Sonar back.</summary>
        private void CancelBattle(MarchState march, DateTimeOffset now)
        {
            if (march.Battle is not { Reported: false } battle)
                return;
            var kind = MonsterRules.Kind(march.NodeType);
            if (kind != null)
                SetSonar(Sonar(now) + kind.Sonar * (battle.Won ? 1.0 : 0.5), now);
            march.Battle = null;
        }
    }
}
