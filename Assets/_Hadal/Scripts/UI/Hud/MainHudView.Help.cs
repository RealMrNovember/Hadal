using System.Linq;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI.Hud
{
    /// <summary>
    /// Last Asylum's floating alliance-help buttons (docs/25 §4), on the base and the world map alike: a medical kit
    /// bottom left for mates' healing, clasped hands on the right for their constructions and research. Each shows only
    /// while there are requests this player can still help, with their count; one tap helps all of that kind.
    /// </summary>
    public sealed partial class MainHudView
    {
        private sealed class HelpBubble
        {
            public GameObject Root;
            public Text Count;
            public string Kind;

            /// <summary>Hidden after a tap until the help list next changes (the server's answer).</summary>
            public int HiddenAtVersion = -1;
        }

        private HelpBubble _healBubble;
        private HelpBubble _generalBubble;

        private void BuildHelpBubbles()
        {
            _healBubble = HelpBubbleAt("HealHelp", HelpKinds.Heal, 0.155f, 0.195f, 0.275f, 0.27f, Loc.T("help.bubble.heal"));
            _generalBubble = HelpBubbleAt("GeneralHelp", HelpKinds.General, 0.845f, 0.545f, 0.985f, 0.615f, Loc.T("help.bubble.general"));
        }

        private HelpBubble HelpBubbleAt(string name, string kind, float x0, float y0, float x1, float y1, string caption)
        {
            var bubble = new HelpBubble { Kind = kind };
            var root = Tap(_lower.transform, name, x0, y0, x1, y1, () => SendHelp(bubble), new Color(0, 0, 0, 0.001f));
            bubble.Root = root.gameObject;

            if (kind == HelpKinds.Heal)
            {
                // A white medical kit with a red cross, drawn so it needs no art.
                var kit = UiKit.Box("Kit", root, Color.white);
                kit.sprite = UiKit.Rounded;
                kit.raycastTarget = false;
                kit.rectTransform.Place(0.18f, 0.32f, 0.82f, 0.98f);
                var red = new Color(0.86f, 0.12f, 0.12f, 1f);
                var bar = UiKit.Box("CrossH", kit.transform, red);
                bar.raycastTarget = false;
                bar.rectTransform.Place(0.18f, 0.4f, 0.82f, 0.6f);
                var post = UiKit.Box("CrossV", kit.transform, red);
                post.raycastTarget = false;
                post.rectTransform.Place(0.4f, 0.18f, 0.6f, 0.82f);
            }
            else
            {
                HudIcons.Image(root, "left_help", 0.08f, 0.25f, 0.92f, 1f);
            }

            Outlined(UiKit.Label("Caption", root, caption, 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold)).rectTransform.Place(-0.2f, 0f, 1.2f, 0.28f);

            // The count badge on the top right corner.
            var badge = UiKit.Box("Badge", root, new Color(0.95f, 0.18f, 0.15f, 1f));
            badge.sprite = UiKit.Rounded;
            badge.raycastTarget = false;
            badge.rectTransform.anchorMin = badge.rectTransform.anchorMax = new Vector2(0.9f, 0.92f);
            badge.rectTransform.sizeDelta = new Vector2(44, 40);
            bubble.Count = UiKit.Label("Count", badge.transform, "0", 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            bubble.Count.rectTransform.Place(0, 0, 1, 1);

            // A gentle pulse so the bubble catches the eye, like the genre's.
            root.gameObject.AddComponent<HelpPulse>();
            bubble.Root.SetActive(false);
            return bubble;
        }

        /// <summary>Open requests of this kind the player can still help.</summary>
        private HelpRequestDto[] OpenHelp(string kind) =>
            View.MyAlliance == null
                ? System.Array.Empty<HelpRequestDto>()
                : View.HelpRequests.Where(r => !r.HelpedByMe && HelpKinds.Matches(kind, r.Kind)).ToArray();

        private void RefreshHelpBubbles()
        {
            foreach (var bubble in new[] { _healBubble, _generalBubble })
            {
                if (bubble == null)
                    continue;
                if (bubble.HiddenAtVersion >= 0 && bubble.HiddenAtVersion != View.AllianceVersion)
                    bubble.HiddenAtVersion = -1;
                var open = OpenHelp(bubble.Kind);
                bubble.Root.SetActive(open.Length > 0 && bubble.HiddenAtVersion < 0);
                bubble.Count.text = open.Length > 99 ? "99+" : open.Length.ToString();
            }

            // Mates who helped this player: a short note each.
            while (View.TryTakeHelpReceived(out var received))
                _actions.Toast?.Invoke(Loc.F("help.received", received.HelperName, received.Count, Loc.Duration(received.Seconds)), UiKit.Ok);
        }

        private void SendHelp(HelpBubble bubble)
        {
            var open = OpenHelp(bubble.Kind);
            if (open.Length == 0)
                return;
            _actions.HelpAll?.Invoke(bubble.Kind);
            var mates = open.Select(r => r.OwnerId).Distinct().Count();
            _actions.Toast?.Invoke(Loc.F("help.sent", mates), UiKit.Ok);
            bubble.HiddenAtVersion = View.AllianceVersion;
            bubble.Root.SetActive(false);
        }

        /// <summary>Breathes the bubble's scale a little while it shows.</summary>
        private sealed class HelpPulse : MonoBehaviour
        {
            private void Update() => transform.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(Time.unscaledTime * 4f));

            private void OnDisable() => transform.localScale = Vector3.one;
        }
    }
}
