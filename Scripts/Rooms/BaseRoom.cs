using System.Collections.Generic;
using Godot;
using DungeonCrawler.Generation;

namespace DungeonCrawler.Rooms
{
    /// <summary>
    /// Abstract base for all room types. Manages doors, enemies, items,
    /// and the room-cleared lifecycle.
    /// </summary>
    public abstract partial class BaseRoom : Node2D
    {
        // ── Signals ────────────────────────────────────────────────────────────
        [Signal] public delegate void RoomClearedEventHandler(int roomId);
        [Signal] public delegate void RoomEnteredEventHandler(Node player);

        // ── Exports ────────────────────────────────────────────────────────────
        [Export] public Generation.RoomType RoomType { get; set; } = Generation.RoomType.Regular;
        [Export] public bool IsStartRoom { get; set; } = false;

        // ── State ──────────────────────────────────────────────────────────────
        public bool IsCleared { get; protected set; } = false;
        public bool IsLocked { get; protected set; } = false;
        public bool HasBeenVisited { get; protected set; } = false;

        // ── Runtime data ───────────────────────────────────────────────────────
        protected RoomData? _roomData;
        protected List<Node2D> _enemies = new List<Node2D>();
        protected List<Node2D> _items = new List<Node2D>();

        // ── Child node references (assigned in _Ready or SetupRoom) ────────────
        protected Node2D? _enemyContainer;
        protected Node2D? _itemContainer;
        protected Node2D? _interactableContainer;
        protected TileMapLayer? _tileMap;

        // Doors: index 0=North, 1=South, 2=East, 3=West.
        protected Node2D[] _doors = new Node2D[4];

        // ── Godot lifecycle ────────────────────────────────────────────────────
        public override void _Ready()
        {
            _enemyContainer        = GetNodeOrNull<Node2D>("EnemyContainer");
            _itemContainer         = GetNodeOrNull<Node2D>("ItemContainer");
            _interactableContainer = GetNodeOrNull<Node2D>("InteractableContainer");
            _tileMap               = GetNodeOrNull<TileMapLayer>("TileMap");

            _doors[0] = GetNodeOrNull<Node2D>("DoorNorth");
            _doors[1] = GetNodeOrNull<Node2D>("DoorSouth");
            _doors[2] = GetNodeOrNull<Node2D>("DoorEast");
            _doors[3] = GetNodeOrNull<Node2D>("DoorWest");
        }

        public override void _Process(double delta)
        {
            if (IsLocked)
                CheckClearCondition();
        }

        // ── Abstract interface ─────────────────────────────────────────────────

        /// <summary>Called once when the room is first constructed with its data.</summary>
        public abstract void Initialize(RoomData roomData);

        /// <summary>Called every time the player enters this room.</summary>
        public abstract void OnRoomEntered(Node player);

        /// <summary>Called when the room's clear condition is satisfied.</summary>
        public abstract void OnRoomCleared();

        // ── Virtual setup hook ─────────────────────────────────────────────────

        /// <summary>Override in subclasses to place enemies, items, and interactables.</summary>
        public virtual void SetupRoom() { }

        // ── Door control ───────────────────────────────────────────────────────

        public void LockDoors()
        {
            IsLocked = true;
            foreach (var door in _doors)
            {
                if (door == null) continue;
                door.SetMeta("locked", true);
                // Visually indicate locked state if the door has a Sprite2D child.
                var sprite = door.GetNodeOrNull<Sprite2D>("Sprite2D");
                if (sprite != null)
                    sprite.Modulate = new Color(0.6f, 0.1f, 0.1f);
            }
        }

        public void UnlockDoors()
        {
            IsLocked = false;
            foreach (var door in _doors)
            {
                if (door == null) continue;
                door.SetMeta("locked", false);
                var sprite = door.GetNodeOrNull<Sprite2D>("Sprite2D");
                if (sprite != null)
                    sprite.Modulate = Colors.White;
            }
        }

        public void OpenDoors()
        {
            IsLocked = false;
            foreach (var door in _doors)
            {
                if (door == null) continue;
                door.SetMeta("open", true);
                door.SetMeta("locked", false);
                var sprite = door.GetNodeOrNull<Sprite2D>("Sprite2D");
                if (sprite != null)
                    sprite.Modulate = Colors.Green;
            }
        }

        // ── Clear condition ────────────────────────────────────────────────────

        /// <summary>
        /// Checks whether all enemies are dead. If so, triggers OnRoomCleared.
        /// </summary>
        public void CheckClearCondition()
        {
            // Remove freed nodes from the tracking list.
            _enemies.RemoveAll(e => !IsInstanceValid(e));
            if (_enemies.Count == 0 && IsLocked && !IsCleared)
                TriggerRoomCleared();
        }

        protected void TriggerRoomCleared()
        {
            IsCleared = true;
            UnlockDoors();
            OnRoomCleared();
            EmitSignal(SignalName.RoomCleared, _roomData?.Id ?? 0);
            Core.GameEvents.RaiseRoomCleared(_roomData?.UniqueId ?? Name);
            GD.Print($"[BaseRoom] Room '{Name}' cleared.");
        }

        // ── Enemy / item spawning helpers ──────────────────────────────────────

        public void SpawnEnemy(PackedScene enemyScene, Vector2 position)
        {
            if (enemyScene == null) return;
            var enemy = enemyScene.Instantiate<Node2D>();
            var container = _enemyContainer ?? this;
            container.AddChild(enemy);
            enemy.GlobalPosition = position;
            _enemies.Add(enemy);
        }

        public void SpawnItem(PackedScene itemScene, Vector2 position)
        {
            if (itemScene == null) return;
            var item = itemScene.Instantiate<Node2D>();
            var container = _itemContainer ?? this;
            container.AddChild(item);
            item.GlobalPosition = position;
            _items.Add(item);
        }

        // ── Player entry ───────────────────────────────────────────────────────

        /// <summary>
        /// Should be called by the dungeon manager when the player transitions into
        /// this room. Marks the room visited and fires the signal.
        /// </summary>
        public void PlayerEntered(Node player)
        {
            HasBeenVisited = true;
            OnRoomEntered(player);
            EmitSignal(SignalName.RoomEntered, player);
        }
    }
}
