#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections.Generic;
using HADAL.Shared.Simulation;
using Unity.Notifications.Android;

namespace Hadal.UI
{
    /// <summary>
    /// Android side of <see cref="PhoneNotifications"/>: Unity Mobile Notifications (com.unity.mobile.notifications).
    /// One channel per category, the white HADAL mark as the small icon (res/drawable hadal_notify, added by
    /// HadalNotificationIcons), the launcher icon as the large one, and the screen to open in the intent data.
    /// </summary>
    internal sealed class AndroidPhoneNotificationBackend : IPhoneNotificationBackend
    {
        /// <summary>The launcher icon's resource (Unity names it app_icon).</summary>
        private const string LargeIcon = "app_icon";

        private int _lastTapped = int.MinValue;

        public void Initialize() => AndroidNotificationCenter.Initialize();

        public bool RegisterChannels(IReadOnlyList<(string Id, string Name, bool Important)> channels)
        {
            foreach (var (id, name, important) in channels)
            {
                AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel
                {
                    Id = id,
                    Name = name,
                    Description = name,
                    Importance = important ? Importance.High : Importance.Default,
                    CanShowBadge = true,
                    EnableVibration = true,
                    LockScreenVisibility = LockScreenVisibility.Public
                });
            }

            return true;
        }

        public void Schedule(PhoneNotification notification)
        {
            var n = new AndroidNotification
            {
                Title = notification.Title,
                Text = notification.Body,
                FireTime = notification.FireAtLocal,
                SmallIcon = NotificationRules.SmallIcon,
                LargeIcon = LargeIcon,
                IntentData = notification.Screen,
                Group = notification.Group,
                ShouldAutoCancel = true,
                ShowTimestamp = true,
                Style = NotificationStyle.BigTextStyle
            };
            AndroidNotificationCenter.SendNotification(n, notification.ChannelId);
        }

        public void CancelAll() => AndroidNotificationCenter.CancelAllNotifications();

        public void RequestPermission() => _ = new PermissionRequest(); // the package remembers the answer

        public bool Blocked => AndroidNotificationCenter.UserPermissionToPost is PermissionStatus.Denied
            or PermissionStatus.DeniedDontAskAgain or PermissionStatus.NotificationsBlockedForApp;

        public void OpenSettings() => AndroidNotificationCenter.OpenNotificationSettings();

        public string TakeTappedScreen()
        {
            var data = AndroidNotificationCenter.GetLastNotificationIntent();
            if (data == null || data.Id == _lastTapped)
                return null;
            _lastTapped = data.Id;
            return data.Notification.IntentData;
        }
    }
}
#endif
