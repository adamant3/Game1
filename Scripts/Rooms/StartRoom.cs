using System;
using Godot;
using DungeonCrawler.Generation;

namespace DungeonCrawler.Rooms
{
    /// <summary>
    /// The first room the player spawns in on each floor.
    /// Always cleared immediately; has a 50% chance to spawn the Gambler NPC.
    /// </summary>
    public partial class StartRoom : BaseRoom
    {
        private static readonly Random _rng = new Random();

        /// <summary>Exported PackedScene for the Gambler NPC.</summary>
        [Export] public PackedScene? GamblerScene { get; set; }

        public override void Initialize(RoomData roomData)
        {
            _roomData   = roomData;
            RoomType    = Generation.RoomType.Start;
            IsStartRoom = true;

            SetupRoom();
        }

        public override void SetupRoom()
        {
            // Start room has no enemies and doors are always open.
            IsCleared = true;
            OpenDoors();

            // 50% chance to spawn the Gambler NPC.
            if (_rng.NextDouble() < 0.5 && GamblerScene != null)
            {
                var gambler = GamblerScene.Instantiate<Node2D>();
                var container = _interactableContainer ?? this;
                container.AddChild(gambler);
                // Place the gambler slightly off-center.
                gambler.Position = new Vector2(80f, 0f);
                GD.Print("[StartRoom] Gambler NPC spawned.");
            }
        }

        public override void OnRoomEntered(Node player)
        {
            // Show a brief floor-entry hint via the global event bus.
            if (_roomData != null)
                GD.Print($"[StartRoom] Entered floor {_roomData.Id}. Find the portal to advance!");
        }

        public override void OnRoomCleared()
        {
            // Start room is pre-cleared; nothing special to do here.
        }
    }
}
