using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;
using Hadal.Core.Catalog;
using Hadal.Core.Events;
using Hadal.Core.Network;
using Hadal.Core.StateSync;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// A player's profile card over any screen, as Last Asylum's "Doktor Bilgisi" dialog (tap a face in chat or a base
    /// on the map): the portrait card with the level, the power and level pills, then the name, alliance and kingdom
    /// rows, and the navy icon bar under it: Message · Mail · Invite · Show on map for another player; War Log ·
    /// Account · Settings for the player's own, whose portrait opens the ready-made faces to pick from and whose name
    /// row leads to renaming.
    /// </summary>
    public sealed class ProfileCard : MonoBehaviour
    {
        public sealed class Actions
        {
            public Action<string, string> Message;

            /// <summary>Writes this player a mail (the mailbox's compose page).</summary>
            public Action<string, string> Mail;
            public Action<int, int> ShowOnMap;
            public Action Settings;
        }

        public static ProfileCard Instance { get; private set; }

        private IStateSyncService _stateSync;
        private ICommandDispatcher _commands;
        private ILocalEventBus _localBus;
        private Actions _actions;
        private ulong _sequence;

        private Canvas _canvas;
        private UiFrames.DialogSheet _sheet;
        private bool _picking;
        private string _playerId = string.Empty;
        private string _nameHint = string.Empty;
        private string _avatarHint = string.Empty;
        private bool _mine;
        private int _renderedVersion = -1;

        private ClientStateView View => _stateSync.View;

        public bool IsOpen => _sheet is { IsOpen: true };

        public void Initialize(IStateSyncService stateSync, ICommandDispatcher commands, ILocalEventBus localBus, Actions actions)
        {
            Instance = this;
            _stateSync = stateSync;
            _commands = commands;
            _localBus = localBus;
            _actions = actions ?? new Actions();
            _canvas = UiKit.Canvas("ProfileUi", 47);
            _canvas.transform.SetParent(transform, false);
            _localBus?.Subscribe<CommandResolvedEvent>(OnCommandResolved);
        }

        private void OnDestroy()
        {
            _localBus?.Unsubscribe<CommandResolvedEvent>(OnCommandResolved);
            if (Instance == this)
                Instance = null;
        }

        /// <summary>Another player's card; the name and face known so far show until the server answers.</summary>
        public void Show(string playerId, string nameHint = "", string avatarHint = "")
        {
            if (string.IsNullOrEmpty(playerId))
                return;
            if (playerId == View.PlayerId)
            {
                ShowMine();
                return;
            }

            _mine = false;
            _playerId = playerId;
            _nameHint = nameHint ?? string.Empty;
            _avatarHint = avatarHint ?? string.Empty;
            _commands.Dispatch(new ViewProfileCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, PlayerId = playerId });
            Render();
        }

        /// <summary>The player's own card: the avatar picker.</summary>
        public void ShowMine()
        {
            _mine = true;
            _playerId = View.PlayerId;
            Render();
        }

        public void Close()
        {
            _sheet?.Close();
            _sheet = null;
        }

        private void Update()
        {
            if (_sheet is { IsOpen: true } && _renderedVersion != View.ProfileVersion)
                Render();
        }

        private void OnCommandResolved(CommandResolvedEvent result)
        {
            if (result.Type is not (CommandType.ViewProfile or CommandType.SetProfile))
                return;
            if (result.Result == CommandResultCode.Rejected)
            {
                Toaster.Show(Loc.Reason(result.Reason), UiKit.Danger);
                if (result.Type == CommandType.ViewProfile)
                    Close();
                return;
            }

            if (result.Type == CommandType.SetProfile)
                Toaster.Show(Loc.T("profile.saved"), UiKit.Accent);
        }

        private void Render()
        {
            _renderedVersion = View.ProfileVersion;
            if (_sheet is not { IsOpen: true })
            {
                _picking = false;
                _sheet = UiFrames.OpenSheet(_canvas.transform, Loc.T("profile.title"), Close, footer: 150f);
            }

            UiFrames.ClearSheet(_sheet);
            if (_mine)
                RenderMine(_sheet.Content);
            else
                RenderOther(_sheet.Content);
            UiFrames.FitSheet(_sheet);
        }

        /// <summary>
        /// Last Asylum's "Doktor Bilgisi" head: the portrait card on the left (the face, the level under it), the power
        /// and level pills on the right, and the online state for another player.
        /// </summary>
        private void Head(Transform content, string avatarId, string name, string seed, long power, int level, string note, Action onFace)
        {
            var head = UiKit.Rect("Head", content).Row(330);
            var frame = UiFrames.SheetBlock(head, Color.white, "Portrait");
            frame.rectTransform.Place(0.02f, 0.02f, 0.42f, 0.98f);
            var shadow = frame.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.1f, 0.16f, 0.28f, 0.4f);
            shadow.effectDistance = new Vector2(3, -5);
            var faceArea = UiKit.Rect("Face", frame.transform).Place(0.06f, 0.22f, 0.94f, 0.96f);
            Face(faceArea, avatarId, name, seed, 0, 0, 1, 1);
            if (onFace != null)
            {
                frame.raycastTarget = true;
                frame.gameObject.AddComponent<Button>().onClick.AddListener(() => onFace());
                frame.gameObject.AddComponent<PressFeedback>();
                UiFrames.Glyph(frame.transform, "✏", 0.76f, 0.76f, 0.96f, 0.96f, UiFrames.InkSoft);
            }

            var lv = UiKit.Label("Level", frame.transform, Loc.F("world.level", level), UiKit.TypeScale.Body, UiFrames.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            lv.rectTransform.Place(0.04f, 0.02f, 0.96f, 0.2f);

            UiFrames.StatPill(head, "power", power.ToString("N0")).Place(0.46f, 0.74f, 0.98f, 0.96f);
            UiFrames.StatPill(head, "left_home", Loc.F("world.level", level)).Place(0.46f, 0.48f, 0.98f, 0.7f);
            if (!string.IsNullOrEmpty(note))
                UiKit.Label("Note", head, note, UiKit.TypeScale.Body, UiFrames.InkSoft, TextAnchor.MiddleCenter).rectTransform.Place(0.46f, 0.08f, 0.98f, 0.44f);
        }

        private void RenderOther(Transform content)
        {
            var known = View.ProfileCard != null && View.ProfileCard.PlayerId == _playerId ? View.ProfileCard : null;
            var name = known?.Name ?? _nameHint;
            var avatar = known?.AvatarId ?? _avatarHint;
            var tag = string.IsNullOrEmpty(known?.AllianceTag) ? string.Empty : $"[{known.AllianceTag}] ";
            var online = known == null ? "…"
                : known.Online ? $"<color=#2E9E4A>● {Loc.T("world.online")}</color>" : $"<color=#6B7A88>● {Loc.T("world.offline")}</color>";
            Head(content, avatar, name, _playerId, known?.Power ?? 0, known?.Level ?? 0, online, null);
            UiFrames.InfoRow(content, null, tag + Escape(name)).Row(84);
            if (known == null)
            {
                Footer(Array.Empty<(string, string, Action, bool)>());
                return;
            }

            var alliance = string.IsNullOrEmpty(known.AllianceName) ? Loc.T("profile.noalliance") : $"{tag}{known.AllianceName}";
            UiFrames.InfoRow(content, "side_alliance", alliance, anchor: TextAnchor.MiddleRight).Row(84);
            if (known.Kingdom > 0)
                UiFrames.InfoRow(content, "🏰", Loc.F("kingdom.number", known.Kingdom), "🔍", () => KingdomList.Show(_stateSync, _commands), TextAnchor.MiddleRight).Row(84);
            var x = known.X;
            var y = known.Y;
            UiFrames.InfoRow(content, "nav_world", Loc.F("world.coords", x, y), "🔍", () => { Close(); _actions.ShowOnMap?.Invoke(x, y); }, TextAnchor.MiddleRight)
                .Row(84);

            // Message · Mail · Invite · Show on map, in the icon bar under the card.
            var id = known.PlayerId;
            var canInvite = AlliancePermissions.CanInvite(View.MyRank()) && string.IsNullOrEmpty(known.AllianceTag);
            Footer(new (string, string, Action, bool)[]
            {
                ("g_chat", Loc.T("alliance.message"), () => { Close(); _actions.Message?.Invoke(id, name); }, true),
                ("side_mail", Loc.T("mail.send"), () => { Close(); _actions.Mail?.Invoke(id, name); }, _actions.Mail != null),
                ("g_invite", Loc.T("alliance.invite"), () => _commands.Dispatch(new InviteToAllianceCommand
                {
                    CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, PlayerId = id
                }), canInvite),
                ("nav_world", Loc.T("profile.map"), () => { Close(); _actions.ShowOnMap?.Invoke(x, y); }, true)
            });
        }

        private void RenderMine(Transform content)
        {
            var me = View.MyProfile ?? new PlayerProfileDto();
            var name = string.IsNullOrEmpty(me.Name) ? View.PlayerId : me.Name;
            var alliance = View.MyAlliance;
            Head(content, me.AvatarId, name, View.PlayerId, me.Power, me.Level, null, () =>
            {
                _picking = !_picking;
                Render();
            });

            // Name (edit: the account page renames), alliance, kingdom (the kingdom list).
            var tag = alliance != null ? $"[{alliance.Tag}] " : string.Empty;
            UiFrames.InfoRow(content, null, tag + Escape(name), "✏", () =>
            {
                Close();
                _actions.Settings?.Invoke();
                FindFirstObjectByType<AccountPanelView>()?.OpenAccount();
            }).Row(84);
            UiFrames.InfoRow(content, "side_alliance", alliance != null ? $"[{alliance.Tag}] {alliance.Name}" : Loc.T("profile.noalliance"),
                anchor: TextAnchor.MiddleRight).Row(84);
            UiFrames.InfoRow(content, "🏰", Loc.F("kingdom.number", View.MyKingdom), "🔍", () => KingdomList.Show(_stateSync, _commands), TextAnchor.MiddleRight)
                .Row(84);

            if (_picking)
                AvatarPicker(content, me, name);

            // Record (the war log) · Account · Settings, Last Asylum's bar under the card.
            Footer(new (string, string, Action, bool)[]
            {
                ("📜", Loc.T("pvp.tab.log"), () => { Close(); PvpCenter.Instance?.Open(1); }, PvpCenter.Instance != null),
                ("g_profile", Loc.T("settings.account"), () =>
                {
                    Close();
                    _actions.Settings?.Invoke();
                    FindFirstObjectByType<AccountPanelView>()?.OpenAccount();
                }, _actions.Settings != null),
                ("⚙", Loc.T("profile.settings"), () => { Close(); _actions.Settings?.Invoke(); }, _actions.Settings != null)
            });
        }

        /// <summary>Ready-made faces, five a row: "none" (the initial) and every painted hero portrait; the chosen one ringed.</summary>
        private void AvatarPicker(Transform content, PlayerProfileDto me, string name)
        {
            UiFrames.Section(content, Loc.T("profile.avatar")).Row(60);
            var choices = new List<string> { string.Empty };
            var library = CharacterLibrary.Instance;
            choices.AddRange(ClientCatalog.Catalog.Heroes.Select(h => h.Portrait).Where(p => !string.IsNullOrEmpty(p) && library?.Portrait(p) != null).Distinct());
            var grid = UiFrames.Grid(content, 5, 1f, 10f);
            foreach (var avatarId in choices.Take(20))
            {
                var cell = UiKit.Rect("Choice", grid);
                var chosen = avatarId == (me.AvatarId ?? string.Empty);
                var ring = cell.gameObject.AddComponent<Image>();
                ring.sprite = UiKit.CircleRing;
                ring.preserveAspect = true;
                ring.color = chosen ? UiKit.Primary : new Color(0, 0, 0, 0);
                var face = UiKit.Rect("Face", cell).Place(0.08f, 0.08f, 0.92f, 0.92f);
                AvatarDisc.Draw(face, avatarId, name, View.PlayerId);
                var id = avatarId;
                cell.gameObject.AddComponent<Button>().onClick.AddListener(() => _commands.Dispatch(new SetProfileCommand
                {
                    CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, AvatarId = id
                }));
                cell.gameObject.AddComponent<PressFeedback>();
            }

            UiFrames.SheetText(content, Loc.T("profile.upload.soon"), UiKit.TypeScale.Caption).Row(48);
        }

        private void Footer((string Icon, string Label, Action Click, bool Enabled)[] buttons)
        {
            if (buttons.Length > 0)
                UiFrames.IconBar(_sheet.Footer, buttons).Place(0, 0, 1, 1);
        }

        private static void Face(Transform card, string avatarId, string name, string seed, float x0, float y0, float x1, float y1)
        {
            var holder = UiKit.Rect("FaceHolder", card).Place(x0, y0, x1, y1);
            var ring = UiKit.Rect("Ring", holder);
            var fitter = ring.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;
            var rim = ring.gameObject.AddComponent<Image>();
            rim.sprite = UiKit.Rounded;
            rim.type = Image.Type.Sliced;
            rim.color = UiFrames.CardAlt;
            rim.raycastTarget = false;
            AvatarDisc.Draw(UiKit.Rect("Inner", ring).Place(0.03f, 0.03f, 0.97f, 0.97f), avatarId, name, seed);
        }

        private static string Escape(string text) => (text ?? string.Empty).Replace("<", "<​");
    }
}
