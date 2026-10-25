namespace Hadal.Core.Events
{
    public readonly struct NetworkReconnectingEvent
    {
        public string Message { get; init; }
    }

    public readonly struct NetworkConnectedEvent
    {
    }

    /// <summary>This build is older than the server's minimum; the client stops connecting.</summary>
    public readonly struct ClientUpdateRequiredEvent
    {
        public string RequiredVersion { get; init; }
        public string LatestVersion { get; init; }
    }

    /// <summary>The server is in maintenance; the client keeps retrying slowly.</summary>
    public readonly struct ServerMaintenanceEvent
    {
        public string Message { get; init; }
    }

    public readonly struct RollbackVisualRequestedEvent
    {
        public string EntityId { get; init; }
        public float DurationSeconds { get; init; }
    }
}
