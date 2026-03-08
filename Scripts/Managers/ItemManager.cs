using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using DungeonCrawler.Core;

namespace DungeonCrawler.Managers
{
    public enum ItemCategory { Weapon, Consumable, Passive, Active, KeyItem }

    /// <summary>
    /// Registry for all item definitions. Handles lookup, filtering, and random selection.
    /// Singleton autoload.
    /// </summary>
    public partial class ItemManager : Node
    {
        public static ItemManager Instance { get; private set; } = null!;

        // ── Nested ItemData ────────────────────────────────────────────────────
        public class ItemData
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public ItemCategory Category { get; set; } = ItemCategory.Passive;
            public float BaseValue { get; set; } = 10f;
            public float Weight { get; set; } = 1.0f;       // spawn weight
            public string ScenePath { get; set; } = "";
        }

        // ── State ──────────────────────────────────────────────────────────────
        private readonly Dictionary<string, ItemData> _itemRegistry = new();
        private readonly Dictionary<string, PackedScene> _sceneCache = new();
        private int _idCounter = 0;

        // ── Godot lifecycle ────────────────────────────────────────────────────
        public override void _Ready()
        {
            if (Instance != null && Instance != this) { QueueFree(); return; }
            Instance = this;
            Initialize();
        }

        // ── Registration ───────────────────────────────────────────────────────
        public void RegisterItem(string id, ItemData data)
        {
            data.Id = id;
            _itemRegistry[id] = data;
        }

        public ItemData? GetItem(string id)
            => _itemRegistry.TryGetValue(id, out ItemData? d) ? d : null;

        public List<ItemData> GetItemsByCategory(ItemCategory category)
            => _itemRegistry.Values.Where(d => d.Category == category).ToList();

        // ── Random selection ───────────────────────────────────────────────────
        public List<ItemData> GetRandomItems(int count, ItemCategory? filter = null, float luckMultiplier = 1.0f)
        {
            List<ItemData> pool = filter.HasValue
                ? GetItemsByCategory(filter.Value)
                : _itemRegistry.Values.ToList();

            if (pool.Count == 0) return new List<ItemData>();

            RNGManager rng = RNGManager.Instance;
            Dictionary<ItemData, float> weighted = new();
            foreach (ItemData item in pool)
                weighted[item] = item.Weight * luckMultiplier;

            List<ItemData> result = new();
            HashSet<string> picked = new();

            for (int i = 0; i < count && weighted.Count > 0; i++)
            {
                ItemData chosen = rng != null
                    ? rng.PickWeighted(weighted, "items")
                    : pool[new Random().Next(pool.Count)];

                result.Add(chosen);
                picked.Add(chosen.Id);
                weighted.Remove(chosen);
            }

            return result;
        }

        /// <summary>Returns a cached PackedScene for the item, or null if none registered.</summary>
        public PackedScene? GetItemScene(string itemId)
        {
            if (!_itemRegistry.TryGetValue(itemId, out ItemData? data)) return null;
            if (string.IsNullOrEmpty(data.ScenePath)) return null;

            if (_sceneCache.TryGetValue(itemId, out PackedScene? cached)) return cached;

            if (!ResourceLoader.Exists(data.ScenePath)) return null;
            PackedScene scene = ResourceLoader.Load<PackedScene>(data.ScenePath);
            _sceneCache[itemId] = scene;
            return scene;
        }

        // ── Shop helpers ───────────────────────────────────────────────────────
        public List<ItemData> GetShopInventory(int floorNumber, int count, float luckMultiplier)
        {
            // Exclude KeyItems from shops
            List<ItemData> pool = _itemRegistry.Values
                .Where(d => d.Category != ItemCategory.KeyItem)
                .ToList();

            if (pool.Count == 0) return new List<ItemData>();

            RNGManager rng = RNGManager.Instance;
            Dictionary<ItemData, float> weighted = new();
            foreach (ItemData item in pool)
            {
                // Items with higher BaseValue become rarer; luck partially counters this
                float adjustedWeight = item.Weight / (1f + item.BaseValue * 0.01f) * luckMultiplier;
                weighted[item] = MathF.Max(0.01f, adjustedWeight);
            }

            List<ItemData> result = new();
            for (int i = 0; i < count && weighted.Count > 0; i++)
            {
                ItemData chosen = rng != null
                    ? rng.PickWeighted(weighted, "items")
                    : pool[new Random().Next(pool.Count)];
                result.Add(chosen);
                weighted.Remove(chosen);
            }

            return result;
        }

        public string GenerateItemId()
            => $"item_{++_idCounter}_{Guid.NewGuid().ToString("N")[..6]}";

        // ── Predefined item catalogue ──────────────────────────────────────────
        public void Initialize()
        {
            // Passives
            RegisterItem("health_up", new ItemData
            {
                Name = "Vital Crystal", Description = "+20 Max HP",
                Category = ItemCategory.Passive, BaseValue = 15f, Weight = 1.2f
            });
            RegisterItem("speed_boots", new ItemData
            {
                Name = "Swift Boots", Description = "+30% Movement Speed",
                Category = ItemCategory.Passive, BaseValue = 12f, Weight = 1.0f
            });
            RegisterItem("damage_ring", new ItemData
            {
                Name = "Iron Ring", Description = "+10 Damage",
                Category = ItemCategory.Passive, BaseValue = 18f, Weight = 0.9f
            });
            RegisterItem("crit_eye", new ItemData
            {
                Name = "Crit Eye", Description = "+10% Crit Chance",
                Category = ItemCategory.Passive, BaseValue = 20f, Weight = 0.8f
            });
            RegisterItem("lucky_charm", new ItemData
            {
                Name = "Lucky Charm", Description = "+15% Luck",
                Category = ItemCategory.Passive, BaseValue = 14f, Weight = 1.1f
            });
            RegisterItem("armor_vest", new ItemData
            {
                Name = "Armor Vest", Description = "+15 Armor",
                Category = ItemCategory.Passive, BaseValue = 16f, Weight = 1.0f
            });

            // Actives
            RegisterItem("shield_bubble", new ItemData
            {
                Name = "Shield Bubble", Description = "Temporary invulnerability",
                Category = ItemCategory.Active, BaseValue = 25f, Weight = 0.7f
            });
            RegisterItem("damage_aura", new ItemData
            {
                Name = "Damage Aura", Description = "Deal AOE damage around you",
                Category = ItemCategory.Active, BaseValue = 22f, Weight = 0.75f
            });

            // Consumables
            RegisterItem("health_potion_small", new ItemData
            {
                Name = "Small Potion", Description = "Restore 25 HP",
                Category = ItemCategory.Consumable, BaseValue = 8f, Weight = 1.5f
            });
            RegisterItem("health_potion_large", new ItemData
            {
                Name = "Large Potion", Description = "Restore 60 HP",
                Category = ItemCategory.Consumable, BaseValue = 15f, Weight = 0.8f
            });
            RegisterItem("mana_potion", new ItemData
            {
                Name = "Mana Potion", Description = "Restore 30 Mana",
                Category = ItemCategory.Consumable, BaseValue = 10f, Weight = 1.2f
            });
            RegisterItem("bomb", new ItemData
            {
                Name = "Bomb", Description = "Explodes for 80 damage in area",
                Category = ItemCategory.Consumable, BaseValue = 12f, Weight = 1.0f
            });

            // Weapons
            RegisterItem("basic_gun", new ItemData
            {
                Name = "Basic Gun", Description = "Standard firearm",
                Category = ItemCategory.Weapon, BaseValue = 20f, Weight = 1.0f
            });
            RegisterItem("shotgun", new ItemData
            {
                Name = "Shotgun", Description = "Close-range spread",
                Category = ItemCategory.Weapon, BaseValue = 25f, Weight = 0.8f
            });
            RegisterItem("sniper_rifle", new ItemData
            {
                Name = "Sniper Rifle", Description = "High damage, slow fire",
                Category = ItemCategory.Weapon, BaseValue = 30f, Weight = 0.6f
            });
            RegisterItem("auto_rifle", new ItemData
            {
                Name = "Auto Rifle", Description = "Rapid fire",
                Category = ItemCategory.Weapon, BaseValue = 22f, Weight = 0.9f
            });

            GD.Print($"[ItemManager] Registered {_itemRegistry.Count} items.");
        }
    }
}
