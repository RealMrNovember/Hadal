using UnityEngine;
using HADAL.Shared.Enums;
using Hadal.Data.Enums;
using Hadal.Data.Models;

namespace Hadal.Data.Events
{
    [CreateAssetMenu(fileName = "PressureChangedEvent", menuName = "Hadal/Events/Pressure Changed Event")]
    public class PressureChangedEventSO : ScriptableObject
    {
        public event System.Action<PressureSnapshot> OnRaised;
        public void Raise(PressureSnapshot snapshot) => OnRaised?.Invoke(snapshot);
    }
}
