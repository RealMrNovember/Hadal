using System;
using System.Collections.Generic;
using UnityEngine;
using Hadal.Data.Config;
using Hadal.Data.Enums;
using Hadal.Data.Models;

namespace Hadal.Managers.Services
{
    /// <summary>
    /// Pure C# circular grid model. Ring + Sector indexing with polar layout metadata.
    /// </summary>
    public sealed class CircularGridModel
    {
        private readonly CircularGridConfigSO _config;
        private readonly Dictionary<PolarGridSlotId, PolarGridSlot> _slots = new();
        private int _unlockedRingCount;

        public CircularGridConfigSO Config => _config;
        public int UnlockedRingCount => _unlockedRingCount;

        public CircularGridModel(CircularGridConfigSO config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void Initialize()
        {
            _slots.Clear();
            _unlockedRingCount = Math.Max(1, _config.StartingUnlockedRings);
            BuildSlotsUpToRing(_unlockedRingCount);
        }

        public void SetUnlockedRingCount(int ringCount)
        {
            _unlockedRingCount = Math.Clamp(ringCount, 1, _config.MaxRings);
            BuildSlotsUpToRing(_unlockedRingCount);
        }

        public bool TryExpandNextRing()
        {
            if (_unlockedRingCount >= _config.MaxRings)
                return false;

            _unlockedRingCount++;
            EnsureRingBuilt(_unlockedRingCount);
            return true;
        }

        public bool TryUnlockRing(int ring)
        {
            if (ring < 1 || ring > _config.MaxRings || ring <= _unlockedRingCount)
                return false;

            for (var r = _unlockedRingCount + 1; r <= ring; r++)
                EnsureRingBuilt(r);

            _unlockedRingCount = ring;
            UnlockRingSlots(ring);
            return true;
        }

        public bool TryGetSlot(PolarGridSlotId id, out PolarGridSlot slot) => _slots.TryGetValue(id, out slot);

        public bool IsSlotAvailable(PolarGridSlotId id)
        {
            if (!_slots.TryGetValue(id, out var slot))
                return false;

            return slot.IsUnlocked && !slot.IsOccupied;
        }

        public PlacementValidationResult ValidatePlacement(BuildingDefinitionSO building, PolarGridSlotId id, float rotationDegrees)
        {
            if (building == null)
                return PlacementValidationResult.Fail("Missing building definition.");

            if (!_slots.TryGetValue(id, out var slot))
                return PlacementValidationResult.Fail("Slot does not exist.");

            if (!slot.IsUnlocked)
                return PlacementValidationResult.Fail("Ring not unlocked.");

            if (slot.IsOccupied)
                return PlacementValidationResult.Fail("Slot occupied.");

            if (id.Ring < building.MinRing || id.Ring > building.MaxRing)
                return PlacementValidationResult.Fail("Invalid ring for this building.");

            if (!building.MatchesSlotType(slot.SlotType))
                return PlacementValidationResult.Fail("Building type mismatch.");

            var ringDef = _config.GetRing(id.Ring);
            var sectorDef = ringDef?.GetSector(id.Sector);
            if (sectorDef != null && !sectorDef.AllowsBuilding(building.Id))
                return PlacementValidationResult.Fail("Building not allowed in this sector.");

            return PlacementValidationResult.Success();
        }

        public bool TryOccupySlot(PolarGridSlotId id, string buildingInstanceId, float rotationDegrees)
        {
            if (!_slots.TryGetValue(id, out var slot) || !slot.IsUnlocked || slot.IsOccupied)
                return false;

            slot.IsOccupied = true;
            slot.BuildingInstanceId = buildingInstanceId;
            slot.BuildingRotationDegrees = rotationDegrees;
            _slots[id] = slot;
            return true;
        }

        public bool TryReleaseSlot(PolarGridSlotId id)
        {
            if (!_slots.TryGetValue(id, out var slot))
                return false;

            slot.IsOccupied = false;
            slot.BuildingInstanceId = null;
            slot.BuildingRotationDegrees = 0f;
            _slots[id] = slot;
            return true;
        }

        public IReadOnlyList<PolarGridSlot> GetAllSlots()
        {
            var list = new List<PolarGridSlot>(_slots.Count);
            foreach (var pair in _slots)
                list.Add(pair.Value);

            return list;
        }

        public IReadOnlyList<PolarGridSlot> GetSlotsInRing(int ring)
        {
            var list = new List<PolarGridSlot>();
            foreach (var pair in _slots)
            {
                if (pair.Key.Ring == ring)
                    list.Add(pair.Value);
            }

            return list;
        }

        public int GetSectorCount(int ring) => _config.GetSectorCount(ring);

        public float GetSlotAngleDegrees(PolarGridSlotId id)
        {
            if (_slots.TryGetValue(id, out var slot))
                return slot.SlotAngleDegrees;

            return ComputeSlotAngle(id.Ring, id.Sector);
        }

        public float SnapRotation(float rotationDegrees)
        {
            var snap = Math.Max(1f, _config.RotationSnapDegrees);
            return Mathf.Round(rotationDegrees / snap) * snap;
        }

        public void ApplySaveEntries(IReadOnlyList<PolarGridSlotSaveEntry> entries, int unlockedRings)
        {
            SetUnlockedRingCount(unlockedRings);

            if (entries == null)
                return;

            foreach (var entry in entries)
            {
                var id = new PolarGridSlotId(entry.ring, entry.sector);
                if (!_slots.TryGetValue(id, out var slot))
                    continue;

                slot.SlotType = (GridSlotType)entry.slotType;
                slot.IsOccupied = entry.isOccupied;
                slot.BuildingInstanceId = entry.buildingInstanceId;
                slot.BuildingRotationDegrees = entry.buildingRotation;
                slot.SlotAngleDegrees = entry.slotAngle;
                slot.IsUnlocked = entry.isUnlocked;
                slot.PreferredBuildingId = entry.preferredBuildingId;
                _slots[id] = slot;
            }
        }

        public PolarGridSlotSaveEntry[] CreateSaveEntries()
        {
            var entries = new PolarGridSlotSaveEntry[_slots.Count];
            var index = 0;

            foreach (var pair in _slots)
            {
                var slot = pair.Value;
                entries[index++] = new PolarGridSlotSaveEntry
                {
                    ring = slot.Id.Ring,
                    sector = slot.Id.Sector,
                    slotType = (int)slot.SlotType,
                    isOccupied = slot.IsOccupied,
                    buildingInstanceId = slot.BuildingInstanceId,
                    buildingRotation = slot.BuildingRotationDegrees,
                    slotAngle = slot.SlotAngleDegrees,
                    isUnlocked = slot.IsUnlocked,
                    preferredBuildingId = slot.PreferredBuildingId
                };
            }

            return entries;
        }

        public bool TryMigrateLegacyCell(GridCellSaveEntry entry)
        {
            var id = new PolarGridSlotId(entry.ring, entry.sector >= 0 ? entry.sector : 0);
            if (entry.sector < 0 && entry.ring > 0)
            {
                var angle = Math.Atan2(entry.y, entry.x) * Mathf.Rad2Deg;
                if (angle < 0f)
                    angle += 360f;

                var sectorCount = GetSectorCount(entry.ring);
                var ringDef = _config.GetRing(entry.ring);
                var offset = ringDef?.StartAngleOffset ?? 0f;
                var normalized = (angle - offset + 360f) % 360f;
                var sector = (int)Math.Round(normalized / (360f / sectorCount)) % sectorCount;
                id = new PolarGridSlotId(entry.ring, sector);
            }

            if (!_slots.TryGetValue(id, out var slot))
                return false;

            slot.IsOccupied = entry.isOccupied;
            slot.BuildingInstanceId = entry.buildingId;
            slot.BuildingRotationDegrees = entry.rotation;
            _slots[id] = slot;
            return true;
        }

        private void BuildSlotsUpToRing(int ringCount)
        {
            for (var ring = 0; ring <= ringCount; ring++)
                EnsureRingBuilt(ring);

            for (var ring = ringCount + 1; ring <= _config.MaxRings; ring++)
                RemoveRing(ring);
        }

        private void EnsureRingBuilt(int ring)
        {
            var sectorCount = GetSectorCount(ring);
            var ringDef = _config.GetRing(ring);

            for (var sector = 0; sector < sectorCount; sector++)
            {
                var id = new PolarGridSlotId(ring, sector);
                if (_slots.ContainsKey(id))
                    continue;

                var sectorDef = ringDef?.GetSector(sector);
                _slots[id] = new PolarGridSlot
                {
                    Id = id,
                    SlotType = sectorDef?.SlotType ?? ringDef?.DefaultSlotType ?? GridSlotType.Universal,
                    IsUnlocked = ring <= _unlockedRingCount && !(sectorDef?.StartsLocked ?? false),
                    PreferredBuildingId = sectorDef?.PreferredBuildingId,
                    SlotAngleDegrees = ComputeSlotAngle(ring, sector),
                    IsOccupied = false
                };
            }
        }

        private void RemoveRing(int ring)
        {
            var toRemove = new List<PolarGridSlotId>();
            foreach (var pair in _slots)
            {
                if (pair.Key.Ring == ring)
                    toRemove.Add(pair.Key);
            }

            foreach (var id in toRemove)
                _slots.Remove(id);
        }

        private void UnlockRingSlots(int ring)
        {
            foreach (var pair in _slots)
            {
                if (pair.Key.Ring != ring)
                    continue;

                var slot = pair.Value;
                slot.IsUnlocked = true;
                _slots[pair.Key] = slot;
            }
        }

        private float ComputeSlotAngle(int ring, int sector)
        {
            var ringDef = _config.GetRing(ring);
            var sectorCount = GetSectorCount(ring);
            var offset = ringDef?.StartAngleOffset ?? 0f;

            if (ring == 0)
                return offset;

            var sectorArc = 360f / sectorCount;
            return offset + sector * sectorArc;
        }
    }
}
