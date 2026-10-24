using UnityEngine;
using HADAL.Shared.Enums;
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
}
