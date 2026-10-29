using System;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using UnityEngine;

namespace Hadal.UI
{
    /// <summary>
    /// Paints the strategic map (docs/30 §6.4) into a texture: regions in their owner's colour with brighter borders,
    /// unowned water dark, and every base as a dot (cyan mine, blue alliance, pale red others). Two pixels per tile.
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

            var map = TerritoryRules.Map(size);
            var owner = new Color32[TerritoryRules.Cells * TerritoryRules.Cells];
            var border = new Color32[owner.Length];
            for (var i = 0; i < owner.Length; i++)
            {
                owner[i] = new Color32(6, 18, 28, 150);
                border[i] = new Color32(70, 100, 120, 200);
            }

            foreach (var s in territory.Strongholds ?? Array.Empty<StrongholdDto>())
            {
                if (s.Region < 0 || s.Region >= owner.Length || s.Color < 0)
                    continue;
                var c = AllianceColor(s.Color, 1f);
                owner[s.Region] = new Color(c.r * 0.55f, c.g * 0.55f, c.b * 0.55f, s.Hub ? 0.8f : 0.65f);
                border[s.Region] = new Color(Mathf.Min(1, c.r * 1.3f), Mathf.Min(1, c.g * 1.3f), Mathf.Min(1, c.b * 1.3f), 1f);
            }

            var width = size * Scale;
            var pixels = new Color32[width * width];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var r = map[y * size + x];
                    var edge = (x > 0 && map[y * size + x - 1] != r) || (x < size - 1 && map[y * size + x + 1] != r)
                               || (y > 0 && map[(y - 1) * size + x] != r) || (y < size - 1 && map[(y + 1) * size + x] != r);
                    Dot(pixels, width, x, y, edge ? border[r] : owner[r]);
                }
            }

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
