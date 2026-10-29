using HADAL.Shared.DTOs;

namespace Hadal.Core.StateSync
{
    /// <summary>The phone notification choices the server keeps for push (docs/36).</summary>
    public sealed partial class ClientStateView
    {
        /// <summary>Null until the server sent them (older servers never do).</summary>
        public NotificationPrefsDto ServerNotificationPrefs { get; private set; }

        public int NotificationPrefsVersion { get; private set; }

        public void ApplyNotificationPrefs(NotificationPrefsDto prefs)
        {
            ServerNotificationPrefs = prefs;
            NotificationPrefsVersion++;
        }
    }
}
