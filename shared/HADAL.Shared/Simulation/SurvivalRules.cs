using System;
using System.Collections.Generic;
using HADAL.Shared.Catalog;
using HADAL.Shared.Enums;

namespace HADAL.Shared.Simulation
{
    /// <summary>How a life-support resource stands (wire values of <c>SurvivalStateDto</c>).</summary>
    public enum SurvivalStatus
    {
        /// <summary>Enough, or the loop is off.</summary>
        Normal = 0,

        /// <summary>Falling and under the warning share of the store: a warning, no penalty.</summary>
        Low = 1,

        /// <summary>The store is empty and demand is above supply: the penalty applies.</summary>
        Critical = 2
    }

    /// <summary>Random emergencies (docs/35 §4).</summary>
    public static class EmergencyKinds
    {
        /// <summary>An oxygen leak: a share of the oxygen production is lost until repaired.</summary>
        public const string Leak = "leak";

        /// <summary>A breakdown: one producer stops until repaired.</summary>
        public const string Breakdown = "breakdown";
    }

    /// <summary>Why a repair was refused (<c>CommandAckDto.Reason</c>).</summary>
    public enum EmergencyRejectReason
    {
        None = 0,
        NoEmergency = 1,
        AlreadyRepairing = 2,
        InsufficientResources = 3,
        SurvivalOff = 4
    }

    /// <summary>
    /// What the survival loop does to one economy step: energy users run at <see cref="EnergyPermille"/> of their rate,
    /// and every production but oxygen and energy loses <see cref="OutputPenaltyPercent"/>.
    /// </summary>
    public readonly struct SurvivalEffects
    {
        public readonly int EnergyPermille;
        public readonly int OutputPenaltyPercent;

        public SurvivalEffects(int energyPermille, int outputPenaltyPercent)
        {
            EnergyPermille = Math.Max(0, Math.Min(1000, energyPermille));
            OutputPenaltyPercent = Math.Max(0, Math.Min(100, outputPenaltyPercent));
        }

        public static SurvivalEffects None => new SurvivalEffects(1000, 0);

        public bool IsNone => EnergyPermille >= 1000 && OutputPenaltyPercent <= 0;
    }

    /// <summary>
    /// The survival loop's shared rules (docs/35): the energy chain, the oxygen thresholds and penalties, and the seeded
    /// emergency roll. Integer-only and deterministic, like <see cref="ProductionFormulas"/>; the server applies them,
    /// the client only shows what the server sends.
    /// </summary>
    public static class SurvivalRules
    {
        public static bool On(SurvivalCatalogData? rules) => rules != null && rules.enabled;

        public static bool EmergenciesOn(SurvivalCatalogData? rules) => On(rules) && rules!.emergencies != null && rules.emergencies.enabled;

        // --- Energy chain -----------------------------------------------------------------------------------------

        /// <summary>
        /// How hard energy users may run, in thousandths: full (1000) while the store holds energy or supply covers
        /// demand; with the store empty, supply/demand rounded up (so the store stays at 0 instead of flickering), never
        /// under the catalog floor.
        /// </summary>
        public static int EnergyPermille(long producedMilli, long consumedMilli, long energyBalance, SurvivalCatalogData? rules)
        {
            if (!On(rules) || consumedMilli <= 0 || producedMilli >= consumedMilli || energyBalance > 0)
                return 1000;
            var floor = Math.Max(1, Math.Min(100, rules!.energyFloorPercent)) * 10;
            var ratio = (Math.Max(0, producedMilli) * 1000 + consumedMilli - 1) / consumedMilli;
            return (int)Math.Max(floor, Math.Min(1000, ratio));
        }

        /// <summary>Energy: Critical when the store is empty and demand is above supply, Low when it is falling under the warning share.</summary>
        public static SurvivalStatus EnergyStatus(long producedMilli, long consumedMilli, long energyBalance, long capacity, SurvivalCatalogData? rules)
        {
            if (!On(rules) || producedMilli >= consumedMilli)
                return SurvivalStatus.Normal;
            if (energyBalance <= 0)
                return SurvivalStatus.Critical;
            return Below(energyBalance, capacity, rules!.warnPercent) ? SurvivalStatus.Low : SurvivalStatus.Normal;
        }

        // --- Oxygen -----------------------------------------------------------------------------------------------

        /// <summary>Oxygen: Critical at 0 while still falling, Low when falling under the warning share of the store.</summary>
        public static SurvivalStatus OxygenStatus(long netMilliPerTick, long oxygenBalance, long capacity, SurvivalCatalogData? rules)
        {
            if (!On(rules) || netMilliPerTick >= 0)
                return SurvivalStatus.Normal;
            if (oxygenBalance <= 0)
                return SurvivalStatus.Critical;
            return Below(oxygenBalance, capacity, rules!.warnPercent) ? SurvivalStatus.Low : SurvivalStatus.Normal;
        }

        private static bool Below(long balance, long capacity, int percent) =>
            capacity > 0 && capacity < long.MaxValue / 100 && balance < capacity * Math.Max(0, Math.Min(100, percent)) / 100;

        /// <summary>The production penalty in percent: only while oxygen is critical.</summary>
        public static int OutputPenalty(SurvivalStatus oxygen, SurvivalCatalogData? rules) =>
            On(rules) && oxygen == SurvivalStatus.Critical ? Math.Max(0, Math.Min(90, rules!.outputPenaltyPercent)) : 0;

        /// <summary>The time penalty in percent: only while oxygen is critical.</summary>
        public static int TimePenalty(SurvivalStatus oxygen, SurvivalCatalogData? rules) =>
            On(rules) && oxygen == SurvivalStatus.Critical ? Math.Max(0, Math.Min(200, rules!.timePenaltyPercent)) : 0;

        /// <summary>A timer started while oxygen is critical takes longer (constructions, researches, training).</summary>
        public static int SlowSeconds(int seconds, int timePenaltyPercent)
        {
            if (seconds <= 0 || timePenaltyPercent <= 0)
                return seconds;
            return (int)Math.Min(int.MaxValue, (long)seconds * (100 + timePenaltyPercent) / 100);
        }

        /// <summary>A per-hour amount as thousandths per simulation tick.</summary>
        public static long PerHourToMilliPerTick(long perHour) =>
            perHour <= 0 ? 0 : perHour * ProductionFormulas.Milli * SimulationTick.IntervalSeconds / 3600;

        /// <summary>The base crew's oxygen per tick (thousandths): per Core Reactor level.</summary>
        public static long CrewOxygenMilli(int reactorLevel, SurvivalCatalogData? rules) =>
            On(rules) ? PerHourToMilliPerTick(Math.Max(0, rules!.crewOxygenPerReactorLevel) * Math.Max(1, reactorLevel)) : 0;

        /// <summary>A leak's oxygen loss per tick (thousandths): a share of the gross oxygen production, at least the minimum.</summary>
        public static long LeakMilli(long grossOxygenMilli, SurvivalCatalogData? rules)
        {
            if (!EmergenciesOn(rules))
                return 0;
            var e = rules!.emergencies;
            return Math.Max(Math.Max(0, grossOxygenMilli) * Math.Max(0, Math.Min(100, e.leakPercent)) / 100, PerHourToMilliPerTick(e.leakMinPerHour));
        }

        /// <summary>
        /// Ticks until a falling balance reaches 0 (the economy splits its step there, so the penalty starts on the
        /// exact tick, online and offline alike); 0 when it is not falling or already empty.
        /// </summary>
        public static ulong TicksUntilEmpty(long balance, long carryMilli, long netMilliPerTick)
        {
            if (netMilliPerTick >= 0 || balance <= 0)
                return 0;
            var stock = balance > long.MaxValue / ProductionFormulas.Milli - 1 ? long.MaxValue : balance * ProductionFormulas.Milli + carryMilli;
            if (stock <= 0)
                return 1;
            var drain = -netMilliPerTick;
            return (ulong)(stock / drain + (stock % drain == 0 ? 0 : 1));
        }

        // --- Emergencies ------------------------------------------------------------------------------------------

        /// <summary>The clock hour a roll belongs to (one roll per player and hour).</summary>
        public static long HourIndex(DateTimeOffset now) => now.ToUnixTimeSeconds() / 3600;

        /// <summary>A stable 64-bit seed of a player and an hour (FNV-1a; string.GetHashCode differs per process).</summary>
        public static ulong Seed(string playerId, long hour)
        {
            var hash = 14695981039346656037UL;
            void Mix(byte b)
            {
                hash ^= b;
                hash *= 1099511628211UL;
            }

            foreach (var c in playerId ?? string.Empty)
            {
                Mix((byte)c);
                Mix((byte)(c >> 8));
            }

            for (var i = 0; i < 8; i++)
                Mix((byte)(hour >> (8 * i)));

            // Final avalanche so neighbouring hours do not give neighbouring numbers.
            hash ^= hash >> 33;
            hash *= 0xff51afd7ed558ccdUL;
            hash ^= hash >> 33;
            return hash;
        }

        /// <summary>Whether this player's roll for this hour starts an emergency.</summary>
        public static bool Rolls(string playerId, long hour, int chancePercent) =>
            chancePercent > 0 && (int)(Seed(playerId, hour) % 100UL) < Math.Min(100, chancePercent);

        /// <summary>Leak or breakdown, from the same seed (a base without a producer to break gets a leak).</summary>
        public static string Kind(string playerId, long hour, bool canBreak) =>
            canBreak && (Seed(playerId, hour) >> 20) % 2UL == 1UL ? EmergencyKinds.Breakdown : EmergencyKinds.Leak;

        /// <summary>Which of <paramref name="count"/> candidates (sorted by id) is hit.</summary>
        public static int Pick(string playerId, long hour, int count) =>
            count <= 0 ? -1 : (int)((Seed(playerId, hour) >> 32) % (ulong)count);

        /// <summary>
        /// Whether an emergency may start now: the loop and emergencies are on, the player is online, past the guide and
        /// at the minimum reactor level, none is running, and the cooldown since the last start has passed.
        /// </summary>
        public static bool MayStart(SurvivalCatalogData? rules, bool online, bool inGuide, int reactorLevel, bool active,
            DateTimeOffset? lastStartedAt, DateTimeOffset now)
        {
            if (!EmergenciesOn(rules) || !online || inGuide || active)
                return false;
            var e = rules!.emergencies;
            if (reactorLevel < e.minReactorLevel)
                return false;
            return lastStartedAt == null || (now - lastStartedAt.Value).TotalHours >= Math.Max(1, e.cooldownHours);
        }

        /// <summary>The repair bill: a share of an hour's Titanium income at this reactor level, at least the minimum.</summary>
        public static IReadOnlyList<KeyValuePair<ResourceType, long>> RepairCost(GameCatalog catalog, int reactorLevel)
        {
            var e = catalog.Survival.emergencies ?? new EmergencyCatalogData();
            var titanium = Math.Max(Math.Max(0, e.repairCostMin), catalog.IncomeHour(reactorLevel) * Math.Max(0, e.repairCostIncomePercent) / 100);
            return titanium <= 0
                ? Array.Empty<KeyValuePair<ResourceType, long>>()
                : new[] { new KeyValuePair<ResourceType, long>(ResourceType.Titanium, titanium) };
        }
    }
}
