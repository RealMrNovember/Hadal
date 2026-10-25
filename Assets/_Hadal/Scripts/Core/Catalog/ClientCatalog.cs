using HADAL.Shared.Catalog;
using UnityEngine;

namespace Hadal.Core.Catalog
{
    /// <summary>
    /// The shared game catalog inside the player build (Resources/catalog.json, copied from
    /// shared/catalog/catalog.json by Hadal > Setup). The client shows costs, times and rules with
    /// the same file and formulas the server enforces. When the Overseer publishes a new balance the
    /// manifest's catalogVersion changes and <see cref="Apply"/> swaps in the live copy.
    /// </summary>
    public static class ClientCatalog
    {
        public const string ResourcePath = "catalog";

        private static GameCatalog _catalog;

        /// <summary>0 = the bundled file; otherwise the server's published version.</summary>
        public static int Version { get; private set; }

        /// <summary>Raised after <see cref="Apply"/> so open screens re-read costs and times.</summary>
        public static event System.Action Changed;

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

        /// <summary>Use the server's live catalog. A file that does not parse keeps the current one.</summary>
        public static bool Apply(string json, int version)
        {
            GameCatalog catalog;
            try
            {
                catalog = GameCatalog.Create(JsonUtility.FromJson<GameCatalogData>(json));
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"[ClientCatalog] Live catalog v{version} rejected: {exception.Message}");
                return false;
            }

            _catalog = catalog;
            Version = version;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Back to the bundled file (tests, account switch).</summary>
        public static void Reset()
        {
            _catalog = null;
            Version = 0;
        }
    }
}
