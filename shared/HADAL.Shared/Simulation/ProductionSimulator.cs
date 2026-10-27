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

        public BuildingProductionInput(string definitionId, int level, int bonusPercent = 0)
        {
            DefinitionId = definitionId;
            Level = level;
            BonusPercent = bonusPercent;
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
            IReadOnlyDictionary<ResourceType, int>? resourceBonusPercent = null)
        {
            var net = new Dictionary<ResourceType, long>();
            if (buildings == null || catalog == null)
                return net;

            for (var i = 0; i < buildings.Count; i++)
            {
                var building = buildings[i];

                // Balance-table buildings: hourly rates per level, bonuses on production only.
                if (catalog.TryGetLevel(building.DefinitionId, building.Level, out var produce, out var consume))
                {
                    foreach (var pair in produce)
                    {
                        var bonus = building.BonusPercent;
                        if (resourceBonusPercent != null && resourceBonusPercent.TryGetValue(pair.Key, out var researched))
                            bonus += researched;
                        net.TryGetValue(pair.Key, out var current);
                        net[pair.Key] = current + pair.Value * (100L + bonus) / 100L;
                    }

                    foreach (var pair in consume)
                    {
                        net.TryGetValue(pair.Key, out var current);
                        net[pair.Key] = current - pair.Value;
                    }

                    continue;
                }

                var rules = catalog.GetRules(building.DefinitionId);
                for (var r = 0; r < rules.Count; r++)
                {
                    var rule = rules[r];
                    if (rule == null || rule.Resource == ResourceType.Unknown)
                        continue;

                    var bonus = building.BonusPercent;
                    if (resourceBonusPercent != null && resourceBonusPercent.TryGetValue(rule.Resource, out var researched))
                        bonus += researched;

                    var delta = ProductionFormulas.ScaleByLevelMilli(rule.ProductionPerTick, building.Level, bonus)
                                - ProductionFormulas.ScaleByLevelMilli(rule.ConsumptionPerTick, building.Level);
                    if (delta == 0L)
                        continue;

                    net.TryGetValue(rule.Resource, out var current);
                    net[rule.Resource] = current + delta;
                }
            }

            return net;
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
