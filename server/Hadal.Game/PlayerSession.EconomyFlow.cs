using Hadal.Game.Economy;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;

namespace Hadal.Game
{
    /// <summary>
    /// Economy flow telemetry (docs/30 budget): wallet movements grouped by source. Production and upkeep are
    /// accumulated from the exact wallet delta and flushed hourly; explicit actions (rewards, spends) are written at once.
    /// </summary>
    public sealed partial class PlayerSession
    {
        /// <summary>Where economy events go; set by the world. Called under the session lock: must not block.</summary>
        internal Action<EconomyFlowEvent>? EconomyLog { get; set; }

        private readonly Dictionary<ResourceType, long> _economyAccum = new();
        private DateTimeOffset? _lastEconomyFlush;

        private Dictionary<ResourceType, long> WalletSnapshot() => new(_state.Wallet);

        private void RecordEconomyEvent(string source, ResourceType resource, long amount, DateTimeOffset at)
        {
            if (amount != 0)
                EconomyLog?.Invoke(new EconomyFlowEvent(PublicId, source, resource.ToString(), amount, at));
        }

        private void TrackProductionDelta(Dictionary<ResourceType, long> before, DateTimeOffset at)
        {
            foreach (var resource in before.Keys.Union(_state.Wallet.Keys))
            {
                var delta = SaturatingDelta(_state.Wallet.TryGetValue(resource, out var after) ? after : 0,
                    before.TryGetValue(resource, out var old) ? old : 0);
                if (delta == 0)
                    continue;

                _economyAccum[resource] = SaturatingAdd(_economyAccum.TryGetValue(resource, out var accumulated) ? accumulated : 0, delta);
            }

            if (_lastEconomyFlush is null || at - _lastEconomyFlush.Value >= TimeSpan.FromHours(1))
                FlushEconomyFlow(at);
        }

        private void FlushEconomyFlow(DateTimeOffset at)
        {
            foreach (var (resource, amount) in _economyAccum)
            {
                if (amount != 0)
                    EconomyLog?.Invoke(new EconomyFlowEvent(PublicId, amount >= 0 ? "production" : "upkeep", resource.ToString(), amount, at));
            }

            _economyAccum.Clear();
            _lastEconomyFlush = at;
        }

        /// <summary>Logs an explicit wallet change between <paramref name="before"/> and now. Caller holds the lock.</summary>
        private void LogWalletDelta(string source, IReadOnlyDictionary<ResourceType, long> before, DateTimeOffset at)
        {
            foreach (var resource in before.Keys.Union(_state.Wallet.Keys))
            {
                var delta = SaturatingDelta(_state.Wallet.TryGetValue(resource, out var after) ? after : 0,
                    before.TryGetValue(resource, out var old) ? old : 0);
                RecordEconomyEvent(source, resource, delta, at);
            }
        }

        /// <summary>Charges a build/boost/etc. cost and reports the exact wallet movement. Caller holds the lock.</summary>
        private void Spend(string source, IReadOnlyList<KeyValuePair<ResourceType, long>> cost, DateTimeOffset at)
        {
            var before = WalletSnapshot();
            BuildingProgression.Charge(cost, _state.Wallet);
            LogWalletDelta(source, before, at);
        }

        private static long SaturatingDelta(long after, long before)
        {
            var delta = (Int128)after - before;
            return delta > long.MaxValue ? long.MaxValue : delta < long.MinValue ? long.MinValue : (long)delta;
        }

        private static long SaturatingAdd(long left, long right)
        {
            var sum = (Int128)left + right;
            return sum > long.MaxValue ? long.MaxValue : sum < long.MinValue ? long.MinValue : (long)sum;
        }
    }
}
