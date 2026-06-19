using HADAL.Shared.Enums;

namespace Hadal.Core.Events
{
    public readonly struct BuildingPlacementPendingEvent
    {
        public string CommandId { get; init; }
        public string BuildingDefinitionId { get; init; }
    }

    public readonly struct BuildingPlacementResolvedEvent
    {
        public string CommandId { get; init; }
        public CommandResultCode Result { get; init; }
    }

    public readonly struct BuildingStateChangedEvent
    {
    }

    public readonly struct GhostRejectionShakeEvent
    {
        public string CommandId { get; init; }
        public float DurationSeconds { get; init; }
    }
}
