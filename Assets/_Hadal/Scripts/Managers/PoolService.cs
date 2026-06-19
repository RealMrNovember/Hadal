using System.Collections.Generic;
using UnityEngine;
using Hadal.Core.Contracts;
using Hadal.Core.Pooling;
using Hadal.Core.Services;
using Hadal.Data.Config;

namespace Hadal.Managers
{
    public sealed class PoolService : IPoolService, IGameService
    {
        private readonly Dictionary<string, object> _pools = new();
        private readonly int _defaultCapacity;
        private readonly int _maxSize;
        private readonly Transform _poolRoot;

        public PoolService(MobilePerformanceConfigSO performanceConfig, Transform poolRoot)
        {
            _defaultCapacity = performanceConfig != null ? performanceConfig.DefaultPoolCapacity : 16;
            _maxSize = performanceConfig != null ? performanceConfig.MaxPoolSize : 128;
            _poolRoot = poolRoot;
        }

        public void Initialize() { }

        public void Shutdown() => ClearAll();

        public ObjectPool<T> GetOrCreatePool<T>(string key, T prefab, Transform parent = null)
            where T : Component
        {
            if (_pools.TryGetValue(key, out var existing))
                return (ObjectPool<T>)existing;

            var pool = new ObjectPool<T>(
                prefab,
                defaultCapacity: _defaultCapacity,
                maxSize: _maxSize,
                parent: parent != null ? parent : _poolRoot);

            _pools[key] = pool;
            return pool;
        }

        public void ClearAll() => _pools.Clear();
    }
}
