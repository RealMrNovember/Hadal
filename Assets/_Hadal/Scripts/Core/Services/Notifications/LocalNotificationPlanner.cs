using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;

namespace Hadal.Core.Services.Notifications
{
    /// <summary>One timer the phone could announce (docs/36): what it is and when it ends, on this device's UTC clock.</summary>
    public readonly struct NotificationCandidate
    {
        public readonly NotificationCategory Category;

        /// <summary>The text family ("build", "research", "train", "heal", "arrive", "gather", "home", "shield", "rewards", "event").</summary>
        public readonly string Kind;

        /// <summary>Unix ms on this device's clock.</summary>
        public readonly long FireAtMs;

        /// <summary>Optional text argument (an event's title).</summary>
        public readonly string Arg;

        public NotificationCandidate(NotificationCategory category, string kind, long fireAtMs, string arg = "")
        {
            Category = category;
            Kind = kind ?? string.Empty;
            FireAtMs = fireAtMs;
            Arg = arg ?? string.Empty;
        }
    }

    /// <summary>A notification to hand to the phone: one per category cluster.</summary>
    public sealed class PlannedNotification
    {
        public NotificationCategory Category;
        public string Kind;
        public string Arg;
        public long FireAtMs;

        /// <summary>How many timers it stands for (2+ reads "N tasks are done").</summary>
        public int Count;

        /// <summary>The screen a tap opens ("base", "research", "map", "vip", "events").</summary>
        public string Screen;
    }

    /// <summary>
    /// Turns the timers the client knows into the phone's schedule (docs/36). Pure: no Unity, no clock of its own, so
    /// EditMode tests drive it. Rules: only categories the player allows; nothing in the past or in the first seconds;
    /// quiet hours move a notification to when they end; within a category notifications that end close together merge
    /// into one, and two of a category are never closer than the coalescing window; at most <see cref="MaxScheduled"/>.
    /// </summary>
    public static class LocalNotificationPlanner
    {
        /// <summary>Android keeps a limited number of alarms per app; the soonest win.</summary>
        public const int MaxScheduled = 32;

        /// <summary>Timers ending sooner than this are not worth a notification (the player is still looking).</summary>
        public const long MinLeadMs = 60_000;

        /// <summary>Nothing further than this is scheduled (the next login refreshes the plan anyway).</summary>
        public const long MaxAheadMs = 8L * 24 * 3600 * 1000;

        public static List<PlannedNotification> Plan(IEnumerable<NotificationCandidate> candidates, NotificationPrefsDto prefs, long nowMs,
            int utcOffsetMinutes)
        {
            var window = (long)NotificationRules.CoalesceWindow.TotalMilliseconds;
            var result = new List<PlannedNotification>();
            if (prefs != null && prefs.Off)
                return result;

            var due = new List<NotificationCandidate>();
            foreach (var c in candidates ?? Enumerable.Empty<NotificationCandidate>())
            {
                if (!NotificationRules.IsLocal(c.Category) || !NotificationRules.IsEnabled(prefs, c.Category))
                    continue;
                if (c.FireAtMs < nowMs + MinLeadMs || c.FireAtMs > nowMs + MaxAheadMs)
                    continue;
                var at = NotificationRules.DeferPastQuietHours(prefs, DateTimeOffset.FromUnixTimeMilliseconds(c.FireAtMs), utcOffsetMinutes)
                    .ToUnixTimeMilliseconds();
                due.Add(new NotificationCandidate(c.Category, c.Kind, at, c.Arg));
            }

            foreach (var group in due.GroupBy(c => c.Category))
            {
                PlannedNotification current = null;
                long clusterStart = 0;
                long lastFire = long.MinValue;
                foreach (var c in group.OrderBy(c => c.FireAtMs).ThenBy(c => c.Kind, StringComparer.Ordinal))
                {
                    if (current != null && c.FireAtMs <= clusterStart + window)
                    {
                        // Same cluster: it fires when its last member is done.
                        current.Count++;
                        current.FireAtMs = Math.Max(current.FireAtMs, c.FireAtMs);
                        if (current.Kind != c.Kind)
                            current.Kind = Mixed(current.Kind, c.Kind);
                        continue;
                    }

                    if (current != null)
                        lastFire = current.FireAtMs;
                    clusterStart = c.FireAtMs;
                    current = new PlannedNotification
                    {
                        Category = c.Category, Kind = c.Kind, Arg = c.Arg, Count = 1,
                        FireAtMs = lastFire == long.MinValue ? c.FireAtMs : Math.Max(c.FireAtMs, lastFire + window)
                    };
                    result.Add(current);
                }
            }

            foreach (var p in result)
            {
                // A cluster pushed into quiet hours by the spacing goes to their end as well.
                p.FireAtMs = NotificationRules.DeferPastQuietHours(prefs, DateTimeOffset.FromUnixTimeMilliseconds(p.FireAtMs), utcOffsetMinutes)
                    .ToUnixTimeMilliseconds();
                p.Screen = ScreenOf(p.Kind);
            }

            return result.OrderBy(p => p.FireAtMs).ThenBy(p => (int)p.Category).Take(MaxScheduled).ToList();
        }

        /// <summary>Two kinds in one notification: a category's own wording for both (build + research = "build").</summary>
        private static string Mixed(string a, string b) => a == "research" || b == "research" ? "build" : a;

        public static string ScreenOf(string kind) => kind switch
        {
            "research" => "research",
            "arrive" or "gather" or "home" => "map",
            "rewards" => "vip",
            "event" => "events",
            _ => "base"
        };

        /// <summary>A short fingerprint of a plan: the phone is only rescheduled when it changes.</summary>
        public static string Signature(IEnumerable<PlannedNotification> plan, string language)
        {
            var text = new StringBuilder(language ?? string.Empty);
            foreach (var p in plan)
                text.Append('|').Append((int)p.Category).Append(':').Append(p.Kind).Append(':').Append(p.Count).Append(':')
                    .Append(p.FireAtMs / 1000).Append(':').Append(p.Arg);
            return text.ToString();
        }
    }
}
