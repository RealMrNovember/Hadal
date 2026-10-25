namespace Hadal.Core.Events
{
    /// <summary>The player tapped a base slot (empty or built) — opens the build menu or the building panel.</summary>
    public readonly struct BaseSlotTappedEvent
    {
        public int Ring { get; init; }
        public int Sector { get; init; }

        /// <summary>Instance key of the building on the slot; empty when the slot is free.</summary>
        public string BuildingInstanceKey { get; init; }
    }
}
