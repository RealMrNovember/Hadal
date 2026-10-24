namespace HADAL.Shared.Simulation
{
    /// <summary>
    /// Canonical simulation clock. Server and client must agree on these values or
    /// production totals diverge — treat every constant here as part of the wire contract.
    /// </summary>
    public static class SimulationTick
    {
        /// <summary>
        /// Wall-clock seconds represented by one simulation tick.
        /// This is an ECONOMY BALANCE constant: changing it rescales every production rate
        /// in the game. It is deliberately separate from <see cref="ReplicationIntervalSeconds"/>
        /// so that tuning network chatter cannot silently rebalance the economy.
        /// </summary>
        public const int IntervalSeconds = 1;

        /// <summary>
        /// How often accumulated state is pushed to clients. A NETWORK constant — safe to
        /// tune. Raising it makes the HUD update less often; it does not change what is earned.
        /// </summary>
        public const int ReplicationIntervalSeconds = 5;

        public const long TicksPerMinute = 60 / IntervalSeconds;
        public const long TicksPerHour = 3600 / IntervalSeconds;

        /// <summary>Simulation ticks covered by one replication frame.</summary>
        public const long TicksPerReplication = ReplicationIntervalSeconds / IntervalSeconds;

        /// <summary>
        /// Upper bound on accrual granted for a single reconnect (12 hours).
        /// Caps both honest long absences and a tampered device clock.
        /// </summary>
        public const long MaxOfflineAccrualTicks = 12 * TicksPerHour;

        public static ulong TicksFromSeconds(double elapsedSeconds)
        {
            if (elapsedSeconds <= 0d)
                return 0UL;

            return (ulong)(elapsedSeconds / IntervalSeconds);
        }

        public static double SecondsFromTicks(ulong ticks) => ticks * (double)IntervalSeconds;

        /// <summary>Clamps an elapsed-tick count to <see cref="MaxOfflineAccrualTicks"/>.</summary>
        public static ulong ClampOfflineTicks(ulong elapsedTicks)
        {
            return elapsedTicks > (ulong)MaxOfflineAccrualTicks
                ? (ulong)MaxOfflineAccrualTicks
                : elapsedTicks;
        }
    }
}
