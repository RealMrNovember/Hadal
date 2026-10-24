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
}
