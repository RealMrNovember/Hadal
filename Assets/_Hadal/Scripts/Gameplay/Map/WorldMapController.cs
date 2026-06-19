using Hadal.Data.Enums;
using UnityEngine;
using Hadal.Core.DI;
using Hadal.Managers;

namespace Hadal.Gameplay.Map
{
    public class WorldMapController : MonoBehaviour
    {
        [SerializeField] private Transform _mapRoot;
        [SerializeField] private float _mapRadius = 100f;

        private MapManager _mapManager;

        private void Start()
        {
            if (GameContext.Current != null)
                GameContext.Current.TryResolve(out _mapManager);
        }

        public Vector3 GetZonePosition(DepthZone zone)
        {
            var t = (int)zone / (float)DepthZone.TheCore;
            var angle = t * Mathf.PI * 2f;
            return _mapRoot.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _mapRadius;
        }

        public bool TravelTo(DepthZone zone) => _mapManager != null && _mapManager.TryTravelToZone(zone);
    }
}
