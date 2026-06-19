using System.Collections.Generic;
using UnityEngine;
using Hadal.Core.Contracts;
using Hadal.Data.Config;
using Hadal.Data.Models;
using Hadal.Gameplay.Grid;
using Hadal.Managers.Base;
using Hadal.Managers.Services;

namespace Hadal.Managers
{
    public class GridManager : ManagerBase, ICircularGridService, ISaveParticipant
    {
        private CircularGridModel _model;

        public CircularGridConfigSO Config => _model?.Config;

        public int UnlockedRingCount => _model?.UnlockedRingCount ?? 0;

        protected override void OnInitialize(GameConfigSO config)
        {
            if (config.CircularGridConfig == null)
            {
                Debug.LogError("[GridManager] CircularGridConfigSO is not assigned.");
                return;
            }

            _model = new CircularGridModel(config.CircularGridConfig);
            _model.Initialize();
        }

        public int GetSectorCount(int ring) => _model.GetSectorCount(ring);

        public bool TryGetSlot(PolarGridSlotId id, out PolarGridSlot slot) => _model.TryGetSlot(id, out slot);

        public bool IsSlotAvailable(PolarGridSlotId id) => _model.IsSlotAvailable(id);

        public PlacementValidationResult ValidatePlacement(BuildingDefinitionSO building, PolarGridSlotId id, float rotationDegrees)
            => _model.ValidatePlacement(building, id, rotationDegrees);

        public bool TryOccupySlot(PolarGridSlotId id, string buildingInstanceId, float rotationDegrees)
            => _model.TryOccupySlot(id, buildingInstanceId, rotationDegrees);

        public bool TryReleaseSlot(PolarGridSlotId id) => _model.TryReleaseSlot(id);

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

        protected override void OnShutdown() => _model = null;
    }
}
