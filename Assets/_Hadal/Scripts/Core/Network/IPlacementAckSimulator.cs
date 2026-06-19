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
}
