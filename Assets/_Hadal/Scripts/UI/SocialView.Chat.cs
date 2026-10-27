using System;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
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
        private InputField _chatInput;
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
            _chatTabs = UiKit.Choice("Channels", _sheet.transform, new[] { Loc.T("chat.world"), inAlliance ? $"[{View.MyAlliance.Tag}] {Loc.T("chat.alliance")}" : Loc.T("chat.alliance"), Loc.T("chat.private") },
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
            _chatTabs.Place(0.03f, 0.875f, 0.97f, 0.922f);

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
            // Bubble width follows the text (about 28 characters a line at full width), height the wrapped lines.
            var lines = Math.Max(1, (int)Math.Ceiling(text.Length / 28f));
            var width = lines > 1 ? 0.66f : Mathf.Clamp(0.12f + text.Length * 0.021f, 0.16f, 0.66f);
            var row = UiKit.Rect("Line", _list).Row(96 + 40 * lines);

            // Avatar: a disc in a colour picked from the name, with the initial.
            var avatar = UiKit.Rect("Avatar", row);
            avatar.Place(mine ? 0.87f : 0.01f, Mathf.Max(0f, 1f - 100f / (96 + 40 * lines)), mine ? 0.99f : 0.13f, 1f);
            var disc = avatar.gameObject.AddComponent<Image>();
            disc.sprite = UiKit.Circle;
            disc.preserveAspect = true;
            disc.color = mine ? new Color(0.13f, 0.52f, 0.86f, 1f) : Color.HSVToRGB(Math.Abs((line.SenderId ?? string.Empty).GetHashCode() % 360) / 360f, 0.45f, 0.75f);
            disc.raycastTarget = false;
            var name = line.SenderName ?? string.Empty;
            UiKit.Label("Initial", avatar, name.Length > 0 ? name.Substring(0, 1).ToUpperInvariant() : "?", 40, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.Place(0, 0, 1, 1);

            var tag = string.IsNullOrEmpty(line.AllianceTag) ? string.Empty : $"[{line.AllianceTag}] ";
            var time = DateTimeOffset.FromUnixTimeMilliseconds(line.SentAtUnixMs).ToLocalTime().ToString("HH:mm");
            var header = mine ? $"<color=#5E7C88>{time}</color>  {Loc.T("chat.you")}" : $"{tag}{Escape(name)}  <color=#5E7C88>{time}</color>";
            UiKit.Label("Name", row, header, 22, mine ? UiKit.Accent : UiKit.Amber, mine ? TextAnchor.UpperRight : TextAnchor.UpperLeft, FontStyle.Bold)
                .rectTransform.Place(mine ? 0.15f : 0.16f, 0.72f, mine ? 0.84f : 0.85f, 1f);

            var x0 = mine ? 0.84f - width : 0.16f;
            var bubble = UiKit.Box("Bubble", row, mine ? new Color(0.13f, 0.52f, 0.86f, 1f) : new Color(0.88f, 0.92f, 0.95f, 1f));
            bubble.rectTransform.Place(x0, 0.02f, x0 + width, 0.74f);
            bubble.raycastTarget = false;
            var body = UiKit.Label("Text", bubble.transform, text, 28, mine ? Color.white : new Color(0.08f, 0.14f, 0.2f, 1f), TextAnchor.MiddleLeft);
            body.supportRichText = false;
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
                CardText(card.transform, last.Text, 26, UiKit.Muted, 0.04f, 0.05f, 0.7f, 0.5f);
                CardButton(card.transform, Loc.T("chat.open"), true, () => OpenPrivateChat(id, name), UiKit.Accent);
            }
        }

        private void BuildChatFooter()
        {
            if (_chatInput != null)
                return;

            // Last Asylum's input bar: a white rounded field and a round blue send button.
            _chatInput = UiKit.Input("Message", _footer, Loc.T("chat.hint"), characterLimit: Math.Max(1, _catalog.Chat.maxLength));
            ((RectTransform)_chatInput.transform).Place(0, 0, 0.83f, 1);
            _chatInput.image.color = new Color(0.96f, 0.97f, 0.98f, 1f);
            foreach (var frame in _chatInput.GetComponentsInChildren<Image>())
                if (frame != _chatInput.image)
                    frame.enabled = false;
            _chatInput.textComponent.color = new Color(0.08f, 0.14f, 0.2f, 1f);
            ((Text)_chatInput.placeholder).color = new Color(0.45f, 0.52f, 0.58f, 1f);
            // Android: the system edit box over the keyboard shows what is typed even if the lift below fails.
            _chatInput.shouldHideMobileInput = false;
            _chatInput.onEndEdit.AddListener(_ =>
            {
                // The keyboard's "Done" sends as well.
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
                    || _chatInput.touchScreenKeyboard?.status == TouchScreenKeyboard.Status.Done)
                    SendChat();
            });

            var send = UiKit.Rect("Send", _footer).Place(0.85f, -0.05f, 1f, 1.05f);
            var disc = send.gameObject.AddComponent<Image>();
            disc.sprite = UiKit.Circle;
            disc.preserveAspect = true;
            disc.color = new Color(0.13f, 0.52f, 0.86f, 1f);
            send.gameObject.AddComponent<Button>().onClick.AddListener(SendChat);
            send.gameObject.AddComponent<PressFeedback>();
            UiKit.Label("Icon", send, "▶", 40, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Place(0.08f, 0, 1, 1);
        }

        private Button _newMessages;
        private int _chatSeen;

        /// <summary>"N new messages" pill above the input when the player has scrolled up (the list stays where they are).</summary>
        private void ShowNewMessages(int count)
        {
            if (_newMessages == null)
            {
                _newMessages = UiKit.Button("NewMessages", _sheet.transform, string.Empty, () =>
                {
                    _list.anchoredPosition = new Vector2(0, 100000);
                    _newMessages.gameObject.SetActive(false);
                    _chatSeen = View.Chat.Count;
                }, new Color(0.13f, 0.52f, 0.86f, 0.95f), 26);
                ((RectTransform)_newMessages.transform).Place(0.3f, 0.105f, 0.7f, 0.145f);
                _newMessages.GetComponentInChildren<Text>().color = Color.white;
            }

            _newMessages.gameObject.SetActive(count > 0);
            if (count > 0)
                _newMessages.GetComponentInChildren<Text>().text = "▼ " + Loc.F("chat.new", count);
        }

        /// <summary>
        /// Keeps the chat above the on-screen keyboard (Last Asylum lifts the whole page): the keyboard's height in
        /// canvas units while the input is focused, else 0.
        /// </summary>
        private float KeyboardLift()
        {
            // Measured from the keyboard itself, not the field's focus: on GameActivity the field reports
            // unfocused while the player types, which dropped the page back under the keyboard.
            if (_page != Page.Chat || _chatInput == null)
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
                body = $"<color={color}>{tag}{Escape(line.SenderName)}</color>: {Escape(line.Text)}";
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
