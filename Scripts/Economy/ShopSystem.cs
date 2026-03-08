using System;
using System.Collections.Generic;
using Godot;
using DungeonCrawler.Core;
using DungeonCrawler.Items;
using DungeonCrawler.Player;

namespace DungeonCrawler.Economy
{
    /// <summary>One item for sale in the shop.</summary>
    public class ShopEntry
    {
        public ItemBase Item  { get; set; }
        public int      Price { get; set; }
        public bool     Sold  { get; set; } = false;

        public ShopEntry(ItemBase item, int price)
        {
            Item  = item;
            Price = price;
        }
    }

    /// <summary>
    /// Manages the in-game shop: generates stock, handles purchases.
    /// </summary>
    public partial class ShopSystem : Node
    {
        // ── Signals ────────────────────────────────────────────────────────────
        [Signal] public delegate void ItemPurchasedEventHandler(string itemId);
        [Signal] public delegate void ShopRefreshedEventHandler();

        // ── State ──────────────────────────────────────────────────────────────
        private readonly List<ShopEntry> _stock = new();
        [Export] public int StockSize { get; set; } = 3;
        [Export] public int RerollCost { get; set; } = 5;

        private static readonly Random _rng = new();

        // ── Public API ─────────────────────────────────────────────────────────
        public void GenerateStock(int floor, float luck)
        {
            _stock.Clear();
            for (int i = 0; i < StockSize; i++)
            {
                var item  = GenerateRandomItem(floor, luck);
                int price = RollPrice(floor, luck);
                _stock.Add(new ShopEntry(item, price));
            }
            EmitSignal(SignalName.ShopRefreshed);
            GD.Print($"[Shop] Generated {_stock.Count} items.");
        }

        public bool Purchase(int index, PlayerController player)
        {
            if (index < 0 || index >= _stock.Count) return false;
            ShopEntry entry = _stock[index];
            if (entry.Sold) return false;
            if (!player.SpendCoins(entry.Price)) return false;

            bool added = player.Inventory.AddItem(entry.Item);
            if (!added)
            {
                // Refund if inventory full.
                player.AddCoins(entry.Price);
                GD.Print("[Shop] Inventory full — refunded.");
                return false;
            }

            entry.Sold = true;
            EmitSignal(SignalName.ItemPurchased, entry.Item.ItemId);
            GD.Print($"[Shop] Sold {entry.Item.ItemName} for {entry.Price} coins.");
            return true;
        }

        public bool Reroll(PlayerController player, int floor, float luck)
        {
            if (!player.SpendCoins(RerollCost)) return false;
            GenerateStock(floor, luck);
            return true;
        }

        public IReadOnlyList<ShopEntry> GetStock() => _stock.AsReadOnly();

        // ── Generation helpers ─────────────────────────────────────────────────
        private static ItemBase GenerateRandomItem(int floor, float luck)
        {
            int roll = _rng.Next(5);
            return roll switch
            {
                0 => new HealthUpItem(),
                1 => new SpeedBoostItem(),
                2 => new DamageUpItem(),
                3 => new CritEyeItem(),
                _ => new LuckyCharmItem()
            };
        }

        private static int RollPrice(int floor, float luck)
        {
            float luckDiscount = Math.Min(0.5f, luck * 0.01f);
            int   basePrice    = _rng.Next(Constants.SHOP_ITEM_COST_MIN, Constants.SHOP_ITEM_COST_MAX + 1);
            return (int)Math.Max(1, basePrice * (1f - luckDiscount));
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Chest drop system
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Chest that drops coins and/or an item when opened.</summary>
    public partial class Chest : Node2D, IInteractable
    {
        [Export] public bool IsOpened { get; private set; } = false;
        [Export] public bool HasItem  { get; set; } = true;
        [Export] public int  MinCoins { get; set; } = Constants.CHEST_COIN_MIN;
        [Export] public int  MaxCoins { get; set; } = Constants.CHEST_COIN_MAX;

        private static readonly Random _rng = new();

        public string InteractionPrompt => "Open Chest";
        public bool   CanInteract       => !IsOpened;

        public void Interact(Node interactor)
        {
            if (IsOpened || interactor is not PlayerController player) return;

            IsOpened = true;

            // Give coins.
            int coins = _rng.Next(MinCoins, MaxCoins + 1);
            player.AddCoins(coins);
            GD.Print($"[Chest] Gave {coins} coins.");

            // Drop an item with some probability.
            if (HasItem && _rng.NextDouble() < 0.6)
            {
                var item = GenerateRandomItem();
                item.GlobalPosition = GlobalPosition + new Vector2(0, 40f);
                GetParent().AddChild(item);
                GD.Print($"[Chest] Dropped {item.ItemName}.");
            }
        }

        private static ItemBase GenerateRandomItem()
        {
            int roll = _rng.Next(4);
            return roll switch
            {
                0 => new HealthUpItem(),
                1 => new SpeedBoostItem(),
                2 => new DamageUpItem(),
                _ => new CritEyeItem()
            };
        }
    }
}
