using System.Collections.Generic;
using Godot;
using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Rooms;
using DungeonCrawler.Enemies;

namespace DungeonCrawler.Dungeon
{
    /// <summary>
    /// Instantiates and connects all room nodes for a single floor.
    /// Attach to the main scene.
    /// </summary>
    public partial class DungeonManager : Node2D
    {
        // ── Signals ────────────────────────────────────────────────────────────
        [Signal] public delegate void FloorGeneratedEventHandler(int floorNumber);
        [Signal] public delegate void AllRoomsClearedEventHandler();

        // ── Configuration ──────────────────────────────────────────────────────
        [Export] public int CurrentFloor    { get; private set; } = 1;
        [Export] public int RoomsPerFloor   { get; set; } = 10;

        // ── State ──────────────────────────────────────────────────────────────
        private readonly List<RoomData> _roomData   = new();
        private readonly List<RoomNode> _roomNodes  = new();
        private RoomNode?               _activeRoom;
        private int                     _clearedCount = 0;

        // Room world-space spacing.
        private const float RoomSpacingX = Constants.ROOM_WIDTH  * Constants.TILE_SIZE + 200f;
        private const float RoomSpacingY = Constants.ROOM_HEIGHT * Constants.TILE_SIZE + 200f;

        // ── Godot lifecycle ────────────────────────────────────────────────────
        public override void _Ready()
        {
            GenerateFloor(CurrentFloor);
        }

        // ── Public API ─────────────────────────────────────────────────────────
        public void GenerateFloor(int floor)
        {
            CurrentFloor  = floor;
            _clearedCount = 0;

            // Clear old rooms.
            foreach (var node in _roomNodes)
                node.QueueFree();
            _roomNodes.Clear();
            _roomData.Clear();

            // Generate layout data.
            var data = DungeonGenerator.Generate(RoomsPerFloor, floor);
            _roomData.AddRange(data);

            // Instantiate room nodes.
            foreach (var rd in _roomData)
            {
                var node = CreateRoomNode(rd);
                _roomNodes.Add(node);
                if (rd.Type == RoomType.Start)
                    _activeRoom = node;
            }

            GameEvents.RaiseDungeonGenerated();
            EmitSignal(SignalName.FloorGenerated, floor);
            GD.Print($"[DungeonManager] Floor {floor} generated with {_roomData.Count} rooms.");
        }

        public void AdvanceFloor()
        {
            GameEvents.RaiseFloorCompleted(CurrentFloor);
            GenerateFloor(CurrentFloor + 1);
            GameEvents.RaiseFloorChanged(CurrentFloor);
        }

        // ── Room instantiation ─────────────────────────────────────────────────
        private RoomNode CreateRoomNode(RoomData data)
        {
            var node = new RoomNode
            {
                RoomId    = data.RoomId,
                HasDoorN  = data.HasDoorNorth,
                HasDoorS  = data.HasDoorSouth,
                HasDoorE  = data.HasDoorEast,
                HasDoorW  = data.HasDoorWest,
                Position  = new Vector2(data.GridPosition.X * RoomSpacingX,
                                        data.GridPosition.Y * RoomSpacingY)
            };
            AddChild(node);
            node.RoomCleared += OnRoomCleared;

            // Populate with enemies based on room type.
            if (data.Type == RoomType.Normal || data.Type == RoomType.Boss)
                PopulateRoom(node, data);

            return node;
        }

        private void PopulateRoom(RoomNode room, RoomData data)
        {
            int count = data.Type == RoomType.Boss ? 1
                      : (int)GD.RandRange(1, 4);

            var enemies = new List<EnemyBase>();
            for (int i = 0; i < count; i++)
                enemies.Add(SpawnEnemy(data.Type, CurrentFloor));

            room.SpawnEnemies(enemies);
        }

        private static EnemyBase SpawnEnemy(RoomType roomType, int floor)
        {
            if (roomType == RoomType.Boss)
                return new BossEnemy();

            int roll = (int)GD.RandRange(0, 3);
            return roll switch
            {
                0 => new MeleeEnemy(),
                1 => new RangedEnemy(),
                _ => new TankEnemy()
            };
        }

        // ── Events ─────────────────────────────────────────────────────────────
        private void OnRoomCleared(string roomId)
        {
            _clearedCount++;
            GD.Print($"[DungeonManager] Rooms cleared: {_clearedCount}/{_roomData.Count}");

            if (_clearedCount >= _roomData.Count)
            {
                EmitSignal(SignalName.AllRoomsCleared);
                GD.Print("[DungeonManager] All rooms cleared! Floor complete.");
            }
        }
    }
}
