using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using Hadal.Core.Services.Notifications;
using Hadal.Core.StateSync;
using Hadal.UI;
using NUnit.Framework;

namespace Hadal.Tests.EditMode
{
    /// <summary>
    /// Phone notifications (docs/36): the planner that turns the client's timers into the phone's schedule (choices,
    /// quiet hours, coalescing, limits), the timers read from the state, the texts in every language and the choices
    /// kept on the device.
    /// </summary>
    public sealed class NotificationSchedulerTests
    {
        private static readonly long Now = new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        private const long Minute = 60_000;

        private static NotificationCandidate At(NotificationCategory category, string kind, long minutes) =>
            new(category, kind, Now + minutes * Minute);

        [Test]
        public void Only_allowed_local_categories_are_planned()
        {
            var candidates = new[]
            {
                At(NotificationCategory.Build, "build", 30), At(NotificationCategory.Shield, "shield", 40),
                At(NotificationCategory.Attack, "build", 50) // a push category is never scheduled on the phone
            };
            var all = LocalNotificationPlanner.Plan(candidates, new NotificationPrefsDto(), Now, 0);
            CollectionAssert.AreEqual(new[] { NotificationCategory.Build, NotificationCategory.Shield }, all.Select(p => p.Category));

            var noShield = LocalNotificationPlanner.Plan(candidates,
                new NotificationPrefsDto { DisabledMask = 1 << (int)NotificationCategory.Shield }, Now, 0);
            CollectionAssert.AreEqual(new[] { NotificationCategory.Build }, noShield.Select(p => p.Category));

            Assert.IsEmpty(LocalNotificationPlanner.Plan(candidates, new NotificationPrefsDto { Off = true }, Now, 0));
        }

        [Test]
        public void Past_imminent_and_far_timers_are_skipped()
        {
            var plan = LocalNotificationPlanner.Plan(new[]
            {
                At(NotificationCategory.Build, "build", -5), new NotificationCandidate(NotificationCategory.Troops, "train", Now + 20_000),
                At(NotificationCategory.Marches, "home", 9 * 24 * 60), At(NotificationCategory.Events, "event", 120)
            }, null, Now, 0);
            Assert.AreEqual(1, plan.Count);
            Assert.AreEqual(NotificationCategory.Events, plan[0].Category);
            Assert.AreEqual("events", plan[0].Screen);
        }

        [Test]
        public void Timers_ending_close_together_make_one_notification_and_categories_keep_their_distance()
        {
            var plan = LocalNotificationPlanner.Plan(new[]
            {
                At(NotificationCategory.Build, "build", 10), At(NotificationCategory.Build, "research", 12), At(NotificationCategory.Build, "build", 14),
                At(NotificationCategory.Build, "build", 16), // a new cluster, but not closer than the window to the first
                At(NotificationCategory.Troops, "train", 11)
            }, new NotificationPrefsDto(), Now, 0);

            var build = plan.Where(p => p.Category == NotificationCategory.Build).ToList();
            Assert.AreEqual(2, build.Count);
            Assert.AreEqual(3, build[0].Count);
            Assert.AreEqual("build", build[0].Kind); // build + research read as "build"
            Assert.AreEqual(Now + 14 * Minute, build[0].FireAtMs); // when the last of the three is done
            Assert.GreaterOrEqual(build[1].FireAtMs - build[0].FireAtMs, (long)NotificationRules.CoalesceWindow.TotalMilliseconds);
            Assert.AreEqual(1, plan.Count(p => p.Category == NotificationCategory.Troops));
        }

        [Test]
        public void Quiet_hours_move_notifications_to_their_end()
        {
            // UTC+3: 12:00 UTC is 15:00 local. A timer at 02:00 local (23:00 UTC) waits until 08:00 local (05:00 UTC).
            var prefs = new NotificationPrefsDto { QuietHours = true, QuietStartMinute = 23 * 60, QuietEndMinute = 8 * 60 };
            var plan = LocalNotificationPlanner.Plan(new[] { At(NotificationCategory.Build, "build", 11 * 60), At(NotificationCategory.Troops, "heal", 60) },
                prefs, Now, 180);
            var build = plan.Single(p => p.Category == NotificationCategory.Build);
            Assert.AreEqual(new DateTimeOffset(2026, 10, 31, 5, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds(), build.FireAtMs);
            Assert.AreEqual(Now + 60 * Minute, plan.Single(p => p.Category == NotificationCategory.Troops).FireAtMs);
        }

        [Test]
        public void The_schedule_is_capped_and_its_signature_follows_changes()
        {
            var many = Enumerable.Range(0, 100).Select(i => At(NotificationCategory.Marches, "home", 10 + i * 10)).ToList();
            var plan = LocalNotificationPlanner.Plan(many, null, Now, 0);
            Assert.AreEqual(LocalNotificationPlanner.MaxScheduled, plan.Count);
            Assert.IsTrue(plan.Zip(plan.Skip(1), (a, b) => a.FireAtMs <= b.FireAtMs).All(x => x));

            var a1 = LocalNotificationPlanner.Signature(LocalNotificationPlanner.Plan(many, null, Now, 0), "tr");
            var a2 = LocalNotificationPlanner.Signature(LocalNotificationPlanner.Plan(many, null, Now, 0), "tr");
            var b = LocalNotificationPlanner.Signature(LocalNotificationPlanner.Plan(many.Skip(1), null, Now, 0), "tr");
            var c = LocalNotificationPlanner.Signature(LocalNotificationPlanner.Plan(many, null, Now, 0), "en");
            Assert.AreEqual(a1, a2);
            Assert.AreNotEqual(a1, b);
            Assert.AreNotEqual(a1, c);
        }

        [Test]
        public void Timers_are_read_from_the_state_on_the_device_clock()
        {
            var view = new ClientStateView();
            var serverNow = Now + 5_000; // the server runs 5 s ahead of this device
            view.ApplyMarches(new MarchListDto
            {
                ServerNowMs = serverNow,
                Marches = new[]
                {
                    new MarchDto { Id = "m1", Kind = "gather", Phase = MarchPhase.Gathering, GatheredAtMs = serverNow + 30 * Minute, HomeAtMs = serverNow + 40 * Minute },
                    new MarchDto { Id = "m2", Kind = "attack", Phase = MarchPhase.Outbound, ArrivesAtMs = serverNow + 5 * Minute, HomeAtMs = serverNow + 9 * Minute }
                }
            });
            view.ApplyArmy(new ArmyDto
            {
                Training = new[] { new TrainingDto { EndsAtMs = serverNow + 20 * Minute } },
                Healing = new HealingDto { EndsAtMs = serverNow + 25 * Minute }
            });
            view.ApplyPvp(new PvpStatusDto { ServerNowMs = serverNow, ShieldUntilMs = serverNow + 120 * Minute });

            var device = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var offset = view.ServerNowMs - device; // whatever the real clock says, timers land relative to it
            var list = NotificationCandidates.Collect(view, device);
            long Expect(long serverMs) => serverMs - offset;

            Assert.That(list.Single(c => c.Kind == "gather").FireAtMs, Is.EqualTo(Expect(serverNow + 30 * Minute)).Within(50));
            Assert.That(list.Single(c => c.Kind == "arrive").FireAtMs, Is.EqualTo(Expect(serverNow + 5 * Minute)).Within(50));
            Assert.AreEqual(2, list.Count(c => c.Kind == "home"));
            Assert.That(list.Single(c => c.Kind == "train").FireAtMs, Is.EqualTo(Expect(serverNow + 20 * Minute)).Within(50));
            Assert.That(list.Single(c => c.Kind == "heal").FireAtMs, Is.EqualTo(Expect(serverNow + 25 * Minute)).Within(50));
            Assert.That(list.Single(c => c.Kind == "shield").FireAtMs, Is.EqualTo(Expect(serverNow + 90 * Minute)).Within(50));
            Assert.AreEqual(NotificationCategory.Rewards, list.Single(c => c.Kind == "rewards").Category);
        }

        [Test]
        public void Daily_rewards_never_wake_anyone()
        {
            var local = new DateTimeOffset(2026, 10, 31, 3, 1, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 10, 31, 3, 1, 0)));
            var moved = DateTimeOffset.FromUnixTimeMilliseconds(NotificationCandidates.DaytimeAfter(local.ToUnixTimeMilliseconds())).ToLocalTime();
            Assert.AreEqual(9, moved.Hour);
            Assert.AreEqual(31, moved.Day);

            var noon = new DateTimeOffset(2026, 10, 31, 12, 0, 0, local.Offset).ToUnixTimeMilliseconds();
            Assert.AreEqual(noon, NotificationCandidates.DaytimeAfter(noon));
        }

        [Test]
        public void Every_notification_has_a_text_in_the_game_language()
        {
            var previous = Loc.Current;
            try
            {
                foreach (var (code, _) in Loc.Languages)
                {
                    Loc.Use(code);
                    foreach (var kind in new[] { "build", "research", "train", "heal", "arrive", "gather", "home", "shield", "rewards", "event" })
                    {
                        var (title, body) = NotificationTexts.For(new PlannedNotification { Kind = kind, Count = 1, Arg = "Deep Hunt" });
                        Assert.IsFalse(title.StartsWith("notify."), $"{code} {kind} title");
                        Assert.IsFalse(body.StartsWith("notify."), $"{code} {kind} body");
                    }

                    var (_, multi) = NotificationTexts.For(new PlannedNotification { Kind = "build", Count = 3 });
                    StringAssert.Contains("3", multi, code);
                    for (var i = 0; i < NotificationRules.CategoryCount; i++)
                        Assert.IsTrue(Loc.Has(NotificationTexts.CategoryKey((NotificationCategory)i)), code);
                }

                Loc.Use("tr");
                Assert.AreEqual("İnşaat tamamlandı", NotificationTexts.For(new PlannedNotification { Kind = "build", Count = 1 }).Title);
                StringAssert.Contains("Deep Hunt", NotificationTexts.For(new PlannedNotification { Kind = "event", Count = 1, Arg = "Deep Hunt" }).Body);
            }
            finally
            {
                Loc.Use(previous);
            }
        }

        [Test]
        public void Choices_on_the_device_round_trip_and_a_new_device_can_adopt_the_servers()
        {
            NotificationPrefsStore.ResetForTests();
            try
            {
                Assert.IsFalse(NotificationPrefsStore.HasLocal);
                Assert.IsTrue(NotificationPrefsStore.IsEnabled(NotificationCategory.Build));

                var changed = 0;
                void Count() => changed++;
                NotificationPrefsStore.Changed += Count;
                NotificationPrefsStore.SetEnabled(NotificationCategory.Shield, false);
                NotificationPrefsStore.Changed -= Count;
                Assert.AreEqual(1, changed);
                Assert.IsTrue(NotificationPrefsStore.HasLocal);
                Assert.IsFalse(NotificationPrefsStore.IsEnabled(NotificationCategory.Shield));
                Assert.IsTrue(NotificationPrefsStore.IsEnabled(NotificationCategory.Build));

                var server = new NotificationPrefsDto { Off = true, QuietHours = true, QuietStartMinute = 22 * 60, QuietEndMinute = 7 * 60, Saved = true };
                NotificationPrefsStore.Adopt(server);
                Assert.IsTrue(NotificationPrefsStore.Current.SameChoices(new NotificationPrefsDto
                {
                    Off = true, QuietHours = true, QuietStartMinute = 22 * 60, QuietEndMinute = 7 * 60
                }));
            }
            finally
            {
                NotificationPrefsStore.ResetForTests();
            }
        }
    }
}
