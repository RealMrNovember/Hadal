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

        /// <summary>Where the new build is downloaded from (OTA).</summary>
        public string Url { get; init; }
    }

    /// <summary>A newer build exists but this one may still play; the player can update or skip.</summary>
    public readonly struct ClientUpdateAvailableEvent
    {
        public string LatestVersion { get; init; }
        public string Url { get; init; }
    }

    /// <summary>The server is in maintenance; the client keeps retrying slowly.</summary>
    public readonly struct ServerMaintenanceEvent
    {
        public string Message { get; init; }
    }

    /// <summary>The account is suspended (Overseer ban); the client stops connecting.</summary>
    public readonly struct AccountBannedEvent
    {
    }

    /// <summary>Overseer announcement from the version manifest; empty text clears it.</summary>
    public readonly struct ServerAnnouncementEvent
    {
        public string Text { get; init; }
    }

    public readonly struct RollbackVisualRequestedEvent
    {
        public string EntityId { get; init; }
        public float DurationSeconds { get; init; }
    }
}
