using System;

namespace HADAL.Shared.Simulation
{
    /// <summary>
    /// Pure, deterministic economy math shared by the server simulation and the client stub.
    /// Integer-only on purpose: floating point would let server and client disagree by a
    /// unit after a few thousand ticks, and resources are money.
    /// </summary>
    public static class ProductionFormulas
    {
        /// <summary>Additive output bonus per building level above 1, in percent.</summary>
        public const int LevelBonusPercentPerLevel = 25;

        /// <summary>
        /// Scales a level-1 per-tick rate to the building's level.
        /// Level 1 = 100%, level 2 = 125%, level 3 = 150%, ...
        /// </summary>
        public static long ScaleByLevel(long basePerTick, int level)
        {
            if (basePerTick == 0L)
                return 0L;

            var effectiveLevel = level < 1 ? 1 : level;
            var percent = 100L + LevelBonusPercentPerLevel * (long)(effectiveLevel - 1);
            return basePerTick * percent / 100L;
        }

        /// <summary>Resources are simulated in thousandths so percentage bonuses survive on small rates.</summary>
        public const long Milli = 1000L;

        /// <summary>
        /// <see cref="ScaleByLevel"/> in thousandths of a unit per tick, with an extra percentage bonus
        /// (boosts, research). 2 per tick at level 2 is 2500, not the 2 the integer form rounds to.
        /// </summary>
        public static long ScaleByLevelMilli(long basePerTick, int level, int bonusPercent = 0)
        {
            if (basePerTick == 0L)
                return 0L;

            var effectiveLevel = level < 1 ? 1 : level;
            var levelPercent = 100L + LevelBonusPercentPerLevel * (long)(effectiveLevel - 1);
            var bonus = 100L + (bonusPercent < -100 ? -100 : bonusPercent);
            return basePerTick * 10L * levelPercent * bonus / 100L;
        }

        /// <summary>
        /// Total accrued over <paramref name="elapsedTicks"/>. Saturates instead of
        /// overflowing so a bad tick count can never wrap a balance negative.
        /// </summary>
        public static long Accrue(long netPerTick, ulong elapsedTicks)
        {
            if (netPerTick == 0L || elapsedTicks == 0UL)
                return 0L;

            if (elapsedTicks > long.MaxValue)
                return netPerTick > 0L ? long.MaxValue : long.MinValue;

            var ticks = (long)elapsedTicks;
            var magnitude = netPerTick < 0L ? -netPerTick : netPerTick;

            if (magnitude > long.MaxValue / ticks)
                return netPerTick > 0L ? long.MaxValue : long.MinValue;

            return netPerTick * ticks;
        }

        /// <summary>
        /// Applies a signed change to a balance, clamped to [0, capacity].
        /// A non-positive capacity is treated as "uncapped".
        /// </summary>
        public static long ApplyToBalance(long current, long change, long capacity)
        {
            long next;

            if (change > 0L && current > long.MaxValue - change)
                next = long.MaxValue;
            else if (change < 0L && current < long.MinValue - change)
                next = long.MinValue;
            else
                next = current + change;

            if (next < 0L)
                next = 0L;

            // Production stops at the cap but never takes away what is already above it (capsules, grants and
            // rewards may pass the cap); spending from an over-cap balance still works.
            if (capacity > 0L && next > capacity)
                next = Math.Max(capacity, Math.Min(current, next));

            return next;
        }

        /// <summary>Converts a per-tick rate to a per-hour rate for display.</summary>
        public static long PerTickToPerHour(long netPerTick) =>
            Accrue(netPerTick, (ulong)SimulationTick.TicksPerHour);
    }
}
