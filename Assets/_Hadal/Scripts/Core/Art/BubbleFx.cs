using UnityEngine;

namespace Hadal.Core.Art
{
    /// <summary>
    /// Bubble particles shared by everything that moves through the water: the world-map submarines
    /// (WorldMapView.Routes) and the base's Challengers (ChallengerVehicle). One procedural texture and
    /// material for all of them, drawn on first use.
    /// </summary>
    public static class BubbleFx
    {
        private static Material _material;

        /// <summary>A bubble: clear middle, bright rim and a small highlight.</summary>
        public static Material Material()
        {
            if (_material != null)
                return _material;
            const int n = 32;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (var y = 0; y < n; y++)
            {
                for (var x = 0; x < n; x++)
                {
                    var dx = (x + 0.5f) / n * 2f - 1f;
                    var dy = (y + 0.5f) / n * 2f - 1f;
                    var r = Mathf.Sqrt(dx * dx + dy * dy);
                    var rim = Mathf.Clamp01(1f - Mathf.Abs(r - 0.82f) / 0.16f);
                    var fill = r < 0.82f ? 0.18f : 0f;
                    var hx = dx + 0.35f;
                    var hy = dy - 0.35f;
                    var highlight = Mathf.Clamp01(1f - Mathf.Sqrt(hx * hx + hy * hy) / 0.22f);
                    var alpha = r > 1f ? 0f : Mathf.Max(Mathf.Max(rim, fill), highlight);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            _material = new Material(Shader.Find("Sprites/Default")) { mainTexture = texture };
            return _material;
        }

        /// <summary>Alpha ramp of a bubble: fades in, lingers, fades out.</summary>
        public static Gradient Fade()
        {
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0.7f, 0.6f), new GradientAlphaKey(0f, 1f) });
            return fade;
        }
    }
}
