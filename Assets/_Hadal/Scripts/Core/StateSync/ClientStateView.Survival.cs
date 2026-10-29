using HADAL.Shared.DTOs;

namespace Hadal.Core.StateSync
{
    /// <summary>
    /// The survival loop's alarms and the running emergency (docs/35), as the server last sent them with the production
    /// summary. Timers are turned into realtime end points on arrival, like the other countdowns.
    /// </summary>
    public sealed partial class ClientStateView
    {
        /// <summary>Null from older servers or while the loop is off.</summary>
        public SurvivalStateDto Survival { get; private set; }

        /// <summary>Realtime (Time.realtimeSinceStartup) when the emergency fixes itself and when its repair ends; 0 = none.</summary>
        public float EmergencyExpiresAt { get; private set; }
        public float RepairEndsAt { get; private set; }

        /// <summary>Bumped whenever the survival state arrives (the HUD and the base redraw on change).</summary>
        public int SurvivalVersion { get; private set; }

        private void ApplySurvival(SurvivalStateDto survival)
        {
            Survival = survival;
            var now = UnityEngine.Time.realtimeSinceStartup;
            EmergencyExpiresAt = survival != null && survival.ExpiresRemainingMs > 0 ? now + survival.ExpiresRemainingMs / 1000f : 0f;
            RepairEndsAt = survival != null && survival.RepairRemainingMs > 0 ? now + survival.RepairRemainingMs / 1000f : 0f;
            SurvivalVersion++;
        }

        private void ResetSurvival()
        {
            Survival = null;
            EmergencyExpiresAt = 0f;
            RepairEndsAt = 0f;
            SurvivalVersion++;
        }
    }
}
