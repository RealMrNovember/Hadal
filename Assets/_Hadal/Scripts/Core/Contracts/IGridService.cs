using HADAL.Shared.DTOs;
using UnityEngine;

namespace Hadal.Core.Contracts
{
    /// <summary>
    /// Phase 1 polar grid service — world ↔ GridSlot mapping and snap.
    /// </summary>
    public interface IGridService
    {
        CircularGridDefinition Definition { get; }

        bool TrySnapWorldPoint(Vector3 worldPoint, out GridSlot slot);

        Vector3 SlotToWorld(in GridSlot slot);

        Quaternion SlotToRotation(in GridSlot slot, float buildingRotationOffsetDegrees);

        bool IsSlotAvailable(in GridSlot slot);
    }
}
