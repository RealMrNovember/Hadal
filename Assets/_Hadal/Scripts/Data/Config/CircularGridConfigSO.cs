using System;
using System.Collections.Generic;
using UnityEngine;
using Hadal.Data.Enums;

namespace Hadal.Data.Config
{
    [CreateAssetMenu(fileName = "CircularGridConfig", menuName = "Hadal/Grid/Circular Grid Config")]
    public class CircularGridConfigSO : ScriptableObject
    {
        [Header("Layout")]
        [SerializeField] private float _slotYOffset = 0.05f;
        [SerializeField] private int _maxRings = 12;
        [SerializeField] private int _startingUnlockedRings = 4;
        [SerializeField] private float _rotationSnapDegrees = 60f;

        [Header("Outer Ring Expansion")]
        [SerializeField] private int _outerRingBaseSectors = 6;
        [SerializeField] private int _outerRingSectorIncrement = 2;
        [SerializeField] private int _outerRingMaxSectors = 16;

        [Header("Ring Definitions")]
        [SerializeField] private List<RingDefinition> _rings = new();

        public float SlotYOffset => _slotYOffset;
        public int MaxRings => _maxRings;
        public int StartingUnlockedRings => _startingUnlockedRings;
        public float RotationSnapDegrees => _rotationSnapDegrees;
        public IReadOnlyList<RingDefinition> Rings => _rings;

        public RingDefinition GetRing(int ringIndex)
        {
            foreach (var ring in _rings)
            {
                if (ring != null && ring.RingIndex == ringIndex)
                    return ring;
            }

            return null;
        }

        public int GetSectorCount(int ringIndex)
        {
            var ring = GetRing(ringIndex);
            if (ring != null)
                return ring.SectorCount;

            if (ringIndex <= 0)
                return 1;

            return ComputeOuterSectorCount(ringIndex);
        }

        public int ComputeOuterSectorCount(int ringIndex)
        {
            if (ringIndex <= 3)
                return 3;

            var sectors = _outerRingBaseSectors + (ringIndex - 4) * _outerRingSectorIncrement;
            return Mathf.Min(sectors, _outerRingMaxSectors);
        }

        public float GetRingRadius(int ringIndex)
        {
            var ring = GetRing(ringIndex);
            return ring != null ? ring.Radius : ringIndex * 4f;
        }
    }

    [Serializable]
    public class RingDefinition
    {
        [SerializeField] private int _ringIndex;
        [SerializeField] private float _radius = 4f;
        [SerializeField] private int _sectorCount = 3;
        [SerializeField] private float _startAngleOffset;
        [SerializeField] private GridSlotType _defaultSlotType = GridSlotType.Universal;
        [SerializeField] private bool _expandable;
        [SerializeField] private List<SectorDefinition> _sectors = new();

        public int RingIndex => _ringIndex;
        public float Radius => _radius;
        public int SectorCount => _sectorCount;
        public float StartAngleOffset => _startAngleOffset;
        public GridSlotType DefaultSlotType => _defaultSlotType;
        public bool Expandable => _expandable;
        public IReadOnlyList<SectorDefinition> Sectors => _sectors;

        public SectorDefinition GetSector(int sectorIndex)
        {
            foreach (var sector in _sectors)
            {
                if (sector != null && sector.SectorIndex == sectorIndex)
                    return sector;
            }

            return null;
        }
    }

    [Serializable]
    public class SectorDefinition
    {
        [SerializeField] private int _sectorIndex;
        [SerializeField] private GridSlotType _slotType = GridSlotType.Universal;
        [SerializeField] private string _preferredBuildingId;
        [SerializeField] private string[] _allowedBuildingIds = Array.Empty<string>();
        [SerializeField] private bool _startsLocked;

        public int SectorIndex => _sectorIndex;
        public GridSlotType SlotType => _slotType;
        public string PreferredBuildingId => _preferredBuildingId;
        public IReadOnlyList<string> AllowedBuildingIds => _allowedBuildingIds;
        public bool StartsLocked => _startsLocked;

        public bool AllowsBuilding(string buildingId)
        {
            if (_allowedBuildingIds == null || _allowedBuildingIds.Length == 0)
                return true;

            foreach (var allowed in _allowedBuildingIds)
            {
                if (allowed == buildingId)
                    return true;
            }

            return false;
        }
    }
}
