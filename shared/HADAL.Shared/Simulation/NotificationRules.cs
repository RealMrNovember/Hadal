using System;
using HADAL.Shared.DTOs;

namespace HADAL.Shared.Simulation
{
    /// <summary>
    /// What a phone notification is about (docs/36). The first six are scheduled on the phone from the timers the
    /// client knows; the rest are pushed by the server (Firebase Cloud Messaging). The number is the bit in
    /// <see cref="NotificationPrefsDto.DisabledMask"/> and must never change.
    /// </summary>
    public enum NotificationCategory
    {
        /// <summary>Construction, upgrades and research finished.</summary>
        Build = 0,

        /// <summary>Troop training and healing finished.</summary>
        Troops = 1,

        /// <summary>A march arrived, gathering finished, a team is home.</summary>
        Marches = 2,

        /// <summary>The base shield ends soon.</summary>
        Shield = 3,

        /// <summary>The daily VIP chest and sign-in reward are ready.</summary>
        Rewards = 4,

        /// <summary>An event ends soon.</summary>
        Events = 5,

        /// <summary>Push: an attack is coming, a battle report arrived.</summary>
        Attack = 6,

        /// <summary>Push: a mail arrived.</summary>
        Mail = 7,

        /// <summary>Push: an alliance mate asked for help or started a rally.</summary>
        Alliance = 8,

        /// <summary>Push: someone mentioned the player in chat or wrote privately.</summary>
        Chat = 9
    }

    public static class NotificationRules
    {
        public const int CategoryCount = 10;

        /// <summary>Default quiet hours when the player switches them on: 23:00–08:00 local.</summary>
        public const int DefaultQuietStart = 23 * 60;
        public const int DefaultQuietEnd = 8 * 60;

        /// <summary>Push tokens kept per player (phones); the oldest goes when a new one comes.</summary>
        public const int MaxDevices = 5;

        public const int MaxTokenLength = 4096;
        public const int MaxDeviceIdLength = 128;

        /// <summary>At most one notification per category in this window (phone and server alike).</summary>
        public static readonly TimeSpan CoalesceWindow = TimeSpan.FromMinutes(5);

        /// <summary>The shield warning comes this long before it ends.</summary>
        public static readonly TimeSpan ShieldWarning = TimeSpan.FromMinutes(30);

        /// <summary>The event warning comes this long before it ends.</summary>
        public static readonly TimeSpan EventWarning = TimeSpan.FromHours(1);

        /// <summary>Scheduled locally by the phone (the rest are pushed by the server).</summary>
        public static bool IsLocal(NotificationCategory category) => category <= NotificationCategory.Events;

        /// <summary>Pushed by the server; the settings show these as "soon" until push is live.</summary>
        public static bool IsPush(NotificationCategory category) => !IsLocal(category);

        /// <summary>The Android notification channel of a category (the phone creates them; push names them too).</summary>
        public static string ChannelId(NotificationCategory category) => "hadal_" + category.ToString().ToLowerInvariant();

        /// <summary>The small icon's drawable name (Assets/_Hadal/Art/Branding/Notify~, copied into the APK).</summary>
        public const string SmallIcon = "hadal_notify";

        public static bool IsEnabled(NotificationPrefsDto? prefs, NotificationCategory category) =>
            prefs == null || (!prefs.Off && (prefs.DisabledMask & (1 << (int)category)) == 0);

        public static NotificationPrefsDto With(NotificationPrefsDto prefs, NotificationCategory category, bool enabled)
        {
            var copy = prefs.Copy();
            copy.DisabledMask = enabled ? copy.DisabledMask & ~(1 << (int)category) : copy.DisabledMask | (1 << (int)category);
            return copy;
        }

        /// <summary>Keeps a client's document inside the rules (minutes of a day, a real UTC offset, known bits).</summary>
        public static NotificationPrefsDto Sanitize(NotificationPrefsDto? prefs)
        {
            var p = prefs?.Copy() ?? new NotificationPrefsDto();
            p.DisabledMask &= (1 << CategoryCount) - 1;
            p.QuietStartMinute = Mod(p.QuietStartMinute, 1440);
            p.QuietEndMinute = Mod(p.QuietEndMinute, 1440);
            p.UtcOffsetMinutes = Math.Max(-14 * 60, Math.Min(14 * 60, p.UtcOffsetMinutes));
            var language = (p.Language ?? string.Empty).Trim();
            p.Language = language.Length <= 10 ? language : string.Empty;
            return p;
        }

        /// <summary>Whether a local minute of the day (0..1439) lies in the player's quiet hours.</summary>
        public static bool InQuietHours(NotificationPrefsDto? prefs, int localMinute)
        {
            if (prefs == null || !prefs.QuietHours || prefs.QuietStartMinute == prefs.QuietEndMinute)
                return false;
            var m = Mod(localMinute, 1440);
            return prefs.QuietStartMinute < prefs.QuietEndMinute
                ? m >= prefs.QuietStartMinute && m < prefs.QuietEndMinute
                : m >= prefs.QuietStartMinute || m < prefs.QuietEndMinute;
        }

        /// <summary>
        /// When a notification due at <paramref name="atUtc"/> may be shown: unchanged outside quiet hours, else when
        /// they end (the phone's local time is UTC plus <paramref name="utcOffsetMinutes"/>).
        /// </summary>
        public static DateTimeOffset DeferPastQuietHours(NotificationPrefsDto? prefs, DateTimeOffset atUtc, int utcOffsetMinutes)
        {
            var local = atUtc.ToUniversalTime().AddMinutes(utcOffsetMinutes);
            var minute = local.Hour * 60 + local.Minute;
            if (!InQuietHours(prefs, minute))
                return atUtc;
            var wait = Mod(prefs!.QuietEndMinute - minute, 1440);
            return new DateTimeOffset(atUtc.UtcDateTime.AddSeconds(-local.Second).AddMilliseconds(-local.Millisecond), TimeSpan.Zero)
                .AddMinutes(wait);
        }

        private static int Mod(int value, int m) => ((value % m) + m) % m;
    }
}
