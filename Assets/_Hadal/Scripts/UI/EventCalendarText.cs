using System;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;

namespace Hadal.UI
{
    /// <summary>
    /// The event calendar's texts and choices (docs/29 §7), kept apart from the view so EditMode can test them: the
    /// phone's offset from server time, an event's time window on one day, and its template icon.
    /// </summary>
    public static class EventCalendarText
    {
        /// <summary>The phone's offset from UTC at <paramref name="nowMs"/>, in milliseconds (whole minutes).</summary>
        public static long LocalOffsetMs(long nowMs)
        {
            try
            {
                return (long)TimeZoneInfo.Local.GetUtcOffset(DateTimeOffset.FromUnixTimeMilliseconds(nowMs)).TotalMinutes * 60_000L;
            }
            catch (Exception)
            {
                return (long)Math.Round((DateTime.Now - DateTime.UtcNow).TotalMinutes) * 60_000L;
            }
        }

        /// <summary>
        /// The event's window as seen from the day starting at <paramref name="dayStartMs"/>: "09:00 – 13:00" inside the
        /// day, "00:00 – 24:00" for the whole day, and a part outside it with its weekday and date ("Mon 2, 03:00").
        /// </summary>
        public static string Window(EventDto e, long dayStartMs, long offsetMs, Func<int, string> weekday)
        {
            var dayEnd = dayStartMs + EventCalendar.DayMs;

            string At(long ms, bool end)
            {
                if (end && ms == dayEnd)
                    return "24:00";
                var clock = Clock(ms + offsetMs);
                if (ms >= dayStartMs && ms < dayEnd)
                    return clock;
                var date = DateTimeOffset.FromUnixTimeMilliseconds(ms + offsetMs).UtcDateTime.Day;
                return $"{weekday(EventCalendar.Weekday(ms, offsetMs))} {date}, {clock}";
            }

            return At(e.StartsAtMs, false) + " – " + At(e.EndsAtMs, true);
        }

        /// <summary>"07:05" for a local time in milliseconds.</summary>
        public static string Clock(long localMs)
        {
            var minutes = (localMs % EventCalendar.DayMs + EventCalendar.DayMs) % EventCalendar.DayMs / 60_000L;
            return $"{minutes / 60:00}:{minutes % 60:00}";
        }

        /// <summary>The day of month of a local day start.</summary>
        public static int DayOfMonth(long dayStartMs, long offsetMs) => DateTimeOffset.FromUnixTimeMilliseconds(dayStartMs + offsetMs).UtcDateTime.Day;

        /// <summary>The icon of an event template (the HUD icon set).</summary>
        public static string Icon(string template) => template switch
        {
            EventTemplates.Alliance => "side_alliance",
            EventTemplates.Login => "event_gift",
            EventTemplates.Mobilization => "g_army",
            EventTemplates.AllianceDuel => "g_attack",
            _ => "event_war"
        };

        /// <summary>The frame colour of a template's icon tile.</summary>
        public static string Frame(string template) => template switch
        {
            EventTemplates.Alliance or EventTemplates.Mobilization => "rare",
            EventTemplates.Login => "legendary",
            _ => "epic"
        };
    }
}
