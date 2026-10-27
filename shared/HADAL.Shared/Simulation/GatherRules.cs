using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Catalog;
using HADAL.Shared.Enums;

namespace HADAL.Shared.Simulation
{
    /// <summary>
    /// Resource nodes and gathering marches (docs/rehber 09 §4). A march travels to a node, gathers until its
    /// load is full or the node is empty, and carries the load home; the wallet gets it on arrival.
    /// </summary>
    public static class GatherRules
    {
        public static int Center(WorldCatalogData world) => Math.Max(10, world.size) / 2;

        /// <summary>Chebyshev distance to the map centre (docs/rehber K-02).</summary>
        public static int Distance(WorldCatalogData world, int x, int y) =>
            Math.Max(Math.Abs(x - Center(world)), Math.Abs(y - Center(world)));

        public static int Tiles(int x1, int y1, int x2, int y2) => Math.Max(Math.Abs(x1 - x2), Math.Abs(y1 - y2));

        public static BeltData? BeltOf(WorldCatalogData world, int x, int y)
        {
            var d = Distance(world, x, y);
            return world.belts.FirstOrDefault(b => d >= b.inner && d <= b.outer);
        }

        /// <summary>Marches the player may have out at once (one per Submarine Hangar).</summary>
        public static int Queues(GatherCatalogData gather, int reactorLevel) => gather.queueReactors.Count(r => r <= reactorLevel);

        /// <summary>Divers aboard one march; they stand in for troops until the army arrives (docs/rehber 07).</summary>
        public static int Crew(int reactorLevel) => 10 + 5 * Math.Max(1, reactorLevel);

        public static long Load(GatherCatalogData gather, int reactorLevel) =>
            gather.loadByReactor.Length == 0 ? 0
                : (long)Math.Round(gather.loadByReactor[Math.Clamp(reactorLevel, 0, gather.loadByReactor.Length - 1)] * Scale(gather));

        private static double Scale(GatherCatalogData gather) => gather.yieldScale > 0 ? gather.yieldScale : 1;

        public static int TravelSeconds(WorldCatalogData world, int x1, int y1, int x2, int y2) =>
            Math.Max(1, Tiles(x1, y1, x2, y2)) * Math.Max(1, world.marchSecondsPerTile);

        public static double Weight(WorldCatalogData world, ResourceType resource)
        {
            foreach (var w in world.loadWeights)
                if (Enum.TryParse(w.resource, out ResourceType r) && r == resource)
                    return w.weight;
            return 1;
        }

        public static NodeTypeData? Type(WorldCatalogData world, string typeId) => world.nodeTypes.FirstOrDefault(t => t.id == typeId);

        public static NodeLevelData? Level(WorldCatalogData world, int level) => world.nodeLevels.FirstOrDefault(l => l.level == level);

        /// <summary>Load a fresh node of this kind and level holds.</summary>
        public static long Capacity(WorldCatalogData world, GatherCatalogData gather, string typeId, int level) =>
            (long)Math.Round((Level(world, level)?.capacity ?? 0) * (Type(world, typeId)?.capacityFactor ?? 1) * Scale(gather));

        /// <summary>
        /// Units per hour one march gathers at a node: the guide's yield with the typical bonus of the level.
        /// Before the Alloy Foundry (<see cref="GatherCatalogData.alloyReactor"/>) the Superalloy share comes as Titanium.
        /// </summary>
        public static Dictionary<ResourceType, double> Rates(WorldCatalogData world, GatherCatalogData gather, string typeId, int level, int reactorLevel)
        {
            var result = new Dictionary<ResourceType, double>();
            var yield = Type(world, typeId)?.yields.FirstOrDefault(y => y.level == level);
            if (yield == null)
                return result;
            var bonus = Level(world, level)?.bonus ?? 1;
            foreach (var rate in yield.rates)
            {
                if (!Enum.TryParse(rate.resource, out ResourceType resource))
                    continue;
                var perHour = rate.perHour * bonus * (resource == ResourceType.Hadalite ? 1 : Scale(gather));
                if (resource == ResourceType.Alloy && reactorLevel < gather.alloyReactor)
                {
                    resource = ResourceType.Titanium;
                    perHour *= Weight(world, ResourceType.Alloy);
                }

                result[resource] = (result.TryGetValue(resource, out var have) ? have : 0) + perHour;
            }

            return result;
        }

        /// <summary>Load per hour the rates fill (Hadalite weighs nothing).</summary>
        public static double LoadPerHour(WorldCatalogData world, IReadOnlyDictionary<ResourceType, double> rates) =>
            rates.Sum(r => r.Value * Weight(world, r.Key));

        /// <summary>Seconds to gather <paramref name="load"/> at these rates.</summary>
        public static int GatherSeconds(WorldCatalogData world, IReadOnlyDictionary<ResourceType, double> rates, long load)
        {
            var perHour = LoadPerHour(world, rates);
            return perHour <= 0 ? 0 : (int)Math.Floor(load / perHour * 3600); // rounding down keeps the cargo within the load
        }

        /// <summary>What <paramref name="seconds"/> of gathering yields, whole units.</summary>
        public static Dictionary<ResourceType, long> Gathered(IReadOnlyDictionary<ResourceType, double> rates, double seconds)
        {
            var result = new Dictionary<ResourceType, long>();
            foreach (var r in rates)
            {
                var amount = (long)Math.Floor(r.Value * seconds / 3600);
                if (amount > 0)
                    result[r.Key] = amount;
            }

            return result;
        }

        /// <summary>Load the gathered amounts take.</summary>
        public static long LoadOf(WorldCatalogData world, IReadOnlyDictionary<ResourceType, long> amounts) =>
            (long)Math.Ceiling(amounts.Sum(a => a.Value * Weight(world, a.Key)));

        /// <summary>A node level for a tile of this belt and kind: deeper in the belt, higher the level.</summary>
        public static int PickLevel(WorldCatalogData world, BeltData belt, NodeTypeData type, int distance, Random random)
        {
            var lo = Math.Max(belt.nodeMin, type.minLevel);
            var hi = Math.Min(belt.nodeMax, type.maxLevel);
            var levels = Enumerable.Range(lo, Math.Max(0, hi - lo + 1)).Where(l => type.yields.Any(y => y.level == l)).ToList();
            if (levels.Count == 0)
                return 0;
            var depth = belt.outer == belt.inner ? 1.0 : (belt.outer - distance) / (double)(belt.outer - belt.inner);
            var index = (int)Math.Round(depth * (levels.Count - 1) + (random.NextDouble() - 0.5) * 1.5);
            return levels[Math.Clamp(index, 0, levels.Count - 1)];
        }

        /// <summary>A node kind for a belt, by the guide's mix.</summary>
        public static NodeTypeData? PickType(WorldCatalogData world, BeltData belt, Random random, Func<string, bool>? allowed = null)
        {
            var mix = world.density.FirstOrDefault(d => d.belt == belt.id);
            if (mix == null)
                return null;
            var options = mix.weights.Where(w => allowed == null || allowed(w.type)).ToList();
            var total = options.Sum(w => w.percent);
            if (total <= 0)
                return null;
            var roll = random.Next(total);
            foreach (var w in options)
            {
                if ((roll -= w.percent) < 0)
                    return Type(world, w.type);
            }

            return null;
        }
    }
}
