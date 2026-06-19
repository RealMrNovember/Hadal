using System.Collections.Generic;
using UnityEngine;
using Hadal.Core.Tick;
using Hadal.Data.Config;
using Hadal.Managers.Base;

namespace Hadal.Managers
{
    /// <summary>
    /// Single Update loop for all tickable systems — reduces mobile CPU overhead.
    /// </summary>
    public class TickManager : ManagerBase
    {
        private readonly List<IGameTickable> _tickables = new();
        private MobilePerformanceConfigSO _performanceConfig;
        private float _tickScale = 1f;

        protected override void OnInitialize(GameConfigSO config)
        {
            _performanceConfig = config.MobilePerformanceConfig;
        }

        public void Register(IGameTickable tickable)
        {
            if (tickable == null || _tickables.Contains(tickable))
                return;

            _tickables.Add(tickable);
            _tickables.Sort((a, b) => a.TickPriority.CompareTo(b.TickPriority));
        }

        public void Unregister(IGameTickable tickable) => _tickables.Remove(tickable);

        private void Update()
        {
            if (!IsInitialized)
                return;

            ApplyPowerProfile();
            var delta = Time.deltaTime * _tickScale;

            for (var i = 0; i < _tickables.Count; i++)
                _tickables[i].Tick(delta);
        }

        private void ApplyPowerProfile()
        {
            if (_performanceConfig == null || !_performanceConfig.ReduceUpdatesOnLowPower)
            {
                _tickScale = 1f;
                return;
            }

            _tickScale = SystemInfo.batteryStatus == BatteryStatus.Discharging
                         && SystemInfo.batteryLevel > 0f
                         && SystemInfo.batteryLevel < 0.2f
                ? _performanceConfig.LowPowerTickMultiplier
                : 1f;
        }

        protected override void OnShutdown() => _tickables.Clear();
    }
}
