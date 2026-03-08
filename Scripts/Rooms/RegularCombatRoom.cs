using System;
using Godot;
using DungeonCrawler.Generation;
using DungeonCrawler.Economy;

namespace DungeonCrawler.Rooms
{
    /// <summary>
    /// A standard combat room. Doors lock on entry and unlock when all enemies
    /// are defeated. Rewards the player with coins, mana, and occasionally an item.
    /// </summary>
    public partial class RegularCombatRoom : BaseRoom
    {
        [Export] public int MinEnemies { get; set; } = 2;
        [Export] public int MaxEnemies { get; set; } = 5;
        [Export] public bool HasBreakableCrates { get; set; } = true;

        [Export] public PackedScene? EnemyScene { get; set; }
        [Export] public PackedScene? CrateScene { get; set; }
        [Export] public PackedScene? CoinScene { get; set; }
        [Export] public PackedScene? ItemScene { get; set; }

        private static readonly Random _rng = new Random();

        // Half-room dimensions in pixels (matches Constants.ROOM_WIDTH/HEIGHT * TILE_SIZE / 2).
        private const float HalfW = 400f;
        private const float HalfH = 400f;

        public override void Initialize(RoomData roomData)
        {
            _roomData = roomData;
            RoomType  = Generation.RoomType.Regular;
            SetupRoom();
        }

        public override void SetupRoom()
        {
            if (HasBreakableCrates)
                SpawnCrates();

            SpawnEnemies();
        }

        public override void OnRoomEntered(Node player)
        {
            if (IsCleared) return;
            if (_enemies.Count > 0)
                LockDoors();
        }

        public override void OnRoomCleared()
        {
            SpawnRewards();
            GD.Print("[RegularCombatRoom] Room cleared – rewards spawned.");
        }

        // ── Enemy spawning ─────────────────────────────────────────────────────

        private void SpawnEnemies()
        {
            if (EnemyScene == null) return;
            int count = _rng.Next(MinEnemies, MaxEnemies + 1);
            for (int i = 0; i < count; i++)
            {
                Vector2 pos = GetRandomRoomPosition(excludeCenter: true);
                SpawnEnemy(EnemyScene, pos);
            }
        }

        // ── Reward spawning ────────────────────────────────────────────────────

        private void SpawnRewards()
        {
            // Always spawn 2-4 coin pickups scattered in the room.
            int coinCount = _rng.Next(2, 5);
            for (int i = 0; i < coinCount; i++)
            {
                if (CoinScene == null) continue;
                var coin = CoinScene.Instantiate<Node2D>();
                var container = _itemContainer ?? this;
                container.AddChild(coin);
                coin.GlobalPosition = GetRandomRoomPosition(excludeCenter: false);

                // Set coin amount if the node exposes it.
                if (coin.HasMethod("Initialize"))
                    coin.Call("Initialize", _rng.Next(1, 4));
            }

            // Spawn a mana pickup (represented as a special coin with higher value).
            if (CoinScene != null && _rng.NextDouble() < 0.5)
            {
                var mana = CoinScene.Instantiate<Node2D>();
                var container = _itemContainer ?? this;
                container.AddChild(mana);
                mana.GlobalPosition = GetRandomRoomPosition(excludeCenter: false);
                if (mana.HasMethod("Initialize"))
                    mana.Call("Initialize", 5);
            }

            // Small chance (15%) for a bonus item drop.
            if (ItemScene != null && _rng.NextDouble() < 0.15)
                SpawnItem(ItemScene, GlobalPosition);
        }

        // ── Crate spawning ─────────────────────────────────────────────────────

        private void SpawnCrates()
        {
            if (CrateScene == null) return;
            int count = _rng.Next(2, 5);
            for (int i = 0; i < count; i++)
            {
                var crate = CrateScene.Instantiate<Node2D>();
                var container = _itemContainer ?? this;
                container.AddChild(crate);
                // Keep crates away from the center spawn and near the edges.
                crate.Position = GetRandomRoomPosition(excludeCenter: true);
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private Vector2 GetRandomRoomPosition(bool excludeCenter)
        {
            const float margin = 64f;
            const float centerExcludeRadius = 80f;

            Vector2 pos;
            int attempts = 0;
            do
            {
                float x = (float)(_rng.NextDouble() * (HalfW * 2f - margin * 2f) - (HalfW - margin));
                float y = (float)(_rng.NextDouble() * (HalfH * 2f - margin * 2f) - (HalfH - margin));
                pos = new Vector2(x, y);
                attempts++;
            }
            while (excludeCenter && pos.Length() < centerExcludeRadius && attempts < 20);

            return GlobalPosition + pos;
        }
    }
}
