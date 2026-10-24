using System.Collections.Generic;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;
using Hadal.Data.Models;

namespace Hadal.Core.StateSync
{
    /// <summary>
    /// Non-authoritative in-memory visual state cache (RAM only — never written to disk).
    /// </summary>
    public sealed class ClientStateView
    {
        private SaveGameData _data = new();
        private readonly Dictionary<ResourceType, long> _productionPerTick = new();

        public SaveGameData Data => _data;

        public bool HasPersistedData { get; private set; }

        /// <summary>
        /// Server-computed net production per simulation tick. Display only — balances come
        /// from <see cref="SaveGameData.resources"/>, which the server owns.
        /// </summary>
        public IReadOnlyDictionary<ResourceType, long> ProductionPerTick => _productionPerTick;

        public int ProductionTickIntervalSeconds { get; private set; } = SimulationTick.IntervalSeconds;

        /// <summary>Server tick of the newest balance set applied, for stale-frame rejection.</summary>
        public ulong LastResourceTick { get; private set; }

        public void Replace(SaveGameData data)
        {
            _data = data ?? new SaveGameData();
            HasPersistedData = true;
        }

        public void ResetSession()
        {
            _data = new SaveGameData
            {
                version = SaveGameData.CurrentVersion
            };
            _productionPerTick.Clear();
            ProductionTickIntervalSeconds = SimulationTick.IntervalSeconds;
            LastResourceTick = 0UL;
            HasPersistedData = false;
        }

        public void MarkCaptured() => HasPersistedData = true;

        public void ApplyProduction(ProductionStateDto production)
        {
            if (production == null)
                return;

            _productionPerTick.Clear();

            if (production.NetPerTick != null)
            {
                foreach (var pair in production.NetPerTick)
                    _productionPerTick[pair.Key] = pair.Value;
            }

            if (production.TickIntervalSeconds > 0)
                ProductionTickIntervalSeconds = production.TickIntervalSeconds;
        }

        /// <summary>
        /// Gates an absolute balance write. Because balances replicate as totals rather than
        /// increments, a frame that overtakes a newer one would roll the wallet backwards;
        /// an unversioned frame (tick 0) is always accepted.
        /// </summary>
        public bool TryAcceptResourceTick(ulong serverTick)
        {
            if (serverTick == 0UL)
                return true;

            if (serverTick < LastResourceTick)
                return false;

            LastResourceTick = serverTick;
            return true;
        }

        /// <summary>
        /// Adopts a snapshot's tick unconditionally. A snapshot is the full authoritative
        /// state, so it replaces the wallet even if it reports an older tick than the last
        /// delta (which happens when the server rebuilds state after a restart).
        /// </summary>
        public void ForceResourceTick(ulong serverTick) => LastResourceTick = serverTick;
    }
}
