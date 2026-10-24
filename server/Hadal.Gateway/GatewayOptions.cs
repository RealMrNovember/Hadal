namespace Hadal.Gateway
{
    /// <summary>Bound from the "Gateway" configuration section.</summary>
    public sealed class GatewayOptions
    {
        public const string Section = "Gateway";

        /// <summary>
        /// Test-only: delay every server → client frame by this many milliseconds (order kept).
        /// Used by `unity.sh latency` to exercise prediction/rollback under 100/200 ms. Keep 0 in production.
        /// </summary>
        public int SimulatedLatencyMs { get; set; }
    }
}
