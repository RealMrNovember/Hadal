using HADAL.Shared.Catalog;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Simulation;

namespace Hadal.Game
{
    /// <summary>A battle resolved when the team set out, revealed when it arrives (docs/rehber 07 §4).</summary>
    public sealed class BattleState
    {
        public bool Won { get; set; }
        public int Rounds { get; set; }
        public Dictionary<string, long> Lost { get; set; } = new(StringComparer.Ordinal);
        public Dictionary<string, long> EnemyStart { get; set; } = new(StringComparer.Ordinal);
        public Dictionary<string, long> EnemyLost { get; set; } = new(StringComparer.Ordinal);

        /// <summary>Bag items won, handed over when the team is home.</summary>
        public Dictionary<string, int> Rewards { get; set; } = new(StringComparer.Ordinal);
        public bool FirstWin { get; set; }
        public bool Reported { get; set; }
    }

    /// <summary>A march on the map, gathering or attacking (docs/rehber 09 §4–5); persisted in PlayerExtras.</summary>
    public sealed class MarchState
    {
        /// <summary>"gather" or "attack" (NodeId/NodeType/NodeLevel then name the monster).</summary>
        public string Kind { get; init; } = "gather";

        public BattleState? Battle { get; set; }

        /// <summary>A march against or for another player (docs/30); the world settles it at its arrival.</summary>
        public PvpInfo? Pvp { get; set; }

        /// <summary>Troops wounded out on the map, coming home to the Med Bay with the team.</summary>
        public Dictionary<string, long> WoundedAboard { get; set; } = new(StringComparer.Ordinal);

        public string Id { get; init; } = string.Empty;
        public string NodeId { get; init; } = string.Empty;
        public string NodeType { get; init; } = string.Empty;
        public int NodeLevel { get; init; }
        public int X { get; init; }
        public int Y { get; init; }
        public int FromX { get; init; }
        public int FromY { get; init; }

        /// <summary>The hero leading the march (the team's first) and the troops aboard.</summary>
        public string HeroId { get; init; } = string.Empty;
        public int Crew { get; set; }

        /// <summary>The team (docs/rehber 19 §1.2): its heroes and how many troops each leads, same order.</summary>
        public List<string> HeroIds { get; init; } = new();
        public List<long> HeroTroops { get; init; } = new();

        /// <summary>Troops aboard by "type:tier"; they come home with the march.</summary>
        public Dictionary<string, long> Troops { get; init; } = new(StringComparer.Ordinal);

        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset ArrivesAt { get; set; }
        public DateTimeOffset GatheredAt { get; set; }
        public DateTimeOffset HomeAt { get; set; }

        /// <summary>Load taken from the node when the march set out (returned to it on recall).</summary>
        public long Reserved { get; set; }

        public Dictionary<ResourceType, double> Rates { get; init; } = new();
        public Dictionary<ResourceType, long> Cargo { get; set; } = new();

        public MarchPhase PhaseAt(DateTimeOffset now) =>
            now < ArrivesAt ? MarchPhase.Outbound : now < GatheredAt ? MarchPhase.Gathering : MarchPhase.Returning;
    }

    /// <summary>A node as the world hands it to a session that wants to gather it.</summary>
    public sealed record NodeOffer(string Id, string Type, int Level, int X, int Y, long Remaining, DateTimeOffset? FadesAt);

    public enum MarchRejectReason
    {
        None = 0,
        UnknownDeposit = 1,
        DepositBusy = 2,
        DepositEmpty = 3,
        NoFreeQueue = 4,
        UnknownMarch = 5,
        AlreadyReturning = 6,
        NotYourDeposit = 7,
        HeroBusy = 8,
        UnknownHero = 9,
        NoTroops = 10,
        TooManyHeroes = 11
    }


    public sealed partial class PlayerSession
    {
        private bool _marchesDirty;

        /// <summary>
        /// Plans and starts a march to <paramref name="node"/> from the base at (homeX, homeY). The world holds its
        /// lock and reserves the returned load on the node. Returns the march or why not.
        /// </summary>
        internal (MarchState? March, MarchRejectReason Reason) TryStartMarch(NodeOffer node, int homeX, int homeY, string commandId, ulong sequence, DateTimeOffset now,
            string heroId = "", IReadOnlyList<string>? heroIds = null, double rateBonus = 0)
        {
            lock (_gate)
            {
                var changes = AdvanceTo(now);
                if (changes.Count > 0)
                    SendDelta(now, changes);

                var reason = MarchRejectReason.None;
                var reactor = ReactorLevel();
                if (_state.Marches.Count >= GatherRules.Queues(_catalog.Gather, reactor))
                    reason = MarchRejectReason.NoFreeQueue;

                var troops = _catalog.Troops;
                var armed = troops.types.Length > 0;
                var (team, shares, aboard, teamReason) = PickTeam(heroId, heroIds);
                if (reason == MarchRejectReason.None)
                    reason = teamReason;

                var world = _catalog.World;
                var rates = GatherRules.Rates(world, _catalog.Gather, node.Type, node.Level, reactor);
                // Alliance territory gathers faster (docs/30 §6.3).
                if (rateBonus > 0)
                    rates = rates.ToDictionary(p => p.Key, p => p.Value * (1 + rateBonus));
                // Alliance research: gathering and troop load (docs/31 step 6).
                if (_modifiers.GatherPercent > 0)
                    rates = rates.ToDictionary(p => p.Key, p => p.Value * (1 + _modifiers.GatherPercent / 100.0));
                var speed = armed ? TroopRules.MarchSpeed(troops, aboard.Keys) : 100;
                var travel = _modifiers.MarchSeconds((int)Math.Ceiling(GatherRules.TravelSeconds(world, homeX, homeY, node.X, node.Y) * 100.0 / speed));
                var load = Math.Min((armed ? TroopRules.MarchLoad(troops, aboard) : GatherRules.Load(_catalog.Gather, reactor)) * (100 + _modifiers.MarchLoadPercent) / 100,
                    node.Remaining);
                var gather = GatherRules.GatherSeconds(world, rates, load);
                var arrives = now.AddSeconds(travel);
                if (node.FadesAt is { } fades && arrives.AddSeconds(gather) > fades)
                    gather = Math.Max(0, (int)(fades - arrives).TotalSeconds);
                if (reason == MarchRejectReason.None && (load <= 0 || gather <= 0 || rates.Count == 0))
                    reason = MarchRejectReason.DepositEmpty;

                if (reason != MarchRejectReason.None)
                {
                    Reject(commandId, sequence, reason.ToString(), now);
                    return (null, reason);
                }

                var cargo = GatherRules.Gathered(rates, gather);
                var march = new MarchState
                {
                    Id = "m" + Guid.NewGuid().ToString("N")[..10],
                    NodeId = node.Id,
                    NodeType = node.Type,
                    NodeLevel = node.Level,
                    X = node.X,
                    Y = node.Y,
                    FromX = homeX,
                    FromY = homeY,
                    HeroId = team.FirstOrDefault()?.HeroId ?? string.Empty,
                    Crew = armed ? (int)Math.Min(int.MaxValue, aboard.Values.Sum()) : GatherRules.Crew(reactor),
                    HeroIds = team.Select(h => h.HeroId).ToList(),
                    HeroTroops = shares.Select(s => s.Values.Sum()).ToList(),
                    Troops = aboard,
                    StartedAt = now,
                    ArrivesAt = arrives,
                    GatheredAt = arrives.AddSeconds(gather),
                    HomeAt = arrives.AddSeconds(gather + _modifiers.ReturnSeconds(travel)),
                    Reserved = Math.Min(node.Remaining, GatherRules.LoadOf(world, cargo)),
                    Rates = rates,
                    Cargo = cargo
                };
                _state.Marches.Add(march);
                TakeTroops(aboard);

                _state.IsDirty = true;
                Record("gather");
                SendDelta(now, new List<EntityChangeDto> { MarchesChange(now), ArmyChange(now), Accepted(commandId, sequence) });
                return (march, MarchRejectReason.None);
            }
        }

        /// <summary>
        /// The team for a march: the heroes asked for, or the strongest free ones (Last Asylum's auto-fill), each taking
        /// their command of troops from home. Nothing changes until <see cref="TakeTroops"/>. Caller holds the lock.
        /// </summary>
        private (List<OwnedHeroState> Team, List<Dictionary<string, long>> Shares, Dictionary<string, long> Aboard, MarchRejectReason Reason)
            PickTeam(string heroId, IReadOnlyList<string>? heroIds)
        {
            var reason = MarchRejectReason.None;
            // The team: the heroes asked for, or the strongest who are not out already (Last Asylum's auto-fill).
            var troops = _catalog.Troops;
            var marching = _state.Marches.SelectMany(m => m.HeroIds.Count > 0 ? m.HeroIds : new List<string> { m.HeroId })
                .ToHashSet(StringComparer.Ordinal);
            var asked = (heroIds ?? Array.Empty<string>()).Where(id => !string.IsNullOrEmpty(id)).Distinct(StringComparer.Ordinal).ToList();
            if (asked.Count == 0 && !string.IsNullOrEmpty(heroId))
                asked.Add(heroId);
            var maxHeroes = Math.Max(1, troops.command.maxHeroes);
            List<OwnedHeroState> team;
            if (asked.Count > 0)
            {
                team = asked.Select(id => _state.Heroes.FirstOrDefault(h => h.HeroId == id)).Where(h => h != null).Select(h => h!).ToList();
                if (reason == MarchRejectReason.None && asked.Count > maxHeroes)
                    reason = MarchRejectReason.TooManyHeroes;
                else if (reason == MarchRejectReason.None && team.Count < asked.Count)
                    reason = MarchRejectReason.UnknownHero;
                else if (reason == MarchRejectReason.None && team.Any(h => marching.Contains(h.HeroId)))
                    reason = MarchRejectReason.HeroBusy;
            }
            else
            {
                team = _state.Heroes.Where(h => !marching.Contains(h.HeroId))
                    .OrderByDescending(h => _catalog.TryGetHero(h.HeroId, out var d) ? HeroRules.Power(d, h.Level, h.Stars) : 0)
                    .Take(troops.types.Length > 0 ? maxHeroes : 1).ToList();
            }

            // Each hero takes their command of troops from home; the army decides load and speed.
            var armed = troops.types.Length > 0;
            var shares = armed ? TroopRules.FillTeam(troops, _state.Troops, team.Select(h => (Affinity(h.HeroId), CommandOf(h))).ToList())
                : team.Select(_ => new Dictionary<string, long>()).ToList();
            // An automatic team keeps only heroes who get troops (a hero with nobody to lead stays home).
            if (asked.Count == 0 && armed)
            {
                var keep = shares.Select((share, i) => (share, i)).Where(x => x.share.Values.Sum() > 0).Select(x => x.i).ToList();
                if (keep.Count > 0)
                {
                    team = keep.Select(i => team[i]).ToList();
                    shares = keep.Select(i => shares[i]).ToList();
                }
            }

            var aboard = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (var share in shares)
                foreach (var pair in share)
                    aboard[pair.Key] = (aboard.TryGetValue(pair.Key, out var n) ? n : 0) + pair.Value;
            if (reason == MarchRejectReason.None && armed && aboard.Values.Sum() <= 0)
                reason = MarchRejectReason.NoTroops;
            return (team, shares, aboard, reason);
        }

        /// <summary>Troops leave home with a march. Caller holds the lock.</summary>
        private void TakeTroops(Dictionary<string, long> aboard)
        {
            foreach (var pair in aboard)
            {
                _state.Troops[pair.Key] -= pair.Value;
                if (_state.Troops[pair.Key] <= 0)
                    _state.Troops.Remove(pair.Key);
            }
        }

        /// <summary>
        /// Turns a march home. Returns the node it worked and the load the node gets back, or why not.
        /// Outbound: nothing gathered, home after the time already travelled. Gathering: keeps what it has.
        /// </summary>
        internal (string NodeId, long GiveBack, MarchRejectReason Reason) Recall(string marchId, string commandId, ulong sequence, DateTimeOffset now)
        {
            lock (_gate)
            {
                var changes = AdvanceTo(now);
                if (changes.Count > 0)
                    SendDelta(now, changes);

                var march = _state.Marches.FirstOrDefault(m => m.Id == marchId);
                var reason = march == null ? MarchRejectReason.UnknownMarch
                    : march.PhaseAt(now) == MarchPhase.Returning ? MarchRejectReason.AlreadyReturning
                    : MarchRejectReason.None;
                if (reason != MarchRejectReason.None)
                {
                    Reject(commandId, sequence, reason.ToString(), now);
                    return (string.Empty, 0, reason);
                }

                var world = _catalog.World;
                CancelBattle(march!, now);
                if (march!.Pvp != null)
                    march.Pvp.Settled = true;
                if (march!.PhaseAt(now) == MarchPhase.Outbound)
                {
                    march.Cargo = new Dictionary<ResourceType, long>();
                    march.HomeAt = now + HomeLeg(march, now - march.StartedAt);
                }
                else
                {
                    march.Cargo = GatherRules.Gathered(march.Rates, (now - march.ArrivesAt).TotalSeconds);
                    march.HomeAt = now + HomeLeg(march, march.ArrivesAt - march.StartedAt);
                }

                march.ArrivesAt = now < march.ArrivesAt ? now : march.ArrivesAt;
                march.GatheredAt = now;
                var giveBack = Math.Max(0, march.Reserved - GatherRules.LoadOf(world, march.Cargo));
                march.Reserved -= giveBack;
                _state.IsDirty = true;
                SendDelta(now, new List<EntityChangeDto> { MarchesChange(now), Accepted(commandId, sequence) });
                return (march.NodeId, giveBack, MarchRejectReason.None);
            }
        }

        /// <summary>
        /// The way home of a team that sailed <paramref name="outbound"/> out: a gathering team comes back faster with the
        /// "Hızlı Dönüş" research (<see cref="PlayerModifiers.ReturnSpeedPercent"/>); every other team takes as long as it
        /// took out. Caller holds the lock.
        /// </summary>
        private TimeSpan HomeLeg(MarchState march, TimeSpan outbound)
        {
            if (march.Pvp != null || march.Kind != "gather" || _modifiers.ReturnSpeedPercent <= 0 || outbound <= TimeSpan.Zero)
                return outbound;
            return TimeSpan.FromSeconds(outbound.TotalSeconds / (1.0 + _modifiers.ReturnSpeedPercent / 100.0));
        }

        /// <summary>A PvP team whose battle the world has not settled yet does not unload at home (docs/30 §2).</summary>
        private static bool AwaitsBattle(MarchState march) => march.Pvp is { Settled: false } p && p.Mode != PvpRules.Reinforce;

        /// <summary>Unloads a march that reached home. Caller holds the lock (economy step).</summary>
        private void CompleteMarch(MarchState march, DateTimeOffset at)
        {
            _state.Marches.Remove(march);
            foreach (var pair in march.Battle?.Rewards ?? new Dictionary<string, int>())
                _state.Items[pair.Key] = (_state.Items.TryGetValue(pair.Key, out var held) ? held : 0) + pair.Value;
            foreach (var pair in march.Troops)
                _state.Troops[pair.Key] = (_state.Troops.TryGetValue(pair.Key, out var home) ? home : 0) + pair.Value;
            foreach (var pair in march.WoundedAboard)
                _state.Wounded[pair.Key] = (_state.Wounded.TryGetValue(pair.Key, out var hurt) ? hurt : 0) + pair.Value;
            if (march.WoundedAboard.Count > 0)
                _armyDirty = true;
            if (march.Troops.Count > 0)
                _armyDirty = true;
            var cargo = new Dictionary<ResourceType, long>(march.Cargo);

            // Hadalite from veins counts towards a daily limit (docs/rehber 09 §4.4).
            if (cargo.TryGetValue(ResourceType.Hadalite, out var hadalite))
            {
                var used = _state.DailyCounters.TryGetValue(DailyMapHadalite, out var u) ? u : 0;
                var allowed = Math.Clamp(_catalog.World.hadaliteDailyCap - used, 0, hadalite);
                cargo[ResourceType.Hadalite] = allowed;
                _state.DailyCounters[DailyMapHadalite] = used + allowed;
            }

            // Gathered resources are not held back by the storage cap (docs/rehber 09 §4.7, rule 5).
            var walletBefore = WalletSnapshot();
            foreach (var pair in cargo.Where(p => p.Value > 0))
                WalletOps.Credit(_state.Wallet, pair.Key, pair.Value);
            LogWalletDelta("march_loot", walletBefore, at);
            Score(EventActions.Gather, cargo.Values.Where(v => v > 0).Sum());
            _walletDirty = true;
            _marchesDirty = true;
            _state.IsDirty = true;
        }

        private const string DailyMapHadalite = "mapHadalite";

        private EntityChangeDto MarchesChange(DateTimeOffset now)
        {
            _marchesDirty = false;
            return new EntityChangeDto
            {
                EntityId = EntityIds.Marches,
                Kind = EntityChangeKind.Update,
                Payload = _serializer.Serialize(MarchListDto(now))
            };
        }

        private MarchListDto MarchListDto(DateTimeOffset now)
        {
            var reactor = ReactorLevel();
            var gather = _catalog.Gather;
            return new MarchListDto
            {
                Queues = GatherRules.Queues(gather, reactor),
                Load = GatherRules.Load(gather, reactor),
                NextQueueReactor = gather.queueReactors.Where(r => r > reactor).DefaultIfEmpty(0).Min(),
                ServerNowMs = now.ToUnixTimeMilliseconds(),
                SonarEnergy = (int)Math.Floor(Sonar(now)),
                SonarCapacity = MonsterRules.SonarCapacity,
                Marches = _state.Marches.Select(m => new MarchDto
                {
                    Id = m.Id,
                    NodeId = m.NodeId,
                    NodeType = m.NodeType,
                    NodeLevel = m.NodeLevel,
                    X = m.X,
                    Y = m.Y,
                    Phase = m.PhaseAt(now),
                    StartedAtMs = m.StartedAt.ToUnixTimeMilliseconds(),
                    ArrivesAtMs = m.ArrivesAt.ToUnixTimeMilliseconds(),
                    GatheredAtMs = m.GatheredAt.ToUnixTimeMilliseconds(),
                    HomeAtMs = m.HomeAt.ToUnixTimeMilliseconds(),
                    Cargo = ResourceAmountEntry.FromMap(m.Cargo),
                    HeroId = m.HeroId,
                    Crew = m.Crew,
                    FromX = m.FromX,
                    FromY = m.FromY,
                    Kind = m.Kind,
                    HeroIds = m.HeroIds.ToArray(),
                    HeroTroops = m.HeroTroops.ToArray(),
                    Troops = m.Troops.Select(p => TroopRules.TryParseKey(p.Key, out var type, out var tier)
                        ? new TroopStackDto { Type = type, Tier = tier, Count = p.Value } : null).Where(t => t != null).Select(t => t!).ToArray()
                }).ToArray()
            };
        }

        /// <summary>The node ids this player's marches are working (for the map's "mine" flag).</summary>
        /// <summary>This player's teams as the map shows them to everyone (relation and owner filled in by the world).</summary>
        internal List<WorldMarchDto> MarchViews()
        {
            lock (_gate)
            {
                var result = new List<WorldMarchDto>();
                foreach (var m in _state.Marches)
                {
                    var view = new WorldMarchDto
                    {
                        Id = m.Id, OwnerId = PublicId, OwnerName = _displayName, FromX = m.FromX, FromY = m.FromY, ToX = m.X, ToY = m.Y,
                        StartedAtMs = m.StartedAt.ToUnixTimeMilliseconds(), ArrivesAtMs = m.ArrivesAt.ToUnixTimeMilliseconds(),
                        GatheredAtMs = m.GatheredAt.ToUnixTimeMilliseconds(), HomeAtMs = m.HomeAt.ToUnixTimeMilliseconds(), Kind = m.Kind
                    };

                    // A rally (docs/30 §5.3): joiners first sail to the leader's base, the army then sails from there as the
                    // leader's march; teams waiting inside the leader's base or riding with the army are not drawn twice.
                    if (m.Pvp is { Mode: PvpRules.Rally, DepartsAt: { } departs } rally)
                    {
                        if (!rally.Leader && rally.ViaAt is { } viaAt)
                        {
                            result.Add(new WorldMarchDto
                            {
                                Id = m.Id + ":via", OwnerId = PublicId, OwnerName = _displayName, FromX = m.FromX, FromY = m.FromY, ToX = rally.ViaX,
                                ToY = rally.ViaY, StartedAtMs = view.StartedAtMs, ArrivesAtMs = viaAt.ToUnixTimeMilliseconds(),
                                GatheredAtMs = viaAt.ToUnixTimeMilliseconds(), HomeAtMs = viaAt.ToUnixTimeMilliseconds(), Kind = m.Kind
                            });
                            // Then only the way home from the target, once the army has fought.
                            view.StartedAtMs = view.ArrivesAtMs;
                        }
                        else
                        {
                            view.FromX = rally.ViaX;
                            view.FromY = rally.ViaY;
                            view.StartedAtMs = departs.ToUnixTimeMilliseconds();
                        }
                    }

                    result.Add(view);
                }

                return result;
            }
        }

        internal HashSet<string> MarchNodes()
        {
            lock (_gate)
                return _state.Marches.Select(m => m.NodeId).ToHashSet(StringComparer.Ordinal);
        }

        internal IReadOnlyList<MarchState> MarchesForTests
        {
            get { lock (_gate) return _state.Marches.ToList(); }
        }
    }
}
