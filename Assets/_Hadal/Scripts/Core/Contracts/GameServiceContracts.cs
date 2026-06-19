using System.Collections.Generic;
using Hadal.Core.Pooling;
using Hadal.Data.Config;
using Hadal.Data.Enums;
using Hadal.Data.Models;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Hadal.Core.Contracts
{
    public interface IResourceService
    {
        long Get(ResourceType type);
        bool CanAfford(IReadOnlyList<ResourceAmount> costs);
        bool TrySpend(IReadOnlyList<ResourceAmount> costs);
        void Add(ResourceType type, long amount);
        ResourceWallet Wallet { get; }
    }

    public interface ICircularGridService
    {
        CircularGridConfigSO Config { get; }
        int UnlockedRingCount { get; }
        int GetSectorCount(int ring);
        bool TryGetSlot(PolarGridSlotId id, out PolarGridSlot slot);
        bool IsSlotAvailable(PolarGridSlotId id);
        PlacementValidationResult ValidatePlacement(BuildingDefinitionSO building, PolarGridSlotId id, float rotationDegrees);
        bool TryOccupySlot(PolarGridSlotId id, string buildingInstanceId, float rotationDegrees);
        bool TryReleaseSlot(PolarGridSlotId id);
        bool TryExpandNextRing();
        bool TryUnlockRing(int ring);
        Vector3 GetWorldPosition(PolarGridSlotId id);
        Quaternion GetWorldRotation(PolarGridSlotId id, float buildingRotationOffset);
        bool TryGetNearestSlot(Vector3 worldPoint, out PolarGridSlotId id);
        IReadOnlyList<PolarGridSlot> GetAllSlots();
        IReadOnlyList<PolarGridSlot> GetSlotsInRing(int ring);
        float SnapRotation(float rotationDegrees);
    }

    public interface IBuildingService
    {
        bool TryPlaceBuilding(string buildingId, PolarGridSlotId slotId, float rotationDegrees, int level = 1);
        bool TryRemoveBuilding(PolarGridSlotId slotId);
        IReadOnlyDictionary<string, BuildingInstanceData> Instances { get; }
    }

    public struct BuildingInstanceData
    {
        public string InstanceId;
        public string DefinitionId;
        public int Level;
        public PolarGridSlotId SlotId;
        public float RotationDegrees;
    }

    public interface IPressureService
    {
        PressureSnapshot EvaluateDepth(float depthMeters);
        float HullStrength { get; }
        float PressureShield { get; }
        void UpgradeHull(float amount);
        void UpgradeShield(float amount);
        void ApplyNanoAlloyBonus(float bonus);
    }

    public interface ISaveParticipant
    {
        void CaptureSave(SaveGameData data);
        void RestoreSave(SaveGameData data);
    }

    public interface IAssetProvider
    {
        void PreloadCatalog();
        AsyncOperationHandle<T> LoadAsync<T>(string address) where T : UnityEngine.Object;
        void Release<T>(AsyncOperationHandle<T> handle) where T : UnityEngine.Object;
    }

    public interface IPoolService
    {
        ObjectPool<T> GetOrCreatePool<T>(string key, T prefab, UnityEngine.Transform parent = null)
            where T : UnityEngine.Component;
        void ClearAll();
    }
}
