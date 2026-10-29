using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Catalog;

namespace HADAL.Shared.Simulation
{
    /// <summary>
    /// Alliance territory (docs/30 §6): the map is cut into blocky regions around jittered seeds on a 10 × 10 grid (a tile
    /// belongs to the nearest seed by Chebyshev distance, ties to the lower index). Deterministic, so the client draws
    /// the same regions the server uses. Each region has a Stronghold at its seed; its level follows the belt.
    /// </summary>
    public static class TerritoryRules
    {
        public const int Cells = 10;
        public const int Jitter = 5;

        /// <summary>Hadal Tahtı, the centre stronghold: held by the trench until a later event.</summary>
        public const int ThroneLevel = 7;

        public const int MaxPlantLevel = 2;
        public const int MaxRegions = 12;
        public const double GatherBonus = 0.25;
        public const double RefillHours = 12;
        public static readonly TimeSpan HubMoveCooldown = TimeSpan.FromHours(24);

        public const string Plant = "plant";
        public const string Move = "move";
        public const string Abandon = "abandon";

        /// <summary>Regions an alliance may hold: one plus one for every three members, up to <see cref="MaxRegions"/>.</summary>
        public static int Allowed(int members) => Math.Min(MaxRegions, 1 + Math.Max(0, members) / 3);

        public static string IdOf(int region) => "k" + region;

        public static bool TryParseId(string? id, out int region)
        {
            region = -1;
            return id != null && id.Length > 1 && id[0] == 'k' && int.TryParse(id.Substring(1), out region) && region >= 0 && region < Cells * Cells;
        }

        private static int Hash(int i, int salt)
        {
            unchecked
            {
                var h = (uint)(i * 73856093) ^ (uint)(salt * 19349663) ^ 0x9E3779B9u;
                h ^= h >> 15;
                h *= 0x2C1B3C6Du;
                h ^= h >> 12;
                return (int)(h & 0x7FFFFFFF);
            }
        }

        /// <summary>The seed tile of every region, index = cy * Cells + cx.</summary>
        public static (int X, int Y)[] Seeds(int size)
        {
            var cell = size / (double)Cells;
            var seeds = new (int, int)[Cells * Cells];
            for (var i = 0; i < seeds.Length; i++)
            {
                var cx = i % Cells;
                var cy = i / Cells;
                var x = (int)Math.Floor(cx * cell + cell / 2) + Hash(i, 1) % (2 * Jitter + 1) - Jitter;
                var y = (int)Math.Floor(cy * cell + cell / 2) + Hash(i, 2) % (2 * Jitter + 1) - Jitter;
                seeds[i] = (Math.Clamp(x, 0, size - 1), Math.Clamp(y, 0, size - 1));
            }

            return seeds;
        }

        private static readonly Dictionary<int, int[]> MapCache = new();

        /// <summary>The region of every tile (index y * size + x). Cached per map size.</summary>
        public static int[] Map(int size)
        {
            lock (MapCache)
            {
                if (MapCache.TryGetValue(size, out var cached))
                    return cached;
                var seeds = Seeds(size);
                var cell = size / (double)Cells;
                var map = new int[size * size];
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var cx = Math.Min(Cells - 1, (int)(x / cell));
                        var cy = Math.Min(Cells - 1, (int)(y / cell));
                        var best = -1;
                        var bestD = int.MaxValue;
                        for (var ny = Math.Max(0, cy - 1); ny <= Math.Min(Cells - 1, cy + 1); ny++)
                        {
                            for (var nx = Math.Max(0, cx - 1); nx <= Math.Min(Cells - 1, cx + 1); nx++)
                            {
                                var i = ny * Cells + nx;
                                var d = Math.Max(Math.Abs(seeds[i].X - x), Math.Abs(seeds[i].Y - y));
                                if (d < bestD || (d == bestD && i < best))
                                {
                                    bestD = d;
                                    best = i;
                                }
                            }
                        }

                        map[y * size + x] = best;
                    }
                }

                MapCache[size] = map;
                return map;
            }
        }

        public static int RegionOf(int size, int x, int y) =>
            x < 0 || y < 0 || x >= size || y >= size ? -1 : Map(size)[y * size + x];

        private static readonly Dictionary<int, HashSet<int>[]> AdjacencyCache = new();

        /// <summary>Regions that share an edge with each region.</summary>
        public static HashSet<int>[] Adjacency(int size)
        {
            lock (AdjacencyCache)
            {
                if (AdjacencyCache.TryGetValue(size, out var cached))
                    return cached;
                var map = Map(size);
                var result = Enumerable.Range(0, Cells * Cells).Select(_ => new HashSet<int>()).ToArray();
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var a = map[y * size + x];
                        if (x + 1 < size && map[y * size + x + 1] is var b && b != a)
                        {
                            result[a].Add(b);
                            result[b].Add(a);
                        }

                        if (y + 1 < size && map[(y + 1) * size + x] is var c && c != a)
                        {
                            result[a].Add(c);
                            result[c].Add(a);
                        }
                    }
                }

                AdjacencyCache[size] = result;
                return result;
            }
        }

        /// <summary>A stronghold's level: by the belt of its seed (outer 1 … core 6); the centre region is the Throne (7).</summary>
        public static int Level(WorldCatalogData world, int region)
        {
            var size = Math.Max(10, world.size);
            var seeds = Seeds(size);
            var center = GatherRules.Center(world);
            if (RegionOf(size, center, center) == region)
                return ThroneLevel;
            var (x, y) = seeds[region];
            var belt = GatherRules.BeltOf(world, x, y);
            if (belt == null)
                return 1;
            // Belts run outside in; the outermost is level 1.
            var order = world.belts.OrderByDescending(b => b.outer).Select(b => b.id).ToList();
            return Math.Clamp(order.IndexOf(belt.id) + 1, 1, ThroneLevel - 1);
        }

        /// <summary>The garrison of a stronghold at full strength: the crawler army three times over at level 3L+1.</summary>
        public static BattleSide Garrison(int level, double share = 1)
        {
            var kind = MonsterRules.Kinds[0];
            var army = MonsterRules.Army(kind, Math.Min(kind.MaxLevel, 3 * level + 1));
            foreach (var key in army.Troops.Keys.ToList())
                army.Troops[key] = Math.Max(1, Math.Round(army.Troops[key] * 3 * Math.Clamp(share, 0, 1)));
            return army;
        }

        /// <summary>The garrison's strength share at <paramref name="now"/>: it refills linearly over <see cref="RefillHours"/>.</summary>
        public static double GarrisonShare(double share, DateTimeOffset at, DateTimeOffset now) =>
            Math.Min(1, share + Math.Max(0, (now - at).TotalHours) / RefillHours);

        /// <summary>Twelve alliance colours (RGB) for the strategic map.</summary>
        public static readonly int[] Colors =
        {
            0x4FC3F7, 0xEF5350, 0x66BB6A, 0xFFCA28, 0xAB47BC, 0xFF7043,
            0x26A69A, 0xEC407A, 0x9CCC65, 0x5C6BC0, 0xFFA726, 0x8D6E63
        };

        /// <summary>A stable colour index for an alliance id.</summary>
        public static int ColorOf(string allianceId)
        {
            if (string.IsNullOrEmpty(allianceId))
                return -1;
            unchecked
            {
                var h = 17;
                foreach (var c in allianceId)
                    h = h * 31 + c;
                return (int)((uint)h % (uint)Colors.Length);
            }
        }
    }
}
