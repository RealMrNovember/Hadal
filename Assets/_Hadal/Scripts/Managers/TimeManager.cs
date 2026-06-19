using UnityEngine;
using Hadal.Data.Config;
using Hadal.Managers.Base;

namespace Hadal.Managers
{
    public class TimeManager : ManagerBase
    {
        private float _gameTimeScale = 1f;
        private double _sessionStartUtc;

        public float GameTimeScale
        {
            get => _gameTimeScale;
            set => _gameTimeScale = Mathf.Max(0f, value);
        }

        public double SessionElapsedSeconds => Time.realtimeSinceStartupAsDouble;

        protected override void OnInitialize(GameConfigSO config)
        {
            _sessionStartUtc = System.DateTime.UtcNow.Ticks;
        }

        public float ScaledDeltaTime => Time.deltaTime * _gameTimeScale;
    }
}
