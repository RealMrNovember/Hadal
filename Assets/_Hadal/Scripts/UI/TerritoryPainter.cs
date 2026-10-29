using System;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using UnityEngine;

namespace Hadal.UI
{
    /// <summary>
    /// Paints the strategic map (docs/32 §2.5) into a texture: alliances' land in their colour with brighter edges, open
    /// water dark, cities as small dots, and every base as a dot (cyan mine, blue alliance, pale red others). Two pixels
    /// per tile.
    /// </summary>
    public static class TerritoryPainter
    {
        public const int Scale = 2;

        public static Color AllianceColor(int index, float alpha)
        {
            var rgb = TerritoryRules.Colors[Mathf.Clamp(index, 0, TerritoryRules.Colors.Length - 1)];
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);
        }

        /// <summary>Paints into <paramref name="texture"/> (made or remade at the right size) and returns it.</summary>
        public static Texture2D Paint(TerritoryDto territory, Texture2D texture)
        {
            var size = Math.Max(10, territory.MapSize);
            if (texture == null || texture.width != size * Scale)
                texture = new Texture2D(size * Scale, size * Scale, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };

            // The alliances' land (docs/32): each claimed tile in its owner's colour, brighter where the land ends.
            var owner = new int[size * size];
            for (var i = 0; i < owner.Length; i++)
                owner[i] = -1;
            foreach (var packed in territory.Land ?? Array.Empty<int>())
            {
                var lx = packed / 1_000_000;
                var ly = packed / 1_000 % 1_000;
                if (lx >= 0 && ly >= 0 && lx < size && ly < size)
                    owner[ly * size + lx] = packed % 1_000;
            }

            var colors = territory.LandColors ?? Array.Empty<int>();
            var width = size * Scale;
            var pixels = new Color32[width * width];
            var sea = new Color32(6, 18, 28, 110);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var o = owner[y * size + x];
                    if (o < 0 || o >= colors.Length)
                    {
                        Dot(pixels, width, x, y, sea);
                        continue;
                    }

                    var c = AllianceColor(colors[o], 1f);
                    var edge = x == 0 || y == 0 || x == size - 1 || y == size - 1 || owner[y * size + x - 1] != o || owner[y * size + x + 1] != o
                               || owner[(y - 1) * size + x] != o || owner[(y + 1) * size + x] != o;
                    Dot(pixels, width, x, y, edge
                        ? new Color(Mathf.Min(1, c.r * 1.3f), Mathf.Min(1, c.g * 1.3f), Mathf.Min(1, c.b * 1.3f), 1f)
                        : new Color(c.r * 0.55f, c.g * 0.55f, c.b * 0.55f, 0.6f));
                }
            }

            // Cities as small grey (or owner-coloured) dots.
            foreach (var city in territory.Strongholds ?? Array.Empty<StrongholdDto>())
                if (city.X >= 0 && city.Y >= 0 && city.X < size && city.Y < size)
                    Dot(pixels, width, city.X, city.Y, city.Color >= 0 ? (Color32)AllianceColor(city.Color, 1f) : new Color32(200, 200, 210, 255));

            foreach (var packed in territory.Bases ?? Array.Empty<int>())
            {
                var bx = packed / 1_000_000;
                var by = packed / 1_000 % 1_000;
                var relation = packed % 1_000;
                if (bx >= 0 && by >= 0 && bx < size && by < size)
                    Dot(pixels, width, bx, by, relation == 0 ? new Color32(80, 240, 255, 255) : relation == 1 ? new Color32(90, 150, 255, 255) : new Color32(255, 120, 110, 255));
            }

            texture.SetPixels32(pixels);
            texture.Apply(false);
            return texture;
        }

        private static void Dot(Color32[] pixels, int width, int x, int y, Color32 color)
        {
            for (var dy = 0; dy < Scale; dy++)
                for (var dx = 0; dx < Scale; dx++)
                    pixels[(y * Scale + dy) * width + x * Scale + dx] = color;
        }
    }
}
