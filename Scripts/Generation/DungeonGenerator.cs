using System;
using System.Collections.Generic;
using Godot;
using DungeonCrawler.Core;

namespace DungeonCrawler.Dungeon
{
    public enum RoomType { Normal, Start, Boss, Item, Shop, Secret }

    /// <summary>Data representation of one room in the dungeon graph.</summary>
    public class RoomData
    {
        public string   RoomId       { get; set; } = Guid.NewGuid().ToString();
        public RoomType Type         { get; set; } = RoomType.Normal;
        public Vector2I GridPosition { get; set; }
        public bool     IsCleared    { get; set; } = false;
        public bool     IsVisited    { get; set; } = false;
        public List<Vector2I> Neighbors { get; set; } = new();

        // North=up, South=down, East=right, West=left
        public bool HasDoorNorth { get; set; } = false;
        public bool HasDoorSouth { get; set; } = false;
        public bool HasDoorEast  { get; set; } = false;
        public bool HasDoorWest  { get; set; } = false;

        public RoomData(Vector2I gridPos, RoomType type = RoomType.Normal)
        {
            GridPosition = gridPos;
            Type         = type;
        }
    }

    /// <summary>
    /// Procedurally generates a floor layout using a simple drunk-walk / BSP hybrid.
    /// Call Generate() to get a new layout.
    /// </summary>
    public static class DungeonGenerator
    {
        private static readonly Random _rng = new();

        public static List<RoomData> Generate(int targetRoomCount, int floor)
        {
            targetRoomCount = Math.Clamp(targetRoomCount,
                Constants.MIN_ROOMS, Constants.MAX_ROOMS);

            var roomMap = new Dictionary<Vector2I, RoomData>();
            var positions = new List<Vector2I>();

            // Drunk-walk to place rooms.
            Vector2I current = Vector2I.Zero;
            roomMap[current] = new RoomData(current, RoomType.Start);
            positions.Add(current);

            int attempts = 0;
            while (positions.Count < targetRoomCount && attempts < 500)
            {
                attempts++;
                Vector2I next = current + GetRandomCardinal();
                if (!roomMap.ContainsKey(next))
                {
                    var type = RollRoomType(positions.Count, targetRoomCount, floor);
                    roomMap[next] = new RoomData(next, type);
                    positions.Add(next);
                }
                // Random walk: sometimes jump back to a random existing room.
                if (_rng.NextDouble() < 0.35)
                    current = positions[_rng.Next(positions.Count)];
                else
                    current = next;
            }

            // Force a boss room at the end.
            if (floor % Constants.BOSS_FLOOR_INTERVAL == 0)
            {
                // Replace the furthest room from start with a boss room.
                Vector2I farthest  = FindFarthest(positions, Vector2I.Zero);
                roomMap[farthest].Type = RoomType.Boss;
            }

            // Wire up doors between adjacent rooms.
            foreach (var kvp in roomMap)
            {
                var pos  = kvp.Key;
                var room = kvp.Value;

                if (roomMap.ContainsKey(pos + Vector2I.Up))
                {
                    room.HasDoorNorth = true;
                    room.Neighbors.Add(pos + Vector2I.Up);
                }
                if (roomMap.ContainsKey(pos + Vector2I.Down))
                {
                    room.HasDoorSouth = true;
                    room.Neighbors.Add(pos + Vector2I.Down);
                }
                if (roomMap.ContainsKey(pos + Vector2I.Right))
                {
                    room.HasDoorEast = true;
                    room.Neighbors.Add(pos + Vector2I.Right);
                }
                if (roomMap.ContainsKey(pos + Vector2I.Left))
                {
                    room.HasDoorWest = true;
                    room.Neighbors.Add(pos + Vector2I.Left);
                }
            }

            GD.Print($"[DungeonGenerator] Generated {roomMap.Count} rooms for floor {floor}.");
            return new List<RoomData>(roomMap.Values);
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private static Vector2I GetRandomCardinal()
        {
            return _rng.Next(4) switch
            {
                0 => Vector2I.Up,
                1 => Vector2I.Down,
                2 => Vector2I.Right,
                _ => Vector2I.Left
            };
        }

        private static RoomType RollRoomType(int index, int total, int floor)
        {
            // Never overwrite start (index 0).
            if (index == 0) return RoomType.Start;

            double roll = _rng.NextDouble();
            if (roll < Constants.ITEM_ROOM_CHANCE) return RoomType.Item;
            if (roll < Constants.ITEM_ROOM_CHANCE + Constants.SHOP_ROOM_CHANCE) return RoomType.Shop;
            return RoomType.Normal;
        }

        private static Vector2I FindFarthest(List<Vector2I> positions, Vector2I origin)
        {
            Vector2I farthest = origin;
            float    maxDist  = 0f;
            foreach (var p in positions)
            {
                float d = (p - origin).ToVector2().Length();
                if (d > maxDist) { maxDist = d; farthest = p; }
            }
            return farthest;
        }
    }
}
