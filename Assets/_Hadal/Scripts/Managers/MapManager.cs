using UnityEngine;
using Hadal.Core.Contracts;
using Hadal.Data.Config;
using Hadal.Data.Enums;
using Hadal.Data.Models;
using Hadal.Managers.Base;

namespace Hadal.Managers
{
    public class MapManager : ManagerBase, ISaveParticipant
    {
        private DepthZoneDatabaseSO _database;
        private DepthZone _currentZone = DepthZone.SafeZone;

        public DepthZone CurrentZone => _currentZone;

        protected override void OnInitialize(GameConfigSO config)
        {
            _database = config.DepthZoneDatabase;
        }

        public DepthZoneDefinitionSO GetZoneDefinition(DepthZone zone) => _database?.Get(zone);

        public bool TryTravelToZone(DepthZone zone)
        {
            if (_database?.Get(zone) == null)
                return false;

            _currentZone = zone;
            return true;
        }

        public float GetResourceMultiplier(DepthZone zone)
            => _database?.Get(zone)?.ResourceMultiplier ?? 1f;

        public float GetDangerMultiplier(DepthZone zone)
            => _database?.Get(zone)?.DangerMultiplier ?? 1f;

        public void CaptureSave(SaveGameData data)
        {
            data.currentDepthZone = _currentZone.ToString();
        }

        public void RestoreSave(SaveGameData data)
        {
            if (string.IsNullOrWhiteSpace(data.currentDepthZone))
                return;

            if (System.Enum.TryParse(data.currentDepthZone, out DepthZone zone))
                _currentZone = zone;
        }
    }
}
