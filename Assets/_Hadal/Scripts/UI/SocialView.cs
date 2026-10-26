using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Catalog;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;
using Hadal.Core.Catalog;
using Hadal.Core.Network;
using Hadal.Core.Events;
using Hadal.Core.StateSync;
using Hadal.Data.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// Heroes, alliance and chat (docs/25_Heroes_Alliance_Chat.md): the bottom bar, a chat ticker and one
    /// full-height sheet with a page per feature. Created by <see cref="BaseManagementView"/>, which
    /// hands over its services. Pages with text fields redraw only when their data changes, so typing
    /// is never wiped by a refresh.
    /// </summary>
    public sealed partial class SocialView : MonoBehaviour
    {
        private enum Page
        {
            None,
            Heroes,
            HeroDetail,
            PodResult,
            Alliance,
            Chat,
            Quests,
            Bag,
            Reward
        }

        private static readonly CommandType[] OwnCommands =
        {
            CommandType.ExecuteGacha, CommandType.LevelUpHero, CommandType.PromoteHero, CommandType.AssignHero, CommandType.SendChat,
            CommandType.CreateAlliance, CommandType.JoinAlliance, CommandType.LeaveAlliance, CommandType.BrowseAlliances,
            CommandType.KickMember, CommandType.RequestHelp, CommandType.HelpAll,
            CommandType.ClaimQuest, CommandType.ClaimDailyChest, CommandType.OpenItem, CommandType.RecruitHero
        };

        private IStateSyncService _stateSync;
        private ICommandDispatcher _commands;
        private ILocalEventBus _localBus;
        private ulong _sequence;
        private static GameCatalog _catalog => ClientCatalog.Catalog;

        private Canvas _canvas;
        private GameObject _sheet;
        private Text _title;
        private Text _subtitle;
        private RectTransform _list;
        private RectTransform _footer;
        private Text _toast;
        private float _toastUntil;

        private Page _page = Page.None;
        private string _renderedKey = string.Empty;
        private float _nextRefresh;
        private float _lastPointerTime = -1f;

        private ClientStateView View => _stateSync.View;

        public bool IsOpen => _sheet != null && _sheet.activeSelf;

        public void Initialize(IStateSyncService stateSync, ICommandDispatcher commands, ILocalEventBus localBus)
        {
            _stateSync = stateSync;
            _commands = commands;
            _localBus = localBus;
            Build();
            _localBus.Subscribe<CommandResolvedEvent>(OnCommandResolved);

            var stage = new GameObject("HeroStage");
            stage.transform.SetParent(transform, false);
            _stage = stage.AddComponent<HeroStage>();
        }

        private HeroStage _stage;

        private void OnDestroy() => _localBus?.Unsubscribe<CommandResolvedEvent>(OnCommandResolved);

        // --- Layout --------------------------------------------------------------------------

        private void Build()
        {
            _canvas = UiKit.Canvas("SocialUi", 45);
            _canvas.transform.SetParent(transform, false);

            // The HUD (Hud/MainHudView) opens these pages; the sheet sits under the HUD header.

            var safe = UiKit.Rect("Safe", _canvas.transform).Place(0, 0, 1, 1);
            safe.gameObject.AddComponent<Hud.SafeArea>();
            var sheetImage = UiKit.Box("Sheet", safe, UiKit.Panel);
            sheetImage.rectTransform.Place(0.01f, 0.005f, 0.99f, 0.89f);
            _sheet = sheetImage.gameObject;

            _title = UiKit.Label("Title", sheetImage.transform, string.Empty, 52, UiKit.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            _title.rectTransform.Place(0.05f, 0.93f, 0.8f, 0.99f);
            _subtitle = UiKit.Label("Subtitle", sheetImage.transform, string.Empty, 30, UiKit.Muted);
            _subtitle.rectTransform.Place(0.05f, 0.885f, 0.95f, 0.93f);

            var close = UiKit.Button("Close", sheetImage.transform, "X", Close, UiKit.Field, 40);
            close.GetComponentInChildren<Text>().color = UiKit.Text;
            ((RectTransform)close.transform).Place(0.84f, 0.93f, 0.96f, 0.99f);

            _list = UiKit.ScrollList("List", sheetImage.transform);
            ((RectTransform)_list.parent).Place(0.04f, 0.02f, 0.96f, 0.875f);

            // Chat input lives outside the list so redraws never touch it.
            _footer = UiKit.Rect("Footer", sheetImage.transform).Place(0.04f, 0.015f, 0.96f, 0.075f);
            _footer.gameObject.SetActive(false);

            _toast = UiKit.Label("Toast", _canvas.transform, string.Empty, 38, UiKit.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            _toast.rectTransform.Place(0.05f, 0.46f, 0.95f, 0.52f);
            var outline = _toast.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.8f);
            outline.effectDistance = new Vector2(2, -2);
            _toast.raycastTarget = false;

            _sheet.SetActive(false);
        }

        // --- Navigation ----------------------------------------------------------------------

        public void OpenHeroes() => Open(Page.Heroes);

        public void OpenAlliance(bool help = false)
        {
            _allianceTab = help && View.MyAlliance != null ? 1 : 0;
            Open(Page.Alliance);
        }

        public void OpenChat() => Open(Page.Chat);

        private void Open(Page page)
        {
            _page = page;
            _sheet.SetActive(true);
            if (page == Page.Alliance && View.MyAlliance == null)
                Browse(string.Empty);
            Render(force: true);
        }

        public void Close()
        {
            _stage?.Hide();
            _sheet.SetActive(false);
            _footer.gameObject.SetActive(false);
            _page = Page.None;
        }

        private void Update()
        {
            if (_canvas == null || _stateSync == null)
                return;

            if (_toast.text.Length > 0 && Time.unscaledTime > _toastUntil)
                _toast.text = string.Empty;

            UpdateBar();

            while (View.TryTakeGachaResult(out var result))
            {
                _podResult = result;
                Open(Page.PodResult);
            }

            while (View.TryTakeRewardReport(out var report))
            {
                _reward = report;
                Open(Page.Reward);
            }

            if (!IsOpen)
                return;

            if (Input.touchCount > 0 || Input.GetMouseButton(0))
                _lastPointerTime = Time.unscaledTime;
            if (Time.unscaledTime - _lastPointerTime <= 0.3f)
                return;

            // Hero pages tick once a second (free pull timer); the others only when their data changed.
            var key = PageKey();
            if (key != _renderedKey || ((_page is Page.Heroes or Page.HeroDetail) && Time.unscaledTime >= _nextRefresh))
                Render(force: false);
        }

        private string PageKey() => _page switch
        {
            Page.Heroes or Page.HeroDetail => $"{_page}|{_heroId}|{HeroesKey()}|{View.QuestVersion}",
            Page.Alliance => $"{_page}|{View.AllianceVersion}|{_confirmLeave}|{_allianceTab}",
            Page.Chat => $"{_page}|{View.ChatVersion}|{_chatChannel}",
            Page.Quests or Page.Bag => $"{_page}|{QuestKey()}",
            _ => _page.ToString()
        };

        private bool _wasInAlliance;

        private void UpdateBar()
        {
            if (_page == Page.Chat && IsOpen)
                View.UnreadChat = 0;

            // Joining, founding or leaving starts on the first tab.
            var inAlliance = View.MyAlliance != null;
            if (inAlliance != _wasInAlliance)
            {
                _wasInAlliance = inAlliance;
                _allianceTab = 0;
                _confirmLeave = false;
            }
        }

        private void Render(bool force)
        {
            _nextRefresh = Time.unscaledTime + 1f;
            var key = PageKey();
            var scroll = !force && key.Split('|')[0] == _renderedKey.Split('|')[0] ? _list.anchoredPosition : Vector2.zero;
            _renderedKey = key;

            for (var i = _list.childCount - 1; i >= 0; i--)
                Destroy(_list.GetChild(i).gameObject);

            var listRect = (RectTransform)_list.parent;
            listRect.Place(0.04f, _page == Page.Chat ? 0.085f : 0.02f, 0.96f, 0.875f);
            _footer.gameObject.SetActive(_page == Page.Chat);

            if (_page != Page.HeroDetail)
                _stage.Hide();

            switch (_page)
            {
                case Page.Heroes: RenderHeroes(); break;
                case Page.HeroDetail: RenderHeroDetail(); break;
                case Page.PodResult: RenderPodResult(); break;
                case Page.Alliance: RenderAlliance(); break;
                case Page.Chat: RenderChat(force); break;
                case Page.Quests: RenderQuests(); break;
                case Page.Bag: RenderBag(); break;
                case Page.Reward: RenderReward(); break;
            }

            if (_page == Page.Chat)
                _list.anchoredPosition = new Vector2(0, 100000); // newest at the bottom; ScrollRect clamps
            else
                _list.anchoredPosition = scroll;
        }

        // --- Commands ------------------------------------------------------------------------

        private string NewId() => Guid.NewGuid().ToString("N");

        private ulong NextSequence() => ++_sequence;

        private void OnCommandResolved(CommandResolvedEvent result)
        {
            if (Array.IndexOf(OwnCommands, result.Type) < 0)
                return;

            if (result.Result == CommandResultCode.Rejected)
            {
                Toast(Loc.Reason(result.Reason), UiKit.Danger);
                return;
            }

            var text = result.Type switch
            {
                CommandType.LevelUpHero => Loc.T("hero.trained"),
                CommandType.PromoteHero => Loc.T("hero.promoted"),
                CommandType.AssignHero => Loc.T("hero.assigned"),
                CommandType.CreateAlliance => Loc.T("alliance.created"),
                CommandType.JoinAlliance => Loc.T("alliance.joined"),
                CommandType.LeaveAlliance => Loc.T("alliance.left"),
                CommandType.KickMember => Loc.T("alliance.kicked"),
                CommandType.RequestHelp => Loc.T("alliance.help.requested"),
                CommandType.HelpAll => Loc.T("alliance.help.sent"),
                CommandType.RecruitHero => Loc.T("hero.recruited"),
                _ => string.Empty
            };
            if (text.Length > 0)
                Toast(text, UiKit.Accent);
            if (IsOpen && _page != Page.Chat)
                Render(force: false);
        }

        private void Toast(string text, Color color)
        {
            _toast.text = text;
            _toast.color = color;
            _toastUntil = Time.unscaledTime + 3f;
        }

        // --- Shared helpers ------------------------------------------------------------------

        private Dictionary<ResourceType, long> Wallet()
        {
            var wallet = new Dictionary<ResourceType, long>();
            foreach (var entry in View.Data.resources ?? Array.Empty<ResourceSaveEntry>())
                wallet[ResourceTypeMap.FromStoredValue(entry.resourceType)] = entry.amount;
            return wallet;
        }

        private int ReactorLevel() =>
            (View.Data.buildings ?? Array.Empty<BuildingSaveEntry>()).Where(b => b.definitionId == BuildingProgression.ReactorId)
                .Select(b => b.level).DefaultIfEmpty(0).Max();

        private static string Cost(IReadOnlyList<KeyValuePair<ResourceType, long>> cost, IReadOnlyDictionary<ResourceType, long> wallet) =>
            cost.Count == 0 ? "-" : string.Join("  ", cost.Select(c =>
            {
                var enough = wallet.TryGetValue(c.Key, out var have) && have >= c.Value;
                var text = $"{c.Value} {Loc.Resource(c.Key.ToString())}";
                return enough ? text : $"<color=#FF7369>{text}</color>";
            }));

        private Image Card(float height) => UiKit.Box("Card", _list, UiKit.Field).Row(height);

        private static Button CardButton(Transform card, string label, bool enabled, Action onClick, Color? fill = null,
            float x0 = 0.71f, float y0 = 0.22f, float x1 = 0.97f, float y1 = 0.78f)
        {
            var button = UiKit.Button("Action", card, label, () => onClick(), enabled ? fill ?? UiKit.Accent : UiKit.Panel, 34);
            button.interactable = enabled;
            ((RectTransform)button.transform).Place(x0, y0, x1, y1);
            if (fill == UiKit.Field)
                button.GetComponentInChildren<Text>().color = UiKit.Text; // light text on dark buttons
            return button;
        }

        private static Text CardText(Transform card, string text, int size, Color color, float x0, float y0, float x1, float y1,
            TextAnchor anchor = TextAnchor.UpperLeft, FontStyle style = FontStyle.Normal)
        {
            var label = UiKit.Label("Text", card, text, size, color, anchor, style);
            label.rectTransform.Place(x0, y0, x1, y1);
            return label;
        }

        private void Header(string text) =>
            UiKit.Label("Header", _list, text, 34, UiKit.Accent, TextAnchor.LowerLeft, FontStyle.Bold).Row(60);

        private void Note(string text) =>
            UiKit.Label("Note", _list, text, 30, UiKit.Muted, TextAnchor.MiddleCenter).Row(110);
    }
}
