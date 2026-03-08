using System.Collections.Generic;

namespace DungeonCrawler.Generation
{
    public enum RoomType
    {
        Start,
        Regular,
        SpecialCombat,
        Shop,
        Gift,
        Misc,
        Portal,
        Secret
    }

    public class RoomData
    {
        public int Id { get; set; }
        public RoomType Type { get; set; }
        public int GridX { get; set; }
        public int GridY { get; set; }
        public List<int> ConnectedRoomIds { get; set; } = new List<int>();
        public bool IsCleared { get; set; } = false;
        public bool IsVisited { get; set; } = false;
        public int TemplateIndex { get; set; } = 0;
        public string UniqueId => $"room_{GridX}_{GridY}";

        public RoomData(int id, RoomType type, int gridX, int gridY)
        {
            Id = id;
            Type = type;
            GridX = gridX;
            GridY = gridY;
        }
    }

    public class FloorData
    {
        public int FloorNumber { get; set; }
        public List<RoomData> Rooms { get; set; } = new List<RoomData>();
        public int StartRoomId { get; set; }
        public int PortalRoomId { get; set; }
        public int TotalRooms => Rooms.Count;
        public int ClearedRooms => Rooms.FindAll(r => r.IsCleared).Count;

        // Portal room doesn't count toward completion.
        public bool IsComplete => ClearedRooms >= TotalRooms - 1;

        public RoomData? GetRoom(int id) => Rooms.Find(r => r.Id == id);
        public RoomData? GetRoomAtGrid(int x, int y) => Rooms.Find(r => r.GridX == x && r.GridY == y);
        public List<RoomData> GetRoomsByType(RoomType type) => Rooms.FindAll(r => r.Type == type);
    }
}
