namespace Hadal.Core.Network
{
    /// <summary>
    /// Compares server acknowledgements and corrective state against client predictions.
    /// Sprint 5: ingest-only stub — rollback logic arrives in a later sprint.
    /// </summary>
    public interface ICommandReconciliationSystem
    {
        void EnqueueInbound(NetworkInboundPacket packet);
    }
}
