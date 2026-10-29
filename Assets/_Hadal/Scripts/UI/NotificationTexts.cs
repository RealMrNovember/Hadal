using System.Collections.Generic;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using Hadal.Core.Services.Notifications;
using Hadal.Core.StateSync;
using UnityEngine;

namespace Hadal.UI
{
    /// <summary>
    /// The timers a phone notification can announce, read from the client's state (docs/36). Server timestamps go
    /// through <see cref="ClientStateView.ServerNowMs"/>, the client's realtime countdowns through
    /// Time.realtimeSinceStartup; both end up on this device's clock.
    /// </summary>
    public static class NotificationCandidates
    {
        public static List<NotificationCandidate> Collect(ClientStateView view, long deviceNowMs)
        {
            var list = new List<NotificationCandidate>();
            if (view == null)
                return list;

            var serverNow = view.ServerNowMs;
            long FromServer(long serverMs) => deviceNowMs + (serverMs - serverNow);
            var realtime = Time.realtimeSinceStartup;
            long FromRealtime(float endsAt) => deviceNowMs + (long)((endsAt - realtime) * 1000f);

            foreach (var b in view.Data?.buildings ?? System.Array.Empty<Hadal.Data.Models.BuildingSaveEntry>())
            {
                if (b.IsUnderConstruction && b.constructionEndsAt > realtime)
                    list.Add(new NotificationCandidate(NotificationCategory.Build, "build", FromRealtime(b.constructionEndsAt)));
            }

            foreach (var r in view.ActiveResearch)
            {
                if (r.EndsAt > realtime)
                    list.Add(new NotificationCandidate(NotificationCategory.Build, "research", FromRealtime(r.EndsAt)));
            }

            if (view.Army is { } army)
            {
                foreach (var t in army.Training ?? System.Array.Empty<TrainingDto>())
                {
                    if (t.EndsAtMs > serverNow)
                        list.Add(new NotificationCandidate(NotificationCategory.Troops, "train", FromServer(t.EndsAtMs)));
                }

                if (army.Healing is { } heal && heal.EndsAtMs > serverNow)
                    list.Add(new NotificationCandidate(NotificationCategory.Troops, "heal", FromServer(heal.EndsAtMs)));
            }

            foreach (var m in view.Marches?.Marches ?? System.Array.Empty<MarchDto>())
            {
                if (m.Kind == "gather")
                {
                    if (m.Phase != MarchPhase.Returning && m.GatheredAtMs > serverNow && m.GatheredAtMs < m.HomeAtMs)
                        list.Add(new NotificationCandidate(NotificationCategory.Marches, "gather", FromServer(m.GatheredAtMs)));
                }
                else if (m.Phase == MarchPhase.Outbound && m.ArrivesAtMs > serverNow)
                {
                    list.Add(new NotificationCandidate(NotificationCategory.Marches, "arrive", FromServer(m.ArrivesAtMs)));
                }

                if (m.HomeAtMs > serverNow)
                    list.Add(new NotificationCandidate(NotificationCategory.Marches, "home", FromServer(m.HomeAtMs)));
            }

            var shieldUntil = view.Pvp?.ShieldUntilMs ?? 0;
            var warning = (long)NotificationRules.ShieldWarning.TotalMilliseconds;
            if (shieldUntil - warning > serverNow)
                list.Add(new NotificationCandidate(NotificationCategory.Shield, "shield", FromServer(shieldUntil - warning)));

            // Both daily VIP claims reset at the next UTC midnight (VipStateDto.ResetInMs).
            if (view.Vip != null)
            {
                const long day = 24L * 3600 * 1000;
                var reset = FromServer((serverNow / day + 1) * day + 60_000);
                list.Add(new NotificationCandidate(NotificationCategory.Rewards, "rewards", DaytimeAfter(reset)));
            }

            var eventWarning = (long)NotificationRules.EventWarning.TotalMilliseconds;
            foreach (var e in view.Events?.Events ?? System.Array.Empty<EventDto>())
            {
                if (e.StartsAtMs <= serverNow + eventWarning && e.EndsAtMs - eventWarning > serverNow)
                    list.Add(new NotificationCandidate(NotificationCategory.Events, "event", FromServer(e.EndsAtMs - eventWarning),
                        EventRules.Text(e.Title, Loc.Current)));
            }

            return list;
        }

        /// <summary>
        /// The daily rewards are no reason to wake anyone: a reset between 22:00 and 09:00 on the phone is announced at
        /// 09:00 (quiet hours, when set, apply on top).
        /// </summary>
        public static long DaytimeAfter(long deviceMs)
        {
            var local = System.DateTimeOffset.FromUnixTimeMilliseconds(deviceMs).ToLocalTime();
            if (local.Hour >= 9 && local.Hour < 22)
                return deviceMs;
            var nine = new System.DateTimeOffset(local.Year, local.Month, local.Day, 9, 0, 0, local.Offset);
            if (local.Hour >= 22)
                nine = nine.AddDays(1);
            return nine.ToUnixTimeMilliseconds();
        }
    }

    /// <summary>Notification texts and the settings labels of the categories, in the player's game language (Loc).</summary>
    public static class NotificationTexts
    {
        public static string CategoryKey(NotificationCategory category) => "notify.cat." + category.ToString().ToLowerInvariant();

        public static (string Title, string Body) For(PlannedNotification p)
        {
            var title = Loc.Plain("notify.msg." + p.Kind + ".title");
            string body;
            if (p.Count > 1)
                body = Loc.Plain("notify.msg.multi", p.Count);
            else if (p.Kind == "event")
                body = Loc.Plain("notify.msg.event.body", string.IsNullOrWhiteSpace(p.Arg) ? Loc.Plain("notify.cat.events") : p.Arg);
            else
                body = Loc.Plain("notify.msg." + p.Kind + ".body");
            return (title, body);
        }
    }
}
