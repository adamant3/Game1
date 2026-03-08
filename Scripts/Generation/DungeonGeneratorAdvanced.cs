using System;
using System.Collections.Generic;
using Godot;

namespace DungeonCrawler.Generation
{
    /// <summary>
    /// Advanced procedural dungeon generator using a drunk-walk algorithm.
    /// Produces a FloorData with typed rooms connected via a grid graph.
    /// </summary>
    public class DungeonGeneratorAdvanced
    {
        private const int MIN_ROOMS = 10;
        private const int MAX_ROOMS = 30;
        private const int GRID_SIZE = 10;

        // 0 = empty, 1 = room, 2 = start, 3 = portal
        private int[,] _grid = new int[GRID_SIZE, GRID_SIZE];
        private List<RoomData> _rooms = new List<RoomData>();
        private Random _rng = new Random();

        // ── Public entry point ─────────────────────────────────────────────────

        public FloorData Generate(int floorNumber, int seed)
        {
            _rng = new Random(seed);
            ResetState();

            int targetRooms = _rng.Next(MIN_ROOMS, MAX_ROOMS + 1);
            int centerX = GRID_SIZE / 2;
            int centerY = GRID_SIZE / 2;

            // Place the starting room at the grid center.
            PlaceRoom(centerX, centerY, RoomType.Start, 0);
            _grid[centerX, centerY] = 2;

            int curX = centerX;
            int curY = centerY;
            int attempts = 0;

            // Drunk-walk until we reach the target room count.
            while (_rooms.Count < targetRooms && attempts < 1000)
            {
                attempts++;
                var adjacent = GetAdjacentCells(curX, curY);
                if (adjacent.Count == 0)
                {
                    // Jump to a random existing room.
                    var randomRoom = _rooms[_rng.Next(_rooms.Count)];
                    curX = randomRoom.GridX;
                    curY = randomRoom.GridY;
                    continue;
                }

                var next = adjacent[_rng.Next(adjacent.Count)];
                int nx = next.X;
                int ny = next.Y;

                if (_grid[nx, ny] == 0)
                {
                    // Temporarily assign Regular; AssignRoomTypes finalises types later.
                    PlaceRoom(nx, ny, RoomType.Regular, _rooms.Count);
                    _grid[nx, ny] = 1;
                }

                // Occasionally jump to a random existing room to create branching.
                if (_rng.NextDouble() < 0.35)
                {
                    var r = _rooms[_rng.Next(_rooms.Count)];
                    curX = r.GridX;
                    curY = r.GridY;
                }
                else
                {
                    curX = nx;
                    curY = ny;
                }
            }

            var floor = new FloorData { FloorNumber = floorNumber };
            floor.Rooms.AddRange(_rooms);

            AssignRoomTypes(floor, floorNumber);
            ConnectRooms(floor);

            GD.Print($"[DungeonGeneratorAdvanced] Floor {floorNumber}: {floor.TotalRooms} rooms generated (seed={seed}).");
            return floor;
        }

        // ── Room type assignment ───────────────────────────────────────────────

        public void AssignRoomTypes(FloorData floor, int floorNumber)
        {
            if (floor.Rooms.Count == 0) return;

            // The first room is always Start.
            RoomData startRoom = floor.Rooms[0];
            startRoom.Type = RoomType.Start;
            floor.StartRoomId = startRoom.Id;
            _grid[startRoom.GridX, startRoom.GridY] = 2;

            // Portal goes to the room farthest from start.
            RoomData portalRoom = FindFarthestRoom(startRoom, floor.Rooms);
            portalRoom.Type = RoomType.Portal;
            floor.PortalRoomId = portalRoom.Id;
            _grid[portalRoom.GridX, portalRoom.GridY] = 3;

            // Collect rooms that are still unassigned (Regular).
            var unassigned = floor.Rooms.FindAll(r =>
                r.Id != startRoom.Id && r.Id != portalRoom.Id);

            Shuffle(unassigned);

            int idx = 0;
            int total = unassigned.Count;

            // 1-2 Shop rooms.
            int shopCount = (total >= 6) ? _rng.Next(1, 3) : 1;
            for (int i = 0; i < shopCount && idx < total; i++, idx++)
                unassigned[idx].Type = RoomType.Shop;

            // 2-4 Special Combat rooms.
            int specialCount = (total >= 8) ? _rng.Next(2, 5) : 2;
            for (int i = 0; i < specialCount && idx < total; i++, idx++)
                unassigned[idx].Type = RoomType.SpecialCombat;

            // 1-2 Gift rooms.
            int giftCount = (total >= 5) ? _rng.Next(1, 3) : 1;
            for (int i = 0; i < giftCount && idx < total; i++, idx++)
                unassigned[idx].Type = RoomType.Gift;

            // 1 Misc room.
            if (idx < total)
            {
                unassigned[idx].Type = RoomType.Misc;
                idx++;
            }

            // Everything else becomes Regular Combat.
            for (; idx < total; idx++)
                unassigned[idx].Type = RoomType.Regular;
        }

        // ── Pathfinding helper ─────────────────────────────────────────────────

        /// <summary>
        /// BFS from <paramref name="start"/> to find the room in <paramref name="rooms"/>
        /// with the greatest graph distance.
        /// </summary>
        public RoomData FindFarthestRoom(RoomData start, List<RoomData> rooms)
        {
            // Build a quick lookup: UniqueId -> RoomData.
            var lookup = new Dictionary<string, RoomData>();
            foreach (var r in rooms)
                lookup[r.UniqueId] = r;

            var visited = new HashSet<string>();
            var queue = new Queue<(RoomData room, int dist)>();
            queue.Enqueue((start, 0));
            visited.Add(start.UniqueId);

            RoomData farthest = start;
            int maxDist = 0;

            while (queue.Count > 0)
            {
                var (current, dist) = queue.Dequeue();
                if (dist > maxDist)
                {
                    maxDist = dist;
                    farthest = current;
                }

                // Walk to connected neighbours that exist in the floor.
                foreach (int neighborId in current.ConnectedRoomIds)
                {
                    var neighbor = rooms.Find(r => r.Id == neighborId);
                    if (neighbor == null) continue;
                    if (visited.Contains(neighbor.UniqueId)) continue;
                    visited.Add(neighbor.UniqueId);
                    queue.Enqueue((neighbor, dist + 1));
                }

                // Also check grid adjacency (connections may not be wired yet during type assignment).
                foreach (var adj in GetAdjacentCells(current.GridX, current.GridY))
                {
                    if (_grid[adj.X, adj.Y] == 0) continue;
                    var adjRoom = rooms.Find(r => r.GridX == adj.X && r.GridY == adj.Y);
                    if (adjRoom == null) continue;
                    if (visited.Contains(adjRoom.UniqueId)) continue;
                    visited.Add(adjRoom.UniqueId);
                    queue.Enqueue((adjRoom, dist + 1));
                }
            }

            return farthest;
        }

        // ── Grid helpers ───────────────────────────────────────────────────────

        /// <summary>Returns the four cardinal neighbours of (x, y) that are in bounds.</summary>
        public List<Vector2I> GetAdjacentCells(int x, int y)
        {
            var result = new List<Vector2I>(4);
            if (x > 0)              result.Add(new Vector2I(x - 1, y));
            if (x < GRID_SIZE - 1)  result.Add(new Vector2I(x + 1, y));
            if (y > 0)              result.Add(new Vector2I(x, y - 1));
            if (y < GRID_SIZE - 1)  result.Add(new Vector2I(x, y + 1));
            return result;
        }

        /// <summary>Connects each room to every adjacent room that exists in the grid.</summary>
        public void ConnectRooms(FloorData floor)
        {
            foreach (var room in floor.Rooms)
            {
                room.ConnectedRoomIds.Clear();
                foreach (var adj in GetAdjacentCells(room.GridX, room.GridY))
                {
                    if (_grid[adj.X, adj.Y] == 0) continue;
                    var neighbor = floor.GetRoomAtGrid(adj.X, adj.Y);
                    if (neighbor == null) continue;
                    if (!room.ConnectedRoomIds.Contains(neighbor.Id))
                        room.ConnectedRoomIds.Add(neighbor.Id);
                }
            }
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private void ResetState()
        {
            _grid = new int[GRID_SIZE, GRID_SIZE];
            _rooms = new List<RoomData>();
        }

        private void PlaceRoom(int x, int y, RoomType type, int id)
        {
            _rooms.Add(new RoomData(id, type, x, y));
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
