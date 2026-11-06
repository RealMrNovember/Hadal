using HADAL.Shared.Protocol;
using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;

namespace Hadal.Game
{
    /// <summary>
    /// The survival loop (docs/35_Survival_Loop.md). Energy feeds the producers: with the energy store empty and demand
    /// above supply, every energy user runs at supply/demand (never under the catalog floor). The crew and the troops
    /// breathe oxygen: falling under the warning share shows an alarm, an empty store slows production and new timers.
    /// Now and then, only while the player is online, a seeded roll starts an emergency (a leak or a broken producer)
    /// that a cheap repair, alliance help or simply time ends. Nothing is ever destroyed or killed, and everything
    /// recovers the moment the cause is gone.
    /// </summary>
    public sealed partial class PlayerSession
    {
        /// <summary>The survival state changed (emergency started or ended): push the production summary.</summary>
        private bool _survivalDirty;

        /// <summary>The alarm state last sent to the client, so a change without a wallet change is still pushed.</summary>
        private (SurvivalStatus Energy, SurvivalStatus Oxygen, string Emergency, bool Repairing) _survivalSent;
        private DateTimeOffset _survivalCheckedAt;

        /// <summary>One economy step under the survival loop: the net rates and the alarms they come with.</summary>
        private readonly struct SurvivalView
        {
            public SurvivalView(Dictionary<ResourceType, long> net, SurvivalStatus energy, int energyPermille, SurvivalStatus oxygen,
                int outputPenalty, int timePenalty, long leakMilli)
            {
                Net = net;
                Energy = energy;
                EnergyPermille = energyPermille;
                Oxygen = oxygen;
                OutputPenalty = outputPenalty;
                TimePenalty = timePenalty;
                LeakMilli = leakMilli;
            }

            public Dictionary<ResourceType, long> Net { get; }
            public SurvivalStatus Energy { get; }
            public int EnergyPermille { get; }
            public SurvivalStatus Oxygen { get; }
            public int OutputPenalty { get; }
            public int TimePenalty { get; }
            public long LeakMilli { get; }
        }

        private SurvivalCatalogData SurvivalCatalog => _catalog.Survival;

        /// <summary>The running emergency, if the catalog lets emergencies count at all.</summary>
        private EmergencyState? ActiveEmergency =>
            _state.Emergency != null && SurvivalRules.EmergenciesOn(SurvivalCatalog) ? _state.Emergency : null;

        private bool IsBroken(PlacedBuilding building) =>
            ActiveEmergency is { Kind: EmergencyKinds.Breakdown } e && e.BuildingId == building.BuildingId;

        private Dictionary<ResourceType, long> NetMilliPerTick(DateTimeOffset at) => Survival(at).Net;

        /// <summary>
        /// The economy's rates at <paramref name="at"/> with the survival loop applied. The stores it reads are the
        /// current ones, which is right because the economy is always settled to the start of the step first.
        /// </summary>
        private SurvivalView Survival(DateTimeOffset at)
        {
            var rules = SurvivalCatalog;
            var on = SurvivalRules.On(rules);
            var inputs = Inputs(at);
            var bonuses = _modifiers.ProductionPercent;
            var caps = Capacities();
            long Stored(ResourceType type) => _state.Wallet.TryGetValue(type, out var have) ? have : 0;
            long Cap(ResourceType type) => caps.TryGetValue(type, out var cap) ? cap : 0;

            // 1. The energy chain: supply against demand at full rates.
            var permille = 1000;
            var energy = SurvivalStatus.Normal;
            if (on)
            {
                var (produced, consumed) = ProductionSimulator.FlowMilli(inputs, _production, bonuses, SurvivalEffects.None, ResourceType.Energy);
                permille = SurvivalRules.EnergyPermille(produced, consumed, Stored(ResourceType.Energy), rules);
                energy = SurvivalRules.EnergyStatus(produced, consumed, Stored(ResourceType.Energy), Cap(ResourceType.Energy), rules);
            }

            // 2. What is eaten and breathed on top of the buildings: the army (docs/rehber 07 §2.5; troops at home and out on
            // marches, not the wounded), the base crew, and a leak.
            var drains = new Dictionary<ResourceType, long>();
            var (food, oxygen) = TroopRules.Upkeep(_catalog.Troops, FedTroops());
            var perTick = SimulationTick.SecondsFromTicks(1) / 3600.0 * 1000.0;
            void Drain(ResourceType type, long milli)
            {
                if (milli > 0)
                    drains[type] = (drains.TryGetValue(type, out var have) ? have : 0) + milli;
            }

            Drain(ResourceType.Food, (long)Math.Round(food * perTick));
            Drain(ResourceType.Oxygen, (long)Math.Round(oxygen * perTick));
            Drain(ResourceType.Oxygen, SurvivalRules.CrewOxygenMilli(ReactorLevel(), rules));
            var leak = 0L;
            if (ActiveEmergency is { Kind: EmergencyKinds.Leak })
            {
                var gross = ProductionSimulator.FlowMilli(inputs, _production, bonuses, new SurvivalEffects(permille, 0), ResourceType.Oxygen).Produced;
                leak = SurvivalRules.LeakMilli(gross, rules);
                Drain(ResourceType.Oxygen, leak);
            }

            Dictionary<ResourceType, long> Net(SurvivalEffects effects)
            {
                var net = ProductionSimulator.ComputeNetMilliPerTick(inputs, _production, bonuses, effects);
                foreach (var pair in drains)
                    net[pair.Key] = (net.TryGetValue(pair.Key, out var have) ? have : 0) - pair.Value;
                return net;
            }

            // 3. Oxygen: the penalty never touches oxygen or energy output, so the status it is decided on cannot flip
            // because of the penalty itself, and building oxygen always gets the base out.
            var rates = Net(new SurvivalEffects(permille, 0));
            var oxygenStatus = SurvivalRules.OxygenStatus(rates.TryGetValue(ResourceType.Oxygen, out var o2) ? o2 : 0,
                Stored(ResourceType.Oxygen), Cap(ResourceType.Oxygen), rules);
            var penalty = SurvivalRules.OutputPenalty(oxygenStatus, rules);
            if (penalty > 0)
                rates = Net(new SurvivalEffects(permille, penalty));

            return new SurvivalView(rates, energy, permille, oxygenStatus, penalty, SurvivalRules.TimePenalty(oxygenStatus, rules), leak);
        }

        /// <summary>A timer started now: longer while oxygen is critical (constructions, researches, training).</summary>
        private int Slowed(int seconds, DateTimeOffset now) =>
            SurvivalRules.On(SurvivalCatalog) ? SurvivalRules.SlowSeconds(seconds, Survival(now).TimePenalty) : seconds;

        /// <summary>
        /// Advances the stores step by step: a store that runs dry on the way splits the step on that exact tick, so the
        /// energy chain or the oxygen penalty starts there (offline as online) and not a whole interval early or late.
        /// </summary>
        private void AdvanceStores(DateTimeOffset from, ulong ticks)
        {
            var caps = Capacities();
            var split = SurvivalRules.On(SurvivalCatalog);
            var remaining = ticks;
            for (var step = 0; remaining > 0UL; step++)
            {
                var net = NetMilliPerTick(from);
                var take = remaining;
                if (split && step < 6)
                {
                    foreach (var type in new[] { ResourceType.Energy, ResourceType.Oxygen })
                    {
                        var rate = net.TryGetValue(type, out var r) ? r : 0;
                        var stored = _state.Wallet.TryGetValue(type, out var s) ? s : 0;
                        var carry = _state.CarryMilli.TryGetValue(type, out var c) ? c : 0;
                        var until = SurvivalRules.TicksUntilEmpty(stored, carry, rate);
                        if (until > 0UL && until < take)
                            take = until;
                    }
                }

                var result = ProductionSimulator.AdvanceMilli(_state.Wallet, net, caps, take, _state.CarryMilli);
                remaining -= take;
                if (!result.HasChanges)
                    continue;

                _state.Wallet.Clear();
                foreach (var pair in result.Totals)
                    _state.Wallet[pair.Key] = pair.Value;
                _walletDirty = true;
            }
        }

        // --- Emergencies ---------------------------------------------------------------------------------------------

        /// <summary>
        /// One seeded roll per player and clock hour, only while connected (no emergency ever starts offline), past the
        /// guide, at the minimum reactor level, with none running and the cooldown over. Caller holds the lock and has
        /// settled the economy to <paramref name="now"/>.
        /// </summary>
        private void TryStartEmergency(DateTimeOffset now)
        {
            var rules = SurvivalCatalog;
            var hour = SurvivalRules.HourIndex(now);
            if (_state.EmergencyRollHour == hour)
                return;
            if (!SurvivalRules.MayStart(rules, _connection != null, _state.OnboardingStatus == OnboardingStatus.Active, ReactorLevel(),
                    _state.Emergency != null, _state.LastEmergencyAt, now))
                return;

            _state.EmergencyRollHour = hour;
            _state.IsDirty = true;
            if (!SurvivalRules.Rolls(PublicId, hour, rules.emergencies.chancePercent))
                return;

            StartEmergency(hour, now);
        }

        /// <summary>Starts the emergency of this hour's seed (tests call it directly). Caller holds the lock.</summary>
        private void StartEmergency(long hour, DateTimeOffset now, string? forcedKind = null)
        {
            // Producers that may break: finished, not the Core Reactor (a dead reactor would starve the whole base).
            var producers = _state.Buildings
                .Where(b => b.Level > 0 && b.DefinitionId != BuildingProgression.ReactorId && Produces(b, null))
                .OrderBy(b => b.BuildingId, StringComparer.Ordinal).ToList();
            var kind = forcedKind ?? SurvivalRules.Kind(PublicId, hour, producers.Count > 0);
            if (kind == EmergencyKinds.Breakdown && producers.Count == 0)
                kind = EmergencyKinds.Leak;
            PlacedBuilding? target;
            if (kind == EmergencyKinds.Breakdown)
            {
                target = producers[SurvivalRules.Pick(PublicId, hour, producers.Count)];
            }
            else
            {
                var vents = producers.Where(b => Produces(b, ResourceType.Oxygen)).ToList();
                target = vents.Count > 0
                    ? vents[SurvivalRules.Pick(PublicId, hour, vents.Count)]
                    : _state.Buildings.FirstOrDefault(b => b.DefinitionId == BuildingProgression.ReactorId);
            }

            _state.Emergency = new EmergencyState
            {
                Id = "e" + hour.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Kind = kind,
                BuildingId = target?.BuildingId ?? string.Empty,
                StartedAt = now,
                ExpiresAt = now.AddHours(Math.Max(1, SurvivalCatalog.emergencies.autoResolveHours))
            };
            _state.LastEmergencyAt = now;
            _state.IsDirty = true;
            _survivalDirty = true;
        }

        /// <summary>Whether a building produces anything (or <paramref name="resource"/>) at its level.</summary>
        private bool Produces(PlacedBuilding building, ResourceType? resource)
        {
            if (_production.TryGetLevel(building.DefinitionId, building.Level, out var produce, out _))
                return resource == null ? produce.Values.Any(v => v > 0) : produce.TryGetValue(resource.Value, out var v) && v > 0;
            return _production.GetRules(building.DefinitionId).Any(r => r != null && r.ProductionPerTick > 0 && (resource == null || r.Resource == resource));
        }

        /// <summary>Ends the emergency (repaired, fixed itself or switched off). Caller holds the lock.</summary>
        private void ResolveEmergency()
        {
            if (_state.Emergency == null)
                return;
            _state.Emergency = null;
            _state.IsDirty = true;
            _survivalDirty = true;
        }

        private void RepairEmergency(RepairEmergencyCommand command, DateTimeOffset now)
        {
            var emergency = ActiveEmergency;
            var cost = SurvivalRules.RepairCost(_catalog, ReactorLevel());
            var verdict = !SurvivalRules.EmergenciesOn(SurvivalCatalog) ? EmergencyRejectReason.SurvivalOff
                : emergency == null || (!string.IsNullOrEmpty(command.EmergencyId) && command.EmergencyId != emergency.Id) ? EmergencyRejectReason.NoEmergency
                : emergency.RepairEndsAt != null ? EmergencyRejectReason.AlreadyRepairing
                : !BuildingProgression.CanAfford(cost, _state.Wallet) ? EmergencyRejectReason.InsufficientResources
                : EmergencyRejectReason.None;
            if (verdict != EmergencyRejectReason.None)
            {
                Reject(command.CommandId, command.ClientSequence, verdict.ToString(), now);
                return;
            }

            Spend("repair", cost, now);
            var seconds = Math.Max(0, SurvivalCatalog.emergencies.repairSeconds);
            if (seconds == 0)
            {
                ResolveEmergency();
            }
            else
            {
                emergency!.RepairEndsAt = now.AddSeconds(seconds);
                emergency.RepairSeconds = seconds;
            }

            Record("repair");
            _state.IsDirty = true;
            _survivalDirty = false;
            SendDelta(now, new List<EntityChangeDto>
            {
                WalletChange(),
                ProductionChange(now),
                Accepted(command.CommandId, command.ClientSequence)
            });
        }

        /// <summary>The repair as an alliance help job (HelpKinds.Repair), or null when no repair is running.</summary>
        private HelpJobInfo? RepairJob(string emergencyId, DateTimeOffset now)
        {
            var emergency = ActiveEmergency;
            if (emergency == null || emergency.Id != emergencyId || emergency.RepairEndsAt is not { } ends || ends <= now)
                return null;
            var definition = FindBuilding(emergency.BuildingId)?.DefinitionId ?? string.Empty;
            return new HelpJobInfo(definition, 0, Math.Max(1, emergency.RepairSeconds), ends);
        }

        /// <summary>An alliance mate's help on the repair: it ends sooner, never before the economy's current point.</summary>
        private bool ShortenRepair(string emergencyId, int seconds, DateTimeOffset floor)
        {
            var emergency = ActiveEmergency;
            if (emergency == null || emergency.Id != emergencyId || emergency.RepairEndsAt is not { } ends || ends <= floor)
                return false;
            var cut = ends.AddSeconds(-seconds);
            emergency.RepairEndsAt = cut < floor ? floor : cut;
            _state.IsDirty = true;
            return true;
        }

        /// <summary>
        /// The tick's survival duties: emergencies switched off end at once, an online player may roll a new one, and an
        /// alarm that changed without a wallet change (a store pinned at 0) is still pushed. Caller holds the lock.
        /// </summary>
        private void TickSurvival(DateTimeOffset now, List<EntityChangeDto> changes)
        {
            if (_state.Emergency != null && !SurvivalRules.EmergenciesOn(SurvivalCatalog))
                ResolveEmergency();
            if (_connection != null)
                TryStartEmergency(now);

            var due = (now - _survivalCheckedAt).TotalSeconds >= SimulationTick.ReplicationIntervalSeconds;
            if (!_survivalDirty && !due)
                return;
            _survivalCheckedAt = now;
            if (!_survivalDirty && SurvivalSignature(now) == _survivalSent)
                return;
            _survivalDirty = false;
            if (!changes.Any(c => c.EntityId == EntityIds.ProductionSummary))
                changes.Add(ProductionChange(now));
        }

        private (SurvivalStatus, SurvivalStatus, string, bool) SurvivalSignature(DateTimeOffset now)
        {
            if (!SurvivalRules.On(SurvivalCatalog))
                return default;
            var view = Survival(now);
            var emergency = ActiveEmergency;
            return (view.Energy, view.Oxygen, emergency?.Id ?? string.Empty, emergency?.RepairEndsAt != null);
        }

        private SurvivalStateDto? SurvivalDto(SurvivalView view, DateTimeOffset now)
        {
            if (!SurvivalRules.On(SurvivalCatalog))
            {
                _survivalSent = default;
                return null;
            }

            var emergency = ActiveEmergency;
            _survivalSent = (view.Energy, view.Oxygen, emergency?.Id ?? string.Empty, emergency?.RepairEndsAt != null);
            var dto = new SurvivalStateDto
            {
                Energy = (int)view.Energy,
                EnergyPercent = view.EnergyPermille / 10,
                Oxygen = (int)view.Oxygen,
                OutputPenaltyPercent = view.OutputPenalty,
                TimePenaltyPercent = view.TimePenalty,
                LeakMilliPerTick = view.LeakMilli
            };
            if (emergency == null)
                return dto;

            dto.EmergencyId = emergency.Id;
            dto.EmergencyKind = emergency.Kind;
            dto.EmergencyBuildingId = emergency.BuildingId;
            dto.EmergencyDefinitionId = FindBuilding(emergency.BuildingId)?.DefinitionId ?? string.Empty;
            dto.ExpiresRemainingMs = Remaining(emergency.ExpiresAt, now);
            dto.RepairRemainingMs = Remaining(emergency.RepairEndsAt, now);
            dto.RepairTotalMs = emergency.RepairEndsAt.HasValue ? emergency.RepairSeconds * 1000L : 0L;
            dto.RepairCost = SurvivalRules.RepairCost(_catalog, ReactorLevel())
                .Select(p => new ResourceAmountEntry { Key = p.Key, Value = p.Value }).ToArray();
            return dto;
        }

        /// <summary>Tests: the survival state and net rates as the client would get them now.</summary>
        internal (SurvivalStateDto? Dto, Dictionary<ResourceType, long> Net) SurvivalForTests(DateTimeOffset now)
        {
            lock (_gate)
            {
                var view = Survival(now);
                return (SurvivalDto(view, now), view.Net);
            }
        }

        /// <summary>Tests: start this hour's emergency now, skipping the roll and its chance (the rules still apply).</summary>
        internal bool ForceEmergencyForTests(DateTimeOffset now, string? kind = null)
        {
            lock (_gate)
            {
                AdvanceTo(now);
                if (!SurvivalRules.MayStart(SurvivalCatalog, true, _state.OnboardingStatus == OnboardingStatus.Active, ReactorLevel(),
                        _state.Emergency != null, _state.LastEmergencyAt, now))
                    return false;
                StartEmergency(SurvivalRules.HourIndex(now), now, kind);
                return true;
            }
        }
    }
}
