using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Hadal.Core.Contracts;
using Hadal.Core.Services;
using Hadal.Data.Config;

namespace Hadal.Managers.Addressables
{
    public sealed class AddressableAssetProvider : IAssetProvider, IGameService
    {
        private readonly AssetCatalogSO _catalog;
        private readonly int _maxCachedHandles;
        private readonly List<AsyncOperationHandle> _activeHandles = new();

        public AddressableAssetProvider(AssetCatalogSO catalog, int maxCachedHandles)
        {
            _catalog = catalog;
            _maxCachedHandles = maxCachedHandles;
        }

        public void Initialize() { }

        public void Shutdown()
        {
            for (var i = _activeHandles.Count - 1; i >= 0; i--)
            {
                if (_activeHandles[i].IsValid())
                    Addressables.Release(_activeHandles[i]);
            }

            _activeHandles.Clear();
        }

        public void PreloadCatalog()
        {
            if (_catalog == null)
                return;

            foreach (var entry in _catalog.Entries)
            {
                if (entry.LoadPolicy != AssetLoadPolicy.PreloadAtBoot)
                    continue;

                var handle = Addressables.LoadAssetAsync<Object>(entry.Address);
                TrackHandle(handle);
            }
        }

        public AsyncOperationHandle<T> LoadAsync<T>(string address) where T : Object
        {
            if (_catalog != null && _catalog.TryGetAddress(address, out var catalogAddress))
                address = catalogAddress;

            var handle = Addressables.LoadAssetAsync<T>(address);
            TrackHandle(handle);
            return handle;
        }

        public void Release<T>(AsyncOperationHandle<T> handle) where T : Object
        {
            if (handle.IsValid())
                Addressables.Release(handle);

            _activeHandles.Remove(handle);
        }

        private void TrackHandle(AsyncOperationHandle handle)
        {
            _activeHandles.Add(handle);
            TrimCacheIfNeeded();
        }

        private void TrimCacheIfNeeded()
        {
            while (_activeHandles.Count > _maxCachedHandles)
            {
                var oldest = _activeHandles[0];
                if (oldest.IsValid())
                    Addressables.Release(oldest);

                _activeHandles.RemoveAt(0);
            }
        }
    }
}
