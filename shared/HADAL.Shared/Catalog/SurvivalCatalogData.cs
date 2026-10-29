using System;

namespace HADAL.Shared.Catalog
{
    /// <summary>
    /// The survival loop (docs/35_Survival_Loop.md): energy feeds the producers, the crew breathes oxygen, and now and
    /// then something breaks. Every penalty is recoverable: the base never dies, it only works slower.
    /// A catalog without this section (design 12 and older) leaves the whole loop off.
    /// </summary>
    [Serializable]
    public sealed class SurvivalCatalogData
    {
        /// <summary>Master switch for the energy chain, the oxygen penalties and emergencies; off when missing.</summary>
        public bool enabled;

        /// <summary>With the energy store empty and demand above supply, energy users run at supply/demand, never below this.</summary>
        public int energyFloorPercent = 50;

        /// <summary>A falling store under this share of its cap shows the warning (no penalty yet).</summary>
        public int warnPercent = 20;

        /// <summary>Oxygen at 0 and still falling: production of everything but oxygen and energy drops by this much.</summary>
        public int outputPenaltyPercent = 20;

        /// <summary>Oxygen at 0 and still falling: new constructions, researches and training batches take this much longer.</summary>
        public int timePenaltyPercent = 20;

        /// <summary>The base crew breathes this much oxygen per hour per Core Reactor level (troops breathe on top).</summary>
        public long crewOxygenPerReactorLevel = 10;

        public EmergencyCatalogData emergencies = new EmergencyCatalogData();
    }

    /// <summary>Random emergencies: an oxygen leak or a broken producer, repaired with a small cost and a short timer.</summary>
    [Serializable]
    public sealed class EmergencyCatalogData
    {
        /// <summary>Second switch: emergencies only (the energy chain and oxygen stay on with <see cref="SurvivalCatalogData.enabled"/>).</summary>
        public bool enabled;

        /// <summary>No emergency below this Core Reactor level, nor while the first-session guide runs.</summary>
        public int minReactorLevel = 5;

        /// <summary>At least this long between the starts of two emergencies.</summary>
        public int cooldownHours = 6;

        /// <summary>Chance per online clock hour (one seeded roll per player and hour, only while connected).</summary>
        public int chancePercent = 10;

        /// <summary>An ignored emergency fixes itself after this long, so it never blocks for ever.</summary>
        public int autoResolveHours = 4;

        /// <summary>The repair crew's work time; alliance mates can help it.</summary>
        public int repairSeconds = 600;

        /// <summary>Repair cost in Titanium: this share of an hour's Titanium income at the reactor level ("Gelir Saati").</summary>
        public int repairCostIncomePercent = 10;

        /// <summary>The smallest repair cost in Titanium.</summary>
        public long repairCostMin = 20;

        /// <summary>A leak loses this share of the base's oxygen production …</summary>
        public int leakPercent = 20;

        /// <summary>… and at least this much oxygen per hour.</summary>
        public long leakMinPerHour = 60;
    }
}
