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

        public static bool InView(int x, int y, int centerX, int centerY, int radius) =>
            Math.Abs(x - centerX) <= radius && Math.Abs(y - centerY) <= radius;
    }
}
