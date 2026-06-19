using System;
using System.Collections.Generic;
using HADAL.Shared.DTOs;
using Hadal.Core.Contracts;
using Hadal.Data.Config;
using Hadal.Data.Enums;
using Hadal.Data.Models;
using Hadal.Gameplay.Grid;
using Hadal.Managers.Services;
using UnityEngine;
using VContainer.Unity;

namespace Hadal.Managers
{
    /// <summary>
    /// Injectable circular grid service — polar snap, validation, and ICircularGridService adapter.
    /// </summary>
    public sealed class CircularGridManager : ICircularGridService, IGridService, ISaveParticipant, IStartable
    {
        private readonly GameConfigSO _config;
        private CircularGridModel _model;

        public CircularGridManager(GameConfigSO config)
        {
            _config = config;
        }

        public CircularGridConfigSO Config => _model?.Config;

        public CircularGridDefinition Definition => BuildDefinition();

        public int UnlockedRingCount => _model?.UnlockedRingCount ?? 0;

        public void Start()
        {
            if (_config?.CircularGridConfig == null)
            {
                Debug.LogError("[CircularGridManager] CircularGridConfigSO is not assigned.");
                return;
            }

            _model = new CircularGridModel(_config.CircularGridConfig);
            _model.Initialize();
        }

        public bool TrySnapWorldPoint(Vector3 worldPoint, out GridSlot slot)
        {
            slot = default;

            if (_model == null)
                return false;

            if (!PolarGridLayoutUtility.TryGetNearestSlot(this, worldPoint, out var nearest))
                return false;

            if (!_model.TryGetSlot(nearest, out var polarSlot))
                return false;

            var radius = Config.GetRingRadius(nearest.Ring);
            slot = GridSlot.Create(radius, polarSlot.SlotAngleDegrees, nearest.Ring, nearest.Sector);
            return true;
        }

        public Vector3 SlotToWorld(in GridSlot slot)
        {
            var id = new PolarGridSlotId(slot.RingIndex, slot.SectorIndex);
            return GetWorldPosition(id);
        }

        public Quaternion SlotToRotation(in GridSlot slot, float buildingRotationOffsetDegrees)
        {
            var id = new PolarGridSlotId(slot.RingIndex, slot.SectorIndex);
            return GetWorldRotation(id, buildingRotationOffsetDegrees);
        }

        public bool IsSlotAvailable(in GridSlot slot)
        {
            return IsSlotAvailable(new PolarGridSlotId(slot.RingIndex, slot.SectorIndex));
        }

        public int GetSectorCount(int ring) => _model.GetSectorCount(ring);

        public bool TryGetSlot(PolarGridSlotId id, out PolarGridSlot slot) => _model.TryGetSlot(id, out slot);

        public bool IsSlotAvailable(PolarGridSlotId id) => _model.IsSlotAvailable(id);

        public PlacementValidationResult ValidatePlacement(BuildingDefinitionSO building, PolarGridSlotId id, float rotationDegrees)
            => _model.ValidatePlacement(building, id, rotationDegrees);

        public bool TryOccupySlot(PolarGridSlotId id, string buildingInstanceId, float rotationDegrees)
            => _model.TryOccupySlot(id, buildingInstanceId, rotationDegrees);

        public bool TryReleaseSlot(PolarGridSlotId id) => _model.TryReleaseSlot(id);

        public void SyncOccupancyFromBuildings(BuildingSaveEntry[] buildings)
        {
            if (_model == null)
                return;

            foreach (var slot in _model.GetAllSlots())
            {
                if (slot.IsOccupied)
                    _model.TryReleaseSlot(slot.Id);
            }

            if (buildings == null || buildings.Length == 0)
                return;

            foreach (var building in buildings)
            {
                var id = new PolarGridSlotId(building.cellRing, building.cellSector);
                _model.TryOccupySlot(id, building.instanceKey, building.rotation);
            }
        }

        public bool TryExpandNextRing() => _model.TryExpandNextRing();

        public bool TryUnlockRing(int ring) => _model.TryUnlockRing(ring);

        public Vector3 GetWorldPosition(PolarGridSlotId id)
        {
            if (!_model.TryGetSlot(id, out var slot))
                return Vector3.zero;

            return PolarGridLayoutUtility.GetSlotWorldPosition(Config, id, slot.SlotAngleDegrees);
        }

        public Quaternion GetWorldRotation(PolarGridSlotId id, float buildingRotationOffset)
        {
            if (!_model.TryGetSlot(id, out var slot))
                return Quaternion.identity;

            return PolarGridLayoutUtility.GetSlotWorldRotation(slot.SlotAngleDegrees, buildingRotationOffset);
        }

        public bool TryGetNearestSlot(Vector3 worldPoint, out PolarGridSlotId id)
            => PolarGridLayoutUtility.TryGetNearestSlot(this, worldPoint, out id);

        public IReadOnlyList<PolarGridSlot> GetAllSlots() => _model.GetAllSlots();

        public IReadOnlyList<PolarGridSlot> GetSlotsInRing(int ring) => _model.GetSlotsInRing(ring);

        public float SnapRotation(float rotationDegrees) => _model.SnapRotation(rotationDegrees);

        public void CaptureSave(SaveGameData data)
        {
            data.unlockedGridRings = UnlockedRingCount;
            data.polarGridSlots = _model.CreateSaveEntries();
        }

        public void RestoreSave(SaveGameData data)
        {
            if (data.version >= 2 && data.polarGridSlots != null && data.polarGridSlots.Length > 0)
            {
                _model.ApplySaveEntries(data.polarGridSlots, data.unlockedGridRings);
                return;
            }

            _model.SetUnlockedRingCount(data.unlockedGridRings > 0 ? data.unlockedGridRings : Config.StartingUnlockedRings);

            if (data.gridCells == null)
                return;

            foreach (var legacy in data.gridCells)
                _model.TryMigrateLegacyCell(legacy);
        }

        private CircularGridDefinition BuildDefinition()
        {
            if (Config == null)
                return new CircularGridDefinition();

            var ringCount = Config.MaxRings;
            var sectors = new int[ringCount + 1];
            var radii = new float[ringCount + 1];

            for (var ring = 0; ring <= ringCount; ring++)
            {
                sectors[ring] = Config.GetSectorCount(ring);
                radii[ring] = Config.GetRingRadius(ring);
            }

            return new CircularGridDefinition
            {
                RingCount = ringCount,
                SectorsPerRing = sectors,
                RingRadii = radii,
                SlotYOffset = Config.SlotYOffset,
                RotationSnapDegrees = Config.RotationSnapDegrees
            };
        }
    }
}
