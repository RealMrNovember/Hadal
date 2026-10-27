using System;
using System.Collections.Generic;
using HADAL.Shared.Catalog;

namespace HADAL.Shared.Simulation
{
    /// <summary>Where bases go on the world map (docs/28).</summary>
    public static class WorldRules
    {
        /// <summary>
        /// The first free spot on a square spiral around the map centre, <see cref="WorldCatalogData.spacing"/>
        /// tiles apart, so new players land near each other and near the action.
        /// </summary>
        public static (int X, int Y) Place(WorldCatalogData world, Func<int, int, bool> taken)
        {
            var size = Math.Max(10, world.size);
            var step = Math.Max(1, world.spacing);
            var center = size / 2;
            int x = 0, y = 0, dx = 1, dy = 0, legLength = 1, legStep = 0, legsDone = 0;
            var limit = (size / step + 2) * (size / step + 2);
            for (var i = 0; i < limit; i++)
            {
                var tx = center + x * step;
                var ty = center + y * step;
                if (tx >= 1 && ty >= 1 && tx < size - 1 && ty < size - 1 && !taken(tx, ty))
                    return (tx, ty);

                x += dx;
                y += dy;
                if (++legStep == legLength)
                {
                    legStep = 0;
                    (dx, dy) = (-dy, dx);
                    if (++legsDone % 2 == 0)
                        legLength++;
                }
            }

            throw new InvalidOperationException("The world map is full.");
        }

        /// <summary>
        /// A random free tile in the first belt from the rim that allows bases (the Hadal Threshold, docs/rehber K-08),
        /// with no other base closer than <see cref="WorldCatalogData.spacing"/>. Falls back to <see cref="Place"/>
        /// when the catalog has no belts.
        /// </summary>
        public static (int X, int Y) PlaceInBelt(WorldCatalogData world, Func<int, int, bool> baseAt, Func<int, int, bool> nodeAt, Random random)
        {
            BeltData? belt = null;
            foreach (var b in world.belts)
            {
                if (b.bases)
                {
                    belt = b;
                    break;
                }
            }

            if (belt == null)
                return Place(world, baseAt);

            var size = Math.Max(10, world.size);
            var center = size / 2;
            var gap = Math.Max(0, world.spacing - 1);
            for (var attempt = 0; attempt < 5000; attempt++)
            {
                var d = random.Next(belt.inner, belt.outer + 1);
                var along = random.Next(-d, d + 1);
                var (x, y) = random.Next(4) switch
                {
                    0 => (center + along, center - d),
                    1 => (center + d, center + along),
                    2 => (center + along, center + d),
                    _ => (center - d, center + along)
                };
                if (x < 1 || y < 1 || x >= size - 1 || y >= size - 1 || nodeAt(x, y))
                    continue;

                var free = true;
                for (var dx = -gap; dx <= gap && free; dx++)
                    for (var dy = -gap; dy <= gap && free; dy++)
                        free = !baseAt(x + dx, y + dy);
                if (free)
                    return (x, y);
            }

            return Place(world, baseAt);
        }

        public static bool InView(int x, int y, int centerX, int centerY, int radius) =>
            Math.Abs(x - centerX) <= radius && Math.Abs(y - centerY) <= radius;
    }
}
