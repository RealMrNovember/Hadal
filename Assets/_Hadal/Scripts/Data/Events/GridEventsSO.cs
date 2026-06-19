using UnityEngine;
using Hadal.Data.Enums;
using Hadal.Data.Models;

namespace Hadal.Data.Events
{
    [CreateAssetMenu(fileName = "GridSlotHighlightEvent", menuName = "Hadal/Events/Grid Slot Highlight Event")]
    public class GridSlotHighlightEventSO : ScriptableObject
    {
        public event System.Action<PolarGridSlotId, GridHighlightState> OnRaised;

        public void Raise(PolarGridSlotId slotId, GridHighlightState state)
            => OnRaised?.Invoke(slotId, state);
    }

    [CreateAssetMenu(fileName = "BuildPreviewChangedEvent", menuName = "Hadal/Events/Build Preview Changed Event")]
    public class BuildPreviewChangedEventSO : ScriptableObject
    {
        public event System.Action<string, PolarGridSlotId, float, bool> OnRaised;

        public void Raise(string buildingId, PolarGridSlotId slotId, float rotation, bool isValid)
            => OnRaised?.Invoke(buildingId, slotId, rotation, isValid);
    }
}
