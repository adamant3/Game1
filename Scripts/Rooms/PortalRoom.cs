using Godot;
using DungeonCrawler.Generation;
using DungeonCrawler.Managers;

namespace DungeonCrawler.Rooms
{
    /// <summary>
    /// The portal room leads to the next floor.
    /// Clears immediately on entry; the portal triggers floor advancement on interact.
    /// </summary>
    public partial class PortalRoom : BaseRoom
    {
        [Export] public PackedScene? PortalScene { get; set; }

        public override void Initialize(RoomData roomData)
        {
            _roomData = roomData;
            RoomType  = Generation.RoomType.Portal;
            SetupRoom();
        }

        public override void SetupRoom()
        {
            OpenDoors();

            if (PortalScene != null)
            {
                var portal = PortalScene.Instantiate<Node2D>();
                var container = _interactableContainer ?? this;
                container.AddChild(portal);
                portal.Position = Vector2.Zero;

                // Connect the portal's interact signal if it exposes one.
                if (portal.HasSignal("Interacted"))
                    portal.Connect("Interacted", new Callable(this, nameof(OnPortalActivated)));
            }
            else
            {
                // Fallback placeholder portal.
                var glow = new ColorRect
                {
                    Name  = "PortalPlaceholder",
                    Color = new Color(0.3f, 0.0f, 0.9f, 0.8f),
                    Size  = new Vector2(60f, 60f)
                };
                glow.Position = new Vector2(-30f, -30f);
                AddChild(glow);
            }
        }

        public override void OnRoomEntered(Node player)
        {
            if (!IsCleared)
            {
                IsCleared = true;
                Core.GameEvents.RaiseRoomCleared(_roomData?.UniqueId ?? Name);
            }
            GD.Print("[PortalRoom] The portal awaits. Interact to advance to the next floor.");
        }

        public override void OnRoomCleared()
        {
            // Pre-cleared on entry.
        }

        // ── Portal interaction ─────────────────────────────────────────────────

        private void OnPortalActivated()
        {
            GD.Print("[PortalRoom] Portal activated – loading next floor.");

            // Raise the floor-completed event; GameManager listens and calls AdvanceFloor.
            if (_roomData != null)
                Core.GameEvents.RaiseFloorCompleted(_roomData.Id);
        }
    }
}
