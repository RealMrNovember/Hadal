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
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// A player's profile card over any screen (Last Asylum: tap a face in chat or a base on the map): the face,
    /// name, alliance, level, power and where the base is, with Message, Invite and Show on map. The player's own
    /// card picks the avatar from the ready-made portraits and leads to the settings.
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
        private GameObject _popup;
        private string _playerId = string.Empty;
        private string _nameHint = string.Empty;
        private string _avatarHint = string.Empty;
        private bool _mine;
        private int _renderedVersion = -1;

        private ClientStateView View => _stateSync.View;

        public bool IsOpen => _popup != null;

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
            if (_popup != null)
                Destroy(_popup);
            _popup = null;
        }

        private void Update()
        {
            if (_popup != null && _renderedVersion != View.ProfileVersion)
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
            var animate = _popup == null;
            Close();
            _renderedVersion = View.ProfileVersion;

            var dim = UiKit.Box("Popup", _canvas.transform, new Color(0, 0, 0, 0.6f));
            dim.rectTransform.Place(0, 0, 1, 1);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            _popup = dim.gameObject;

            var card = UiKit.Box("Card", dim.transform, UiKit.Panel);
            card.rectTransform.Place(0.04f, _mine ? 0.16f : 0.3f, 0.96f, _mine ? 0.84f : 0.72f);
            card.gameObject.AddComponent<Button>();
            if (animate)
            {
                card.gameObject.AddComponent<CanvasGroup>();
                card.gameObject.AddComponent<PopIn>();
            }

            var close = UiKit.Button("Close", card.transform, "X", Close, UiKit.Field, 40);
            close.GetComponentInChildren<Text>().color = UiKit.Text;
            ((RectTransform)close.transform).Place(0.86f, _mine ? 0.92f : 0.86f, 0.97f, 0.985f);

            if (_mine)
                RenderMine(card.transform);
            else
                RenderOther(card.transform);
        }

        private void RenderOther(Transform card)
        {
            var known = View.ProfileCard != null && View.ProfileCard.PlayerId == _playerId ? View.ProfileCard : null;
            var name = known?.Name ?? _nameHint;
            var avatar = known?.AvatarId ?? _avatarHint;

            Face(card, avatar, name, _playerId, 0.04f, 0.5f, 0.32f, 0.95f);
            var tag = string.IsNullOrEmpty(known?.AllianceTag) ? string.Empty : $"[{known.AllianceTag}] ";
            UiKit.Label("Name", card, tag + Escape(name), 40, UiKit.Text, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(0.36f, 0.76f, 0.85f, 0.94f);
            if (known == null)
            {
                UiKit.Label("Wait", card, "…", 40, UiKit.Muted, TextAnchor.MiddleLeft).rectTransform.Place(0.36f, 0.5f, 0.95f, 0.74f);
                return;
            }

            var dot = known.Online ? $"<color=#5BE37A>● {Loc.T("world.online")}</color>" : $"<color=#5E7C88>● {Loc.T("world.offline")}</color>";
            var alliance = string.IsNullOrEmpty(known.AllianceName) ? Loc.T("profile.noalliance") : known.AllianceName;
            UiKit.Label("Info", card, $"{Loc.F("world.level", known.Level)}   {Loc.F("hero.power", known.Power.ToString("N0"))}\n{alliance}\n" +
                                      $"{Loc.F("world.coords", known.X, known.Y)}   {dot}", 28, UiKit.Muted, TextAnchor.UpperLeft)
                .rectTransform.Place(0.36f, 0.4f, 0.97f, 0.76f);

            // Message · Mail · Invite · Show on map, like the genre's profile footer.
            var id = known.PlayerId;
            var x = known.X;
            var y = known.Y;
            var canInvite = AlliancePermissions.CanInvite(View.MyRank()) && string.IsNullOrEmpty(known.AllianceTag);
            var buttons = new List<(string Label, bool Enabled, Action Click)>
            {
                (Loc.T("alliance.message"), true, () => { Close(); _actions.Message?.Invoke(id, name); }),
                (Loc.T("mail.send"), _actions.Mail != null, () => { Close(); _actions.Mail?.Invoke(id, name); }),
                (Loc.T("alliance.invite"), canInvite, () => _commands.Dispatch(new InviteToAllianceCommand
                {
                    CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, PlayerId = id
                })),
                (Loc.T("profile.map"), true, () => { Close(); _actions.ShowOnMap?.Invoke(x, y); })
            };
            for (var i = 0; i < buttons.Count; i++)
            {
                var b = buttons[i];
                var button = UiKit.Button(b.Label, card, b.Label, () => b.Click(), i == 0 ? UiKit.Accent : UiKit.Field, 30);
                ((RectTransform)button.transform).Place(0.03f + i * 0.24f, 0.06f, 0.255f + i * 0.24f, 0.28f);
                button.interactable = b.Enabled;
                if (i > 0)
                    button.GetComponentInChildren<Text>().color = UiKit.Text;
            }
        }

        private void RenderMine(Transform card)
        {
            var me = View.MyProfile ?? new PlayerProfileDto();
            var name = string.IsNullOrEmpty(me.Name) ? View.PlayerId : me.Name;
            Face(card, me.AvatarId, name, View.PlayerId, 0.04f, 0.76f, 0.24f, 0.98f);
            UiKit.Label("Name", card, Escape(name), 40, UiKit.Text, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(0.28f, 0.86f, 0.85f, 0.96f);
            var alliance = View.MyAlliance;
            UiKit.Label("Alliance", card, alliance != null ? $"[{alliance.Tag}] {alliance.Name}" : Loc.T("profile.noalliance"), 28, UiKit.Muted, TextAnchor.MiddleLeft)
                .rectTransform.Place(0.28f, 0.78f, 0.97f, 0.86f);

            UiKit.Label("Pick", card, Loc.T("profile.avatar"), 30, UiKit.Amber, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(0.04f, 0.7f, 0.96f, 0.76f);

            // Ready-made faces: "none" (the initial) and every painted hero portrait, five a row.
            var choices = new List<string> { string.Empty };
            var library = CharacterLibrary.Instance;
            choices.AddRange(ClientCatalog.Catalog.Heroes.Select(h => h.Portrait).Where(p => !string.IsNullOrEmpty(p) && library?.Portrait(p) != null).Distinct());
            const int columns = 5;
            const float top = 0.69f, size = 0.105f;
            for (var i = 0; i < Math.Min(choices.Count, 20); i++)
            {
                var avatarId = choices[i];
                var col = i % columns;
                var row = i / columns;
                var x0 = 0.04f + col * 0.184f;
                var y1 = top - row * (size + 0.01f);
                var cell = UiKit.Rect("Choice", card).Place(x0, y1 - size, x0 + 0.17f, y1);
                var chosen = avatarId == (me.AvatarId ?? string.Empty);
                if (chosen)
                {
                    var ring = cell.gameObject.AddComponent<Image>();
                    ring.sprite = UiKit.CircleRing;
                    ring.preserveAspect = true;
                    ring.color = UiKit.Accent;
                }

                var face = UiKit.Rect("Face", cell).Place(0.08f, 0.08f, 0.92f, 0.92f);
                AvatarDisc.Draw(face, avatarId, name, View.PlayerId);
                var button = cell.gameObject.AddComponent<Button>();
                if (cell.GetComponent<Image>() == null)
                    cell.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);
                button.onClick.AddListener(() => _commands.Dispatch(new SetProfileCommand
                {
                    CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, AvatarId = avatarId
                }));
                cell.gameObject.AddComponent<PressFeedback>();
            }

            // The player's total march speed from every source the server adds up (research, VIP, alliance research).
            var bonuses = PlayerBonuses.Of(View, ClientCatalog.Catalog);
            var speed = Loc.F("profile.marchspeed", bonuses.MarchSpeedPercent);
            if (bonuses.ReturnSpeedPercent > 0)
                speed += "   " + Loc.F("research.march.return", bonuses.ReturnSpeedPercent);
            UiKit.Label("MarchSpeed", card, speed, 26, UiKit.Accent, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Place(0.04f, 0.175f, 0.96f, 0.235f);
            Note(card, Loc.T("profile.upload.soon"), 0.12f, 0.17f);
            var settings = UiKit.Button("Settings", card, Loc.T("profile.settings"), () => { Close(); _actions.Settings?.Invoke(); }, UiKit.Field, 32);
            settings.GetComponentInChildren<Text>().color = UiKit.Text;
            ((RectTransform)settings.transform).Place(0.04f, 0.02f, 0.96f, 0.11f);
        }

        private static void Note(Transform card, string text, float y0, float y1) =>
            UiKit.Label("Note", card, text, 22, UiKit.Muted, TextAnchor.MiddleCenter).rectTransform.Place(0.04f, y0, 0.96f, y1);

        private static void Face(Transform card, string avatarId, string name, string seed, float x0, float y0, float x1, float y1)
        {
            var holder = UiKit.Rect("FaceHolder", card).Place(x0, y0, x1, y1);
            var ring = UiKit.Rect("Ring", holder);
            var fitter = ring.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;
            var rim = ring.gameObject.AddComponent<Image>();
            rim.sprite = UiKit.Circle;
            rim.color = UiKit.Accent;
            rim.raycastTarget = false;
            AvatarDisc.Draw(UiKit.Rect("Inner", ring).Place(0.04f, 0.04f, 0.96f, 0.96f), avatarId, name, seed);
        }

        private static string Escape(string text) => (text ?? string.Empty).Replace("<", "<​");
    }
}
