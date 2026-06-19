using UnityEngine;
using Hadal.Data.Enums;
using Hadal.Data.Models;

namespace Hadal.Data.Events
{
    [CreateAssetMenu(fileName = "ResourceChangedEvent", menuName = "Hadal/Events/Resource Changed Event")]
    public class ResourceChangedEventSO : ScriptableObject
    {
        public event System.Action<ResourceType, long, long> OnRaised;

        public void Raise(ResourceType type, long oldAmount, long newAmount)
            => OnRaised?.Invoke(type, oldAmount, newAmount);
    }

    [CreateAssetMenu(fileName = "PressureChangedEvent", menuName = "Hadal/Events/Pressure Changed Event")]
    public class PressureChangedEventSO : ScriptableObject
    {
        public event System.Action<PressureSnapshot> OnRaised;
        public void Raise(PressureSnapshot snapshot) => OnRaised?.Invoke(snapshot);
    }

    [CreateAssetMenu(fileName = "BuildingPlacedEvent", menuName = "Hadal/Events/Building Placed Event")]
    public class BuildingPlacedEventSO : ScriptableObject
    {
        public event System.Action<string, PolarGridSlotId, float> OnRaised;

        public void Raise(string buildingId, PolarGridSlotId slotId, float rotationDegrees)
            => OnRaised?.Invoke(buildingId, slotId, rotationDegrees);
    }

    [CreateAssetMenu(fileName = "ExpeditionStartedEvent", menuName = "Hadal/Events/Expedition Started Event")]
    public class ExpeditionStartedEventSO : ScriptableObject
    {
        public event System.Action<ExpeditionParty> OnRaised;
        public void Raise(ExpeditionParty party) => OnRaised?.Invoke(party);
    }
}
