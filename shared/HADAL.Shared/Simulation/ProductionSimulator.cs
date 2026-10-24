using System.Collections.Generic;
using HADAL.Shared.Enums;

namespace HADAL.Shared.Simulation
{
    /// <summary>One placed building as the economy simulation sees it.</summary>
    public readonly struct BuildingProductionInput
    {
        public readonly string DefinitionId;
        public readonly int Level;

        public BuildingProductionInput(string definitionId, int level)
        {
            DefinitionId = definitionId;
            Level = level;
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
            var net = new Dictionary<ResourceType, long>();

            if (buildings == null || catalog == null)
                return net;

            for (var i = 0; i < buildings.Count; i++)
            {
                var building = buildings[i];
                var rules = catalog.GetRules(building.DefinitionId);

                for (var r = 0; r < rules.Count; r++)
                {
                    var rule = rules[r];
                    if (rule == null || rule.Resource == ResourceType.Unknown)
                        continue;

                    var produced = ProductionFormulas.ScaleByLevel(rule.ProductionPerTick, building.Level);
                    var consumed = ProductionFormulas.ScaleByLevel(rule.ConsumptionPerTick, building.Level);
                    var delta = produced - consumed;

                    if (delta == 0L)
                        continue;

                    net.TryGetValue(rule.Resource, out var current);
                    net[rule.Resource] = current + delta;
                }
            }

            return net;
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
