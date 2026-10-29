using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Simulation;

namespace Hadal.Game
{
    /// <summary>What a march against or for another player is about (docs/30); persisted with the march.</summary>
    public sealed class PvpInfo
    {
        /// <summary>PvpRules.Attack, Scout, Reinforce or Rally (leader and joiners alike).</summary>
        public string Mode { get; set; } = string.Empty;
        public string TargetKind { get; set; } = string.Empty;
        public string TargetId { get; set; } = string.Empty;
        public string TargetOwnerId { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;

        public string RallyId { get; set; } = string.Empty;
        public bool Leader { get; set; }

        /// <summary>Rally: the army sets out from the leader's base (ViaX, ViaY) then; a joiner reaches it at ViaAt.</summary>
        public DateTimeOffset? DepartsAt { get; set; }
        public DateTimeOffset? ViaAt { get; set; }
        public int ViaX { get; set; }
        public int ViaY { get; set; }

        /// <summary>March speed (100 = base) the team set out with.</summary>
        public int Speed { get; set; } = 100;

        /// <summary>The world resolved (or called off) the battle; until then the team does not unload at home.</summary>
        public bool Settled { get; set; }
    }

    /// <summary>A PvP march as the world asks a session to start it.</summary>
    public sealed record PvpLaunch(
        string Mode, string TargetKind, string TargetId, string TargetOwnerId, string TargetName, int HomeX, int HomeY, int ToX, int ToY,
        IReadOnlyList<string> HeroIds, string RallyId = "", bool Leader = false, DateTimeOffset? DepartsAt = null, int ViaX = 0, int ViaY = 0)
    {
        /// <summary>Rallies: every team brings at least this many troops (docs/33 §3).</summary>
        public long MinTroops => Mode == PvpRules.Rally ? PvpRules.MinRallyTroops : 1;
    }

    /// <summary>A team as the world reads it for a battle.</summary>
    public sealed record PvpTeam(string MarchId, string OwnerId, Dictionary<string, long> Troops, List<int> HeroPowers, int Speed, int X, int Y,
        int HomeX, int HomeY, DateTimeOffset StartedAt, DateTimeOffset ArrivesAt, DateTimeOffset GatheredAt, string NodeId, PvpInfo? Pvp,
        Dictionary<ResourceType, long> CargoAt);

    /// <summary>A base as the world reads it for a battle, at the arrival instant.</summary>
    public sealed record BaseSnapshot(Dictionary<string, long> Troops, List<int> HeroPowers, Dictionary<ResourceType, long> Wallet, int Reactor,
        bool Shielded, bool Beginner, Dictionary<ResourceType, long> Protected);

    public sealed partial class PlayerSession
    {
        private IncomingDto[] _incoming = Array.Empty<IncomingDto>();
        private GarrisonDto[] _garrison = Array.Empty<GarrisonDto>();

        /// <summary>
        /// The hostile marches on the way to this player and the mates' teams guarding the base, kept by the world; sent with
        /// the shield (docs/32 §2.6).
        /// </summary>
        internal void SetIncoming(IncomingDto[] incoming, GarrisonDto[] garrison, DateTimeOffset now)
        {
            lock (_gate)
            {
                _incoming = incoming;
                _garrison = garrison;
                SendDelta(now, new List<EntityChangeDto> { PvpChange(now) });
            }
        }

        /// <summary>The PvP state on login, in one delta, and only when there is something to show (no shield, nothing coming and an empty log is the default).</summary>
        internal void PushPvpOnLogin(IncomingDto[] incoming, GarrisonDto[] garrison, DateTimeOffset now)
        {
            lock (_gate)
            {
                _incoming = incoming;
                _garrison = garrison;
                if (incoming.Length > 0 || garrison.Length > 0 || _state.ShieldUntil > now || _state.WarLog.Count > 0)
                    SendDelta(now, new List<EntityChangeDto> { PvpChange(now), WarLogChange() });
            }
        }

        private EntityChangeDto PvpChange(DateTimeOffset now) => new()
        {
            EntityId = EntityIds.Pvp,
            Kind = EntityChangeKind.Update,
            Payload = _serializer.Serialize(new PvpStatusDto
            {
                ShieldUntilMs = _state.ShieldUntil > now ? _state.ShieldUntil.Value.ToUnixTimeMilliseconds() : 0,
                Beginner = PvpRules.IsBeginner(ReactorLevel()),
                Incoming = _incoming,
                Garrison = _garrison,
                Protected = ResourceAmountEntry.FromMap(ProtectedAmounts()),
                ServerNowMs = now.ToUnixTimeMilliseconds()
            })
        };

        /// <summary>What a base attack can never take, per lootable resource (docs/33 §2). Caller holds the lock.</summary>
        private Dictionary<ResourceType, long> ProtectedAmounts()
        {
            var caps = Capacities();
            var vault = _state.Buildings.Where(b => b.DefinitionId == PvpRules.VaultBuilding).Select(b => b.Level).DefaultIfEmpty(0).Max();
            var reactor = ReactorLevel();
            return PvpRules.Lootable.ToDictionary(r => r, r => PvpRules.Protected(caps.TryGetValue(r, out var c) ? c : 0, reactor, vault));
        }

        private EntityChangeDto WarLogChange() => new()
        {
            EntityId = EntityIds.WarLog,
            Kind = EntityChangeKind.Update,
            Payload = _serializer.Serialize(new WarLogDto { Reports = _state.WarLog.ToArray() })
        };

        /// <summary>The recovery shield after repeated defeats (docs/33 §2): extends the shield to at least <paramref name="until"/>.</summary>
        internal void GrantShield(DateTimeOffset until, DateTimeOffset now)
        {
            lock (_gate)
            {
                if (!(_state.ShieldUntil >= until))
                    _state.ShieldUntil = until;
                _state.IsDirty = true;
                SendDelta(now, new List<EntityChangeDto> { PvpChange(now) });
            }
        }

        /// <summary>The shield is up at <paramref name="at"/>.</summary>
        internal bool ShieldedAt(DateTimeOffset at)
        {
            lock (_gate)
                return _state.ShieldUntil > at;
        }

        internal bool IsBeginner
        {
            get { lock (_gate) return PvpRules.IsBeginner(ReactorLevel()); }
        }

        /// <summary>Travel seconds between two tiles at <paramref name="speed"/> (100 = base), with this player's march bonuses.</summary>
        internal int Travel(int fromX, int fromY, int toX, int toY, int speed)
        {
            lock (_gate)
                return TravelLocked(fromX, fromY, toX, toY, speed);
        }

        private int TravelLocked(int fromX, int fromY, int toX, int toY, int speed) =>
            _modifiers.MarchSeconds((int)Math.Ceiling(GatherRules.TravelSeconds(_catalog.World, fromX, fromY, toX, toY) * 100.0 / Math.Max(1, speed)));

        private List<int> HeroPowers(IEnumerable<string> heroIds) =>
            heroIds.Select(id => _state.Heroes.FirstOrDefault(h => h.HeroId == id))
                .Select(h => h != null && _catalog.TryGetHero(h.HeroId, out var d) ? HeroRules.Power(d, h.Level, h.Stars) : 0).ToList();

        /// <summary>
        /// Starts a march against or for another player. The world holds its lock and has checked the target; this checks
        /// the player's side (beginner protection, queue, team, rally timing), breaks the shield on hostile marches and
        /// takes the troops. Returns the march or a rejection reason (already sent).
        /// </summary>
        internal (MarchState? March, string? Reason) TryStartPvp(PvpLaunch launch, string commandId, ulong sequence, DateTimeOffset now)
        {
            lock (_gate)
            {
                var changes = AdvanceTo(now);
                if (changes.Count > 0)
                    SendDelta(now, changes);

                var hostile = launch.Mode != PvpRules.Reinforce;
                var scout = launch.Mode == PvpRules.Scout;
                string? reason = null;
                if (hostile && PvpRules.IsBeginner(ReactorLevel()))
                    reason = "Beginner";
                else if (_state.Marches.Count >= GatherRules.Queues(_catalog.Gather, ReactorLevel()))
                    reason = MarchRejectReason.NoFreeQueue.ToString();

                var team = new List<OwnedHeroState>();
                var shares = new List<Dictionary<string, long>>();
                var aboard = new Dictionary<string, long>(StringComparer.Ordinal);
                if (reason == null && !scout)
                {
                    var (t, s, a, teamReason) = PickTeam(string.Empty, launch.HeroIds);
                    (team, shares, aboard) = (t, s, a);
                    if (teamReason != MarchRejectReason.None)
                        reason = teamReason.ToString();
                    else if (aboard.Values.Sum() <= 0)
                        reason = MarchRejectReason.NoTroops.ToString();
                    else if (aboard.Values.Sum() < launch.MinTroops)
                        reason = "TooFewTroops";
                }

                var speed = scout ? PvpRules.ScoutSpeed : TroopRules.MarchSpeed(_catalog.Troops, aboard.Keys);
                DateTimeOffset arrives, gathered, home;
                DateTimeOffset? viaAt = null;
                var rally = launch.Mode == PvpRules.Rally;
                if (rally)
                {
                    var departs = launch.DepartsAt ?? now;
                    if (!launch.Leader)
                    {
                        viaAt = now.AddSeconds(TravelLocked(launch.HomeX, launch.HomeY, launch.ViaX, launch.ViaY, speed));
                        if (reason == null && viaAt > departs)
                            reason = "TooLate";
                    }

                    arrives = departs.AddSeconds(TravelLocked(launch.ViaX, launch.ViaY, launch.ToX, launch.ToY, speed));
                    gathered = arrives;
                    home = arrives.AddSeconds(TravelLocked(launch.ToX, launch.ToY, launch.HomeX, launch.HomeY, speed));
                }
                else
                {
                    var travel = TravelLocked(launch.HomeX, launch.HomeY, launch.ToX, launch.ToY, speed);
                    arrives = now.AddSeconds(travel);
                    gathered = launch.Mode == PvpRules.Reinforce ? arrives + PvpRules.StationTime : arrives;
                    home = gathered.AddSeconds(travel);
                }

                if (reason != null)
                {
                    Reject(commandId, sequence, reason, now);
                    return (null, reason);
                }

                var march = new MarchState
                {
                    Id = "p" + Guid.NewGuid().ToString("N")[..10],
                    Kind = launch.Mode,
                    NodeId = launch.TargetId,
                    NodeType = launch.TargetKind,
                    X = launch.ToX,
                    Y = launch.ToY,
                    FromX = launch.HomeX,
                    FromY = launch.HomeY,
                    HeroId = team.FirstOrDefault()?.HeroId ?? string.Empty,
                    Crew = (int)Math.Min(int.MaxValue, aboard.Values.Sum()),
                    HeroIds = team.Select(h => h.HeroId).ToList(),
                    HeroTroops = shares.Select(s => s.Values.Sum()).ToList(),
                    Troops = aboard,
                    StartedAt = now,
                    ArrivesAt = arrives,
                    GatheredAt = gathered,
                    HomeAt = home,
                    Pvp = new PvpInfo
                    {
                        Mode = launch.Mode, TargetKind = launch.TargetKind, TargetId = launch.TargetId, TargetOwnerId = launch.TargetOwnerId,
                        TargetName = launch.TargetName, RallyId = launch.RallyId, Leader = launch.Leader, DepartsAt = launch.DepartsAt,
                        ViaAt = viaAt, ViaX = launch.ViaX, ViaY = launch.ViaY, Speed = speed
                    }
                };
                _state.Marches.Add(march);
                TakeTroops(aboard);
                if (hostile)
                    _state.ShieldUntil = null;
                _state.IsDirty = true;
                Record(scout ? "scout" : hostile ? "pvp" : "reinforce");
                SendDelta(now, new List<EntityChangeDto> { MarchesChange(now), ArmyChange(now), PvpChange(now), Accepted(commandId, sequence) });
                return (march, null);
            }
        }

        /// <summary>A team as it stands now (null when gone): its troops, heroes, speed and what it has gathered by <paramref name="at"/>.</summary>
        internal PvpTeam? ReadTeam(string marchId, DateTimeOffset at)
        {
            lock (_gate)
            {
                var m = _state.Marches.FirstOrDefault(x => x.Id == marchId);
                if (m == null)
                    return null;
                var cargo = m.Kind == "gather" && at > m.ArrivesAt
                    ? GatherRules.Gathered(m.Rates, (Min(at, m.GatheredAt) - m.ArrivesAt).TotalSeconds)
                    : new Dictionary<ResourceType, long>(m.Cargo);
                return new PvpTeam(m.Id, PublicId, new Dictionary<string, long>(m.Troops, StringComparer.Ordinal), HeroPowers(m.HeroIds),
                    m.Pvp?.Speed ?? TroopRules.MarchSpeed(_catalog.Troops, m.Troops.Keys), m.X, m.Y, m.FromX, m.FromY, m.StartedAt, m.ArrivesAt,
                    m.GatheredAt, m.NodeId, m.Pvp, cargo);
            }
        }

        private static DateTimeOffset Min(DateTimeOffset a, DateTimeOffset b) => a < b ? a : b;

        /// <summary>
        /// The base at <paramref name="at"/>: the economy is brought up to that instant first (never backwards), then the
        /// troops at home, the five strongest heroes not out, the wallet and the shield are read.
        /// </summary>
        internal BaseSnapshot SnapshotBase(DateTimeOffset at)
        {
            lock (_gate)
            {
                if (at > _state.LastSimulatedAt)
                {
                    var changes = AdvanceTo(at);
                    if (changes.Count > 0)
                        SendDelta(at, changes);
                }

                var marching = _state.Marches.SelectMany(m => m.HeroIds).ToHashSet(StringComparer.Ordinal);
                var heroes = _state.Heroes.Where(h => !marching.Contains(h.HeroId))
                    .Select(h => _catalog.TryGetHero(h.HeroId, out var d) ? HeroRules.Power(d, h.Level, h.Stars) : 0)
                    .OrderByDescending(p => p).Take(Math.Max(1, _catalog.Troops.command.maxHeroes)).ToList();
                return new BaseSnapshot(new Dictionary<string, long>(_state.Troops, StringComparer.Ordinal), heroes,
                    new Dictionary<ResourceType, long>(_state.Wallet), ReactorLevel(), _state.ShieldUntil > at, PvpRules.IsBeginner(ReactorLevel()),
                    ProtectedAmounts());
            }
        }

        /// <summary>
        /// The base lost a battle or was plundered: fallen troops at home go to the Med Bay while it has room (the rest die),
        /// the plunder leaves the wallet. Returns (dead, wounded).
        /// </summary>
        internal (long Dead, long Wounded) ApplyBaseLosses(IReadOnlyDictionary<string, long> lost, IReadOnlyDictionary<ResourceType, long> plunder,
            DateTimeOffset now)
        {
            lock (_gate)
            {
                long dead = 0, wounded = 0;
                var room = Math.Max(0, TroopRules.InfirmaryCapacity(_catalog.Troops, Levels()) - _state.Wounded.Values.Sum()
                                       - (_state.Healing?.Troops.Values.Sum() ?? 0));
                foreach (var pair in lost.Where(p => p.Value > 0))
                {
                    var have = _state.Troops.TryGetValue(pair.Key, out var n) ? n : 0;
                    var fall = Math.Min(have, pair.Value);
                    if (fall <= 0)
                        continue;
                    _state.Troops[pair.Key] = have - fall;
                    if (_state.Troops[pair.Key] <= 0)
                        _state.Troops.Remove(pair.Key);
                    var hurt = Math.Min(room, fall);
                    room -= hurt;
                    if (hurt > 0)
                        _state.Wounded[pair.Key] = (_state.Wounded.TryGetValue(pair.Key, out var w) ? w : 0) + hurt;
                    wounded += hurt;
                    dead += fall - hurt;
                }

                foreach (var pair in plunder.Where(p => p.Value > 0))
                    _state.Wallet[pair.Key] = Math.Max(0, (_state.Wallet.TryGetValue(pair.Key, out var have) ? have : 0) - pair.Value);
                _walletDirty = true;
                _state.IsDirty = true;
                SendDelta(now, new List<EntityChangeDto> { ArmyChange(now), WalletChange(), ProductionChange(now) });
                return (dead, wounded);
            }
        }

        /// <summary>
        /// A team that fought out on the map: half its fallen die, half ride home wounded; plunder joins its cargo. With
        /// <paramref name="turnHomeFrom"/> the team turns home from that tile at <paramref name="at"/> (or stays, for a
        /// reinforcement that still has troops). Marks the battle settled. Returns (dead, wounded).
        /// </summary>
        internal (long Dead, long Wounded) SettleTeam(string marchId, DateTimeOffset at, IReadOnlyDictionary<string, long> lost,
            IReadOnlyDictionary<ResourceType, long> plunder, bool turnHome, DateTimeOffset now)
        {
            lock (_gate)
            {
                var march = _state.Marches.FirstOrDefault(m => m.Id == marchId);
                if (march == null)
                    return (0, 0);
                var (dead, wounded) = TakeLosses(march, lost, AttackingTeam(march));
                foreach (var pair in plunder.Where(p => p.Value > 0))
                    march.Cargo[pair.Key] = (march.Cargo.TryGetValue(pair.Key, out var c) ? c : 0) + pair.Value;

                var wiped = march.Troops.Values.Sum() <= 0;
                if (turnHome || wiped)
                {
                    // Home from where it fought: the target, the node it was beaten off or the base it guarded.
                    var speed = march.Pvp?.Speed ?? TroopRules.MarchSpeed(_catalog.Troops, march.Troops.Keys);
                    march.GatheredAt = at;
                    march.HomeAt = at.AddSeconds(TravelLocked(march.X, march.Y, march.FromX, march.FromY, speed));
                }

                if (march.Pvp != null && march.Kind != PvpRules.Reinforce)
                    march.Pvp.Settled = true;
                _marchesDirty = true;
                _state.IsDirty = true;
                SendDelta(now, new List<EntityChangeDto> { MarchesChange(now) });
                return (dead, wounded);
            }
        }

        private static bool AttackingTeam(MarchState march) => march.Pvp is { Mode: PvpRules.Attack or PvpRules.Scout or PvpRules.Rally };

        /// <summary>Med Bay room left for more wounded: capacity minus the wounded, the healing and those riding home.</summary>
        private long MedBayRoom() =>
            Math.Max(0, TroopRules.InfirmaryCapacity(_catalog.Troops, Levels()) - _state.Wounded.Values.Sum() - (_state.Healing?.Troops.Values.Sum() ?? 0)
                        - _state.Marches.Sum(m => m.WoundedAboard.Values.Sum()));

        /// <summary>
        /// A team out on the map loses troops: they ride home wounded while the Med Bay has room, the rest die; attackers
        /// always lose a quarter of their fallen (docs/33 §2). Caller holds the lock.
        /// </summary>
        private (long Dead, long Wounded) TakeLosses(MarchState march, IReadOnlyDictionary<string, long> lost, bool attacking)
        {
            long dead = 0, wounded = 0;
            var room = MedBayRoom();
            foreach (var pair in lost.Where(p => p.Value > 0))
            {
                var have = march.Troops.TryGetValue(pair.Key, out var n) ? n : 0;
                var fall = Math.Min(have, pair.Value);
                if (fall <= 0)
                    continue;
                march.Troops[pair.Key] = have - fall;
                if (march.Troops[pair.Key] <= 0)
                    march.Troops.Remove(pair.Key);
                var (d, w) = PvpRules.Split(fall, room, attacking);
                room -= w;
                dead += d;
                wounded += w;
                if (w > 0)
                    march.WoundedAboard[pair.Key] = (march.WoundedAboard.TryGetValue(pair.Key, out var had) ? had : 0) + w;
            }

            march.Crew = (int)Math.Min(int.MaxValue, march.Troops.Values.Sum());
            return (dead, wounded);
        }

        /// <summary>
        /// A gathering team beaten (or wiped) on its node at <paramref name="at"/>: it keeps what it gathered minus what was
        /// taken and turns home. Returns the node and the load the node gets back, like a recall.
        /// </summary>
        internal (string NodeId, long GiveBack, long Dead, long Wounded) BeatOffNode(string marchId, DateTimeOffset at,
            IReadOnlyDictionary<string, long> lost, IReadOnlyDictionary<ResourceType, long> taken, DateTimeOffset now)
        {
            lock (_gate)
            {
                var march = _state.Marches.FirstOrDefault(m => m.Id == marchId);
                if (march == null)
                    return (string.Empty, 0, 0, 0);
                var (dead, wounded) = TakeLosses(march, lost, attacking: false);
                var cargo = GatherRules.Gathered(march.Rates, Math.Max(0, (Min(at, march.GatheredAt) - march.ArrivesAt).TotalSeconds));
                foreach (var pair in taken)
                    if (cargo.ContainsKey(pair.Key))
                        cargo[pair.Key] = Math.Max(0, cargo[pair.Key] - pair.Value);
                march.Cargo = cargo;
                march.GatheredAt = at;
                march.HomeAt = at + (march.ArrivesAt - march.StartedAt);
                var giveBack = Math.Max(0, march.Reserved - GatherRules.LoadOf(_catalog.World, march.Cargo));
                march.Reserved -= giveBack;
                _marchesDirty = true;
                _state.IsDirty = true;
                SendDelta(now, new List<EntityChangeDto> { MarchesChange(now) });
                return (march.NodeId, giveBack, dead, wounded);
            }
        }

        /// <summary>
        /// Calls a team home from (x, y) at <paramref name="at"/> without a battle (target gone, shielded, rally
        /// cancelled, reinforcement sent away). Nothing gathered is kept by a PvP team; a gathering team keeps its cargo.
        /// </summary>
        internal void SendHome(string marchId, DateTimeOffset at, int x, int y, DateTimeOffset now)
        {
            lock (_gate)
            {
                var march = _state.Marches.FirstOrDefault(m => m.Id == marchId);
                if (march == null)
                    return;
                var speed = march.Pvp?.Speed ?? TroopRules.MarchSpeed(_catalog.Troops, march.Troops.Keys);
                if (at < march.ArrivesAt)
                    march.ArrivesAt = at;
                march.GatheredAt = at;
                march.HomeAt = at.AddSeconds(TravelLocked(x, y, march.FromX, march.FromY, speed));
                if (march.Pvp != null)
                {
                    march.Pvp.Settled = true;
                    march.Pvp.DepartsAt = null;
                }

                _marchesDirty = true;
                _state.IsDirty = true;
                SendDelta(now, new List<EntityChangeDto> { MarchesChange(now) });
            }
        }

        /// <summary>Moves a rally team's arrival and home times when a slower team joins.</summary>
        internal void SetRallyTimes(string marchId, DateTimeOffset arrives, DateTimeOffset now)
        {
            lock (_gate)
            {
                var march = _state.Marches.FirstOrDefault(m => m.Id == marchId);
                if (march?.Pvp == null)
                    return;
                var back = TravelLocked(march.X, march.Y, march.FromX, march.FromY, march.Pvp.Speed);
                march.ArrivesAt = arrives;
                march.GatheredAt = arrives;
                march.HomeAt = arrives.AddSeconds(back);
                _marchesDirty = true;
                _state.IsDirty = true;
                SendDelta(now, new List<EntityChangeDto> { MarchesChange(now) });
            }
        }

        /// <summary>Adds a report to the war log (newest first) and sends it.</summary>
        internal void AddWarLog(PvpReportDto report, DateTimeOffset now)
        {
            lock (_gate)
            {
                _state.WarLog.Insert(0, report);
                if (_state.WarLog.Count > PvpRules.WarLogSize)
                    _state.WarLog.RemoveRange(PvpRules.WarLogSize, _state.WarLog.Count - PvpRules.WarLogSize);
                _state.IsDirty = true;
                SendDelta(now, new List<EntityChangeDto> { WarLogChange() });
            }
        }

        /// <summary>
        /// Raises a shield (docs/30 §3) with the bag item, or for its Hadalite price. Not while a hostile march of this
        /// player is out. Extends a running shield.
        /// </summary>
        internal void UseShield(UseShieldCommand command, DateTimeOffset now)
        {
            lock (_gate)
            {
                var id = command.ItemId ?? string.Empty;
                string? reason = null;
                if (!PvpRules.IsShield(id))
                    reason = QuestRejectReason.UnknownItem.ToString();
                else if (_state.Marches.Any(m => m.Pvp is { Settled: false } p && p.Mode != PvpRules.Reinforce))
                    reason = "AttackOut";
                var held = _state.Items.TryGetValue(id, out var n) ? n : 0;
                var price = PvpRules.ShieldHadalite(id);
                var hadalite = _state.Wallet.TryGetValue(ResourceType.Hadalite, out var h) ? h : 0;
                if (reason == null && held <= 0 && hadalite < price)
                    reason = "NotEnoughHadalite";
                if (reason != null)
                {
                    Reject(command.CommandId, command.ClientSequence, reason, now);
                    return;
                }

                var changes = new List<EntityChangeDto>();
                if (held > 0)
                {
                    TakeItem(id, 1);
                    changes.Add(InventoryChange());
                }
                else
                {
                    _state.Wallet[ResourceType.Hadalite] = hadalite - price;
                    Score(EventActions.HadaliteSpent, price);
                    changes.Add(WalletChange());
                }

                var from = _state.ShieldUntil > now ? _state.ShieldUntil.Value : now;
                _state.ShieldUntil = from + PvpRules.ShieldDuration(id);
                _state.IsDirty = true;
                changes.Add(PvpChange(now));
                changes.Add(Accepted(command.CommandId, command.ClientSequence));
                SendDelta(now, changes);
            }
        }

        internal GarrisonDto[] GarrisonForTests
        {
            get { lock (_gate) return _garrison; }
        }

        internal IncomingDto[] PvpIncomingForTests
        {
            get { lock (_gate) return _incoming; }
        }
    }
}
