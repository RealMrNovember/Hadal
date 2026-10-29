using System.Collections.Generic;
using HADAL.Shared.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI.Hud
{
    /// <summary>
    /// The HUD's part of the first-session guide (docs/34): the task strip shows the guide step while there is one,
    /// the buttons are named for the guide's pulse, and features still closed by the reactor wear a level badge.
    /// </summary>
    public sealed partial class MainHudView
    {
        private static readonly string[] GuideNames = { "Task", "Quests", "Help", "Research", "Expand", "Alliance", "Bag", "Mail", "Heroes", "Territory", "Chat" };

        private bool _taskGuide;
        private GameObject _researchLock;
        private Text _researchLockText;
        private GameObject _pvpLock;
        private Text _pvpLockText;

        private void RegisterGuideTargets()
        {
            foreach (var name in GuideNames)
            {
                var found = FindNamed(_canvas.transform, name, last: false);
                if (found != null)
                    GuideTargets.Register(name, found);
            }

            // Two buttons are called "War": the expedition on the right and, built after it, the PvP window.
            var research = FindNamed(_canvas.transform, "Research", last: false);
            if (research != null)
                (_researchLock, _researchLockText) = LockBadge(research);
            var pvp = FindNamed(_canvas.transform, "War", last: true);
            if (pvp != null)
                (_pvpLock, _pvpLockText) = LockBadge(pvp);
        }

        private static RectTransform FindNamed(Transform root, string name, bool last)
        {
            RectTransform found = null;
            var stack = new Stack<Transform>();
            stack.Push(root);
            var all = new List<Transform>();
            while (stack.Count > 0)
            {
                var t = stack.Pop();
                all.Add(t);
                for (var i = t.childCount - 1; i >= 0; i--)
                    stack.Push(t.GetChild(i));
            }

            foreach (var t in all)
            {
                if (t.name != name || t is not RectTransform rect || rect.GetComponent<Button>() == null)
                    continue;
                found = rect;
                if (!last)
                    break;
            }

            return found;
        }

        /// <summary>A small amber "Lv.6" pill on a button whose feature is still closed.</summary>
        private static (GameObject, Text) LockBadge(RectTransform button)
        {
            var badge = UiKit.Box("Lock", button, new Color(0.08f, 0.06f, 0.02f, 0.92f));
            badge.rectTransform.Place(0.02f, 0.66f, 0.98f, 0.98f);
            badge.raycastTarget = false;
            var outline = badge.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(UiKit.Amber.r, UiKit.Amber.g, UiKit.Amber.b, 0.9f);
            outline.effectDistance = new Vector2(2, -2);
            var text = UiKit.Label("Text", badge.transform, string.Empty, 22, UiKit.Amber, TextAnchor.MiddleCenter, FontStyle.Bold);
            text.rectTransform.Place(0, 0, 1, 1);
            text.raycastTarget = false;
            badge.gameObject.SetActive(false);
            return (badge.gameObject, text);
        }

        /// <summary>Reactor-gated features show the level that opens them (docs/34 §3).</summary>
        private void RefreshLocks()
        {
            var reactor = ReactorLevel();
            if (_researchLock != null)
            {
                var opens = OnboardingRules.BuildingUnlock(_catalog, ResearchRules.CenterId);
                var locked = reactor < opens;
                _researchLock.SetActive(locked);
                if (locked)
                    _researchLockText.text = Loc.F("unlock.badge", opens);
            }

            if (_pvpLock != null)
            {
                var locked = PvpRules.IsBeginner(reactor);
                _pvpLock.SetActive(locked);
                if (locked)
                    _pvpLockText.text = Loc.F("unlock.badge", PvpRules.BeginnerReactor);
            }
        }

        /// <summary>The guide step on the task strip (claim it when done; otherwise a tap runs the guide's "Go").</summary>
        private bool RefreshGuideTask()
        {
            var quests = View.Quests;
            var guide = quests?.Guide;
            _taskGuide = false;
            if (guide == null || !_catalog.TryGetOnboarding(guide.Id, out var step, out _))
                return false;

            _taskGuide = true;
            _taskQuest = step.id;
            _taskDone = guide.Progress >= guide.Target;
            var number = $"<color=#5FE0F2>{Loc.F("guide.number", quests.GuideNumber, quests.GuideTotal)}</color> ";
            _task.text = _taskDone
                ? $"<color=#FFB74D>{Loc.T("quest.claim")}!</color>  {QuestText.Task(step)}"
                : $"{number}{QuestText.Task(step)}  <color=#8AA9B5>({guide.Progress}/{guide.Target})</color>";
            return true;
        }
    }
}
