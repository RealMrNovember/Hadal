using System;
using System.Collections.Generic;
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
            foreach (var cost in costs)
            {
                if (Get(cost.Type) < cost.Amount)
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
            _balances[type] = Get(type) + amount;
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
