using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.Simulation;

namespace Hadal.UI
{
    /// <summary>
    /// The alliance polls page's texts (docs/29 §5), apart from the view so EditMode can test them: a time slot in the
    /// phone's time with its server time, a short voter list and an option's share.
    /// </summary>
    public static class AlliancePollText
    {
        /// <summary>"Mon 02.11 23:00 · 20:00 UTC": the slot in the phone's time, then in server time.</summary>
        public static string Slot(long atMs, long offsetMs, Func<int, string> weekday)
        {
            var local = DateTimeOffset.FromUnixTimeMilliseconds(atMs + offsetMs).UtcDateTime;
            return $"{weekday(EventCalendar.Weekday(atMs, offsetMs))} {local.Day:00}.{local.Month:00} {EventCalendarText.Clock(atMs + offsetMs)} · "
                   + $"{EventCalendarText.Clock(atMs)} UTC";
        }

        /// <summary>"Ana, Ben, Cem +2": the first few voters and how many more.</summary>
        public static string Names(IReadOnlyList<string> names, int shown = 3)
        {
            if (names == null || names.Count == 0)
                return string.Empty;
            var head = string.Join(", ", names.Take(shown));
            return names.Count > shown ? $"{head} +{names.Count - shown}" : head;
        }

        /// <summary>"3 · 60%": an option's votes and its share of the voters.</summary>
        public static string Share(int votes, int voters) => $"{votes} · {AlliancePollRules.Percent(votes, voters)}%";
    }

    /// <summary>
    /// The create dialog's form: a poll (question, 2–6 answers) or a time-slot vote (event, 2–5 times), the voting time
    /// and the switches. Times are kept as server instants and shown in the phone's time.
    /// </summary>
    public sealed class PollDraft
    {
        /// <summary>The voting times the dialog steps through.</summary>
        public static readonly int[] HourSteps = { 1, 2, 4, 8, 12, 24, 48, 72 };

        /// <summary>A time slot moves by this much per tap of its clock arrows.</summary>
        public const long SlotStepMs = 30 * 60_000L;

        public const long DayMs = 86_400_000L;

        public string Kind = AlliancePollRules.KindPoll;
        public string Question = string.Empty;
        public readonly List<string> Options = new List<string> { string.Empty, string.Empty };
        public readonly List<long> Slots = new List<long>();
        public int Hours = AlliancePollRules.PollHours;
        public bool Multiple;
        public bool Anonymous;
        public string Purpose = AlliancePollRules.Purposes[0];

        /// <summary>A fresh form of <paramref name="kind"/>; a time vote offers 20:00 and 21:00 the day after tomorrow, phone time.</summary>
        public void Reset(string kind, long nowMs, long offsetMs)
        {
            Kind = kind;
            Question = string.Empty;
            Options.Clear();
            Options.Add(string.Empty);
            Options.Add(string.Empty);
            Slots.Clear();
            Hours = AlliancePollRules.HoursOrDefault(kind, 0);
            Multiple = false;
            Anonymous = false;
            Purpose = AlliancePollRules.Purposes[0];
            if (kind == AlliancePollRules.KindSlots)
            {
                Slots.Add(LocalTime(nowMs, offsetMs, 2, 20));
                Slots.Add(LocalTime(nowMs, offsetMs, 2, 21));
            }
        }

        /// <summary>The instant of <paramref name="hour"/>:00 phone time, <paramref name="days"/> days after today.</summary>
        public static long LocalTime(long nowMs, long offsetMs, int days, int hour) =>
            EventCalendar.DayStart(nowMs, offsetMs) + days * DayMs + hour * 3_600_000L;

        public void StepHours(int direction)
        {
            var index = Array.IndexOf(HourSteps, Hours);
            if (index < 0)
                index = Array.IndexOf(HourSteps, AlliancePollRules.PollHours);
            Hours = HourSteps[Math.Max(0, Math.Min(HourSteps.Length - 1, index + direction))];
        }

        public void StepPurpose()
        {
            var purposes = AlliancePollRules.Purposes;
            Purpose = purposes[(Array.IndexOf(purposes, Purpose) + 1) % purposes.Length];
        }

        public bool CanAddOption => Kind == AlliancePollRules.KindPoll ? Options.Count < AlliancePollRules.MaxOptions : Slots.Count < AlliancePollRules.MaxSlots;

        public bool CanRemoveOption => Kind == AlliancePollRules.KindPoll ? Options.Count > AlliancePollRules.MinOptions : Slots.Count > AlliancePollRules.MinSlots;

        /// <summary>One more answer, or one more time an hour after the last.</summary>
        public void AddOption()
        {
            if (!CanAddOption)
                return;
            if (Kind == AlliancePollRules.KindPoll)
                Options.Add(string.Empty);
            else
                Slots.Add(Slots.Count > 0 ? Slots[Slots.Count - 1] + 3_600_000L : 0);
        }

        public void RemoveOption(int index)
        {
            if (!CanRemoveOption)
                return;
            if (Kind == AlliancePollRules.KindPoll && index >= 0 && index < Options.Count)
                Options.RemoveAt(index);
            else if (Kind == AlliancePollRules.KindSlots && index >= 0 && index < Slots.Count)
                Slots.RemoveAt(index);
        }

        public void ShiftSlot(int index, long deltaMs)
        {
            if (index >= 0 && index < Slots.Count)
                Slots[index] += deltaMs;
        }

        /// <summary>Why the server would refuse this form (AlliancePollRules.Check), or None.</summary>
        public SocialRejectReason Problem(long nowMs) => AlliancePollRules.Check(Kind, AlliancePollRules.Clean(Question, AlliancePollRules.MaxQuestion),
            Kind == AlliancePollRules.KindPoll ? Options.Select(o => AlliancePollRules.Clean(o, AlliancePollRules.MaxOption)).ToList() : new List<string>(),
            Kind == AlliancePollRules.KindSlots ? Slots.OrderBy(s => s).ToList() : new List<long>(), Hours, Kind == AlliancePollRules.KindSlots ? Purpose : string.Empty,
            nowMs);

        public CreateAlliancePollCommand Command(string commandId, ulong sequence) => new CreateAlliancePollCommand
        {
            CommandId = commandId,
            ClientSequence = sequence,
            Kind = Kind,
            Question = (Question ?? string.Empty).Trim(),
            Options = Kind == AlliancePollRules.KindPoll ? Options.Select(o => (o ?? string.Empty).Trim()).ToArray() : Array.Empty<string>(),
            SlotsMs = Kind == AlliancePollRules.KindSlots ? Slots.ToArray() : Array.Empty<long>(),
            Hours = Hours,
            Multiple = Multiple,
            Anonymous = Kind == AlliancePollRules.KindPoll && Anonymous,
            Purpose = Kind == AlliancePollRules.KindSlots ? Purpose : string.Empty
        };
    }
}
