using System;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using Hadal.Core.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// The event centre's Calendar tab (docs/29 §7, roadmap §7): a week strip from today with today in gold, and under it
    /// the chosen day's events with their time windows in the phone's time and the template icon. A running event opens
    /// its own tab; one not open yet shows its rules and rewards. The recurring events come from the server's calendar
    /// (ViewEventCalendar) before they are created.
    /// </summary>
    public sealed partial class SocialView
    {
        /// <summary>The event tab id of the calendar (no event has it: event ids never start with '@').</summary>
        private const string CalendarTabId = "@calendar";

        /// <summary>The calendar is asked for again after this long (it changes when an occurrence is created).</summary>
        private const float CalendarStaleSeconds = 60f;

        private int _calendarDay;
        private string _previewId = string.Empty;
        private float _calendarAskedAt = -1000f;

        private bool CalendarShown => _eventId == CalendarTabId;

        private void AskCalendar(bool force)
        {
            if (!force && View.EventCalendar != null && Time.unscaledTime - _calendarAskedAt < CalendarStaleSeconds)
                return;
            if (Time.unscaledTime - _calendarAskedAt < 2f)
                return; // one request in flight is enough
            _calendarAskedAt = Time.unscaledTime;
            _commands.Dispatch(new ViewEventCalendarCommand { CommandId = NewId(), ClientSequence = NextSequence(), Lang = Loc.Current });
        }

        private void OpenCalendar()
        {
            _eventId = CalendarTabId;
            _previewId = string.Empty;
            _calendarDay = 0;
            AskCalendar(force: true);
            Render(force: true);
        }

        private void RenderCalendar()
        {
            AskCalendar(force: false);
            var calendar = View.EventCalendar;
            if (calendar == null)
            {
                InkNote(Loc.T("event.loading"));
                return;
            }

            var entries = calendar.Entries ?? Array.Empty<EventDto>();
            if (_previewId.Length > 0)
            {
                var preview = Array.Find(entries, x => x.Id == _previewId);
                if (preview != null)
                {
                    RenderCalendarPreview(preview);
                    return;
                }

                _previewId = string.Empty;
            }

            var now = NowMs;
            var offset = EventCalendarText.LocalOffsetMs(now);
            var week = EventCalendar.Week(now, offset);
            _calendarDay = Mathf.Clamp(_calendarDay, 0, week.Length - 1);
            CalendarWeek(week, offset, entries);

            var day = week[_calendarDay];
            var weekday = Loc.T("event.weekday." + EventCalendar.Weekday(day, offset));
            Band(_calendarDay == 0 ? $"{Loc.T("event.calendar.today")} · {weekday} {EventCalendarText.DayOfMonth(day, offset)}"
                : $"{weekday} {EventCalendarText.DayOfMonth(day, offset)}");

            var list = EventCalendar.OnDay(entries, day);
            if (list.Count == 0)
                InkNote(Loc.T("event.calendar.empty"));
            for (var i = 0; i < list.Count; i++)
                CalendarRow(list[i], day, offset, i);

            InkNote(Loc.T("event.calendar.hint"), 120f);
        }

        /// <summary>Seven day cells: weekday and date, today gold, the chosen day dark; dots for how many events it has.</summary>
        private void CalendarWeek(long[] week, long offset, EventDto[] entries)
        {
            var strip = UiKit.Rect("Week", _list).Row(170);
            for (var i = 0; i < week.Length; i++)
            {
                var index = i;
                var chosen = i == _calendarDay;
                var today = i == 0;
                var x0 = i / (float)week.Length + 0.004f;
                var x1 = (i + 1) / (float)week.Length - 0.004f;
                var cell = UiKit.Box("Day", strip, chosen ? UiFrames.Navy : today ? UiFrames.Highlight : UiFrames.Card);
                cell.sprite = UiKit.Rounded;
                cell.rectTransform.Place(x0, 0.06f, x1, 0.94f);
                if (chosen && today)
                {
                    var ring = cell.gameObject.AddComponent<Outline>();
                    ring.effectColor = UiKit.Gold;
                    ring.effectDistance = new Vector2(4, -4);
                }

                var ink = chosen ? Color.white : UiFrames.Ink;
                var name = UiKit.Label("Weekday", cell.transform, Loc.T("event.weekday." + EventCalendar.Weekday(week[i], offset)), UiKit.TypeScale.Caption,
                    chosen ? Color.white : UiFrames.InkSoft, TextAnchor.MiddleCenter, FontStyle.Bold);
                name.rectTransform.Place(0.04f, 0.64f, 0.96f, 0.95f);
                name.Fit(12, UiKit.TypeScale.Caption);
                UiKit.Label("Date", cell.transform, EventCalendarText.DayOfMonth(week[i], offset).ToString(), UiKit.TypeScale.Subtitle, ink, TextAnchor.MiddleCenter,
                    FontStyle.Bold).rectTransform.Place(0, 0.26f, 1, 0.66f);
                var count = Math.Min(3, EventCalendar.OnDay(entries, week[i]).Count);
                if (count > 0)
                    UiKit.Label("Count", cell.transform, new string('•', count), UiKit.TypeScale.Caption, chosen ? UiKit.Gold : UiFrames.InkGood, TextAnchor.MiddleCenter)
                        .rectTransform.Place(0, 0.04f, 1, 0.3f);

                cell.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                {
                    GameAudio.Play(Sfx.UiTap);
                    _calendarDay = index;
                    _previewId = string.Empty;
                    Render(force: true);
                });
                cell.gameObject.AddComponent<PressFeedback>();
            }
        }

        /// <summary>One event of the day: template icon, title, time window and kind, its state, and Open / Details.</summary>
        private void CalendarRow(EventDto e, long day, long offset, int index)
        {
            var card = InkCard(176, index % 2 == 0 ? UiFrames.Card : UiFrames.CardAlt);
            UiFrames.ItemTile(card.transform, EventCalendarText.Icon(e.Template), EventCalendarText.Frame(e.Template), null).Place(0.02f, 0.1f, 0.16f, 0.9f);

            var title = Ink(card.transform, EventTitle(e), UiKit.TypeScale.Body, UiFrames.Ink, 0.19f, 0.5f, 0.7f, 0.94f, TextAnchor.MiddleLeft, FontStyle.Bold);
            title.Fit(16, UiKit.TypeScale.Body);
            var line = EventCalendarText.Window(e, day, offset, d => Loc.T("event.weekday." + d)) + "  ·  " + Loc.T("event.template." + e.Template)
                       + (string.IsNullOrEmpty(e.ScheduleId) ? string.Empty : "  ·  " + Loc.T("event.calendar.recurring"));
            Ink(card.transform, line, UiKit.TypeScale.Caption, UiFrames.InkSoft, 0.19f, 0.06f, 0.72f, 0.5f, TextAnchor.UpperLeft).Fit(12, UiKit.TypeScale.Caption);

            var phase = EventRules.Phase(e, NowMs);
            var (state, color) = phase switch
            {
                EventPhase.Live => (Loc.T("event.calendar.live"), UiFrames.InkGood),
                EventPhase.Upcoming => (Loc.T("event.calendar.soon"), UiKit.Primary),
                _ => (Loc.T("event.calendar.ended"), UiFrames.InkSoft)
            };
            Ink(card.transform, state, UiKit.TypeScale.Caption, color, 0.72f, 0.6f, 0.97f, 0.94f, TextAnchor.MiddleRight, FontStyle.Bold);

            // A tab of its own when the event is in the player's list (live, ended and paying, or upcoming soon); else its rules.
            var events = View.Events.Events ?? Array.Empty<EventDto>();
            var mine = Array.Exists(events, x => x.Id == e.Id) && Array.Exists(TabEvents(), x => x.Id == e.Id);
            var id = e.Id;
            CardButton(card.transform, Loc.T(mine ? "event.calendar.open" : "event.calendar.details"), true, () =>
            {
                if (mine)
                    _eventId = id;
                else
                    _previewId = id;
                Render(force: true);
            }, mine ? UiKit.Ok : UiKit.Primary, 0.74f, 0.1f, 0.97f, 0.56f);
        }

        /// <summary>An event that is not open yet (or no longer listed): banner, rules and rewards, no claims.</summary>
        private void RenderCalendarPreview(EventDto e)
        {
            var back = UiKit.Rect("Back", _list).Row(100);
            CardButton(back, "‹ " + Loc.T("event.calendar"), true, () =>
            {
                _previewId = string.Empty;
                Render(force: true);
            }, UiKit.Primary, 0f, 0.1f, 0.4f, 0.9f);
            InkNote(Loc.T("event.calendar.preview"), 96f);
            EventBanner(e);
            EventBody(e, preview: true);
        }
    }
}
