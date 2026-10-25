using System;
using System.Collections.Generic;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;
using Hadal.Core.Events;
using Hadal.Core.StateSync;
using Hadal.Data.Config;
using Hadal.Data.Models;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Hadal.UI
{
    /// <summary>
    /// Resource readout driven purely by replicated state.
    /// <para>
    /// It reads <see cref="ClientStateView"/> — the same cache the server writes into — rather
    /// than <c>ResourceManager</c>'s optimistic wallet, so the HUD always shows what the server
    /// last confirmed. It performs no economy arithmetic of its own; rates are server-sent.
    /// </para>
    /// </summary>
    public sealed class ResourceHudView : UIViewBase
    {
        [Serializable]
        private sealed class ResourceRow
        {
            public ResourceType type;
            public Text amountLabel;
            public Text rateLabel;
        }

        [SerializeField] private List<ResourceRow> _rows = new();
        [SerializeField] private Color _positiveRateColor = new(0.55f, 0.85f, 0.70f, 1f);
        [SerializeField] private Color _negativeRateColor = new(0.90f, 0.45f, 0.42f, 1f);
        [SerializeField] private bool _showRates = true;

        private IStateSyncService _stateSync;
        private ILocalEventBus _localBus;
        private ResourceDatabaseSO _resourceDatabase;
        private bool _subscribed;

        private readonly Dictionary<ResourceType, long> _amounts = new();

        [Inject]
        public void Construct(IStateSyncService stateSync, ILocalEventBus localBus, GameConfigSO config)
        {
            _stateSync = stateSync;
            _localBus = localBus;
            _resourceDatabase = config != null ? config.ResourceDatabase : null;

            // Scene objects are enabled before VContainer injects them, so OnEnable alone would
            // never subscribe and the HUD would stay at 0.
            if (isActiveAndEnabled)
                Subscribe();
        }

        private void OnEnable() => Subscribe();

        private void OnDisable()
        {
            if (_subscribed)
                _localBus?.Unsubscribe<ResourceStateChangedEvent>(OnResourceStateChanged);

            _subscribed = false;
        }

        private void Subscribe()
        {
            if (_localBus == null || _subscribed)
                return;

            _localBus.Subscribe<ResourceStateChangedEvent>(OnResourceStateChanged);
            _subscribed = true;
            LocalizeNames();
            Refresh();
        }

        private void OnResourceStateChanged(ResourceStateChangedEvent _) => Refresh();

        // The scene's name labels are authored in English; show them in the player's language.
        private void LocalizeNames()
        {
            foreach (var row in _rows)
            {
                var name = row?.amountLabel != null ? row.amountLabel.transform.parent.Find("Name") : null;
                if (name != null && name.TryGetComponent<Text>(out var label))
                {
                    label.font = UiKit.Font;
                    label.text = Loc.Resource(row.type.ToString());
                }

                if (row?.rateLabel != null)
                    row.rateLabel.font = UiKit.Font;
            }
        }

        public void Refresh()
        {
            if (_stateSync == null)
                return;

            var view = _stateSync.View;
            ReadAmounts(view.Data.resources);

            var intervalSeconds = view.ProductionTickIntervalSeconds > 0
                ? view.ProductionTickIntervalSeconds
                : SimulationTick.IntervalSeconds;

            foreach (var row in _rows)
            {
                if (row == null)
                    continue;

                _amounts.TryGetValue(row.type, out var amount);

                if (row.amountLabel != null)
                    row.amountLabel.text = FormatAmount(row.type, amount);

                if (row.rateLabel != null)
                    ApplyRate(row, intervalSeconds, view);
            }
        }

        private void ApplyRate(ResourceRow row, int intervalSeconds, ClientStateView view)
        {
            if (!_showRates)
            {
                row.rateLabel.text = string.Empty;
                return;
            }

            // Server-sent per-tick value scaled to per-minute purely for readability. The exact
            // thousandths win when the server sends them (small producers gain fractions per level).
            var ticksPerMinute = intervalSeconds > 0 ? 60L / intervalSeconds : SimulationTick.TicksPerMinute;
            long perMinute;
            if (view.ProductionMilliPerTick.TryGetValue(row.type, out var milli))
                perMinute = milli * ticksPerMinute / ProductionFormulas.Milli;
            else
                perMinute = (view.ProductionPerTick.TryGetValue(row.type, out var netPerTick) ? netPerTick : 0L) * ticksPerMinute;

            row.rateLabel.text = perMinute == 0L ? "—" : $"{(perMinute > 0L ? "+" : string.Empty)}{perMinute}{Loc.T("base.permin")}";
            row.rateLabel.color = perMinute < 0L ? _negativeRateColor : _positiveRateColor;
        }

        private string FormatAmount(ResourceType type, long amount)
        {
            var capacity = _stateSync != null && _stateSync.View.Capacities.TryGetValue(type, out var replicated)
                ? replicated
                : _resourceDatabase != null ? _resourceDatabase.Get(type)?.MaxCapacity ?? 0L : 0L;

            return capacity > 0L && capacity != long.MaxValue
                ? $"{Compact(amount)}/{Compact(capacity)}"
                : Compact(amount);
        }

        /// <summary>12345 -> 12.3K so five-digit stores fit the narrow HUD column.</summary>
        private static string Compact(long value) =>
            value >= 1_000_000 ? $"{value / 1_000_000d:0.#}M"
            : value >= 10_000 ? $"{value / 1_000d:0.#}K"
            : value.ToString();

        private void ReadAmounts(ResourceSaveEntry[] entries)
        {
            _amounts.Clear();
            if (entries == null)
                return;

            foreach (var entry in entries)
            {
                var resource = ResourceTypeMap.FromStoredValue(entry.resourceType);
                if (resource != ResourceType.Unknown)
                    _amounts[resource] = entry.amount;
            }
        }
    }
}
