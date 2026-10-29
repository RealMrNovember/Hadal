using System;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using UnityEngine;

namespace Hadal.Core.Services.Notifications
{
    /// <summary>
    /// The player's phone notification choices on this device (PlayerPrefs, docs/36). The settings screen edits them;
    /// <see cref="Changed"/> tells the scheduler to replan and the server sync to save them on the server too.
    /// </summary>
    public static class NotificationPrefsStore
    {
        private const string Prefix = "hadal.notify.";

        public static event Action Changed;

        /// <summary>True once the player changed anything on this device (until then a server copy is adopted).</summary>
        public static bool HasLocal => PlayerPrefs.HasKey(Prefix + "saved");

        /// <summary>The Android permission question was asked once already (it is never asked at app start or twice).</summary>
        public static bool PermissionAsked
        {
            get => PlayerPrefs.GetInt(Prefix + "asked", 0) != 0;
            set
            {
                PlayerPrefs.SetInt(Prefix + "asked", value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>The current choices (a copy; edit and pass to <see cref="Save"/>).</summary>
        public static NotificationPrefsDto Current => new()
        {
            Off = PlayerPrefs.GetInt(Prefix + "off", 0) != 0,
            DisabledMask = PlayerPrefs.GetInt(Prefix + "disabled", 0),
            QuietHours = PlayerPrefs.GetInt(Prefix + "quiet", 0) != 0,
            QuietStartMinute = PlayerPrefs.GetInt(Prefix + "quietFrom", NotificationRules.DefaultQuietStart),
            QuietEndMinute = PlayerPrefs.GetInt(Prefix + "quietTo", NotificationRules.DefaultQuietEnd)
        };

        public static void Save(NotificationPrefsDto prefs) => Write(prefs, true);

        /// <summary>Takes the server's copy on a device that has none of its own (a new phone, a reinstall).</summary>
        public static void Adopt(NotificationPrefsDto prefs) => Write(prefs, true);

        public static bool IsEnabled(NotificationCategory category) => NotificationRules.IsEnabled(Current, category);

        public static void SetEnabled(NotificationCategory category, bool enabled) =>
            Save(NotificationRules.With(Current, category, enabled));

        private static void Write(NotificationPrefsDto prefs, bool notify)
        {
            var p = NotificationRules.Sanitize(prefs);
            PlayerPrefs.SetInt(Prefix + "off", p.Off ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "disabled", p.DisabledMask);
            PlayerPrefs.SetInt(Prefix + "quiet", p.QuietHours ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "quietFrom", p.QuietStartMinute);
            PlayerPrefs.SetInt(Prefix + "quietTo", p.QuietEndMinute);
            PlayerPrefs.SetInt(Prefix + "saved", 1);
            PlayerPrefs.Save();
            if (notify)
                Changed?.Invoke();
        }

        /// <summary>Tests: back to "never changed".</summary>
        public static void ResetForTests()
        {
            foreach (var key in new[] { "off", "disabled", "quiet", "quietFrom", "quietTo", "saved", "asked" })
                PlayerPrefs.DeleteKey(Prefix + key);
        }
    }
}
