using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using Hadal.UI.Hud;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>Chat page: world and alliance channels, newest line at the bottom, input bar below (docs/25).</summary>
    public sealed partial class SocialView
    {
        private ChatChannel _chatChannel = ChatChannel.World;

        // Private channel: the player the conversation is with (empty = the list of conversations).
        private string _chatPeer = string.Empty;
        private string _chatPeerName = string.Empty;

        /// <summary>Opens a one-to-one conversation (from the world map or the member list).</summary>
        public void OpenPrivateChat(string peerId, string peerName)
        {
            _chatChannel = ChatChannel.Private;
            _chatPeer = peerId ?? string.Empty;
            _chatPeerName = peerName ?? string.Empty;
            Open(Page.Chat);
        }

        /// <summary>The other player of a private line, from the viewer's side.</summary>
        private (string Id, string Name) Peer(ChatMessageDto line) =>
            line.SenderId == View.PlayerId ? (line.TargetId, line.TargetName) : (line.SenderId, line.SenderName);
        private TMP_InputField _chatInput;
        private RectTransform _chatTabs;

        private void RenderChat(bool force)
        {
            _title.text = Loc.T("social.chat");
            _subtitle.text = string.Empty;
            BuildChatFooter();

            var inAlliance = View.MyAlliance != null;
            if (!inAlliance && _chatChannel == ChatChannel.Alliance)
                _chatChannel = ChatChannel.World;

            // Channel tabs stay fixed under the title (the list below scrolls to the newest line).
            if (_chatTabs != null)
                Destroy(_chatTabs.gameObject);
            _chatTabs = UiKit.Choice("Channels", _body, new[] { $"{Loc.T("chat.world")} · {Loc.F("kingdom.short", View.MyKingdom)}", inAlliance ? $"[{View.MyAlliance.Tag}] {Loc.T("chat.alliance")}" : Loc.T("chat.alliance"), Loc.T("chat.private") },
                () => (int)_chatChannel, i =>
                {
                    if (i == (int)ChatChannel.Alliance && View.MyAlliance == null)
                    {
                        Toast(Loc.Reason(nameof(SocialRejectReason.NotInAlliance)), UiKit.Danger);
                        return;
                    }

                    _chatChannel = (ChatChannel)i;
                    _chatPeer = string.Empty;
                    Render(force: true);
                });
            _chatTabs.Place(0.03f, 0.915f, 0.97f, 0.975f);

            if (_chatChannel == ChatChannel.Private && _chatPeer.Length == 0)
            {
                RenderConversations();
                return;
            }

            if (_chatChannel == ChatChannel.Private)
            {
                var back = Card(90);
                CardText(back.transform, "‹ " + _chatPeerName, 32, UiKit.Accent, 0.04f, 0.05f, 0.7f, 0.95f, TextAnchor.MiddleLeft, FontStyle.Bold);
                CardButton(back.transform, Loc.T("chat.private.all"), true, () =>
                {
                    _chatPeer = string.Empty;
                    Render(force: true);
                }, UiKit.Field, 0.62f, 0.12f, 0.97f, 0.88f);
            }

            var any = false;
            foreach (var line in View.Chat)
            {
                if (line.Channel != (int)_chatChannel)
                    continue;
                if (_chatChannel == ChatChannel.Private && Peer(line).Id != _chatPeer)
                    continue;
                any = true;
                Bubble(line);
            }

            if (!any)
                Note(Loc.T("chat.empty"));
        }

        /// <summary>
        /// A chat line in the Last Asylum layout: a round avatar with the sender's initial, "[TAG] Name · time" above a
        /// speech bubble; others on the left in a light bubble, mine on the right in blue; system notices centred.
        /// </summary>
        private void Bubble(ChatMessageDto line)
        {
            if (line.IsSystem)
            {
                var notice = UiKit.Box("System", _list, new Color(0f, 0f, 0f, 0.25f)).Row(58);
                notice.raycastTarget = false;
                UiKit.Label("Text", notice.transform, ChatLine(line, compact: false), 22, UiKit.Muted, TextAnchor.MiddleCenter)
                    .rectTransform.Place(0.03f, 0, 0.97f, 1);
                return;
            }

            var mine = line.SenderId == View.PlayerId;
            var text = line.Text ?? string.Empty;
            var pin = Pin.Parse(text);
            if (pin != null)
                text = new string(' ', 60);
            // Bubble width follows the text (about 28 characters a line at full width), height the wrapped lines.
            // TMP_Text elements, not UTF-16 units: an emoji is one character on screen (a little wider than a letter).
            var length = pin != null ? text.Length : SocialRules.TextLength(text) + (Emoji.Has(text) ? 1 : 0);
            var lines = Math.Max(1, (int)Math.Ceiling(length / 28f));
            var width = lines > 1 ? 0.66f : Mathf.Clamp(0.12f + length * 0.021f, 0.16f, 0.66f);
            var row = UiKit.Rect("Line", _list).Row(96 + 40 * lines);

            // Avatar: the chosen portrait (else the initial on the player's colour); tapping it opens the profile.
            var name = line.SenderName ?? string.Empty;
            var avatar = UiKit.Rect("Avatar", row);
            avatar.Place(mine ? 0.87f : 0.01f, Mathf.Max(0f, 1f - 100f / (96 + 40 * lines)), mine ? 0.99f : 0.13f, 1f);
            // My own lines show the face I have now, not the one sent with older lines.
            var senderAvatar = mine ? View.MyProfile?.AvatarId ?? string.Empty : line.SenderAvatar;
            AvatarDisc.Draw(avatar, senderAvatar, name, line.SenderId, mine ? new Color(0.13f, 0.52f, 0.86f, 1f) : null);
            avatar.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            var senderId = line.SenderId;
            avatar.gameObject.AddComponent<Button>().onClick.AddListener(() => ProfileCard.Instance?.Show(senderId, name, senderAvatar));
            avatar.gameObject.AddComponent<PressFeedback>();

            var tag = string.IsNullOrEmpty(line.AllianceTag) ? string.Empty : $"[{line.AllianceTag}] ";
            var time = DateTimeOffset.FromUnixTimeMilliseconds(line.SentAtUnixMs).ToLocalTime().ToString("HH:mm");
            var header = mine ? $"<color=#5E7C88>{time}</color>  {Loc.T("chat.you")}" : $"{tag}{Escape(name)}  <color=#5E7C88>{time}</color>";
            UiKit.Label("Name", row, header, 22, mine ? UiKit.Accent : UiKit.Amber, mine ? TextAnchor.UpperRight : TextAnchor.UpperLeft, FontStyle.Bold)
                .rectTransform.Place(mine ? 0.15f : 0.16f, 0.72f, mine ? 0.84f : 0.85f, 1f);

            var x0 = mine ? 0.84f - width : 0.16f;
            var bubble = UiKit.Box("Bubble", row, mine ? new Color(0.13f, 0.52f, 0.86f, 1f) : new Color(0.88f, 0.92f, 0.95f, 1f));
            bubble.rectTransform.Place(x0, 0.02f, x0 + width, 0.74f);
            bubble.raycastTarget = false;
            if (pin != null)
            {
                PinCard(bubble, pin, mine);
                return;
            }

            var body = UiKit.EmojiLabel("Text", bubble.transform, text, 28, mine ? Color.white : new Color(0.08f, 0.14f, 0.2f, 1f), TextAnchor.MiddleLeft);
            body.richText = false;
            body.rectTransform.Place(0, 0, 1, 1);
            body.rectTransform.offsetMin = new Vector2(18f, 6f);
            body.rectTransform.offsetMax = new Vector2(-18f, -6f);
        }

        /// <summary>One card per person the player has written with, newest first.</summary>
        private void RenderConversations()
        {
            _footer.gameObject.SetActive(false);
            var conversations = View.Chat.Where(l => l.Channel == (int)ChatChannel.Private)
                .GroupBy(l => Peer(l).Id)
                .Select(g => g.OrderBy(l => l.Id).Last())
                .OrderByDescending(l => l.Id)
                .ToList();
            if (conversations.Count == 0)
            {
                Note(Loc.T("chat.private.none"));
                return;
            }

            foreach (var last in conversations)
            {
                var (id, name) = Peer(last);
                var card = Card(130);
                CardText(card.transform, name, 32, UiKit.Text, 0.04f, 0.5f, 0.7f, 0.95f, TextAnchor.UpperLeft, FontStyle.Bold);
                var preview = UiKit.EmojiLabel("Text", card.transform, PinPreview(last.Text) ?? last.Text, 26, UiKit.Muted, TextAnchor.UpperLeft);
                preview.richText = false;
                preview.rectTransform.Place(0.04f, 0.05f, 0.7f, 0.5f);
                CardButton(card.transform, Loc.T("chat.open"), true, () => OpenPrivateChat(id, name), UiKit.Accent);
            }
        }

        private void BuildChatFooter()
        {
            if (_chatInput != null)
                return;

            // Last Asylum's input bar: a white rounded field and a round blue send button.
            // The limit counts text elements (an emoji is one); the field's own limit only bounds UTF-16 units.
            var maxLength = Math.Max(1, _catalog.Chat.maxLength);
            _chatInput = UiKit.Input("Message", _footer, Loc.T("chat.hint"), characterLimit: maxLength * SocialRules.MaxUnitsPerElement);
            ((RectTransform)_chatInput.transform).Place(0.13f, 0, 0.72f, 1);
            _chatInput.onValueChanged.AddListener(value =>
            {
                var cut = SocialRules.CutToTextElements(value, maxLength);
                if (cut.Length != value.Length)
                    _chatInput.text = cut;
            });

            // Location button: shares the player's own base in the open channel (Last Asylum's "share coordinates").
            var share = UiKit.Rect("ShareMine", _footer).Place(0f, -0.05f, 0.11f, 1.05f);
            var shareDisc = share.gameObject.AddComponent<Image>();
            shareDisc.sprite = UiKit.Circle;
            shareDisc.preserveAspect = true;
            shareDisc.color = new Color(0.05f, 0.2f, 0.27f, 1f);
            share.gameObject.AddComponent<Button>().onClick.AddListener(ShareMyBase);
            share.gameObject.AddComponent<PressFeedback>();
            HudIcons.Image(share, "nav_world", 0.18f, 0.18f, 0.82f, 0.82f).raycastTarget = false;
            _chatInput.image.color = new Color(0.96f, 0.97f, 0.98f, 1f);
            foreach (var frame in _chatInput.GetComponentsInChildren<Image>())
                if (frame != _chatInput.image)
                    frame.enabled = false;
            _chatInput.textComponent.color = new Color(0.08f, 0.14f, 0.2f, 1f);
            ((TMP_Text)_chatInput.placeholder).color = new Color(0.45f, 0.52f, 0.58f, 1f);
            // Typed inside the white field with its caret (UiKit.Input hides Android's edit box); the page lifts
            // above the keyboard (KeyboardLift). The keyboard's "Done" sends (onSubmit), Return on a hardware keyboard too.
            _chatInput.onSubmit.AddListener(_ => SendChat());
            _chatInput.onEndEdit.AddListener(_ =>
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    SendChat();
            });

            // Emoji quick-pick: a round button left of send opens a grid above the input bar.
            var smiley = UiKit.Rect("EmojiButton", _footer).Place(0.735f, -0.05f, 0.835f, 1.05f);
            var smileyDisc = smiley.gameObject.AddComponent<Image>();
            smileyDisc.sprite = UiKit.Circle;
            smileyDisc.preserveAspect = true;
            smileyDisc.color = new Color(0.05f, 0.2f, 0.27f, 1f);
            smiley.gameObject.AddComponent<Button>().onClick.AddListener(ToggleEmojiPicker);
            smiley.gameObject.AddComponent<PressFeedback>();
            UiKit.EmojiLabel("Icon", smiley, "🙂", 40, Color.white, TextAnchor.MiddleCenter).rectTransform.Place(0, 0, 1, 1);

            var send = UiKit.Rect("Send", _footer).Place(0.85f, -0.05f, 1f, 1.05f);
            var disc = send.gameObject.AddComponent<Image>();
            disc.sprite = UiKit.Circle;
            disc.preserveAspect = true;
            disc.color = new Color(0.13f, 0.52f, 0.86f, 1f);
            send.gameObject.AddComponent<Button>().onClick.AddListener(() =>
            {
                SendChat();
                CloseEmojiPicker();
            });
            send.gameObject.AddComponent<PressFeedback>();
            UiKit.Label("Icon", send, "▶", 40, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Place(0.08f, 0, 1, 1);
        }

        private GameObject _emojiPicker;

        private void CloseEmojiPicker()
        {
            if (_emojiPicker != null)
                _emojiPicker.SetActive(false);
        }

        /// <summary>A grid of common emoji above the input bar; a tap adds one at the end of the message.</summary>
        private void ToggleEmojiPicker()
        {
            if (_emojiPicker == null)
            {
                var panel = UiKit.Box("EmojiPicker", _body, new Color(0.04f, 0.11f, 0.16f, 0.97f));
                panel.rectTransform.Place(0.03f, 0.105f, 0.97f, 0.36f);
                _emojiPicker = panel.gameObject;
                const int columns = 8;
                var rows = (Emoji.Common.Length + columns - 1) / columns;
                for (var i = 0; i < Emoji.Common.Length; i++)
                {
                    var emoji = Emoji.Common[i];
                    float x = i % columns, y = i / columns;
                    var cell = UiKit.Rect("Emoji", panel.transform).Place(0.01f + x * 0.98f / columns, 0.97f - (y + 1) * 0.94f / rows,
                        0.01f + (x + 1) * 0.98f / columns, 0.97f - y * 0.94f / rows);
                    cell.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);
                    cell.gameObject.AddComponent<Button>().onClick.AddListener(() => AddEmoji(emoji));
                    cell.gameObject.AddComponent<PressFeedback>();
                    UiKit.EmojiLabel("Glyph", cell, emoji, 52, Color.white, TextAnchor.MiddleCenter).rectTransform.Place(0, 0, 1, 1);
                }

                _emojiPicker.SetActive(false);
            }

            _emojiPicker.SetActive(!_emojiPicker.activeSelf && _page == Page.Chat);
            _emojiPicker.transform.SetAsLastSibling();
        }

        private void AddEmoji(string emoji)
        {
            var max = Math.Max(1, _catalog.Chat.maxLength);
            if (SocialRules.TextLength(_chatInput.text) >= max)
                return;
            _chatInput.text += emoji;
            _chatInput.caretPosition = _chatInput.text.Length;
        }

        private Button _newMessages;

        // Newest line id the player has seen in the open conversation. Ids grow; the client keeps only the latest
        // lines, so counting the list (the old way) stopped at its cap and the pill never showed.
        private long _chatSeenId;
        private long _chatSentAfter = long.MaxValue;
        private int _chatBottomFrames;

        /// <summary>The lines of the open conversation (channel, and peer for private chat).</summary>
        private IEnumerable<ChatMessageDto> ChannelLines() => View.Chat.Where(l => l.Channel == (int)_chatChannel
            && (_chatChannel != ChatChannel.Private || Peer(l).Id == _chatPeer));

        private long NewestChatId() => ChannelLines().Select(l => l.Id).DefaultIfEmpty(0).Max();

        private int UnseenChatLines() => ChannelLines().Count(l => l.Id > _chatSeenId && l.SenderId != View.PlayerId);

        /// <summary>Within about one bubble of the bottom, or the list fits without scrolling.</summary>
        private bool ChatNearBottom()
        {
            var viewport = ((RectTransform)_list.parent).rect.height;
            var below = _list.rect.height - viewport - _list.anchoredPosition.y;
            return below <= 200f * UiKit.Scale;
        }

        /// <summary>The player's own line came back from the server after they sent it: sending always goes to the bottom.</summary>
        private bool ChatOwnLineArrived()
        {
            if (_chatSentAfter == long.MaxValue || !ChannelLines().Any(l => l.Id > _chatSentAfter && l.SenderId == View.PlayerId))
                return false;
            _chatSentAfter = long.MaxValue;
            return true;
        }

        /// <summary>
        /// Jumps to the newest line. Rebuilt rows are laid out only at the end of the frame (the destroyed ones still
        /// count until then; the old "y = 100000" was clamped to the old height), so the next frames set the bottom
        /// again (<see cref="FollowChatBottom"/>).
        /// </summary>
        private void ScrollChatToBottom(int frames = 3)
        {
            var scrollRect = _list.parent.GetComponent<ScrollRect>();
            if (scrollRect != null)
            {
                scrollRect.StopMovement();
                scrollRect.verticalNormalizedPosition = 0f;
            }

            _chatBottomFrames = Math.Max(_chatBottomFrames, frames);
            _chatSeenId = Math.Max(_chatSeenId, NewestChatId());
            if (_newMessages != null)
                _newMessages.gameObject.SetActive(false);
        }

        /// <summary>Every frame: finishes a jump to the bottom; reaching the bottom by hand clears the pill.</summary>
        private void FollowChatBottom()
        {
            if (_page != Page.Chat)
                return;
            if (_chatBottomFrames > 0)
            {
                _chatBottomFrames--;
                var scrollRect = _list.parent.GetComponent<ScrollRect>();
                if (scrollRect != null)
                    scrollRect.verticalNormalizedPosition = 0f;
            }
            else if (_newMessages != null && _newMessages.gameObject.activeSelf && ChatNearBottom())
            {
                _chatSeenId = Math.Max(_chatSeenId, NewestChatId());
                _newMessages.gameObject.SetActive(false);
            }
        }

        /// <summary>"↓ New messages (N)" above the input when the player has scrolled up; the list stays where they are.</summary>
        private void ShowNewMessages(int count)
        {
            if (_newMessages == null)
            {
                _newMessages = UiKit.Button("NewMessages", _body, string.Empty, () => ScrollChatToBottom(),
                    new Color(0.13f, 0.52f, 0.86f, 0.95f), 26);
                ((RectTransform)_newMessages.transform).Place(0.3f, 0.105f, 0.7f, 0.145f);
                _newMessages.GetComponentInChildren<TMP_Text>().color = Color.white;
            }

            _newMessages.gameObject.SetActive(count > 0);
            if (count > 0)
            {
                _newMessages.GetComponentInChildren<TMP_Text>().text = "↓ " + Loc.F("chat.new", count);
                _newMessages.transform.SetAsLastSibling();
            }
        }

        /// <summary>
        /// Keeps the chat above the on-screen keyboard (Last Asylum lifts the whole page): the keyboard's height in
        /// canvas units while the input is focused, else 0.
        /// </summary>
        private float KeyboardLift()
        {
            // Measured from the keyboard itself, not the field's focus: on GameActivity the field reports
            // unfocused while the player types, which dropped the page back under the keyboard.
            // Other pages (mail, alliance forms) lift while one of their fields is selected: the text is typed in
            // the field itself now (no system edit box over the keyboard), so it must stay visible.
            var selected = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
            if (_page is not (Page.Chat or Page.Alliance) && (selected == null || selected.GetComponent<TMP_InputField>() == null))
                return 0f;
            var pixels = TouchScreenKeyboard.area.height;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (pixels <= 0f)
            {
                try
                {
                    using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
                    using var window = activity.Call<AndroidJavaObject>("getWindow");
                    using var decor = window.Call<AndroidJavaObject>("getDecorView");
                    using var rect = new AndroidJavaObject("android.graphics.Rect");
                    decor.Call("getWindowVisibleDisplayFrame", rect);
                    pixels = Mathf.Max(0f, decor.Call<int>("getHeight") - rect.Call<int>("height") - rect.Get<int>("top"));
                }
                catch (Exception)
                {
                    pixels = 0f;
                }
            }
#endif
            // Small values are system bars, not a keyboard.
            if (pixels < Screen.height * 0.12f)
                return 0f;
            return pixels / Mathf.Max(0.01f, _canvas.scaleFactor);
        }

        private void SendChat()
        {
            var text = SocialRules.CleanMessage(_chatInput.text, _catalog.Chat);
            if (text.Length == 0)
                return;

            _commands.Dispatch(new SendChatCommand
            {
                CommandId = NewId(), ClientSequence = NextSequence(), Channel = (int)_chatChannel, Text = text,
                TargetId = _chatChannel == ChatChannel.Private ? _chatPeer : string.Empty
            });
            _chatInput.text = string.Empty;
            // Sending always shows the bottom, now and again when the line comes back from the server.
            _chatSentAfter = Math.Min(_chatSentAfter, NewestChatId());
            ScrollChatToBottom();
        }

        // --- Locations in chat ------------------------------------------------------------

        /// <summary>Opens the world map over a tile (wired by the base view).</summary>
        public Action<int, int> ShowOnMap;

        /// <summary>
        /// One line for a shared location outside the chat page (the HUD preview): "Üs konumu X:18 Y:134 · name".
        /// Null when the text is not a location.
        /// </summary>
        internal static string PinPreview(string text)
        {
            var pin = Pin.Parse(text);
            if (pin == null)
                return null;
            var title = pin.Title();
            var line = $"{Loc.T("share.kind." + pin.Kind)} {Loc.F("world.coords", pin.X, pin.Y)}";
            return string.IsNullOrEmpty(title) ? line : $"{line} · {title}";
        }

        /// <summary>A shared map location: chat text "@loc|x|y|kind|label" drawn as a tappable card.</summary>
        private sealed class Pin
        {
            public const string Prefix = "@loc|";
            public int X;
            public int Y;
            public string Kind = "base";
            public string Label = string.Empty;

            public static Pin Parse(string text)
            {
                if (string.IsNullOrEmpty(text) || !text.StartsWith(Prefix, StringComparison.Ordinal))
                    return null;
                var parts = text.Split('|');
                if (parts.Length < 4 || !int.TryParse(parts[1], out var x) || !int.TryParse(parts[2], out var y))
                    return null;
                var kind = parts[3] is "base" or "enemy" or "node" or "me" ? parts[3] : "base";
                return new Pin { X = x, Y = y, Kind = kind, Label = parts.Length > 4 ? string.Join("/", parts.Skip(4)) : string.Empty };
            }

            public static string Format(int x, int y, string kind, string label) =>
                $"{Prefix}{x}|{y}|{kind}|{(label ?? string.Empty).Replace('|', '/')}";

            /// <summary>Resources travel as "level:type" so every reader sees them in their own language.</summary>
            public string Title()
            {
                if (Kind == "node")
                {
                    var colon = Label.IndexOf(':');
                    if (colon > 0 && int.TryParse(Label.Substring(0, colon), out var level))
                        return $"{Loc.F("base.level", level)} {WorldMapView.NodeName(Label.Substring(colon + 1))}";
                }

                return Label;
            }
        }

        private void PinCard(Image bubble, Pin pin, bool mine)
        {
            var ink = mine ? Color.white : new Color(0.08f, 0.14f, 0.2f, 1f);
            var badge = UiKit.Rect("Badge", bubble.transform).Place(0.03f, 0.12f, 0.2f, 0.88f);
            var disc = badge.gameObject.AddComponent<Image>();
            disc.sprite = UiKit.Circle;
            disc.preserveAspect = true;
            disc.raycastTarget = false;
            disc.color = pin.Kind switch
            {
                "enemy" => new Color(0.84f, 0.27f, 0.27f, 1f),
                "node" => new Color(0.17f, 0.71f, 0.45f, 1f),
                _ => new Color(0.05f, 0.35f, 0.55f, 1f)
            };
            HudIcons.Image(badge, pin.Kind == "node" ? "queue_drone" : pin.Kind == "enemy" ? "event_war" : "left_home", 0.2f, 0.2f, 0.8f, 0.8f).raycastTarget = false;
            UiKit.Label("Kind", bubble.transform, Loc.T("share.kind." + pin.Kind), 22, mine ? new Color(0.85f, 0.93f, 1f, 1f) : new Color(0.3f, 0.4f, 0.47f, 1f),
                TextAnchor.UpperLeft, FontStyle.Bold).rectTransform.Place(0.24f, 0.62f, 0.97f, 0.92f);
            var title = UiKit.Label("Title", bubble.transform, pin.Title(), 26, ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            title.richText = false;
            title.rectTransform.Place(0.24f, 0.32f, 0.97f, 0.66f);
            UiKit.Label("Coords", bubble.transform, $"{Loc.F("world.coords", pin.X, pin.Y)}  ›", 22, mine ? Color.white : new Color(0.1f, 0.45f, 0.7f, 1f),
                TextAnchor.LowerLeft, FontStyle.Bold).rectTransform.Place(0.24f, 0.06f, 0.97f, 0.34f);
            bubble.raycastTarget = true;
            var x = pin.X;
            var y = pin.Y;
            bubble.gameObject.AddComponent<Button>().onClick.AddListener(() => ShowOnMap?.Invoke(x, y));
            bubble.gameObject.AddComponent<PressFeedback>();
        }

        private bool _pendingMyPin;
        private GameObject _sharePicker;

        /// <summary>The pin button: the player's own base into the open channel (asks the map where it is first).</summary>
        private void ShareMyBase()
        {
            if (_chatChannel == ChatChannel.Private && _chatPeer.Length == 0)
            {
                Toast(Loc.T("share.pickpeer"), UiKit.Amber);
                return;
            }

            _pendingMyPin = true;
            if (View.WorldView == null)
                _commands.Dispatch(new ViewWorldCommand { CommandId = NewId(), ClientSequence = NextSequence(), X = -1, Y = -1 });
            SendPendingPin();
        }

        private void SendPendingPin()
        {
            var view = View.WorldView;
            if (!_pendingMyPin || view == null)
                return;
            _pendingMyPin = false;
            SendPin(_chatChannel, _chatChannel == ChatChannel.Private ? _chatPeer : string.Empty,
                Pin.Format(view.MyX, view.MyY, "me", View.MyProfile?.Name ?? string.Empty));
        }

        private void SendPin(ChatChannel channel, string peer, string text)
        {
            _commands.Dispatch(new SendChatCommand
            {
                CommandId = NewId(), ClientSequence = NextSequence(), Channel = (int)channel, Text = text, TargetId = peer ?? string.Empty
            });
            Toast(Loc.T("share.sent"), UiKit.Accent);
        }

        /// <summary>
        /// "Share" on a base or a resource on the map: where to post it — world, alliance, or one of the recent
        /// private conversations.
        /// </summary>
        public void ShareLocation(int x, int y, string kind, string label)
        {
            if (_sharePicker != null)
                Destroy(_sharePicker);
            var text = Pin.Format(x, y, kind, label);
            var pin = Pin.Parse(text);

            var dim = UiKit.Box("SharePicker", _canvas.transform, new Color(0, 0, 0, 0.6f));
            dim.rectTransform.Place(0, 0, 1, 1);
            _sharePicker = dim.gameObject;
            dim.gameObject.AddComponent<Button>().onClick.AddListener(() => Destroy(_sharePicker));

            var targets = new List<(string Label, ChatChannel Channel, string Peer, bool Enabled)>
            {
                (Loc.T("chat.world"), ChatChannel.World, string.Empty, true),
                (View.MyAlliance != null ? $"[{View.MyAlliance.Tag}] {Loc.T("chat.alliance")}" : Loc.T("chat.alliance"), ChatChannel.Alliance, string.Empty, View.MyAlliance != null)
            };
            foreach (var last in View.Chat.Where(l => l.Channel == (int)ChatChannel.Private).GroupBy(l => Peer(l).Id)
                         .Select(g => g.OrderBy(l => l.Id).Last()).OrderByDescending(l => l.Id).Take(3))
            {
                var (id, peerName) = Peer(last);
                targets.Add(($"{Loc.T("chat.private")} · {peerName}", ChatChannel.Private, id, true));
            }

            // Sizes in screen fractions: a title row and one button row per target.
            var height = 0.1f + targets.Count * 0.075f;
            var card = UiKit.Box("Card", dim.transform, UiKit.Panel);
            card.rectTransform.Place(0.08f, 0.5f - height / 2f, 0.92f, 0.5f + height / 2f);
            card.gameObject.AddComponent<Button>();
            card.gameObject.AddComponent<CanvasGroup>();
            card.gameObject.AddComponent<PopIn>();
            var heading = UiKit.Label("Title", card.transform, $"{Loc.T("share.title")}: {pin.Title()}  ({pin.X}, {pin.Y})", 30, UiKit.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            heading.richText = false;
            heading.rectTransform.Place(0.04f, 1f - 0.085f / height, 0.96f, 1f - 0.01f / height);
            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                var top = 1f - (0.09f + i * 0.075f) / height;
                var button = UiKit.Button("Target", card.transform, target.Label, () =>
                {
                    Destroy(_sharePicker);
                    SendPin(target.Channel, target.Peer, text);
                }, i == 0 ? UiKit.Accent : UiKit.Field, 30);
                ((RectTransform)button.transform).Place(0.06f, top - 0.065f / height, 0.94f, top);
                button.interactable = target.Enabled;
                if (i > 0)
                    button.GetComponentInChildren<TMP_Text>().color = UiKit.Text;
            }
        }

        /// <summary>"[TAG] Name: text" (with the time in the full view); system lines are localization keys "key|argument".</summary>
        private string ChatLine(ChatMessageDto line, bool compact)
        {
            string body;
            if (line.IsSystem)
            {
                var bar = line.Text.IndexOf('|');
                var key = bar < 0 ? line.Text : line.Text.Substring(0, bar);
                var argument = bar < 0 ? string.Empty : line.Text.Substring(bar + 1);
                body = "· " + Loc.F("chat." + key, argument.Split('|'));
            }
            else
            {
                var tag = string.IsNullOrEmpty(line.AllianceTag) ? string.Empty : $"[{line.AllianceTag}] ";
                var color = line.SenderId == View.PlayerId ? "#38D6F2" : "#FFB74D";
                var pin = Pin.Parse(line.Text);
                var said = pin != null ? $"<color=#7FD8FF>{Loc.T("share.kind." + pin.Kind)}</color> {Escape(pin.Title())}" : Escape(line.Text);
                body = $"<color={color}>{tag}{Escape(line.SenderName)}</color>: {said}";
            }

            if (compact)
            {
                var channel = line.Channel == (int)ChatChannel.Alliance ? Loc.T("chat.alliance") : Loc.T("chat.world");
                return $"<color=#8AA9B5>{channel}</color>  {body}";
            }

            var time = DateTimeOffset.FromUnixTimeMilliseconds(line.SentAtUnixMs).ToLocalTime().ToString("HH:mm");
            return $"<size=24><color=#5E7C88>{time}</color></size>  {body}";
        }

        /// <summary>Players cannot inject rich-text tags into other players' screens.</summary>
        private static string Escape(string text) => (text ?? string.Empty).Replace("<", "<​");
    }
}
