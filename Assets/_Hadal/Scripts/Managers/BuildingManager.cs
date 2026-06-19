using System;
using System.Collections.Generic;
using UnityEngine;
using Hadal.Core.Contracts;
using Hadal.Core.DI;
using Hadal.Data.Config;
using Hadal.Data.Events;
using Hadal.Data.Models;
using Hadal.Managers.Base;

namespace Hadal.Managers
{
    public class BuildingManager : ManagerBase, IBuildingService, ISaveParticipant
    {
        [SerializeField] private BuildingPlacedEventSO _buildingPlacedEvent;

        private BuildingDatabaseSO _database;
        private readonly Dictionary<string, BuildingInstanceData> _instances = new();
        private IResourceService _resourceService;
        private ICircularGridService _gridService;

        public IReadOnlyDictionary<string, BuildingInstanceData> Instances => _instances;

        protected override void OnInitialize(GameConfigSO config)
        {
            _database = config.BuildingDatabase;
        }

        public override void ResolveDependencies(GameServiceContainer container)
        {
            container.TryResolve(out _resourceService);
            container.TryResolve(out _gridService);
        }

        public bool TryPlaceBuilding(string buildingId, PolarGridSlotId slotId, float rotationDegrees, int level = 1)
        {
            var def = _database?.GetById(buildingId);
            if (def == null || _gridService == null)
                return false;

            var snappedRotation = _gridService.SnapRotation(rotationDegrees);
            var validation = _gridService.ValidatePlacement(def, slotId, snappedRotation);
            if (!validation.IsValid)
                return false;

            if (_resourceService != null && !_resourceService.TrySpend(def.BuildCosts))
                return false;

            var instanceId = Guid.NewGuid().ToString("N");
            if (!_gridService.TryOccupySlot(slotId, instanceId, snappedRotation))
                return false;

            _instances[instanceId] = new BuildingInstanceData
            {
                InstanceId = instanceId,
                DefinitionId = buildingId,
                Level = level,
                SlotId = slotId,
                RotationDegrees = snappedRotation
            };

            _buildingPlacedEvent?.Raise(buildingId, slotId, snappedRotation);
            return true;
        }

        public bool TryRemoveBuilding(PolarGridSlotId slotId)
        {
            BuildingInstanceData? target = null;
            string targetKey = null;

            foreach (var pair in _instances)
            {
                if (pair.Value.SlotId != slotId)
                    continue;

                target = pair.Value;
                targetKey = pair.Key;
                break;
            }

            if (targetKey == null)
                return false;

            _gridService?.TryReleaseSlot(slotId);
            _instances.Remove(targetKey);
            return true;
        }

        public void CaptureSave(SaveGameData data)
        {
            var entries = new BuildingSaveEntry[_instances.Count];
            var index = 0;

            foreach (var pair in _instances)
            {
                var instance = pair.Value;
                entries[index++] = new BuildingSaveEntry
                {
                    instanceKey = pair.Key,
                    definitionId = instance.DefinitionId,
                    level = instance.Level,
                    cellRing = instance.SlotId.Ring,
                    cellSector = instance.SlotId.Sector,
                    rotation = instance.RotationDegrees
                };
            }

            data.buildings = entries;
        }

        public void RestoreSave(SaveGameData data)
        {
            _instances.Clear();
            if (data.buildings == null)
                return;

            foreach (var entry in data.buildings)
            {
                var slotId = new PolarGridSlotId(entry.cellRing, entry.cellSector);
                var rotation = entry.rotation;

                if (data.version < 2 && entry.cellSector == 0 && (entry.cellX != 0 || entry.cellY != 0))
                {
                    slotId = new PolarGridSlotId(entry.cellRing, 0);
                }

                var instanceId = string.IsNullOrWhiteSpace(entry.instanceKey)
                    ? Guid.NewGuid().ToString("N")
                    : entry.instanceKey;

                _instances[instanceId] = new BuildingInstanceData
                {
                    InstanceId = instanceId,
                    DefinitionId = entry.definitionId,
                    Level = entry.level,
                    SlotId = slotId,
                    RotationDegrees = rotation
                };

                _gridService?.TryOccupySlot(slotId, instanceId, rotation);
            }
        }

        protected override void OnShutdown() => _instances.Clear();
    }
}
