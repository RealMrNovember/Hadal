using System;

namespace HADAL.Shared.Catalog
{
    /// <summary>The outer world (docs/28_World_Map.md).</summary>
    [Serializable]
    public sealed class WorldCatalogData
    {
        /// <summary>The map is size x size tiles.</summary>
        public int size = 200;

        /// <summary>Tiles between neighbouring new bases.</summary>
        public int spacing = 3;

        /// <summary>Tiles around the centre one view returns.</summary>
        public int viewRadius = 14;
    }
}
