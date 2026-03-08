using System;
using System.Collections.Generic;
using Godot;
using DungeonCrawler.NPCs;

namespace DungeonCrawler.NPCs
{
    /// <summary>
    /// Shopkeeper NPC. Generates a floor-scaled inventory and opens the shop on interact.
    /// </summary>
    public partial class Shopkeeper : NPCBase
    {
        [Export] public int BaseItemCount { get; set; } = 3;

        private readonly List<ShopItem> _inventory = new List<ShopItem>();
        private static readonly Random _rng = new Random();

        // ── Data ───────────────────────────────────────────────────────────────

        public struct ShopItem
        {
            public string ItemId;
            public string DisplayName;
            public int    Price;
            public bool   Sold;

            public ShopItem(string itemId, string displayName, int price)
            {
                ItemId      = itemId;
                DisplayName = displayName;
                Price       = price;
                Sold        = false;
            }
        }

        // ── Godot lifecycle ────────────────────────────────────────────────────

        public override void _Ready()
        {
            base._Ready();
            NPCName  = "Shopkeeper";
            Dialogue = GetDialogue();
        }

        // ── NPCBase overrides ──────────────────────────────────────────────────

        public override void Interact(Node interactor)
        {
            PresentShop(interactor);
        }

        // ── Shop logic ─────────────────────────────────────────────────────────

        /// <summary>Generates a fresh inventory scaled to the given floor number.</summary>
        public void GenerateInventory(int floorNumber)
        {
            _inventory.Clear();

            string[] weaponIds     = { "weapon_pistol", "weapon_shotgun", "weapon_rifle", "weapon_crossbow" };
            string[] consumableIds = { "consumable_health_potion", "consumable_mana_potion", "consumable_bomb" };
            string[] upgradeIds    = { "upgrade_speed", "upgrade_damage", "upgrade_crit", "upgrade_armor" };

            float multiplier = GetPriceMultiplier(floorNumber);

            for (int i = 0; i < BaseItemCount; i++)
            {
                string itemId;
                int basePrice;
                double roll = _rng.NextDouble();

                if (roll < 0.35)
                {
                    itemId    = weaponIds[_rng.Next(weaponIds.Length)];
                    basePrice = 15;
                }
                else if (roll < 0.65)
                {
                    itemId    = consumableIds[_rng.Next(consumableIds.Length)];
                    basePrice = 8;
                }
                else
                {
                    itemId    = upgradeIds[_rng.Next(upgradeIds.Length)];
                    basePrice = 12;
                }

                int price = Mathf.RoundToInt(basePrice * multiplier);
                _inventory.Add(new ShopItem(itemId, FormatName(itemId), price));
            }

            GD.Print($"[Shopkeeper] Inventory generated for floor {floorNumber} ({_inventory.Count} items).");
        }

        /// <summary>Opens the shop UI for the given player.</summary>
        public void PresentShop(Node player)
        {
            GD.Print("[Shopkeeper] Opening shop...");
            Core.GameEvents.RaiseShopOpened();

            // Log items available (a real implementation would open a UI scene).
            foreach (var item in _inventory)
            {
                if (!item.Sold)
                    GD.Print($"  [{item.ItemId}]  {item.DisplayName}  –  {item.Price}g");
            }
        }

        /// <summary>
        /// Attempts to sell item at <paramref name="index"/> to <paramref name="buyer"/>.
        /// Returns true if the transaction succeeded.
        /// </summary>
        public bool TrySellItem(int index, Node buyer)
        {
            if (index < 0 || index >= _inventory.Count) return false;

            ShopItem item = _inventory[index];
            if (item.Sold) return false;

            // Try to spend coins via the player's SpendCoins method.
            if (!buyer.HasMethod("SpendCoins")) return false;
            bool success = (bool)buyer.Call("SpendCoins", item.Price);
            if (!success)
            {
                GD.Print($"[Shopkeeper] {buyer.Name} cannot afford {item.DisplayName} ({item.Price}g).");
                return false;
            }

            // Mark as sold.
            var sold = item;
            sold.Sold = true;
            _inventory[index] = sold;

            // Give item to buyer.
            if (buyer.HasMethod("AddItemById"))
                buyer.Call("AddItemById", item.ItemId);

            GD.Print($"[Shopkeeper] Sold {item.DisplayName} to {buyer.Name} for {item.Price}g.");
            return true;
        }

        /// <summary>Price multiplier increases by 25% per floor above 1.</summary>
        public float GetPriceMultiplier(int floor) => 1.0f + (floor - 1) * 0.25f;

        public string[] GetDialogue() => new[]
        {
            "Welcome, adventurer! See anything you like?",
            "My wares are the finest in the dungeon.",
            "Spend wisely – danger lurks ahead.",
            "Come back anytime!"
        };

        // ── Helpers ────────────────────────────────────────────────────────────

        private static string FormatName(string id) =>
            System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(
                id.Replace('_', ' '));

        public List<ShopItem> GetInventory() => _inventory;
    }
}
