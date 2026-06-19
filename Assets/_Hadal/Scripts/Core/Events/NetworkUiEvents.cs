namespace Hadal.Core.Events
{
    public readonly struct NetworkReconnectingEvent
    {
        public string Message { get; init; }
    }

    public readonly struct NetworkConnectedEvent
    {
    }

    public readonly struct RollbackVisualRequestedEvent
    {
        public string EntityId { get; init; }
        public float DurationSeconds { get; init; }
    }
}
