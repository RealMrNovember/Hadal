using System;
using System.Collections.Generic;
using Hadal.Core.Services;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>What the incoming-attack alarm is showing.</summary>
    public enum AlarmMood
    {
        None,

        /// <summary>A hostile attack or rally on its way and nothing to stop it: deep red, quickening pulse.</summary>
        Threat,

        /// <summary>A scout on its way: amber, slow.</summary>
        Scout,

        /// <summary>A threat is on its way but our shield will block it: calm teal breathing.</summary>
        Shielded
    }

    /// <summary>
    /// The alarm's timing, kept free of Unity objects so it can be tested: how strong and how fast the edge vignette
    /// pulses for the time left, and the pulse shape itself (a heartbeat "lub-dub" with an eased attack and a soft
    /// decay, never an on/off blink). Intensity is the vignette's opacity, 0..1; it never reaches a full-screen red.
    /// </summary>
    public static class AlarmTempo
    {
        /// <summary>(seconds left, period, peak, floor): calm far out, faster under 30 s, strong under 10 s.</summary>
        private static readonly (float Left, float Period, float Peak, float Floor)[] Keys =
        {
            (120f, 2.8f, 0.40f, 0.14f),
            (60f, 2.1f, 0.50f, 0.18f),
            (30f, 1.4f, 0.64f, 0.23f),
            (10f, 0.9f, 0.80f, 0.29f),
            (0f, 0.72f, 0.88f, 0.33f)
        };

        /// <summary>The strongest the vignette ever gets outside the impact flash.</summary>
        public const float MaxIntensity = 0.88f;

        /// <summary>Pulse period, peak and resting floor for a threat this many seconds away (smoothly interpolated).</summary>
        public static (float Period, float Peak, float Floor) Threat(float secondsLeft)
        {
            if (secondsLeft >= Keys[0].Left)
                return (Keys[0].Period, Keys[0].Peak, Keys[0].Floor);
            for (var i = 1; i < Keys.Length; i++)
            {
                if (secondsLeft < Keys[i].Left)
                    continue;
                var a = Keys[i - 1];
                var b = Keys[i];
                var t = Mathf.InverseLerp(a.Left, b.Left, secondsLeft);
                return (Mathf.Lerp(a.Period, b.Period, t), Mathf.Lerp(a.Peak, b.Peak, t), Mathf.Lerp(a.Floor, b.Floor, t));
            }

            var last = Keys[^1];
            return (last.Period, last.Peak, last.Floor);
        }

        /// <summary>0 far out .. 1 at impact: how urgent the threat is (drives the second beat and the ping rate).</summary>
        public static float Urgency(float secondsLeft) => 1f - Mathf.Clamp01(secondsLeft / 60f);

        /// <summary>
        /// A heartbeat over one period (<paramref name="phase"/> 0..1): a quick eased rise, a soft eased fall, and a
        /// second, smaller beat whose weight grows with <paramref name="urgency"/>. Continuous across the wrap at 1 → 0.
        /// </summary>
        public static float Heartbeat(float phase, float urgency)
        {
            phase -= Mathf.Floor(phase);
            var lub = Beat(phase, 0f, 0.08f, 0.34f);
            var dub = Beat(phase, 0.17f, 0.07f, 0.36f) * Mathf.Lerp(0.25f, 0.6f, Mathf.Clamp01(urgency));
            return 1f - (1f - lub) * (1f - dub);
        }

        /// <summary>A slow breath (shield, scout): a raised cosine, 0..1..0 over one period.</summary>
        public static float Breath(float phase) => 0.5f - 0.5f * Mathf.Cos(phase * Mathf.PI * 2f);

        private static float Beat(float phase, float start, float rise, float fall)
        {
            var t = phase - start;
            if (t <= 0f)
                return 0f;
            if (t < rise)
            {
                var u = 1f - t / rise;
                return 1f - u * u * u; // ease-out cubic
            }

            var f = Mathf.Clamp01((t - rise) / fall);
            return 0.5f + 0.5f * Mathf.Cos(f * Mathf.PI); // ease-in-out sine back to rest
        }

        /// <summary>Seconds between low sonar pings: rarer far out, every few seconds near impact.</summary>
        public static float PingInterval(float secondsLeft) =>
            secondsLeft > 60f ? 12f : secondsLeft > 30f ? 8f : secondsLeft > 10f ? 5f : 3f;

        /// <summary>m:ss under an hour, h:mm:ss above (the banner's countdown).</summary>
        public static string Clock(float seconds)
        {
            var total = (long)Mathf.Max(0f, Mathf.Ceil(seconds));
            var h = total / 3600;
            var m = total / 60 % 60;
            var s = total % 60;
            return h > 0 ? $"{h}:{m:00}:{s:00}" : $"{m:00}:{s:00}";
        }
    }

    /// <summary>
    /// The full-screen edge vignette of the alarm: two 9-sliced bands with no centre (only the screen edges are drawn,
    /// so it costs a thin ring of overdraw on phones). A wide black band deepens the corners; a narrower colour band
    /// glows deep red at the rim and amber further in. It sits under the HUD canvases so it never washes out the HUD.
    /// Opacity changes go through CanvasRenderer alpha, so a pulse rebuilds no mesh.
    /// </summary>
    public sealed class AlertVignette : MonoBehaviour
    {
        private Image _shade;
        private Image _glow;
        private AlarmMood _mood = AlarmMood.None;
        private float _envelope;
        private float _phase;
        private float _flash;
        private float _shown;

        /// <summary>The last opacity drawn (tests, screenshots).</summary>
        public float Intensity => _shown;

        public static AlertVignette Create(Transform parent)
        {
            var canvas = UiKit.Canvas("AlarmVignette", 33);
            canvas.transform.SetParent(parent, false);
            canvas.GetComponent<GraphicRaycaster>().enabled = false;
            var vignette = canvas.gameObject.AddComponent<AlertVignette>();
            vignette._shade = Band(canvas.transform, "Shade", Shade, 0.17f);
            vignette._glow = Band(canvas.transform, "Glow", Red, 0.26f);
            vignette.Apply(0f, 0f);
            return vignette;
        }

        private static Image Band(Transform parent, string name, Sprite sprite, float pixelsPerUnit)
        {
            var image = UiKit.Rect(name, parent).Place(0, 0, 1, 1).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.fillCenter = false;
            // The sprite's 63 px border drawn 63 / multiplier reference pixels wide.
            image.pixelsPerUnitMultiplier = pixelsPerUnit;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Starts the impact flash: the rim flares and settles over about half a second.</summary>
        public void Flash() => _flash = GameSettings.ReduceAlertEffects ? 0.45f : 1f;

        /// <summary>Called every frame by the alarm with the mood and the seconds to the first arrival.</summary>
        public void Tick(AlarmMood mood, float secondsLeft, float dt)
        {
            if (mood != AlarmMood.None && mood != _mood)
            {
                _glow.sprite = mood == AlarmMood.Shielded ? Teal : mood == AlarmMood.Scout ? Amber : Red;
                _mood = mood;
            }

            // The whole effect eases in over ~0.6 s and out over ~0.8 s: it returns to rest, never snaps off.
            var target = mood == AlarmMood.None ? 0f : 1f;
            _envelope = Mathf.MoveTowards(_envelope, target, dt / (target > 0f ? 0.6f : 0.8f));
            var envelope = _envelope * _envelope * (3f - 2f * _envelope);
            _flash = Mathf.Max(0f, _flash - dt * 2.2f);

            float value;
            var reduce = GameSettings.ReduceAlertEffects;
            switch (_mood)
            {
                case AlarmMood.Threat:
                {
                    var (period, peak, floor) = AlarmTempo.Threat(secondsLeft);
                    _phase += dt / period;
                    value = reduce ? Mathf.Lerp(floor, peak, 0.45f) : Mathf.Lerp(floor, peak, AlarmTempo.Heartbeat(_phase, AlarmTempo.Urgency(secondsLeft)));
                    break;
                }
                case AlarmMood.Scout:
                    _phase += dt / 3f;
                    value = reduce ? 0.2f : Mathf.Lerp(0.1f, 0.3f, AlarmTempo.Breath(_phase));
                    break;
                case AlarmMood.Shielded:
                    _phase += dt / 4.5f;
                    value = reduce ? 0.2f : Mathf.Lerp(0.12f, 0.3f, AlarmTempo.Breath(_phase));
                    break;
                default:
                    value = 0f;
                    break;
            }

            _phase -= Mathf.Floor(_phase);
            var flash = _flash * _flash;
            Apply(Mathf.Max(value * envelope, flash), Mathf.Max(envelope, flash));
            if (_envelope <= 0f && _flash <= 0f)
                _mood = AlarmMood.None;
        }

        private void Apply(float glow, float shade)
        {
            _shown = glow;
            _glow.canvasRenderer.SetAlpha(Mathf.Clamp01(glow));
            // The dark band breathes a little with the glow, so the pulse reads as depth rather than a tint.
            _shade.canvasRenderer.SetAlpha(Mathf.Clamp01(shade * (0.38f + 0.3f * glow)));
            var visible = glow > 0.001f || shade > 0.001f;
            if (_glow.enabled != visible)
                _glow.enabled = _shade.enabled = visible;
        }

        // --- Procedural sprites ----------------------------------------------------------------------------------------

        private static Sprite _red;
        private static Sprite _teal;
        private static Sprite _amber;
        private static Sprite _shadeSprite;

        private static Sprite Red => _red != null ? _red : _red = Make("HadalVignetteRed", new Color(0.62f, 0.02f, 0.05f), new Color(1f, 0.42f, 0.14f), 2.1f);
        private static Sprite Teal => _teal != null ? _teal : _teal = Make("HadalVignetteTeal", new Color(0.02f, 0.4f, 0.5f), new Color(0.35f, 0.95f, 1f), 2.3f);
        private static Sprite Amber => _amber != null ? _amber : _amber = Make("HadalVignetteAmber", new Color(0.6f, 0.3f, 0.02f), new Color(1f, 0.75f, 0.3f), 2.3f);
        private static Sprite Shade => _shadeSprite != null ? _shadeSprite : _shadeSprite = Make("HadalVignetteShade", Color.black, Color.black, 1.6f);

        /// <summary>
        /// 128² with a 63 px border on every side. Each edge fades with a soft power curve; the two directions combine
        /// as a screen blend (1 − (1 − x)(1 − y)), so corners are a little denser than the edges. Colour runs from
        /// <paramref name="rim"/> at the screen edge to <paramref name="inner"/> where it fades out.
        /// </summary>
        public static Sprite Make(string name, Color rim, Color inner, float falloff, bool readable = false)
        {
            const int size = 128;
            const int border = 63;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = name
            };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                var ey = Edge(y, size, border);
                for (var x = 0; x < size; x++)
                {
                    var ex = Edge(x, size, border);
                    var gx = Mathf.Pow(ex, falloff);
                    var gy = Mathf.Pow(ey, falloff);
                    var a = 1f - (1f - gx) * (1f - gy);
                    var c = Color.Lerp(inner, rim, Mathf.Pow(a, 0.7f));
                    pixels[y * size + x] = new Color(c.r, c.g, c.b, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, !readable);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
        }

        /// <summary>1 at the texture's edge, 0 at the inner end of the border.</summary>
        private static float Edge(int i, int size, int border)
        {
            var d = Mathf.Min(i + 0.5f, size - i - 0.5f);
            return Mathf.Clamp01(1f - d / border);
        }
    }

    /// <summary>
    /// Digits in fixed-width cells (tabular figures) so a countdown does not jitter as "1" and "8" change places: one
    /// Text per character, right-aligned, colons in narrower cells.
    /// </summary>
    public sealed class TabularClock : MonoBehaviour
    {
        private readonly List<Text> _cells = new();
        private RectTransform _rect;
        private int _size;
        private Color _color;
        private string _text = string.Empty;

        public static TabularClock Create(Transform parent, int size, Color color)
        {
            var rect = UiKit.Rect("Clock", parent);
            var clock = rect.gameObject.AddComponent<TabularClock>();
            clock._rect = rect;
            clock._size = Mathf.RoundToInt(size * UiKit.Scale);
            clock._color = color;
            return clock;
        }

        public Color Color
        {
            set
            {
                _color = value;
                foreach (var cell in _cells)
                    cell.color = value;
            }
        }

        public string Text
        {
            get => _text;
            set
            {
                if (value == _text)
                    return;
                var relayout = value.Length != _text.Length || Colons(value) != Colons(_text);
                _text = value;
                while (_cells.Count < value.Length)
                {
                    var cell = UiKit.Label("Cell" + _cells.Count, _rect, string.Empty, 10, _color, TextAnchor.MiddleCenter, FontStyle.Bold);
                    cell.fontSize = _size;
                    cell.horizontalOverflow = HorizontalWrapMode.Overflow;
                    var shadow = cell.gameObject.AddComponent<Shadow>();
                    shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
                    shadow.effectDistance = new Vector2(0f, -3f);
                    _cells.Add(cell);
                }

                if (relayout)
                    Layout();
                for (var i = 0; i < _cells.Count; i++)
                {
                    var on = i < value.Length;
                    _cells[i].gameObject.SetActive(on);
                    if (on)
                        _cells[i].text = value[i].ToString();
                }
            }
        }

        private static int Colons(string s)
        {
            var n = 0;
            foreach (var c in s)
                if (c == ':')
                    n++;
            return n;
        }

        private void Layout()
        {
            var digit = _size * 0.62f;
            var colon = _size * 0.34f;
            var x = 0f;
            for (var i = _text.Length - 1; i >= 0; i--)
            {
                var w = _text[i] == ':' ? colon : digit;
                var r = _cells[i].rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(1f, 0.5f);
                r.pivot = new Vector2(1f, 0.5f);
                r.sizeDelta = new Vector2(w, _size * 1.3f);
                r.anchoredPosition = new Vector2(-x, _text[i] == ':' ? _size * 0.06f : 0f);
                x += w;
            }
        }
    }

    /// <summary>
    /// Impact shake: a decaying "trauma" (shake = trauma²) applied as a small camera rotation only while each camera
    /// renders, then put back, so it works under any camera controller and never moves gameplay transforms. Smooth
    /// (summed sines, not per-frame random). Off with <see cref="GameSettings.ReduceAlertEffects"/>.
    /// </summary>
    public static class ScreenShake
    {
        private const float MaxDegrees = 0.9f;
        private const float Decay = 1.6f;
        private static float _trauma;
        private static float _lastTick;
        private static bool _hooked;
        private static readonly Dictionary<Camera, Quaternion> Restore = new();

        public static float Trauma => _trauma;

        public static void Add(float amount)
        {
            if (GameSettings.ReduceAlertEffects)
                return;
            Hook();
            Tick();
            _trauma = Mathf.Clamp01(_trauma + amount);
        }

        private static void Hook()
        {
            if (_hooked)
                return;
            _hooked = true;
            RenderPipelineManager.beginCameraRendering += (_, camera) => Begin(camera);
            RenderPipelineManager.endCameraRendering += (_, camera) => End(camera);
            Camera.onPreCull += Begin;
            Camera.onPostRender += End;
            _lastTick = Time.unscaledTime;
        }

        private static void Tick()
        {
            var now = Time.unscaledTime;
            _trauma = Mathf.Max(0f, _trauma - Decay * (now - _lastTick));
            _lastTick = now;
        }

        private static void Begin(Camera camera)
        {
            if (camera == null || camera.cameraType != CameraType.Game)
                return;
            End(camera);
            Tick();
            if (_trauma <= 0f)
                return;
            var shake = _trauma * _trauma;
            var t = Time.unscaledTime * 31f;
            var pitch = MaxDegrees * shake * (Mathf.Sin(t * 1.7f) * 0.6f + Mathf.Sin(t * 0.53f) * 0.4f);
            var yaw = MaxDegrees * shake * (Mathf.Sin(t * 2.3f + 1.1f) * 0.6f + Mathf.Sin(t * 0.71f) * 0.4f);
            var roll = MaxDegrees * 0.6f * shake * Mathf.Sin(t * 1.1f + 2.3f);
            var transform = camera.transform;
            Restore[camera] = transform.localRotation;
            transform.localRotation *= Quaternion.Euler(pitch, yaw, roll);
        }

        private static void End(Camera camera)
        {
            if (camera == null || !Restore.TryGetValue(camera, out var rotation))
                return;
            Restore.Remove(camera);
            camera.transform.localRotation = rotation;
        }
    }

    /// <summary>
    /// The alert card at the top of the screen (inside the safe area): a sonar icon with expanding ping rings and a
    /// soft glow, what is coming, the attacker's tag and name, a tabular countdown and a draining time bar. It slides
    /// in with an ease-out-back and out with an ease-in; the colour follows the mood (red, amber, calm teal).
    /// </summary>
    public sealed class AttackBanner : MonoBehaviour
    {
        private const float Travel = 260f;

        private RectTransform _card;
        private CanvasGroup _group;
        private Image _rim;
        private Image _strip;
        private Image _disc;
        private Image _glow;
        private RectTransform _glyph;
        private Image _shieldIcon;
        private readonly Image[] _rings = new Image[2];
        private Image _bar;
        private Text _title;
        private Text _who;
        private Text _more;
        private Text _caption;
        private TabularClock _clock;

        private bool _showing;
        private float _t;
        private float _phase;
        private float _shake;
        private Vector2 _rest;

        public bool Showing => _showing;

        public static AttackBanner Create(Transform parent, Action onTap)
        {
            var root = UiKit.Rect("AttackBanner", parent).Place(0.035f, 0.788f, 0.965f, 0.872f);
            var banner = root.gameObject.AddComponent<AttackBanner>();
            banner.Build(root, onTap);
            return banner;
        }

        private void Build(RectTransform root, Action onTap)
        {
            _card = root;
            _rest = root.anchoredPosition;
            _group = root.gameObject.AddComponent<CanvasGroup>();

            // Drop shadow, the dark glass card, a thin tinted rim and a bright accent strip on the left.
            var shadow = UiKit.Box("Shadow", root, new Color(0f, 0f, 0f, 0.45f));
            shadow.rectTransform.Place(0, 0, 1, 1);
            shadow.rectTransform.offsetMin = new Vector2(-6f, -12f);
            shadow.rectTransform.offsetMax = new Vector2(6f, 2f);
            shadow.raycastTarget = false;

            var card = UiKit.Box("Card", root, new Color(0.055f, 0.03f, 0.045f, 0.93f));
            card.rectTransform.Place(0, 0, 1, 1);
            card.gameObject.AddComponent<Button>().onClick.AddListener(() =>
            {
                GameAudio.Play(Sfx.UiTap);
                onTap?.Invoke();
            });
            card.gameObject.AddComponent<PressFeedback>();

            var sheen = UiKit.Box("Sheen", card.transform, new Color(1f, 1f, 1f, 0.05f));
            sheen.rectTransform.Place(0, 0.5f, 1, 1, 3);
            sheen.raycastTarget = false;

            _rim = UiKit.Box("Rim", card.transform, Color.white);
            _rim.sprite = UiKit.Ring;
            _rim.rectTransform.Place(0, 0, 1, 1);
            _rim.raycastTarget = false;

            _strip = UiKit.Box("Strip", card.transform, Color.white);
            _strip.rectTransform.Place(0, 0.16f, 0, 0.84f);
            _strip.rectTransform.offsetMin = new Vector2(7f, 0f);
            _strip.rectTransform.offsetMax = new Vector2(15f, 0f);
            _strip.raycastTarget = false;

            // The sonar icon: a glow, two expanding ping rings, the disc, and a ring-and-dot glyph (or the shield icon).
            var icon = UiKit.Rect("Icon", card.transform).Place(0.025f, 0.12f, 0.025f, 0.88f);
            var iconSize = icon.gameObject.AddComponent<AspectRatioFitter>();
            iconSize.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            iconSize.aspectRatio = 1f;
            icon.pivot = new Vector2(0f, 0.5f);

            _glow = Disc("Glow", icon, UiKit.Circle, 1.45f);
            _glow.sprite = Glow;
            for (var i = 0; i < _rings.Length; i++)
                _rings[i] = Disc("Ping" + i, icon, UiKit.CircleRing, 1f);
            _disc = Disc("Disc", icon, UiKit.Circle, 1f);
            _glyph = UiKit.Rect("Glyph", icon).Place(0, 0, 1, 1);
            Disc("Inner", _glyph, UiKit.CircleRing, 0.62f).color = new Color(1f, 1f, 1f, 0.6f);
            Disc("Dot", _glyph, UiKit.Circle, 0.22f).color = Color.white;
            _shieldIcon = Hud.HudIcons.Image(icon, "item_shield_24h", 0.1f, 0.1f, 0.9f, 0.9f);
            _shieldIcon.gameObject.SetActive(false);

            _title = UiKit.Label("Title", card.transform, string.Empty, UiKit.TypeScale.Body, Color.white, TextAnchor.LowerLeft, FontStyle.Bold);
            _title.rectTransform.Place(0.2f, 0.56f, 0.68f, 0.92f);
            _title.horizontalOverflow = HorizontalWrapMode.Overflow;
            _who = UiKit.Label("Who", card.transform, string.Empty, UiKit.TypeScale.Subtitle, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            _who.rectTransform.Place(0.2f, 0.1f, 0.68f, 0.54f);
            _who.resizeTextForBestFit = true;
            _who.resizeTextMinSize = 18;
            _who.resizeTextMaxSize = _who.fontSize;
            _who.verticalOverflow = VerticalWrapMode.Truncate;
            UiKit.Stroked(_who).GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);

            _clock = TabularClock.Create(card.transform, UiKit.TypeScale.Display, Color.white);
            ((RectTransform)_clock.transform).Place(0.66f, 0.36f, 0.965f, 0.95f);
            _caption = UiKit.Label("Caption", card.transform, Loc.T("pvp.alarm.eta"), UiKit.TypeScale.Caption, UiKit.Muted, TextAnchor.UpperRight, FontStyle.Bold);
            _caption.rectTransform.Place(0.66f, 0.1f, 0.965f, 0.36f);
            _more = UiKit.Label("More", card.transform, string.Empty, UiKit.TypeScale.Caption, UiKit.Muted, TextAnchor.UpperRight, FontStyle.Bold);
            _more.rectTransform.Place(0.44f, 0.1f, 0.7f, 0.36f);

            var track = UiKit.Box("Track", card.transform, new Color(1f, 1f, 1f, 0.08f));
            track.rectTransform.Place(0.2f, 0f, 0.965f, 0f);
            track.rectTransform.offsetMin = new Vector2(0f, 10f);
            track.rectTransform.offsetMax = new Vector2(0f, 16f);
            track.raycastTarget = false;
            _bar = UiKit.Box("Bar", track.transform, Color.white);
            _bar.sprite = null;
            _bar.type = Image.Type.Filled;
            _bar.fillMethod = Image.FillMethod.Horizontal;
            _bar.rectTransform.Place(0, 0, 1, 1);
            _bar.raycastTarget = false;

            _group.alpha = 0f;
            _card.gameObject.SetActive(false);
        }

        private static Image Disc(string name, RectTransform parent, Sprite sprite, float scale)
        {
            var image = UiKit.Rect(name, parent).Place(0, 0, 1, 1).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            image.transform.localScale = new Vector3(scale, scale, 1f);
            return image;
        }

        /// <summary>Fills the card for the first arrival. <paramref name="fraction"/> is the share of the march still to go.</summary>
        public void Set(AlarmMood mood, string title, string who, string more, float secondsLeft, float fraction)
        {
            var color = mood == AlarmMood.Shielded ? new Color(0.3f, 0.9f, 0.95f) : mood == AlarmMood.Scout ? new Color(1f, 0.72f, 0.28f) : new Color(1f, 0.33f, 0.26f);
            _rim.color = new Color(color.r, color.g, color.b, 0.75f);
            _strip.color = color;
            _disc.color = mood == AlarmMood.Shielded ? new Color(0.04f, 0.36f, 0.44f) : mood == AlarmMood.Scout ? new Color(0.5f, 0.3f, 0.04f) : new Color(0.62f, 0.07f, 0.06f);
            _glow.color = new Color(color.r, color.g, color.b, _glow.color.a);
            foreach (var ring in _rings)
                ring.color = new Color(color.r, color.g, color.b, ring.color.a);
            _bar.color = color;
            _title.color = color;
            _title.text = title;
            _who.text = who;
            _more.text = more;
            _clock.Text = AlarmTempo.Clock(secondsLeft);
            _clock.Color = mood == AlarmMood.Threat && secondsLeft <= 10f ? new Color(1f, 0.82f, 0.74f) : Color.white;
            _bar.fillAmount = Mathf.Clamp01(fraction);
            var shielded = mood == AlarmMood.Shielded;
            _shieldIcon.gameObject.SetActive(shielded && _shieldIcon.sprite != null);
            _glyph.gameObject.SetActive(!_shieldIcon.gameObject.activeSelf);
        }

        public void Show(bool show)
        {
            if (show == _showing)
                return;
            _showing = show;
            _t = 0f;
            if (show)
                _card.gameObject.SetActive(true);
        }

        /// <summary>A short horizontal shake of the card on impact.</summary>
        public void Jolt() => _shake = GameSettings.ReduceAlertEffects ? 0f : 1f;

        /// <summary>Advances the slide, the icon's ping rings and the glow (period follows the vignette's tempo).</summary>
        public void Tick(float dt, float period, bool calm)
        {
            if (!_card.gameObject.activeSelf)
                return;

            _t = Mathf.Min(1f, _t + dt / (_showing ? 0.42f : 0.22f));
            float offset, alpha;
            if (_showing)
            {
                // Ease-out-back: overshoots a few pixels down, then settles.
                const float c1 = 1.5f;
                var u = _t - 1f;
                var eased = 1f + (c1 + 1f) * u * u * u + c1 * u * u;
                offset = Travel * (1f - eased);
                alpha = Mathf.Clamp01(_t * 2.5f);
            }
            else
            {
                var eased = _t * _t * _t; // ease-in
                offset = Travel * eased;
                alpha = 1f - _t;
                if (_t >= 1f)
                {
                    _card.gameObject.SetActive(false);
                    return;
                }
            }

            _shake = Mathf.Max(0f, _shake - dt * 2.4f);
            var jolt = _shake * _shake * 18f * Mathf.Sin(Time.unscaledTime * 70f);
            _card.anchoredPosition = _rest + new Vector2(jolt, offset);
            _group.alpha = alpha;

            var reduce = GameSettings.ReduceAlertEffects;
            _phase += dt / Mathf.Max(0.3f, period);
            _phase -= Mathf.Floor(_phase);
            for (var i = 0; i < _rings.Length; i++)
            {
                // Sonar pings: each ring grows from the disc and fades; the second trails by half a period.
                var p = (_phase + i * 0.5f) % 1f;
                var grow = 1f - (1f - p) * (1f - p);
                var s = reduce || calm ? 1.25f : Mathf.Lerp(1f, 1.6f, grow);
                _rings[i].transform.localScale = new Vector3(s, s, 1f);
                var a = reduce || calm ? (i == 0 ? 0.35f : 0f) : 0.7f * (1f - p) * Mathf.Clamp01(p * 8f);
                _rings[i].canvasRenderer.SetAlpha(a);
            }

            var glow = reduce ? 0.35f : 0.25f + 0.3f * AlarmTempo.Breath(_phase);
            _glow.canvasRenderer.SetAlpha(glow);
        }

        private static Sprite _glowSprite;

        /// <summary>A soft radial glow (alpha falls off as 1 − r², squared) for behind the icon.</summary>
        private static Sprite Glow
        {
            get
            {
                if (_glowSprite != null)
                    return _glowSprite;
                const int size = 64;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "HadalAlarmGlow" };
                var pixels = new Color32[size * size];
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var dx = (x + 0.5f) / size * 2f - 1f;
                        var dy = (y + 0.5f) / size * 2f - 1f;
                        var a = Mathf.Clamp01(1f - (dx * dx + dy * dy));
                        pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * a * 255));
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply(false, true);
                return _glowSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            }
        }
    }
}
