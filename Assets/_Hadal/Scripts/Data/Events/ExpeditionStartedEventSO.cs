using UnityEngine;
using HADAL.Shared.Enums;
using Hadal.Data.Enums;
using Hadal.Data.Models;

namespace Hadal.Data.Events
{
    [CreateAssetMenu(fileName = "ExpeditionStartedEvent", menuName = "Hadal/Events/Expedition Started Event")]
    public class ExpeditionStartedEventSO : ScriptableObject
    {
        public event System.Action<ExpeditionParty> OnRaised;
        public void Raise(ExpeditionParty party) => OnRaised?.Invoke(party);
    }
}
