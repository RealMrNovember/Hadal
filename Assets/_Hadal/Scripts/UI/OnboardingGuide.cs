using System;
using HADAL.Shared.Catalog;
using HADAL.Shared.Simulation;
using Hadal.Core.Catalog;
using Hadal.Core.StateSync;
using Hadal.UI.Hud;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// The first-session guide on screen (docs/34): Commander Rourke's bubble with what to do now and a "Go" button,
    /// a pulsing ring on the HUD button to tap and, on the world map, the target arrow. The server decides the step
    /// (QuestStateDto.Guide); this only shows it. It never traps the player: × hides it until the next step, "Close
    /// guide" turns it off (the quest panel turns it back on), and it always resumes from the server's step.
    /// </summary>
    public sealed class OnboardingGuide : MonoBehaviour
    {
        /// <summary>What "Go" can open; wired by <see cref="BaseManagementView"/>.</summary>
        public sealed class Actions
        {
            public Func<bool> SheetOpen;
            public Func<bool> InWorld;

            /// <summary>Opens the highest building of a kind (or the build list when there is none).</summary>
            public Action<string> OpenBuilding;

            /// <summary>Opens the build list on a free slot that takes this building.</summary>
            public Action<string> BuildNew;

            /// <summary>The chat page on the alliance channel.</summary>
            public Action AllianceChat;

            public Action Heroes;
            public Action Expand;
            public Action Bag;
            public Action Alliance;
            public Action AllianceHelp;
            public Action OpenMap;
            public Action<string> Claim;

            /// <summary>The world map's guide arrow: "monster", "node" or null.</summary>
            public Action<string> MapTarget;
        }

        /// <summary>The guide's voice: the starter commander's portrait (no new art).</summary>
        public const string GuideHero = "mara";

        private const string OffKey = "hadal.guide.off.";
        private const string HiddenKey = "hadal.guide.hidden.";

        private IStateSyncService _stateSync;
        private Actions _actions;
        private Canvas _canvas;
        private RectTransform _root;
        private RectTransform _bubble;
        private TMP_Text _name;
        private TMP_Text _text;
        private TMP_Text _goLabel;
        private RectTransform _ring;
        private RectTransform _pointer;
        private string _stepId = string.Empty;
        private string _shownKey = string.Empty;

        private static GameCatalog Catalog => ClientCatalog.Catalog;
        private ClientStateView View => _stateSync.View;

        public static OnboardingGuide Instance { get; private set; }

        public void Initialize(IStateSyncService stateSync, Actions actions)
        {
            _stateSync = stateSync;
            _actions = actions;
            Instance = this;
            Build();
        }

        // --- Preferences (per player, on this device) --------------------------------------------

        private string Player => View?.PlayerId ?? string.Empty;

        /// <summary>The player turned the guide off ("Close guide"); the server's steps carry on in the quest panel.</summary>
        public bool Off
        {
            get => PlayerPrefs.GetInt(OffKey + Player, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(OffKey + Player, value ? 1 : 0);
                PlayerPrefs.DeleteKey(HiddenKey + Player);
                PlayerPrefs.Save();
            }
        }

        private bool HiddenFor(string stepId) => PlayerPrefs.GetString(HiddenKey + Player, string.Empty) == stepId;

        private void HideFor(string stepId)
        {
            PlayerPrefs.SetString(HiddenKey + Player, stepId);
            PlayerPrefs.Save();
        }

        // --- Layout --------------------------------------------------------------------------------

        private void Build()
        {
            // Just above the HUD (35) so the ring can sit on its buttons; panels (40+) cover the guide.
            _canvas = UiKit.Canvas("GuideUi", 36);
            _canvas.transform.SetParent(transform, false);
            _root = UiKit.Rect("Safe", _canvas.transform).Place(0, 0, 1, 1);
            _root.gameObject.AddComponent<SafeArea>();

            _ring = UiKit.Rect("Ring", _root);
            var ring = _ring.gameObject.AddComponent<Image>();
            ring.sprite = UiKit.Ring;
            ring.type = Image.Type.Sliced;
            ring.color = UiKit.Amber;
            ring.raycastTarget = false;
            _pointer = UiKit.Rect("Pointer", _root);
            _pointer.sizeDelta = new Vector2(70, 70);
            var tip = UiKit.Label("Tip", _pointer, "▼", 56, UiKit.Amber, TextAnchor.MiddleCenter, FontStyle.Bold);
            tip.rectTransform.Place(0, 0, 1, 1);
            tip.raycastTarget = false;
            UiText.Outline(tip, new Color(0, 0, 0, 0.8f));

            var bubble = UiKit.Box("Bubble", _root, new Color(0.02f, 0.08f, 0.12f, 0.95f));
            _bubble = bubble.rectTransform.Place(0.02f, 0.19f, 0.98f, 0.295f);
            var frame = bubble.gameObject.AddComponent<Outline>();
            frame.effectColor = new Color(UiKit.Accent.r, UiKit.Accent.g, UiKit.Accent.b, 0.7f);
            frame.effectDistance = new Vector2(3, -3);

            var face = UiKit.Rect("Face", _bubble).Place(0.015f, 0.1f, 0.17f, 0.9f);
            AvatarDisc.Draw(face, GuideHero, Loc.T("hero." + GuideHero), GuideHero);

            _name = UiKit.Label("Name", _bubble, string.Empty, 26, UiKit.Amber, TextAnchor.MiddleLeft, FontStyle.Bold);
            _name.rectTransform.Place(0.19f, 0.7f, 0.78f, 0.96f);
            _text = UiKit.Label("Text", _bubble, string.Empty, 27, UiKit.Text, TextAnchor.UpperLeft);
            _text.rectTransform.Place(0.19f, 0.05f, 0.78f, 0.7f);
            _text.textWrappingMode = TextWrappingModes.Normal;
            _text.overflowMode = TextOverflowModes.Truncate;
            _text.enableAutoSizing = true;
            _text.fontSizeMin = 18;
            _text.fontSizeMax = 27;

            var go = UiKit.Button("Go", _bubble, Loc.T("guide.go"), Go, UiKit.Primary, 32);
            ((RectTransform)go.transform).Place(0.8f, 0.36f, 0.985f, 0.9f);
            _goLabel = go.GetComponentInChildren<TMP_Text>();
            GuideTargets.Register("GuideGo", (RectTransform)go.transform);

            var off = UiKit.Button("Off", _bubble, Loc.T("guide.off"), () => Off = true, new Color(0, 0, 0, 0.001f), 20);
            off.GetComponentInChildren<TMP_Text>().color = UiKit.Muted;
            ((RectTransform)off.transform).Place(0.8f, 0.02f, 0.985f, 0.34f);

            var close = UiKit.Button("Close", _bubble, "×", () => HideFor(_stepId), UiKit.Field, 34);
            close.GetComponentInChildren<TMP_Text>().color = UiKit.Text;
            ((RectTransform)close.transform).Place(0.935f, 0.86f, 1.01f, 1.16f);

            _bubble.gameObject.SetActive(false);
            _ring.gameObject.SetActive(false);
            _pointer.gameObject.SetActive(false);
        }

        // --- Frame ---------------------------------------------------------------------------------

        private void Update()
        {
            if (_stateSync == null || _canvas == null)
                return;

            var quests = View.Quests;
            var guide = quests?.Guide;
            QuestData step = null;
            // An incoming attack outranks the guide: no arrow to monsters while the base is under fire (live feedback).
            var active = guide != null && Catalog.TryGetOnboarding(guide.Id, out step, out _) && !Off && !HiddenFor(guide.Id)
                         && PvpCenter.Instance?.UnderAttack != true;
            var inWorld = _actions.InWorld?.Invoke() ?? false;
            var sheet = _actions.SheetOpen?.Invoke() ?? false;
            var done = active && guide.Progress >= guide.Target;
            var mapStep = active && !done && step.type == "counter" && step.target is "monster" or "gather";

            _actions.MapTarget?.Invoke(active && inWorld && mapStep ? (step.target == "monster" ? "monster" : "node") : null);

            // The bubble: in the base when no panel covers the screen, on the map only for the map steps.
            var showBubble = active && !sheet && (!inWorld || mapStep || done);
            _bubble.gameObject.SetActive(showBubble);
            if (!showBubble)
            {
                _ring.gameObject.SetActive(false);
                _pointer.gameObject.SetActive(false);
                return;
            }

            _stepId = guide.Id;
            var key = $"{guide.Id}|{done}|{inWorld}|{quests.GuideNumber}";
            if (_shownKey != key)
            {
                _shownKey = key;
                _name.text = $"{Loc.T("hero." + GuideHero)}  <color=#8AA9B5>{Loc.F("guide.number", quests.GuideNumber, quests.GuideTotal)}</color>";
                _text.text = done ? Loc.T("guide.claim") : Loc.T(inWorld ? "guide." + step.id + ".map" : "guide." + step.id);
                _goLabel.text = done ? Loc.T("quest.claim") : Loc.T("guide.go");
            }

            Pulse(inWorld ? null : done ? "Task" : TargetOf(step));
        }

        /// <summary>The HUD button a step is done from (the guide's own Go button for building steps).</summary>
        public static string TargetOf(QuestData step) => step.type switch
        {
            "heroes" => "Heroes",
            "alliance" => "Alliance",
            "counter" when step.target is "monster" or "gather" => "Territory",
            "counter" when step.target == "open" => "Bag",
            "counter" when step.target is "helped" or "assist" => "Help",
            "counter" when step.target == "allianceChat" => "Chat",
            _ => "GuideGo"
        };

        /// <summary>A breathing ring around the target and a bobbing arrow above it.</summary>
        private void Pulse(string targetName)
        {
            var target = GuideTargets.Get(targetName);
            _ring.gameObject.SetActive(target != null);
            _pointer.gameObject.SetActive(target != null);
            if (target == null)
                return;

            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, RectTransformUtility.WorldToScreenPoint(null, corners[0]), null, out var min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, RectTransformUtility.WorldToScreenPoint(null, corners[2]), null, out var max);
            var t = Time.unscaledTime;
            var grow = 10f + 8f * Mathf.Sin(t * 5f);
            _ring.anchorMin = _ring.anchorMax = new Vector2(0.5f, 0.5f);
            _ring.pivot = new Vector2(0.5f, 0.5f);
            _ring.anchoredPosition = (min + max) / 2f;
            _ring.sizeDelta = max - min + new Vector2(grow, grow) * 2f;
            _ring.GetComponent<Image>().color = new Color(UiKit.Amber.r, UiKit.Amber.g, UiKit.Amber.b, 0.65f + 0.35f * Mathf.Sin(t * 5f));

            _pointer.anchorMin = _pointer.anchorMax = new Vector2(0.5f, 0.5f);
            _pointer.anchoredPosition = new Vector2((min.x + max.x) / 2f, max.y + 40f + 12f * Mathf.Abs(Mathf.Sin(t * 3f)));
        }

        // --- Go ------------------------------------------------------------------------------------

        /// <summary>Does the obvious next thing for the current step (the guide bubble's Go and the task strip).</summary>
        public void Go()
        {
            var guide = View.Quests?.Guide;
            if (guide == null || !Catalog.TryGetOnboarding(guide.Id, out var step, out _))
                return;
            if (guide.Progress >= guide.Target)
            {
                _actions.Claim?.Invoke(step.id);
                return;
            }

            GoTo(step.type, step.target);
        }

        /// <summary>Whether <see cref="GoTo"/> has a route for a task of this type and target (the quest list's Go).</summary>
        public bool CanGoTo(string type, string target) => Route(type, target) != null;

        /// <summary>Opens what does a task of this type: the building, its build list, the map, the bag, the alliance…</summary>
        public void GoTo(string type, string target) => Route(type, target)?.Invoke();

        private Action Route(string type, string target) => type switch
        {
            "level" => () => _actions.OpenBuilding?.Invoke(target),
            "count" => () => _actions.BuildNew?.Invoke(target),
            "heroes" => () => _actions.Heroes?.Invoke(),
            "rings" => () => _actions.Expand?.Invoke(),
            "alliance" => () => _actions.Alliance?.Invoke(),
            "counter" when target is "monster" or "gather" or "attack" => () => _actions.OpenMap?.Invoke(),
            "counter" when target == "open" => () => _actions.Bag?.Invoke(),
            "counter" when target == "allianceChat" => () => _actions.AllianceChat?.Invoke(),
            "counter" when target is "helped" or "assist" => () => _actions.AllianceHelp?.Invoke(),
            _ => null
        };
    }
}
