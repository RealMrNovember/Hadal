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
        private readonly Dictionary<ResourceType, long> _productionMilliPerTick = new();
        private readonly Dictionary<ResourceType, long> _capacities = new();
        private readonly Dictionary<string, int> _researchLevels = new(System.StringComparer.Ordinal);
        private readonly List<ActiveResearchView> _activeResearch = new();
        private readonly Queue<SalvageReportDto> _salvageReports = new();

        public SaveGameData Data => _data;

        public bool HasPersistedData { get; private set; }

        /// <summary>
        /// Server-computed net production per simulation tick. Display only — balances come
        /// from <see cref="SaveGameData.resources"/>, which the server owns.
        /// </summary>
        public IReadOnlyDictionary<ResourceType, long> ProductionPerTick => _productionPerTick;

        public int ProductionTickIntervalSeconds { get; private set; } = SimulationTick.IntervalSeconds;

        /// <summary>Exact net rates in thousandths per tick; empty from older servers (use <see cref="ProductionPerTick"/>).</summary>
        public IReadOnlyDictionary<ResourceType, long> ProductionMilliPerTick => _productionMilliPerTick;

        /// <summary>This player's storage caps from the server (research raises them); empty = the catalog's.</summary>
        public IReadOnlyDictionary<ResourceType, long> Capacities => _capacities;

        /// <summary>Finished research levels per node (docs/24).</summary>
        public IReadOnlyDictionary<string, int> ResearchLevels => _researchLevels;

        public IReadOnlyList<ActiveResearchView> ActiveResearch => _activeResearch;

        /// <summary>Salvage runs that came home since the UI last looked.</summary>
        public bool TryTakeSalvageReport(out SalvageReportDto report) => _salvageReports.TryDequeue(out report);

        public void AddSalvageReport(SalvageReportDto report)
        {
            if (report != null)
                _salvageReports.Enqueue(report);
        }

        public void ApplyResearch(ResearchStateDto research)
        {
            _researchLevels.Clear();
            foreach (var entry in research.Levels ?? System.Array.Empty<ResearchLevelEntry>())
                _researchLevels[entry.NodeId] = entry.Level;

            _activeResearch.Clear();
            foreach (var entry in research.Active ?? System.Array.Empty<ActiveResearchEntry>())
            {
                _activeResearch.Add(new ActiveResearchView(entry.NodeId, entry.TargetLevel,
                    UnityEngine.Time.realtimeSinceStartup + entry.RemainingMs / 1000f, entry.TotalMs / 1000f));
            }
        }

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
            _productionMilliPerTick.Clear();
            _capacities.Clear();
            _researchLevels.Clear();
            _activeResearch.Clear();
            _salvageReports.Clear();
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

            _productionMilliPerTick.Clear();
            foreach (var pair in production.NetMilliPerTick ?? new Dictionary<ResourceType, long>())
                _productionMilliPerTick[pair.Key] = pair.Value;

            if (production.Capacities != null && production.Capacities.Count > 0)
            {
                _capacities.Clear();
                foreach (var pair in production.Capacities)
                    _capacities[pair.Key] = pair.Value;
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
