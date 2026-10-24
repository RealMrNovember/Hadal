using UnityEngine;
using HADAL.Shared.Enums;
using Hadal.Data.Enums;
using Hadal.Data.Models;

namespace Hadal.Data.Events
{
    [CreateAssetMenu(fileName = "BuildingPlacedEvent", menuName = "Hadal/Events/Building Placed Event")]
    public class BuildingPlacedEventSO : ScriptableObject
    {
        public event System.Action<string, PolarGridSlotId, float> OnRaised;

        public void Raise(string buildingId, PolarGridSlotId slotId, float rotationDegrees)
            => OnRaised?.Invoke(buildingId, slotId, rotationDegrees);
    }
}
