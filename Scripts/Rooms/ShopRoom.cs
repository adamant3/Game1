using System;
using System.Collections.Generic;
using Godot;
using DungeonCrawler.Generation;

namespace DungeonCrawler.Rooms
{
    /// <summary>
    /// A shop room. Spawns a Shopkeeper NPC and purchasable items.
    /// Clears immediately on entry since there are no enemies.
    /// </summary>
    public partial class ShopRoom : BaseRoom
    {
        public bool HasShopkeeper { get; private set; } = true;

        [Export] public int ItemCount { get; set; } = 3;
        [Export] public PackedScene? ShopkeeperScene { get; set; }
        [Export] public PackedScene? ShopItemScene { get; set; }

        /// <summary>Tracks items currently for sale: item ID, price, and sold state.</summary>
        private readonly List<(string itemId, int price, bool sold)> _shopInventory = new();

        private static readonly Random _rng = new Random();

        public override void Initialize(RoomData roomData)
        {
            _roomData = roomData;
            RoomType  = Generation.RoomType.Shop;
            SetupRoom();
        }

        public override void SetupRoom()
        {
            OpenDoors();

            // Spawn the shopkeeper NPC near the top of the room.
            if (ShopkeeperScene != null)
            {
                var keeper = ShopkeeperScene.Instantiate<Node2D>();
                var container = _interactableContainer ?? this;
                container.AddChild(keeper);
                keeper.Position = new Vector2(0f, -120f);
            }

            // Generate inventory and place item displays.
            int floorNumber = _roomData?.Id ?? 1;
            GenerateShopInventory(floorNumber);
            SpawnShopItems();
        }

        public override void OnRoomEntered(Node player)
        {
            // Shop rooms clear immediately – no combat required.
            if (!IsCleared)
            {
                IsCleared = true;
                Core.GameEvents.RaiseRoomCleared(_roomData?.UniqueId ?? Name);
            }
            GD.Print("[ShopRoom] Welcome to the shop! Browse the wares.");
        }

        public override void OnRoomCleared()
        {
            // Already handled in OnRoomEntered.
        }

        // ── Inventory generation ───────────────────────────────────────────────

        /// <summary>
        /// Picks random weapons/consumables/upgrades with prices scaled by floor number.
        /// </summary>
        public void GenerateShopInventory(int floorNumber)
        {
            _shopInventory.Clear();

            string[] possibleItems =
            {
                "weapon_pistol", "weapon_shotgun", "weapon_rifle",
                "consumable_health_potion", "consumable_mana_potion", "consumable_bomb",
                "upgrade_speed", "upgrade_damage", "upgrade_crit", "upgrade_armor"
            };

            float priceMultiplier = 1.0f + (floorNumber - 1) * 0.25f;

            for (int i = 0; i < ItemCount; i++)
            {
                string item  = possibleItems[_rng.Next(possibleItems.Length)];
                int basePrice = item.StartsWith("weapon") ? 15 : item.StartsWith("upgrade") ? 12 : 8;
                int price = Mathf.RoundToInt(basePrice * priceMultiplier);
                _shopInventory.Add((item, price, false));
            }

            GD.Print($"[ShopRoom] Generated {_shopInventory.Count} items for floor {floorNumber}.");
        }

        private void SpawnShopItems()
        {
            if (ShopItemScene == null) return;

            float spacing = 120f;
            float startX  = -spacing * (ItemCount - 1) / 2f;

            for (int i = 0; i < _shopInventory.Count; i++)
            {
                var (itemId, price, _) = _shopInventory[i];

                var display = ShopItemScene.Instantiate<Node2D>();
                var container = _interactableContainer ?? this;
                container.AddChild(display);
                display.Position = new Vector2(startX + spacing * i, 40f);

                // Pass item data to the display node if it supports it.
                if (display.HasMethod("SetItem"))
                    display.Call("SetItem", itemId, price);

                // Show a price label if the display has a Label child.
                var label = display.GetNodeOrNull<Label>("Label");
                if (label != null)
                    label.Text = $"{itemId}\n{price}g";
            }
        }

        public List<(string itemId, int price, bool sold)> GetInventory() => _shopInventory;
    }
}
