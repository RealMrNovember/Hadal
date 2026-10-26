using System;
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
        private InputField _chatInput;

        private void RenderChat(bool force)
        {
            _title.text = Loc.T("social.chat");
            _subtitle.text = string.Empty;
            BuildChatFooter();

            var inAlliance = View.MyAlliance != null;
            if (!inAlliance)
                _chatChannel = ChatChannel.World;

            UiKit.Choice("Channels", _list, new[] { Loc.T("chat.world"), inAlliance ? $"[{View.MyAlliance.Tag}] {Loc.T("chat.alliance")}" : Loc.T("chat.alliance") },
                () => (int)_chatChannel, i =>
                {
                    if (i == (int)ChatChannel.Alliance && View.MyAlliance == null)
                    {
                        Toast(Loc.Reason(nameof(SocialRejectReason.NotInAlliance)), UiKit.Danger);
                        return;
                    }

                    _chatChannel = (ChatChannel)i;
                    Render(force: true);
                }).Row(80);

            var any = false;
            foreach (var line in View.Chat)
            {
                if (line.Channel != (int)_chatChannel)
                    continue;
                any = true;
                var label = UiKit.Label("Line", _list, ChatLine(line, compact: false), 30, line.IsSystem ? UiKit.Amber : UiKit.Text, TextAnchor.UpperLeft);
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                // Rough height: wrap at about 42 characters a line on a phone.
                var lines = 1 + (line.Text?.Length ?? 0) / 42;
                label.Row(44 + 36 * lines);
            }

            if (!any)
                Note(Loc.T("chat.empty"));
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
                CommandId = NewId(), ClientSequence = NextSequence(), Channel = (int)_chatChannel, Text = text
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
                body = "· " + Loc.F("chat." + key, argument);
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
