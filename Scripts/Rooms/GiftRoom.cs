using Godot;
using DungeonCrawler.Generation;

namespace DungeonCrawler.Rooms
{
    /// <summary>
    /// A gift room containing one free chest. The room clears immediately on entry
    /// so the player can always access the chest without fighting.
    /// </summary>
    public partial class GiftRoom : BaseRoom
    {
        [Export] public PackedScene? ChestScene { get; set; }

        public override void Initialize(RoomData roomData)
        {
            _roomData = roomData;
            RoomType  = Generation.RoomType.Gift;
            SetupRoom();
        }

        public override void SetupRoom()
        {
            OpenDoors();

            // Spawn one chest at the room centre.
            if (ChestScene != null)
            {
                var chest = ChestScene.Instantiate<Node2D>();
                var container = _interactableContainer ?? this;
                container.AddChild(chest);
                chest.Position = Vector2.Zero;
                GD.Print("[GiftRoom] Chest spawned at room centre.");
            }
            else
            {
                // Fallback: create a coloured square as a placeholder chest.
                var placeholder = new ColorRect
                {
                    Name  = "ChestPlaceholder",
                    Color = new Color(0.8f, 0.6f, 0.1f),
                    Size  = new Vector2(40f, 40f)
                };
                placeholder.Position = new Vector2(-20f, -20f);
                AddChild(placeholder);
            }
        }

        public override void OnRoomEntered(Node player)
        {
            if (!IsCleared)
            {
                IsCleared = true;
                Core.GameEvents.RaiseRoomCleared(_roomData?.UniqueId ?? Name);
            }
            GD.Print("[GiftRoom] Free item! Open the chest to claim your reward.");
        }

        public override void OnRoomCleared()
        {
            // Nothing extra – room is pre-cleared on entry.
        }
    }
}
