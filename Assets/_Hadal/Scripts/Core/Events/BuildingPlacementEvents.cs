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

    /// <summary>The server answered a command this client sent (any type); UI shows rejections.</summary>
    public readonly struct CommandResolvedEvent
    {
        public string CommandId { get; init; }
        public CommandType Type { get; init; }
        public CommandResultCode Result { get; init; }

        /// <summary>Server reason code (PlacementRejectReason / ConstructionRejectReason name) on rejection.</summary>
        public string Reason { get; init; }
    }

    public readonly struct GhostRejectionShakeEvent
    {
        public string CommandId { get; init; }
        public float DurationSeconds { get; init; }
    }
}
