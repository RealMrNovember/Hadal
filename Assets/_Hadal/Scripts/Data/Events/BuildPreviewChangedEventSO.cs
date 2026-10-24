using UnityEngine;
using Hadal.Data.Enums;
using Hadal.Data.Models;

namespace Hadal.Data.Events
{
    [CreateAssetMenu(fileName = "BuildPreviewChangedEvent", menuName = "Hadal/Events/Build Preview Changed Event")]
    public class BuildPreviewChangedEventSO : ScriptableObject
    {
        public event System.Action<string, PolarGridSlotId, float, bool> OnRaised;

        public void Raise(string buildingId, PolarGridSlotId slotId, float rotation, bool isValid)
            => OnRaised?.Invoke(buildingId, slotId, rotation, isValid);
    }
}
