namespace Hadal.Core.Events
{
    /// <summary>
    /// Raised on the local bus after replicated balances or production rates land in
    /// <c>ClientStateView</c>. UI subscribes to this, never to the network bus.
    /// </summary>
    public readonly struct ResourceStateChangedEvent
    {
        /// <summary>Server tick the balances were computed at; 0 for unversioned frames.</summary>
        public ulong ServerTick { get; init; }
    }
}
