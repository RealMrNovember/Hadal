using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// The curtain between the base and the world map (Last Asylum, docs/rehber 19 §6.2). Diving out to the world:
    /// the screen goes dark with a rush of bubbles, the logo and a short gauge show while the map is built, then the
    /// logo swells and fades as the world appears. Coming home: a quick dark dissolve. The swap happens behind the
    /// curtain, so neither scene is seen half-built.
    /// </summary>
    public sealed class SceneCurtain : MonoBehaviour
    {
        private static SceneCurtain _instance;

        private CanvasGroup _group;
        private RectTransform _logo;
        private RectTransform _bubbles;
        private Image _gaugeFill;
        private RectTransform _gauge;
        private bool _busy;

        public static bool Busy => _instance != null && _instance._busy;

        /// <summary>Out to the world: dark, logo and gauge, swap, the logo swells away.</summary>
        public static void ToWorld(Action swap) => Instance().Run(swap, true);

        /// <summary>Back to the base: a short dark dissolve.</summary>
        public static void ToBase(Action swap) => Instance().Run(swap, false);

        private static SceneCurtain Instance()
        {
            if (_instance != null)
                return _instance;
            var canvas = UiKit.Canvas("SceneCurtain", 250);
            DontDestroyOnLoad(canvas.gameObject);
            _instance = canvas.gameObject.AddComponent<SceneCurtain>();
            _instance.Build(canvas.transform);
            return _instance;
        }

        private void Build(Transform root)
        {
            _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            var dark = UiKit.Rect("Dark", root).Place(0, 0, 1, 1).gameObject.AddComponent<Image>();
            dark.color = new Color(0.004f, 0.02f, 0.035f, 1f);

            _bubbles = UiKit.Rect("Bubbles", root).Place(0, 0, 1, 1);
            for (var i = 0; i < 40; i++)
            {
                var bubble = UiKit.Rect("Bubble", _bubbles);
                var size = UnityEngine.Random.Range(10f, 34f);
                bubble.sizeDelta = new Vector2(size, size);
                var image = bubble.gameObject.AddComponent<Image>();
                image.sprite = UiKit.CircleRing;
                image.color = new Color(0.6f, 0.9f, 1f, UnityEngine.Random.Range(0.15f, 0.45f));
                image.raycastTarget = false;
            }

            _logo = UiKit.Rect("Logo", root).Place(0.1f, 0.44f, 0.9f, 0.6f);
            var logo = _logo.gameObject.AddComponent<Image>();
            logo.preserveAspect = true;
            logo.raycastTarget = false;
            logo.sprite = TitleScreenView.LogoSprite();
            logo.enabled = logo.sprite != null;

            var track = UiKit.Box("Gauge", root, new Color(0f, 0f, 0f, 0.6f));
            track.rectTransform.Place(0.25f, 0.38f, 0.75f, 0.39f);
            track.raycastTarget = false;
            _gauge = track.rectTransform;
            _gaugeFill = UiKit.Box("Fill", track.transform, new Color(1f, 0.7f, 0.24f, 1f));
            _gaugeFill.raycastTarget = false;
            gameObject.SetActive(false);
        }

        private void Run(Action swap, bool toWorld)
        {
            if (_busy)
            {
                swap?.Invoke();
                return;
            }

            gameObject.SetActive(true);
            StartCoroutine(Play(swap, toWorld));
        }

        private IEnumerator Play(Action swap, bool toWorld)
        {
            _busy = true;
            _group.blocksRaycasts = true;
            _logo.gameObject.SetActive(toWorld);
            _gauge.gameObject.SetActive(toWorld);
            _logo.localScale = Vector3.one;
            foreach (RectTransform bubble in _bubbles)
                bubble.anchorMin = bubble.anchorMax = new Vector2(UnityEngine.Random.value, UnityEngine.Random.Range(-0.6f, 0f));

            // Close: the dark rises with the bubbles.
            var close = toWorld ? 0.3f : 0.18f;
            for (var t = 0f; t < close; t += Time.unscaledDeltaTime)
            {
                _group.alpha = t / close;
                Rise(2.2f);
                yield return null;
            }

            _group.alpha = 1f;
            swap?.Invoke();

            // Out to the world, hold a moment on the logo while the map's first view arrives (the gauge fills).
            if (toWorld)
            {
                for (var t = 0f; t < 0.45f; t += Time.unscaledDeltaTime)
                {
                    _gaugeFill.rectTransform.Place(0, 0, Mathf.Clamp01(0.15f + t / 0.45f), 1);
                    Rise(1.4f);
                    yield return null;
                }
            }

            // Open: the logo swells and everything fades.
            var open = toWorld ? 0.55f : 0.3f;
            for (var t = 0f; t < open; t += Time.unscaledDeltaTime)
            {
                var k = t / open;
                _group.alpha = 1f - k * k;
                _logo.localScale = Vector3.one * (1f + 0.45f * k);
                Rise(1.4f);
                yield return null;
            }

            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _busy = false;
            gameObject.SetActive(false);
        }

        private void Rise(float speed)
        {
            foreach (RectTransform bubble in _bubbles)
            {
                var a = bubble.anchorMin;
                a.y += Time.unscaledDeltaTime * speed * (0.25f + bubble.sizeDelta.x * 0.01f);
                bubble.anchorMin = bubble.anchorMax = a;
            }
        }
    }
}
