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
using TMPro;
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
            Pod,
            PodResult,
            Alliance,
            Chat,
            Quests,
            Bag,
            Reward,
            Expedition,
            DiveResult,
            Events,
            EventRanking,
            Vip,
            Shop,
            Mail,
            MailDetail,
            MailCompose
        }

        private static readonly CommandType[] OwnCommands =
        {
            CommandType.ExecuteGacha, CommandType.LevelUpHero, CommandType.PromoteHero, CommandType.AssignHero, CommandType.SendChat,
            CommandType.CreateAlliance, CommandType.JoinAlliance, CommandType.LeaveAlliance, CommandType.BrowseAlliances,
            CommandType.KickMember, CommandType.RequestHelp, CommandType.HelpAll,
            CommandType.ClaimQuest, CommandType.ClaimDailyChest, CommandType.OpenItem, CommandType.RecruitHero,
            CommandType.DiveStage, CommandType.SetMemberRank, CommandType.TransferLeadership, CommandType.SetAllianceSettings,
            CommandType.AnswerInvite, CommandType.AnswerApplication, CommandType.ClaimEventStage, CommandType.ClaimEventRank,
            CommandType.ClaimVipLogin, CommandType.ClaimVipChest, CommandType.ReadMail, CommandType.ClaimMail, CommandType.DeleteMail,
            CommandType.SendMail, CommandType.SetAllianceInfo, CommandType.BuyAllianceItem, CommandType.CollectAllianceGift,
            CommandType.DonateAllianceResearch, CommandType.SetResearchFocus, CommandType.CreateAlliancePoll, CommandType.VoteAlliancePoll,
            CommandType.CloseAlliancePoll, CommandType.MobilizationTask, CommandType.TrenchConquest, CommandType.LureTrap, CommandType.AbyssSiege
        };

        private IStateSyncService _stateSync;
        private ICommandDispatcher _commands;
        private ILocalEventBus _localBus;
        private ulong _sequence;
        private static GameCatalog _catalog => ClientCatalog.Catalog;

        private Canvas _canvas;
        private GameObject _sheet;
        private TMP_Text _title;
        private TMP_Text _subtitle;
        private RectTransform _list;
        private RectTransform _footer;
        private TMP_Text _toast;
        private float _toastUntil;
        private bool _keyboardUp;

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

        /// <summary>The page's body under the top bar; page parts that float over the list (tabs, bars) sit in it.</summary>
        private RectTransform _body;

        private void OnDestroy() => _localBus?.Unsubscribe<CommandResolvedEvent>(OnCommandResolved);

        // --- Layout --------------------------------------------------------------------------

        private void Build()
        {
            _canvas = UiKit.Canvas("SocialUi", 45);
            _canvas.transform.SetParent(transform, false);

            // The HUD (Hud/MainHudView) opens these pages; the sheet sits under the HUD header.

            var safe = UiKit.Rect("Safe", _canvas.transform).Place(0, 0, 1, 1);
            safe.gameObject.AddComponent<Hud.SafeArea>();
            // The sheet reaches up to the HUD header, which sits at the very top of the screen.
            // Every page is the genre's full screen (docs/41): the navy bar with the back arrow and title over a light body.
            var page = UiKit.Rect("Page", _canvas.transform).Place(0, 0, 1, 1);
            page.gameObject.AddComponent<Hud.SafeArea>();
            var parts = UiFrames.Page(page, PageBack); // one step back inside a page, then one page back, then closed
            _sheet = page.gameObject;
            _sheet.AddComponent<CanvasGroup>();
            _sheet.AddComponent<PopIn>().From = 0.97f;
            _title = parts.Title;
            _subtitle = parts.Subtitle;
            _body = parts.Body;

            _list = UiKit.ScrollList("List", _body);
            ((RectTransform)_list.parent).Place(0.02f, 0.02f, 0.98f, 0.965f);

            // Chat input lives outside the list so redraws never touch it.
            _footer = UiKit.Rect("Footer", _body).Place(0.03f, 0.018f, 0.97f, 0.085f);
            _footer.gameObject.SetActive(false);

            _toast = UiKit.Label("Toast", _canvas.transform, string.Empty, 38, UiKit.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            _toast.rectTransform.Place(0.05f, 0.46f, 0.95f, 0.52f);
            UiText.Outline(_toast, new Color(0, 0, 0, 0.8f));
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
            _title.color = Color.white;
            _stage?.Hide();
            ClosePollDialog();
            CloseDutyPicker();
            _sheet.SetActive(false);
            _footer.gameObject.SetActive(false);
            if (_heroBar != null)
                _heroBar.gameObject.SetActive(false);
            if (_chatTabs != null)
                _chatTabs.gameObject.SetActive(false);
            _page = Page.None;
        }

        private void Update()
        {
            if (_canvas == null || _stateSync == null)
                return;

            if (_toast.text.Length > 0 && Time.unscaledTime > _toastUntil)
                _toast.text = string.Empty;
            SendPendingPin();

            // The page rises above the on-screen keyboard while the chat input is focused.
            if (_sheet.activeSelf)
            {
                if (_page != Page.Chat)
                    CloseEmojiPicker();
                FollowChatBottom();
                var sheet = (RectTransform)_sheet.transform;
                var lift = KeyboardLift();
                sheet.offsetMin = new Vector2(sheet.offsetMin.x, Mathf.Lerp(sheet.offsetMin.y, lift, 0.4f));
                // When the keyboard opens, show the newest lines right above the input.
                // On forms (founding, settings) the field being typed in moves to the top instead.
                if (lift > 0f && !_keyboardUp)
                {
                    var field = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
                    if (_page == Page.Chat)
                        ScrollChatToBottom(frames: 20); // the page rises over several frames; stay on the newest line
                    else
                        _list.anchoredPosition = field != null && field.transform.IsChildOf(_list)
                            ? new Vector2(0, Mathf.Max(0f, -((RectTransform)field.transform).anchoredPosition.y - 120f))
                            : new Vector2(0, 100000);
                }
                _keyboardUp = lift > 0f;
            }

            UpdateBar();

            while (View.TryTakeGachaResult(out var result))
            {
                _podResult = result;
                Open(Page.PodResult);
            }

            while (View.TryTakeDiveReport(out var dive))
            {
                _dive = dive;
                Open(Page.DiveResult);
            }

            while (View.TryTakeRewardReport(out var report))
            {
                // Event rewards return to the event page when the player taps Done.
                if (_page is Page.Events or Page.EventRanking)
                    _afterReward = Page.Events;
                else if (_page is Page.Vip)
                    _afterReward = Page.Vip;
                else if (_page is Page.Mail or Page.MailDetail)
                    _afterReward = _page;
                else if (_page is Page.Alliance)
                    _afterReward = Page.Alliance;
                _reward = report;
                Open(Page.Reward);
            }

            TickEvents();

            if (!IsOpen)
                return;

            if (Input.touchCount > 0 || Input.GetMouseButton(0))
                _lastPointerTime = Time.unscaledTime;
            if (Time.unscaledTime - _lastPointerTime <= 0.3f)
                return;

            // The Cryo-Pod ticks once a second (free pull timer); the others, the living hero card among them, only when
            // their data changed.
            var key = PageKey();
            if (key != _renderedKey || (_page is Page.Pod && Time.unscaledTime >= _nextRefresh))
                Render(force: false);
        }

        private string PageKey() => _page switch
        {
            Page.Heroes or Page.HeroDetail or Page.Pod => $"{_page}|{_heroId}|{HeroesKey()}|{View.QuestVersion}",
            Page.Alliance => $"{_page}|{View.AllianceVersion}|{_confirmLeave}|{_allianceTab}|{View.AllianceLogVersion}|{_logTab}|{View.AllianceCoinsVersion}|{View.AllianceGiftsVersion}|{View.AllianceResearchVersion}|{_researchTab}|{View.AllianceRankingVersion}|{_rankingTab}|{View.AlliancePollsVersion}|{(_allianceTab == AllianceTabPolls ? NowMs / 60_000L : 0)}",
            Page.Chat => $"{_page}|{View.ChatVersion}|{_chatChannel}|{_chatPeer}|{View.ProfileVersion}",
            Page.Quests or Page.Bag => $"{_page}|{QuestKey()}",
            Page.Expedition => $"{_page}|{ExpeditionKey()}",
            Page.Events or Page.EventRanking => $"{_page}|{EventsKey()}",
            Page.Vip or Page.Shop => $"{_page}|{VipKey()}",
            Page.Mail or Page.MailDetail => $"{_page}|{MailKey()}",
            Page.MailCompose => $"{_page}|{ComposeKey()}",
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
            var samePage = key.Split('|')[0] == _renderedKey.Split('|')[0];
            var scroll = !force && samePage ? _list.anchoredPosition : Vector2.zero;
            // Chat follows new lines only while the player is at (or within about a bubble of) the bottom, and always
            // after they sent a line themselves; opening the page or switching channels starts at the bottom.
            var atBottom = force || !samePage || ChatNearBottom() || ChatOwnLineArrived();
            _renderedKey = key;

            for (var i = _list.childCount - 1; i >= 0; i--)
                Destroy(_list.GetChild(i).gameObject);

            // Chat leaves room for its channel tabs and input bar, the hero list for its bottom bar.
            var listRect = (RectTransform)_list.parent;
            listRect.Place(0.02f, _page == Page.Chat ? 0.1f : _page == Page.Heroes ? 0.1f : 0.02f, 0.98f, _page == Page.Chat ? 0.905f : 0.965f);
            if (_heroBar != null)
                _heroBar.gameObject.SetActive(false); // the hero list turns its "Get heroes" bar back on
            if (_page != Page.Chat && _chatTabs != null)
                _chatTabs.gameObject.SetActive(false);
            _footer.gameObject.SetActive(_page == Page.Chat && !(_chatChannel == ChatChannel.Private && _chatPeer.Length == 0));

            if (_page != Page.HeroDetail)
                _stage.Hide();

            switch (_page)
            {
                case Page.Heroes: RenderHeroes(); break;
                case Page.HeroDetail: RenderHeroDetail(); break;
                case Page.Pod: RenderPod(); break;
                case Page.PodResult: RenderPodResult(); break;
                case Page.Alliance: RenderAlliance(); break;
                case Page.Chat: RenderChat(force); break;
                case Page.Quests: RenderQuests(); break;
                case Page.Bag: RenderBag(); break;
                case Page.Reward: RenderReward(); break;
                case Page.Expedition: RenderExpedition(); break;
                case Page.DiveResult: RenderDiveResult(); break;
                case Page.Events: RenderEvents(); break;
                case Page.EventRanking: RenderEventRanking(); break;
                case Page.Vip: RenderVip(); break;
                case Page.Shop: RenderShop(); break;
                case Page.Mail: RenderMail(); break;
                case Page.MailDetail: RenderMailDetail(); break;
                case Page.MailCompose: RenderMailCompose(); break;
            }

            if (_page == Page.Chat && atBottom)
            {
                ScrollChatToBottom();
            }
            else
            {
                _list.anchoredPosition = scroll;
                _chatBottomFrames = 0;
            }

            ShowNewMessages(_page == Page.Chat ? UnseenChatLines() : 0);
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

            if (result.Type == CommandType.CreateAlliancePoll)
                ClosePollDialog();
            if (result.Type is CommandType.VoteAlliancePoll or CommandType.CloseAlliancePoll)
                ForgetPollPicks(); // picks start again from the ballot the server now holds

            var text = result.Type switch
            {
                CommandType.LevelUpHero => Loc.T("hero.trained"),
                CommandType.PromoteHero => Loc.T("hero.promoted"),
                CommandType.AssignHero => Loc.T("hero.assigned"),
                CommandType.CreateAlliance => Loc.T("alliance.created"),
                CommandType.JoinAlliance => View.MyAlliance != null ? Loc.T("alliance.joined") : Loc.T("alliance.applied.toast"),
                CommandType.SetMemberRank => Loc.T("alliance.rank.saved"),
                CommandType.TransferLeadership => Loc.T("alliance.transferred"),
                CommandType.SetAllianceSettings => Loc.T("alliance.saved"),
                CommandType.SetAllianceInfo => Loc.T("alliance.saved"),
                CommandType.DonateAllianceResearch => Loc.T("alliance.research.donated"),
                CommandType.SetResearchFocus => Loc.T("alliance.saved"),
                CommandType.CreateAlliancePoll => Loc.T("alliance.poll.created"),
                CommandType.VoteAlliancePoll => Loc.T("alliance.poll.voted"),
                CommandType.CloseAlliancePoll => Loc.T("alliance.poll.closedToast"),
                CommandType.AnswerInvite => View.MyAlliance != null ? Loc.T("alliance.joined") : string.Empty,
                CommandType.AnswerApplication => Loc.T("alliance.saved"),
                CommandType.LeaveAlliance => Loc.T("alliance.left"),
                CommandType.KickMember => Loc.T("alliance.kicked"),
                CommandType.RequestHelp => Loc.T("alliance.help.requested"),
                CommandType.HelpAll => Loc.T("alliance.help.sent"),
                CommandType.RecruitHero => Loc.T("hero.recruited"),
                CommandType.DeleteMail => Loc.T("mail.deleted"),
                CommandType.SendMail => MailSent(),
                _ => string.Empty
            };
            if (text.Length > 0)
                Toast(text, UiKit.Accent);
            if (IsOpen && _page != Page.Chat)
                Render(force: false);
        }

        private void Toast(string text, Color color) => Toaster.Show(text, color);

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
            if (fill == UiKit.Field || !enabled)
                button.GetComponentInChildren<TMP_Text>().color = enabled ? UiKit.Text : UiKit.Muted; // light text on dark buttons
            return button;
        }

        private static TMP_Text CardText(Transform card, string text, int size, Color color, float x0, float y0, float x1, float y1,
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
