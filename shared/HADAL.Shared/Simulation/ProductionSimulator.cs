using System;
using System.Collections.Generic;
using HADAL.Shared.Enums;

namespace HADAL.Shared.Simulation
{
    /// <summary>One placed building as the economy simulation sees it.</summary>
    public readonly struct BuildingProductionInput
    {
        public readonly string DefinitionId;
        public readonly int Level;

        /// <summary>Extra output of this one building in percent (Boost, Overdrive); consumption is unaffected.</summary>
        public readonly int BonusPercent;

        /// <summary>Broken down (an emergency, docs/35): it neither produces nor consumes until repaired.</summary>
        public readonly bool Stopped;

        public BuildingProductionInput(string definitionId, int level, int bonusPercent = 0, bool stopped = false)
        {
            DefinitionId = definitionId;
            Level = level;
            BonusPercent = bonusPercent;
            Stopped = stopped;
        }
    }

    /// <summary>Outcome of advancing the economy by a number of ticks.</summary>
    public sealed class ProductionTickResult
    {
        /// <summary>Authoritative absolute balances after the advance.</summary>
        public Dictionary<ResourceType, long> Totals { get; } = new Dictionary<ResourceType, long>();

        /// <summary>Net rate per tick at the end of the advance — HUD input only.</summary>
        public Dictionary<ResourceType, long> NetPerTick { get; } = new Dictionary<ResourceType, long>();

        public ulong TicksApplied { get; internal set; }

        public bool HasChanges { get; internal set; }
    }

    /// <summary>
    /// The single source of production truth. The real server calls this from its tick loop;
    /// the Unity stub calls the exact same method so behaviour cannot drift between them.
    /// Deterministic and side-effect free.
    /// </summary>
    public static class ProductionSimulator
    {
        /// <summary>
        /// Computes net per-tick rates for the placed buildings, with level scaling applied.
        /// </summary>
        public static Dictionary<ResourceType, long> ComputeNetPerTick(
            IReadOnlyList<BuildingProductionInput> buildings,
            ProductionCatalog catalog)
        {
            // Whole units per tick, truncated from the exact thousandths (one formula for both).
            var net = new Dictionary<ResourceType, long>();
            foreach (var pair in ComputeNetMilliPerTick(buildings, catalog))
            {
                var whole = pair.Value / ProductionFormulas.Milli;
                if (whole != 0L)
                    net[pair.Key] = whole;
            }

            return net;
        }

        /// <summary>
        /// Net rates in thousandths per tick: level scaling, each building's own bonus and the
        /// per-resource bonus (research) are applied before anything is rounded.
        /// </summary>
        public static Dictionary<ResourceType, long> ComputeNetMilliPerTick(
            IReadOnlyList<BuildingProductionInput> buildings,
            ProductionCatalog catalog,
            IReadOnlyDictionary<ResourceType, int>? resourceBonusPercent = null) =>
            ComputeNetMilliPerTick(buildings, catalog, resourceBonusPercent, SurvivalEffects.None);

        /// <summary>
        /// The net rates under the survival loop (docs/35): energy users run at <see cref="SurvivalEffects.EnergyPermille"/>
        /// (their own energy use rounded up, so a starved store stays at 0), and every production but oxygen and energy
        /// loses <see cref="SurvivalEffects.OutputPenaltyPercent"/>. Stopped buildings neither produce nor consume.
        /// </summary>
        public static Dictionary<ResourceType, long> ComputeNetMilliPerTick(
            IReadOnlyList<BuildingProductionInput> buildings,
            ProductionCatalog catalog,
            IReadOnlyDictionary<ResourceType, int>? resourceBonusPercent,
            SurvivalEffects effects)
        {
            var net = new Dictionary<ResourceType, long>();
            Walk(buildings, catalog, resourceBonusPercent, effects, (resource, produced, consumed) =>
            {
                var delta = produced - consumed;
                if (delta == 0L)
                    return;
                net.TryGetValue(resource, out var current);
                net[resource] = current + delta;
            });
            return net;
        }

        /// <summary>Gross production and consumption of one resource in thousandths per tick (the energy chain's supply and demand).</summary>
        public static (long Produced, long Consumed) FlowMilli(
            IReadOnlyList<BuildingProductionInput> buildings,
            ProductionCatalog catalog,
            IReadOnlyDictionary<ResourceType, int>? resourceBonusPercent,
            SurvivalEffects effects,
            ResourceType resource)
        {
            long produced = 0L, consumed = 0L;
            Walk(buildings, catalog, resourceBonusPercent, effects, (type, p, c) =>
            {
                if (type != resource)
                    return;
                produced += p;
                consumed += c;
            });
            return (produced, consumed);
        }

        /// <summary>Every building's production and consumption per resource, with bonuses and survival effects applied.</summary>
        private static void Walk(
            IReadOnlyList<BuildingProductionInput> buildings,
            ProductionCatalog catalog,
            IReadOnlyDictionary<ResourceType, int>? resourceBonusPercent,
            SurvivalEffects effects,
            Action<ResourceType, long, long> visit)
        {
            if (buildings == null || catalog == null)
                return;

            for (var i = 0; i < buildings.Count; i++)
            {
                var building = buildings[i];
                if (building.Stopped)
                    continue;

                // Balance-table buildings: hourly rates per level, bonuses on production only.
                if (catalog.TryGetLevel(building.DefinitionId, building.Level, out var produce, out var consume))
                {
                    var energyUser = effects.EnergyPermille < 1000 && consume.TryGetValue(ResourceType.Energy, out var energy) && energy > 0;
                    foreach (var pair in produce)
                    {
                        var bonus = building.BonusPercent;
                        if (resourceBonusPercent != null && resourceBonusPercent.TryGetValue(pair.Key, out var researched))
                            bonus += researched;
                        visit(pair.Key, Produced(pair.Key, pair.Value * (100L + bonus) / 100L, energyUser, effects), 0L);
                    }

                    foreach (var pair in consume)
                        visit(pair.Key, 0L, Consumed(pair.Key, pair.Value, energyUser, effects));

                    continue;
                }

                var rules = catalog.GetRules(building.DefinitionId);
                var formulaEnergyUser = false;
                if (effects.EnergyPermille < 1000)
                {
                    for (var r = 0; r < rules.Count; r++)
                    {
                        if (rules[r] != null && rules[r].Resource == ResourceType.Energy && rules[r].ConsumptionPerTick > 0)
                            formulaEnergyUser = true;
                    }
                }

                for (var r = 0; r < rules.Count; r++)
                {
                    var rule = rules[r];
                    if (rule == null || rule.Resource == ResourceType.Unknown)
                        continue;

                    var bonus = building.BonusPercent;
                    if (resourceBonusPercent != null && resourceBonusPercent.TryGetValue(rule.Resource, out var researched))
                        bonus += researched;

                    var produced = Produced(rule.Resource, ProductionFormulas.ScaleByLevelMilli(rule.ProductionPerTick, building.Level, bonus), formulaEnergyUser, effects);
                    var consumed = Consumed(rule.Resource, ProductionFormulas.ScaleByLevelMilli(rule.ConsumptionPerTick, building.Level), formulaEnergyUser, effects);
                    if (produced != 0L || consumed != 0L)
                        visit(rule.Resource, produced, consumed);
                }
            }
        }

        private static long Produced(ResourceType resource, long milli, bool energyUser, SurvivalEffects effects)
        {
            if (milli <= 0L)
                return milli;
            if (energyUser)
                milli = milli * effects.EnergyPermille / 1000L;
            if (effects.OutputPenaltyPercent > 0 && resource != ResourceType.Oxygen && resource != ResourceType.Energy)
                milli = milli * (100L - effects.OutputPenaltyPercent) / 100L;
            return milli;
        }

        // A slowed energy user's own energy use rounds up: supply never quite covers it, so the empty store stays empty.
        private static long Consumed(ResourceType resource, long milli, bool energyUser, SurvivalEffects effects)
        {
            if (!energyUser || milli <= 0L)
                return milli;
            var scaled = milli * effects.EnergyPermille;
            return resource == ResourceType.Energy ? (scaled + 999L) / 1000L : scaled / 1000L;
        }

        /// <summary>
        /// Advances balances by thousandth-per-tick rates. The fraction left over is kept in
        /// <paramref name="carryMilli"/> for the next call, so a rate of 2.5 per tick really gives
        /// 5 every 2 ticks; a balance pinned at 0 or at its cap drops its carry.
        /// </summary>
        public static ProductionTickResult AdvanceMilli(
            IReadOnlyDictionary<ResourceType, long> currentTotals,
            IReadOnlyDictionary<ResourceType, long> netMilliPerTick,
            IReadOnlyDictionary<ResourceType, long> capacities,
            ulong elapsedTicks,
            IDictionary<ResourceType, long> carryMilli)
        {
            var result = new ProductionTickResult { TicksApplied = elapsedTicks };
            if (currentTotals != null)
            {
                foreach (var pair in currentTotals)
                    result.Totals[pair.Key] = pair.Value;
            }

            if (netMilliPerTick == null)
                return result;

            foreach (var pair in netMilliPerTick)
                result.NetPerTick[pair.Key] = pair.Value / ProductionFormulas.Milli;

            if (elapsedTicks == 0UL)
                return result;

            foreach (var pair in netMilliPerTick)
            {
                carryMilli.TryGetValue(pair.Key, out var carry);
                var milli = ProductionFormulas.Accrue(pair.Value, elapsedTicks);
                milli = milli > long.MaxValue - carry ? long.MaxValue : milli + carry;
                var whole = milli / ProductionFormulas.Milli;
                var rest = milli - whole * ProductionFormulas.Milli;

                result.Totals.TryGetValue(pair.Key, out var before);
                var capacity = 0L;
                capacities?.TryGetValue(pair.Key, out capacity);
                var after = ProductionFormulas.ApplyToBalance(before, whole, capacity);

                // Clamped at 0 or at the cap: the fraction would only pile up against the wall.
                var clamped = after != before + whole;
                carryMilli[pair.Key] = clamped ? 0L : rest;

                if (after == before)
                    continue;

                result.Totals[pair.Key] = after;
                result.HasChanges = true;
            }

            return result;
        }

        /// <summary>
        /// Advances the economy by <paramref name="elapsedTicks"/> and returns the new
        /// absolute balances. Callers replicate <see cref="ProductionTickResult.Totals"/>
        /// as-is; incremental replication is deliberately not supported, because a dropped
        /// increment would silently desync a balance.
        /// </summary>
        /// <param name="currentTotals">Authoritative balances before the advance.</param>
        /// <param name="buildings">Placed buildings contributing production.</param>
        /// <param name="catalog">Per-definition production rules.</param>
        /// <param name="capacities">Per-resource storage caps; missing or non-positive means uncapped.</param>
        /// <param name="elapsedTicks">Ticks to advance. Clamp offline gaps before calling.</param>
        public static ProductionTickResult Advance(
            IReadOnlyDictionary<ResourceType, long> currentTotals,
            IReadOnlyList<BuildingProductionInput> buildings,
            ProductionCatalog catalog,
            IReadOnlyDictionary<ResourceType, long> capacities,
            ulong elapsedTicks)
        {
            var result = new ProductionTickResult { TicksApplied = elapsedTicks };

            if (currentTotals != null)
            {
                foreach (var pair in currentTotals)
                    result.Totals[pair.Key] = pair.Value;
            }

            var net = ComputeNetPerTick(buildings, catalog);
            foreach (var pair in net)
                result.NetPerTick[pair.Key] = pair.Value;

            if (elapsedTicks == 0UL)
                return result;

            foreach (var pair in net)
            {
                var change = ProductionFormulas.Accrue(pair.Value, elapsedTicks);
                if (change == 0L)
                    continue;

                result.Totals.TryGetValue(pair.Key, out var before);

                var capacity = 0L;
                capacities?.TryGetValue(pair.Key, out capacity);

                var after = ProductionFormulas.ApplyToBalance(before, change, capacity);
                if (after == before)
                    continue;

                result.Totals[pair.Key] = after;
                result.HasChanges = true;
            }

            return result;
        }
    }
}
