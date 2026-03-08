using System;
using Godot;
using DungeonCrawler.Generation;

namespace DungeonCrawler.Rooms
{
    /// <summary>
    /// A harder combat room featuring a mini-boss flanked by support enemies.
    /// Rewards are doubled compared to a regular room.
    /// </summary>
    public partial class SpecialCombatRoom : BaseRoom
    {
        [Export] public PackedScene? MiniBossScene { get; set; }
        [Export] public PackedScene? SupportEnemyScene { get; set; }
        [Export] public PackedScene? CoinScene { get; set; }
        [Export] public PackedScene? ConsumableScene { get; set; }
        [Export] public int MinSupportEnemies { get; set; } = 1;
        [Export] public int MaxSupportEnemies { get; set; } = 2;

        private static readonly Random _rng = new Random();

        public override void Initialize(RoomData roomData)
        {
            _roomData = roomData;
            RoomType  = Generation.RoomType.SpecialCombat;
            SetupRoom();
        }

        public override void SetupRoom()
        {
            SpawnMiniBoss();
            SpawnSupportEnemies();
        }

        public override void OnRoomEntered(Node player)
        {
            if (IsCleared) return;
            if (_enemies.Count > 0)
                LockDoors();
        }

        public override void OnRoomCleared()
        {
            SpawnDoubleReward();
            GD.Print("[SpecialCombatRoom] Mini-boss defeated – double rewards spawned.");
        }

        // ── Spawning ───────────────────────────────────────────────────────────

        private void SpawnMiniBoss()
        {
            if (MiniBossScene == null) return;
            // Mini-boss spawns at room center.
            SpawnEnemy(MiniBossScene, GlobalPosition);
        }

        private void SpawnSupportEnemies()
        {
            if (SupportEnemyScene == null) return;
            int count = _rng.Next(MinSupportEnemies, MaxSupportEnemies + 1);
            float[] angles = { -45f, 45f };
            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.DegToRad(angles[i % angles.Length]);
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 120f;
                SpawnEnemy(SupportEnemyScene, GlobalPosition + offset);
            }
        }

        // ── Rewards ────────────────────────────────────────────────────────────

        private void SpawnDoubleReward()
        {
            // Double coin reward: 4-8 coins dropped.
            int coinCount = _rng.Next(4, 9);
            for (int i = 0; i < coinCount; i++)
            {
                if (CoinScene == null) continue;
                var coin = CoinScene.Instantiate<Node2D>();
                var container = _itemContainer ?? this;
                container.AddChild(coin);

                float angle = (float)(_rng.NextDouble() * Math.PI * 2.0);
                float radius = (float)(_rng.NextDouble() * 100.0 + 40.0);
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                coin.GlobalPosition = GlobalPosition + offset;

                if (coin.HasMethod("Initialize"))
                    coin.Call("Initialize", _rng.Next(1, 4));
            }

            // Always drop a random consumable.
            if (ConsumableScene != null)
            {
                var consumable = ConsumableScene.Instantiate<Node2D>();
                var container  = _itemContainer ?? this;
                container.AddChild(consumable);
                consumable.GlobalPosition = GlobalPosition + new Vector2(0f, 60f);
            }
        }
    }
}
