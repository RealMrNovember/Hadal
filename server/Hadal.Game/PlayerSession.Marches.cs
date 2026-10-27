using HADAL.Shared.Catalog;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Simulation;

namespace Hadal.Game
{
    /// <summary>A gathering march (docs/rehber 09 §4); persisted in PlayerExtras.</summary>
    public sealed class MarchState
    {
        public string Id { get; init; } = string.Empty;
        public string NodeId { get; init; } = string.Empty;
        public string NodeType { get; init; } = string.Empty;
        public int NodeLevel { get; init; }
        public int X { get; init; }
        public int Y { get; init; }
        public int FromX { get; init; }
        public int FromY { get; init; }

        /// <summary>The hero leading the march and the divers aboard.</summary>
        public string HeroId { get; init; } = string.Empty;
        public int Crew { get; init; }

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
        UnknownHero = 9
    }

    public sealed partial class PlayerSession
    {
        private bool _marchesDirty;

        /// <summary>
        /// Plans and starts a march to <paramref name="node"/> from the base at (homeX, homeY). The world holds its
        /// lock and reserves the returned load on the node. Returns the march or why not.
        /// </summary>
        internal (MarchState? March, MarchRejectReason Reason) TryStartMarch(NodeOffer node, int homeX, int homeY, string commandId, ulong sequence, DateTimeOffset now,
            string heroId = "")
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

                // A hero leads every march: the one asked for, or the strongest who is not out already.
                var marching = _state.Marches.Select(m => m.HeroId).ToHashSet(StringComparer.Ordinal);
                var hero = string.IsNullOrEmpty(heroId)
                    ? _state.Heroes.Where(h => !marching.Contains(h.HeroId))
                        .OrderByDescending(h => _catalog.TryGetHero(h.HeroId, out var d) ? HeroRules.Power(d, h.Level, h.Stars) : 0).FirstOrDefault()
                    : _state.Heroes.FirstOrDefault(h => h.HeroId == heroId);
                if (reason == MarchRejectReason.None && !string.IsNullOrEmpty(heroId) && hero == null)
                    reason = MarchRejectReason.UnknownHero;
                else if (reason == MarchRejectReason.None && hero != null && marching.Contains(hero.HeroId))
                    reason = MarchRejectReason.HeroBusy;

                var world = _catalog.World;
                var rates = GatherRules.Rates(world, _catalog.Gather, node.Type, node.Level, reactor);
                var travel = GatherRules.TravelSeconds(world, homeX, homeY, node.X, node.Y);
                var load = Math.Min(GatherRules.Load(_catalog.Gather, reactor), node.Remaining);
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
                    HeroId = hero?.HeroId ?? string.Empty,
                    Crew = GatherRules.Crew(reactor),
                    StartedAt = now,
                    ArrivesAt = arrives,
                    GatheredAt = arrives.AddSeconds(gather),
                    HomeAt = arrives.AddSeconds(gather + travel),
                    Reserved = Math.Min(node.Remaining, GatherRules.LoadOf(world, cargo)),
                    Rates = rates,
                    Cargo = cargo
                };
                _state.Marches.Add(march);
                _state.IsDirty = true;
                Record("gather");
                SendDelta(now, new List<EntityChangeDto> { MarchesChange(now), Accepted(commandId, sequence) });
                return (march, MarchRejectReason.None);
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
                if (march!.PhaseAt(now) == MarchPhase.Outbound)
                {
                    march.Cargo = new Dictionary<ResourceType, long>();
                    march.HomeAt = now + (now - march.StartedAt);
                }
                else
                {
                    march.Cargo = GatherRules.Gathered(march.Rates, (now - march.ArrivesAt).TotalSeconds);
                    march.HomeAt = now + (march.ArrivesAt - march.StartedAt);
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

        /// <summary>Unloads a march that reached home. Caller holds the lock (economy step).</summary>
        private void CompleteMarch(MarchState march)
        {
            _state.Marches.Remove(march);
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
            foreach (var pair in cargo.Where(p => p.Value > 0))
                _state.Wallet[pair.Key] = (_state.Wallet.TryGetValue(pair.Key, out var have) ? have : 0) + pair.Value;
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
                    FromY = m.FromY
                }).ToArray()
            };
        }

        /// <summary>The node ids this player's marches are working (for the map's "mine" flag).</summary>
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
