using HADAL.Shared.DTOs;

namespace Hadal.Core.Network
{
    /// <summary>
    /// Simulates the server's economy tick until the live game server is deployed.
    /// Mirrors <see cref="IPlacementAckSimulator"/>: it exists to produce server frames,
    /// not to hold gameplay authority on the client.
    /// <para>
    /// While it runs it is the single owner of the stub wallet. Other stubs must charge
    /// through <see cref="TryChargeBuildCost"/> rather than mutating balances themselves,
    /// otherwise the next production frame overwrites their deduction and refunds the spend.
    /// </para>
    /// </summary>
    public interface IProductionTickSimulator
    {
        /// <summary>False once a real gateway owns the economy, or when config is missing.</summary>
        bool IsActive { get; }

        /// <summary>Server tick the stub has simulated up to.</summary>
        ulong SimulatedTick { get; }

        /// <summary>
        /// Deducts a building's costs from the stub wallet.
        /// </summary>
        /// <param name="walletChange">
        /// The resulting absolute wallet frame, to be appended to the caller's
        /// <see cref="StateDelta"/> so the charge lands in the same frame as the placement.
        /// </param>
        /// <returns>False when the wallet cannot cover the cost — the caller should reject.</returns>
        bool TryChargeBuildCost(string buildingDefinitionId, out EntityChangeDto walletChange);
    }
}
