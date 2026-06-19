using System.Collections.Generic;
using UnityEngine;

namespace Hadal.Data.Config
{
    [CreateAssetMenu(fileName = "AssetCatalog", menuName = "Hadal/Addressables/Asset Catalog")]
    public class AssetCatalogSO : ScriptableObject
    {
        [SerializeField] private List<AssetCatalogEntry> _entries = new();

        public IReadOnlyList<AssetCatalogEntry> Entries => _entries;

        public bool TryGetAddress(string key, out string address)
        {
            foreach (var entry in _entries)
            {
                if (entry.Key == key)
                {
                    address = entry.Address;
                    return !string.IsNullOrWhiteSpace(address);
                }
            }

            address = null;
            return false;
        }
    }

    [System.Serializable]
    public struct AssetCatalogEntry
    {
        public string Key;
        public string Address;
        public AssetLoadPolicy LoadPolicy;
    }

    public enum AssetLoadPolicy
    {
        OnDemand,
        PreloadAtBoot,
        PreloadOnBaseEnter
    }
}
