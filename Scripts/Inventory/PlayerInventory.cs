using System;
using System.Collections.Generic;
using Godot;
using DungeonCrawler.Core;
using DungeonCrawler.Items;

namespace DungeonCrawler.Inventory
{
    /// <summary>
    /// Manages the player's collected passive items and active items.
    /// Attach as child of PlayerController.
    /// </summary>
    public partial class PlayerInventory : Node
    {
        // ── Events ─────────────────────────────────────────────────────────────
        public event Action<ItemBase>? OnItemAdded;
        public event Action<ItemBase>? OnItemRemoved;

        // ── Storage ────────────────────────────────────────────────────────────
        private readonly List<ItemBase> _passiveItems = new();
        private readonly List<ItemBase> _activeItems  = new();
        private const int MaxActiveItems = 2;

        // ── Public API ─────────────────────────────────────────────────────────
        public IReadOnlyList<ItemBase> PassiveItems => _passiveItems.AsReadOnly();
        public IReadOnlyList<ItemBase> ActiveItems  => _activeItems.AsReadOnly();

        public bool AddItem(ItemBase item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            if (item.IsActive)
            {
                if (_activeItems.Count >= MaxActiveItems)
                {
                    GD.Print("[Inventory] Active item slots full.");
                    return false;
                }
                _activeItems.Add(item);
            }
            else
            {
                _passiveItems.Add(item);
            }

            item.OnCollected(GetParent());
            OnItemAdded?.Invoke(item);
            GameEvents.RaiseItemPickedUp(item.ItemId);
            GD.Print($"[Inventory] Added item: {item.ItemName}");
            return true;
        }

        public bool RemoveItem(string itemId)
        {
            for (int i = 0; i < _passiveItems.Count; i++)
            {
                if (_passiveItems[i].ItemId != itemId) continue;
                ItemBase item = _passiveItems[i];
                _passiveItems.RemoveAt(i);
                item.OnRemoved(GetParent());
                OnItemRemoved?.Invoke(item);
                return true;
            }
            for (int i = 0; i < _activeItems.Count; i++)
            {
                if (_activeItems[i].ItemId != itemId) continue;
                ItemBase item = _activeItems[i];
                _activeItems.RemoveAt(i);
                item.OnRemoved(GetParent());
                OnItemRemoved?.Invoke(item);
                return true;
            }
            return false;
        }

        public bool HasItem(string itemId)
        {
            foreach (var item in _passiveItems)
                if (item.ItemId == itemId) return true;
            foreach (var item in _activeItems)
                if (item.ItemId == itemId) return true;
            return false;
        }

        /// <summary>Use the active item at slot index (0 or 1).</summary>
        public void UseActiveItem(int slot)
        {
            if (slot < 0 || slot >= _activeItems.Count) return;
            _activeItems[slot].UseActive(GetParent());
        }

        public int TotalItemCount => _passiveItems.Count + _activeItems.Count;
    }
}
