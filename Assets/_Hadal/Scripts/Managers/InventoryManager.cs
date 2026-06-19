using System.Collections.Generic;
using UnityEngine;
using Hadal.Data.Config;
using Hadal.Managers.Base;

namespace Hadal.Managers
{
    public class InventoryManager : ManagerBase
    {
        private readonly Dictionary<string, int> _items = new();

        protected override void OnInitialize(GameConfigSO config) { }

        public int GetQuantity(string itemId)
            => _items.TryGetValue(itemId, out var qty) ? qty : 0;

        public void AddItem(string itemId, int quantity)
        {
            _items[itemId] = GetQuantity(itemId) + quantity;
        }

        public bool TryRemoveItem(string itemId, int quantity)
        {
            var current = GetQuantity(itemId);
            if (current < quantity)
                return false;

            var remaining = current - quantity;
            if (remaining == 0)
                _items.Remove(itemId);
            else
                _items[itemId] = remaining;

            return true;
        }

        public IReadOnlyDictionary<string, int> Items => _items;

        protected override void OnShutdown() => _items.Clear();
    }
}
