using System.Collections.Generic;
using Godot;
using DungeonCrawler.Core;
using DungeonCrawler.Enemies;
using DungeonCrawler.Items;

namespace DungeonCrawler.Rooms
{
    public enum RoomState { Locked, Active, Cleared }

    /// <summary>
    /// Runtime node for a single room. Manages enemies, doors, and clear logic.
    /// </summary>
    public partial class RoomNode : Node2D
    {
        // ── Configuration ──────────────────────────────────────────────────────
        [Export] public string RoomId      { get; set; } = "";
        [Export] public bool   HasDoorN    { get; set; }
        [Export] public bool   HasDoorS    { get; set; }
        [Export] public bool   HasDoorE    { get; set; }
        [Export] public bool   HasDoorW    { get; set; }

        // ── Signals ────────────────────────────────────────────────────────────
        [Signal] public delegate void RoomClearedEventHandler(string roomId);

        // ── State ──────────────────────────────────────────────────────────────
        public RoomState State { get; private set; } = RoomState.Active;

        private readonly List<EnemyBase> _enemies = new();
        private readonly List<Node2D>    _doorNodes = new();

        // ── Room dimensions ────────────────────────────────────────────────────
        private const int HalfW = Constants.ROOM_WIDTH  / 2 * Constants.TILE_SIZE;
        private const int HalfH = Constants.ROOM_HEIGHT / 2 * Constants.TILE_SIZE;

        // ── Godot lifecycle ────────────────────────────────────────────────────
        public override void _Ready()
        {
            BuildWalls();
            BuildDoors();
            // Subscribe to each spawned enemy's death signal.
            foreach (Node child in GetChildren())
            {
                if (child is EnemyBase enemy)
                    RegisterEnemy(enemy);
            }
        }

        public override void _Process(double delta)
        {
            if (State == RoomState.Active && _enemies.Count == 0)
                OnAllEnemiesDefeated();
        }

        // ── Public API ─────────────────────────────────────────────────────────
        public void SpawnEnemies(System.Collections.Generic.IEnumerable<EnemyBase> enemies)
        {
            foreach (var e in enemies)
            {
                AddChild(e);
                e.GlobalPosition = GetRandomSpawnPoint();
                RegisterEnemy(e);
            }
            LockDoors();
        }

        public void OnPlayerEntered()
        {
            if (State == RoomState.Cleared) return;
            GameEvents.RaiseRoomEntered(RoomId);
            if (_enemies.Count > 0)
                LockDoors();
        }

        // ── Enemies ────────────────────────────────────────────────────────────
        private void RegisterEnemy(EnemyBase enemy)
        {
            _enemies.Add(enemy);
            enemy.EnemyDied += OnEnemyDied;
        }

        private void OnEnemyDied(Node enemy)
        {
            if (enemy is EnemyBase e)
                _enemies.Remove(e);
        }

        private void OnAllEnemiesDefeated()
        {
            State = RoomState.Cleared;
            UnlockDoors();
            EmitSignal(SignalName.RoomCleared, RoomId);
            GameEvents.RaiseRoomCleared(RoomId);
            GD.Print($"[Room] {RoomId} cleared!");
        }

        // ── Walls & Doors ──────────────────────────────────────────────────────
        private void BuildWalls()
        {
            // Create a static body for each side wall.
            CreateWall(new Vector2(0, -HalfH), new Vector2(HalfW * 2, Constants.TILE_SIZE)); // top
            CreateWall(new Vector2(0,  HalfH), new Vector2(HalfW * 2, Constants.TILE_SIZE)); // bottom
            CreateWall(new Vector2(-HalfW, 0), new Vector2(Constants.TILE_SIZE, HalfH * 2)); // left
            CreateWall(new Vector2( HalfW, 0), new Vector2(Constants.TILE_SIZE, HalfH * 2)); // right
        }

        private void CreateWall(Vector2 pos, Vector2 size)
        {
            var body    = new StaticBody2D();
            var shape   = new CollisionShape2D();
            var rect    = new RectangleShape2D { Size = size };
            shape.Shape = rect;
            body.AddChild(shape);
            body.AddToGroup(Constants.TAG_WALL);
            body.CollisionLayer = Constants.MASK_WALL;
            body.GlobalPosition = GlobalPosition + pos;
            AddChild(body);
        }

        private void BuildDoors()
        {
            if (HasDoorN) CreateDoor(new Vector2(0, -HalfH));
            if (HasDoorS) CreateDoor(new Vector2(0,  HalfH));
            if (HasDoorE) CreateDoor(new Vector2( HalfW, 0));
            if (HasDoorW) CreateDoor(new Vector2(-HalfW, 0));
        }

        private void CreateDoor(Vector2 localPos)
        {
            var door = new ColorRect
            {
                Color    = Colors.DarkGoldenrod,
                Size     = new Vector2(Constants.TILE_SIZE * 2, Constants.TILE_SIZE * 2),
                Position = GlobalPosition + localPos - new Vector2(Constants.TILE_SIZE, Constants.TILE_SIZE)
            };
            AddChild(door);
            _doorNodes.Add(door);
        }

        private void LockDoors()
        {
            foreach (var d in _doorNodes)
                d.Modulate = Colors.Red;
        }

        private void UnlockDoors()
        {
            foreach (var d in _doorNodes)
                d.Modulate = Colors.White;
        }

        // ── Utility ────────────────────────────────────────────────────────────
        private Vector2 GetRandomSpawnPoint()
        {
            float x = (float)GD.RandRange(-HalfW * 0.7, HalfW * 0.7);
            float y = (float)GD.RandRange(-HalfH * 0.7, HalfH * 0.7);
            return GlobalPosition + new Vector2(x, y);
        }
    }
}
