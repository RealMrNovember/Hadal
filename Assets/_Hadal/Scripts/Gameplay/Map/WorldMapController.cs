using UnityEngine;
using Hadal.Managers;
using VContainer;

namespace Hadal.Gameplay.Map
{
    public class WorldMapController : MonoBehaviour
    {
        private MapManager _mapManager;

        [Inject]
        public void Construct(MapManager mapManager)
        {
            _mapManager = mapManager;
        }

        public void TravelToZone(Data.Enums.DepthZone zone)
        {
            _mapManager?.TryTravelToZone(zone);
        }
    }
}
