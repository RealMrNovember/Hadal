using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// What the alliance, event, chat and mail pages share (Last Asylum's look, UiFrames.Social): light cards in dark
    /// ink, section bands, notes, the ranking table, parts fixed to the page under or over the list (the hub's icon bar,
    /// the pinned own ranking row), and the top bar's back arrow, which steps back inside these pages before it closes.
    /// </summary>
    public sealed partial class SocialView
    {
        /// <summary>
        /// The back arrow: from a sub-page to its parent (alliance pages → hub, ranking → event, a mail → its category →
        /// the mail home, a conversation → the conversation list); else the page closes.
        /// </summary>
        private void PageBack()
        {
            switch (_page)
            {
                case Page.Alliance when View.MyAlliance != null && _allianceTab != AllianceTabHub:
                    _manage = string.Empty;
                    OpenAllianceTab(AllianceTabHub);
                    return;
                case Page.EventRanking:
                    Open(Page.Events);
                    return;
                case Page.MailDetail:
                    _mailId = string.Empty;
                    Open(Page.Mail);
                    return;
                case Page.MailCompose:
                    Open(Page.Mail);
                    return;
                case Page.Mail when _mailTab.Length > 0:
                    _mailTab = string.Empty;
                    Open(Page.Mail);
                    return;
                case Page.Chat when _chatChannel == HADAL.Shared.Simulation.ChatChannel.Private && _chatPeer.Length > 0:
                    _chatPeer = string.Empty;
                    Render(force: true);
                    return;
                default:
                    Close();
                    return;
            }
        }

        // --- Cards ----------------------------------------------------------------------------

        /// <summary>A light card row in the list (dark ink on it).</summary>
        private Image InkCard(float height, Color? color = null) => UiFrames.CardBox(_list, color).Row(height);

        /// <summary>A pale section band with a centred heading.</summary>
        private void Band(string title) => UiFrames.Section(_list, title).Row(64);

        /// <summary>A light note card (empty states, rules).</summary>
        private void InkNote(string text, float height = 110f) => UiFrames.Note(_list, text).Row(height);

        /// <summary>A row of buttons across the list (Save, Collect all…), each <paramref name="width"/> of the row, centred.</summary>
        private Button WideButton(string label, bool enabled, Action click, Color fill, float width = 0.5f, float height = 110f)
        {
            var row = UiKit.Rect("Action", _list).Row(height);
            return CardButton(row, label, enabled, click, fill, 0.5f - width / 2f, 0.08f, 0.5f + width / 2f, 0.92f);
        }

        // --- Ranking table --------------------------------------------------------------------

        /// <summary>The table's header band.</summary>
        private void RankTableHead(string value) =>
            UiFrames.RankHead(_list, Loc.T("rank.col.rank"), Loc.T("rank.col.name"), value).Row(60);

        /// <summary>
        /// The viewer's own row fixed under the list (Last Asylum keeps it in reach while the table scrolls); the list
        /// stops above it. Gone when the page or tab changes.
        /// </summary>
        private RectTransform PinnedRow(Func<bool> alive, float height = 120f)
        {
            var holder = Fixed("Pinned", alive);
            holder.anchorMin = new Vector2(0.02f, 0);
            holder.anchorMax = new Vector2(0.98f, 0);
            holder.pivot = new Vector2(0.5f, 0);
            holder.sizeDelta = new Vector2(0, height * UiKit.Scale);
            holder.anchoredPosition = new Vector2(0, 12);
            ListAbove(height * UiKit.Scale + 24);
            return holder;
        }

        // --- Fixed parts ----------------------------------------------------------------------

        private readonly Dictionary<string, GameObject> _fixed = new Dictionary<string, GameObject>();

        /// <summary>
        /// A part fixed to the page body outside the scrolling list. One per <paramref name="name"/>: drawing it again
        /// replaces it, and it removes itself once the page is drawn again without it (another page, tab or data) or
        /// <paramref name="alive"/> turns false.
        /// </summary>
        private RectTransform Fixed(string name, Func<bool> alive)
        {
            if (_fixed.TryGetValue(name, out var old) && old != null)
                Destroy(old);
            var rect = UiKit.Rect(name, _body);
            var drawn = _renderedKey;
            var self = rect.gameObject;
            // Gone once a newer copy replaced it or the page was drawn for another key (page, tab or data).
            rect.gameObject.AddComponent<PageBound>().Alive = () =>
                _page != Page.None && alive() && _fixed.TryGetValue(name, out var current) && current == self && _renderedKey == drawn;
            _fixed[name] = rect.gameObject;
            return rect;
        }

        /// <summary>Lets the list end <paramref name="bottom"/> canvas units above the page's bottom edge.</summary>
        private void ListAbove(float bottom)
        {
            var rect = (RectTransform)_list.parent;
            rect.anchorMin = new Vector2(rect.anchorMin.x, 0);
            rect.offsetMin = new Vector2(rect.offsetMin.x, bottom);
        }

        /// <summary>Destroys its object as soon as the page it was drawn for is gone.</summary>
        private sealed class PageBound : MonoBehaviour
        {
            public Func<bool> Alive;

            private void LateUpdate()
            {
                if (Alive != null && !Alive())
                    Destroy(gameObject);
            }
        }

        /// <summary>Sets the top bar's title and clears the line under it (pages own their headings).</summary>
        private void PageTitle(string title)
        {
            _title.text = title;
            _subtitle.text = string.Empty;
        }

        /// <summary>Dark ink for a label on a light card.</summary>
        private static TMP_Text Ink(Transform card, string text, int size, Color color, float x0, float y0, float x1, float y1,
            TextAnchor anchor = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var label = CardText(card, text, size, color, x0, y0, x1, y1, anchor, style);
            label.raycastTarget = false;
            return label;
        }
    }
}
