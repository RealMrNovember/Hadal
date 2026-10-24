using HADAL.Shared.Commands;

namespace Hadal.Core.Network
{
    /// <summary>
    /// Simulates gateway placement ack until live server is deployed.
    /// </summary>
    public interface IPlacementAckSimulator
    {
        void SchedulePlacementAck(PlaceBuildingCommand command);
    }

    /// <summary>Used when talking to the real gateway: the server acks placements.</summary>
    public sealed class NullPlacementAckSimulator : IPlacementAckSimulator
    {
        public void SchedulePlacementAck(PlaceBuildingCommand command) { }
    }
}
