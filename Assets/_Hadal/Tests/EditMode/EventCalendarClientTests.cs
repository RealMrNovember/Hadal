using System;
using System.Collections.Generic;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;
using Hadal.Core.Network;
using Hadal.Core.StateSync;
using Hadal.UI;
using NUnit.Framework;
using NetworkSerializationLayer = Hadal.Core.Network.NetworkSerializationLayer;

namespace Hadal.Tests.EditMode
{
    /// <summary>The event centre's week calendar on the client (docs/29 §7, roadmap §7).</summary>
    public sealed class EventCalendarClientTests
    {
        private static readonly string[] Names = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
        private const long Hour = 3_600_000L;

        private static long Utc(int y, int m, int d, int h = 0) => new DateTimeOffset(y, m, d, h, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();

        [Test]
        public void The_calendar_arrives_through_its_own_entity()
        {
            var view = new ClientStateView();
            view.ResetSession();
            Assert.IsNull(view.EventCalendar);
            var serializer = new ProtobufNetNetworkSerializer();
            var calendar = new EventCalendarDto
            {
                Entries = new[] { new EventDto { Id = "depth-race:2026-11-02:w1", Template = EventTemplates.Points, ScheduleId = "depth-race" } },
                ServerNowMs = Utc(2026, 11, 1)
            };
            ClientStateViewMutator.ApplyDelta(view, new StateDelta
            {
                ServerTick = 2,
                Changes = new List<EntityChangeDto> { new() { EntityId = EntityIds.EventCalendar, Kind = EntityChangeKind.Update, Payload = serializer.Serialize(calendar) } }
            }, new NetworkSerializationLayer(), new List<CommandAckDto>());

            Assert.AreEqual(1, view.EventCalendarVersion);
            Assert.AreEqual("depth-race", view.EventCalendar.Entries[0].ScheduleId);
            Assert.AreEqual(CommandType.ViewEventCalendar, CommandDispatcher.ResolveCommandType(new ViewEventCalendarCommand()));
        }

        [Test]
        public void Time_windows_read_in_the_phones_time()
        {
            var day = new EventDto { StartsAtMs = Utc(2026, 10, 29), EndsAtMs = Utc(2026, 10, 30) };

            // At UTC the whole day.
            Assert.AreEqual("00:00 – 24:00", EventCalendarText.Window(day, Utc(2026, 10, 29), 0, d => Names[d]));

            // At UTC+3 the same event runs 03:00 Thursday to 03:00 Friday.
            var plus3 = 3 * Hour;
            var thursday = EventCalendar.DayStart(Utc(2026, 10, 29, 12), plus3);
            Assert.AreEqual("03:00 – Fri 30, 03:00", EventCalendarText.Window(day, thursday, plus3, d => Names[d]));
            Assert.AreEqual("Thu 29, 03:00 – 03:00", EventCalendarText.Window(day, thursday + EventCalendar.DayMs, plus3, d => Names[d]));

            // A week-long event seen on a day in its middle; west of UTC.
            var week = new EventDto { StartsAtMs = Utc(2026, 10, 26), EndsAtMs = Utc(2026, 11, 2) };
            var minus5 = -5 * Hour;
            var wednesday = EventCalendar.DayStart(Utc(2026, 10, 28, 12), minus5);
            Assert.AreEqual("Sun 25, 19:00 – Sun 1, 19:00", EventCalendarText.Window(week, wednesday, minus5, d => Names[d]));

            Assert.AreEqual("23:59", EventCalendarText.Clock(-60_000L));
            Assert.AreEqual(30, EventCalendarText.DayOfMonth(Utc(2026, 10, 29, 21), plus3));
        }

        [Test]
        public void The_week_starts_today_and_shows_each_event_on_every_day_it_covers()
        {
            var now = Utc(2026, 10, 31, 23); // Saturday 23:00 UTC = Sunday 02:00 at UTC+3
            var plus3 = 3 * Hour;
            var week = EventCalendar.Week(now, plus3);
            Assert.AreEqual(7, week.Length);
            Assert.AreEqual((int)DayOfWeek.Sunday, EventCalendar.Weekday(week[0], plus3));
            Assert.AreEqual(1, EventCalendarText.DayOfMonth(week[0], plus3)); // November 1st, across the month

            var entries = new[]
            {
                new EventDto { Id = "b", StartsAtMs = Utc(2026, 11, 1), EndsAtMs = Utc(2026, 11, 2) },
                new EventDto { Id = "a", StartsAtMs = Utc(2026, 10, 26), EndsAtMs = Utc(2026, 11, 2) },
                new EventDto { Id = "later", StartsAtMs = Utc(2026, 11, 9), EndsAtMs = Utc(2026, 11, 10) }
            };
            var today = EventCalendar.OnDay(entries, week[0]);
            Assert.AreEqual(new[] { "a", "b" }, today.ConvertAll(e => e.Id).ToArray());
            Assert.IsFalse(EventCalendar.StartsOn(today[0], week[0]));
            Assert.AreEqual(2, EventCalendar.OnDay(entries, week[1]).Count); // "a" and "b" end at 03:00 local Monday
            Assert.IsEmpty(EventCalendar.OnDay(entries, week[2]));
        }

        [Test]
        public void Templates_have_their_own_icons()
        {
            Assert.AreEqual("event_war", EventCalendarText.Icon(EventTemplates.Points));
            Assert.AreEqual("side_alliance", EventCalendarText.Icon(EventTemplates.Alliance));
            Assert.AreEqual("event_gift", EventCalendarText.Icon(EventTemplates.Login));
            Assert.AreEqual("event_war", EventCalendarText.Icon("something_new"));
        }
    }
}
