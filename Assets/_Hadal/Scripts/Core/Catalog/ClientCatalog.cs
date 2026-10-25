using HADAL.Shared.Catalog;
using UnityEngine;

namespace Hadal.Core.Catalog
{
    /// <summary>
    /// The shared game catalog inside the player build (Resources/catalog.json, copied from
    /// shared/catalog/catalog.json by Hadal > Setup). The client shows costs, times and rules with
    /// the same file and formulas the server enforces.
    /// </summary>
    public static class ClientCatalog
    {
        public const string ResourcePath = "catalog";

        private static GameCatalog _catalog;

        public static GameCatalog Catalog
        {
            get
            {
                if (_catalog != null)
                    return _catalog;

                var asset = Resources.Load<TextAsset>(ResourcePath);
                if (asset == null)
                    throw new System.InvalidOperationException("Resources/catalog.json missing — run Hadal > Setup > Create Full Project Data.");

                _catalog = GameCatalog.Create(JsonUtility.FromJson<GameCatalogData>(asset.text));
                return _catalog;
            }
        }
    }
}
