using System;
using System.Collections.Generic;
using HADAL.Shared.Enums;
using Hadal.Data.Enums;

namespace Hadal.Data.Models
{
    [Serializable]
    public struct ResourceAmount
    {
        public ResourceType Type;
        public long Amount;

        public ResourceAmount(ResourceType type, long amount)
        {
            Type = type;
            Amount = amount;
        }
    }

    [Serializable]
    public class ResourceWallet
    {
        private readonly Dictionary<ResourceType, long> _balances = new();

        public long Get(ResourceType type) => _balances.TryGetValue(type, out var v) ? v : 0;

        public bool CanAfford(IReadOnlyList<ResourceAmount> costs)
        {
            // Sum per resource first: two 60-Titanium lines against a balance of 100 are each
            // affordable on their own but not together.
            var required = new Dictionary<ResourceType, long>();
            foreach (var cost in costs)
            {
                required.TryGetValue(cost.Type, out var sum);
                required[cost.Type] = sum + cost.Amount;
            }

            foreach (var pair in required)
            {
                if (Get(pair.Key) < pair.Value)
                    return false;
            }
            return true;
        }

        public bool TrySpend(IReadOnlyList<ResourceAmount> costs)
        {
            if (!CanAfford(costs))
                return false;

            foreach (var cost in costs)
                _balances[cost.Type] = Get(cost.Type) - cost.Amount;

            return true;
        }

        public void Add(ResourceType type, long amount)
        {
            _balances[type] = Math.Max(0, Get(type) + amount);
        }

        public void Set(ResourceType type, long amount)
        {
            _balances[type] = Math.Max(0, amount);
        }

        public void Clear() => _balances.Clear();

        public IReadOnlyDictionary<ResourceType, long> Snapshot() => _balances;
    }

    [Serializable]
    public struct PressureSnapshot
    {
        public float DepthMeters;
        public PressureTier Tier;
        public float CurrentPressure;
        public float HullStrength;
        public float PressureShield;
        public bool IsSurvivable;
    }

    [Serializable]
    public struct ExpeditionParty
    {
        public string SubmarineId;
        public string[] HeroIds;
        public DepthZone TargetZone;
    }

    [Serializable]
    public struct HeroInstance
    {
        public string InstanceId;
        public string DefinitionId;
        public int Level;
        public int Experience;
        public FactionType Faction;
    }
}
