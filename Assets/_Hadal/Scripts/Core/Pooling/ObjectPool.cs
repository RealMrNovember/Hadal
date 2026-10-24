using UnityEngine;

namespace Hadal.Core.Pooling
{
    /// <summary>
    /// Prefab-backed component pool. Thin wrapper over <see cref="UnityEngine.Pool.ObjectPool{T}"/>
    /// that owns instantiation, parenting and active-state toggling.
    /// </summary>
    public sealed class ObjectPool<T> where T : Component
    {
        private readonly UnityEngine.Pool.ObjectPool<T> _pool;

        public ObjectPool(T prefab, int defaultCapacity, int maxSize, Transform parent = null)
        {
            _pool = new UnityEngine.Pool.ObjectPool<T>(
                createFunc: () => Object.Instantiate(prefab, parent),
                actionOnGet: item => item.gameObject.SetActive(true),
                actionOnRelease: item => item.gameObject.SetActive(false),
                actionOnDestroy: item =>
                {
                    if (item != null)
                        Object.Destroy(item.gameObject);
                },
                collectionCheck: false,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize);
        }

        public int CountInactive => _pool.CountInactive;

        public T Get() => _pool.Get();

        public void Release(T item) => _pool.Release(item);

        public void Clear() => _pool.Clear();
    }
}
