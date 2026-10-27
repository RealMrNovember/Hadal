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

        private void RenderChat(bool force)
        {
            _title.text = Loc.T("social.chat");
            _subtitle.text = string.Empty;
            BuildChatFooter();

            var inAlliance = View.MyAlliance != null;
            if (!inAlliance && _chatChannel == ChatChannel.Alliance)
                _chatChannel = ChatChannel.World;

            UiKit.Choice("Channels", _list, new[] { Loc.T("chat.world"), inAlliance ? $"[{View.MyAlliance.Tag}] {Loc.T("chat.alliance")}" : Loc.T("chat.alliance"), Loc.T("chat.private") },
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
                }).Row(80);

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
        /// A chat bubble (genre layout): others on the left with name and tag above, mine on the right in the accent
        /// tint, system notices centred and muted.
        /// </summary>
        private void Bubble(ChatMessageDto line)
        {
            if (line.IsSystem)
            {
                UiKit.Label("System", _list, ChatLine(line, compact: false), 24, UiKit.Muted, TextAnchor.MiddleCenter, FontStyle.Italic).Row(56);
                return;
            }

            var mine = line.SenderId == View.PlayerId;
            // Rough height: about 30 characters a line in a bubble of 72% width.
            var lines = 1 + (line.Text?.Length ?? 0) / 30;
            var row = UiKit.Rect("Line", _list).Row(78 + 40 * lines);
            var tag = string.IsNullOrEmpty(line.AllianceTag) ? string.Empty : $"[{line.AllianceTag}] ";
            var time = DateTimeOffset.FromUnixTimeMilliseconds(line.SentAtUnixMs).ToLocalTime().ToString("HH:mm");
            UiKit.Label("Name", row, mine ? time : $"{tag}{line.SenderName}  <color=#55707C>{time}</color>", 22, mine ? UiKit.Muted : UiKit.Amber,
                mine ? TextAnchor.UpperRight : TextAnchor.UpperLeft, FontStyle.Bold).rectTransform.Place(mine ? 0.28f : 0.02f, 0.7f, mine ? 0.98f : 0.72f, 1f);
            var bubble = UiKit.Box("Bubble", row, mine ? new Color(0.06f, 0.30f, 0.36f, 0.95f) : UiKit.Field);
            bubble.rectTransform.Place(mine ? 0.28f : 0.02f, 0f, mine ? 0.98f : 0.72f, 0.72f);
            var text = UiKit.Label("Text", bubble.transform, line.Text, 28, UiKit.Text, TextAnchor.MiddleLeft);
            text.supportRichText = false;
            text.rectTransform.Place(0.05f, 0.05f, 0.95f, 0.95f);
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

            _chatInput = UiKit.Input("Message", _footer, Loc.T("chat.hint"), characterLimit: Math.Max(1, _catalog.Chat.maxLength));
            ((RectTransform)_chatInput.transform).Place(0, 0, 0.74f, 1);
            _chatInput.onEndEdit.AddListener(_ =>
            {
                // The keyboard's "Done" sends as well.
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
                    || _chatInput.touchScreenKeyboard?.status == TouchScreenKeyboard.Status.Done)
                    SendChat();
            });

            var send = UiKit.Button("Send", _footer, Loc.T("chat.send"), SendChat, UiKit.Accent, 34);
            ((RectTransform)send.transform).Place(0.76f, 0, 1, 1);
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
