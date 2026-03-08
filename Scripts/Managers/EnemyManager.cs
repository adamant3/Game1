using System;
using System.Collections.Generic;
using Godot;
using DungeonCrawler.Core;

namespace DungeonCrawler.Managers
{
    /// <summary>
    /// Tracks all live enemies, handles spawning and kills.
    /// Singleton autoload.
    /// </summary>
    public partial class EnemyManager : Node
    {
        public static EnemyManager Instance { get; private set; } = null!;

        // ── Exports ────────────────────────────────────────────────────────────
        [Export] public Godot.Collections.Array<PackedScene> EnemyScenes { get; set; } = new();

        // ── State ──────────────────────────────────────────────────────────────
        private readonly List<Node> _activeEnemies = new();
        private readonly Dictionary<string, int> _killCount = new();
        public int TotalKills { get; private set; } = 0;

        // ── Godot lifecycle ────────────────────────────────────────────────────
        public override void _Ready()
        {
            if (Instance != null && Instance != this) { QueueFree(); return; }
            Instance = this;

            GameEvents.OnFloorChanged += _ => OnFloorChanged();
        }

        public override void _ExitTree()
        {
            GameEvents.OnFloorChanged -= _ => OnFloorChanged();
        }

        // ── Registration ───────────────────────────────────────────────────────
        public void RegisterEnemy(Node enemy)
        {
            if (enemy == null || _activeEnemies.Contains(enemy)) return;
            _activeEnemies.Add(enemy);
            GameEvents.RaiseEnemySpawned(enemy);
        }

        public void UnregisterEnemy(Node enemy)
        {
            _activeEnemies.Remove(enemy);
        }

        // ── Spawning ───────────────────────────────────────────────────────────
        public Node? SpawnEnemy(string enemyType, Vector2 position, Node parent)
        {
            PackedScene? scene = FindSceneByType(enemyType);
            if (scene == null)
            {
                GD.PrintErr($"[EnemyManager] No scene found for enemy type '{enemyType}'.");
                return null;
            }

            Node instance = scene.Instantiate();
            parent.AddChild(instance);

            if (instance is Node2D n2d)
                n2d.GlobalPosition = position;

            RegisterEnemy(instance);
            GD.Print($"[EnemyManager] Spawned {enemyType} at {position}");
            return instance;
        }

        // ── Kill tracking ──────────────────────────────────────────────────────
        public void KillEnemy(Node enemy, string enemyType)
        {
            if (enemy == null) return;

            UnregisterEnemy(enemy);
            TotalKills++;

            if (!_killCount.ContainsKey(enemyType)) _killCount[enemyType] = 0;
            _killCount[enemyType]++;

            GameEvents.RaiseEnemyDied(enemy);

            // Attempt to call a Die() method if it exists
            if (enemy.HasMethod("Die"))
                enemy.Call("Die");
            else
                enemy.QueueFree();
        }

        public int GetKillCount(string enemyType)
            => _killCount.TryGetValue(enemyType, out int count) ? count : 0;

        // ── Queries ────────────────────────────────────────────────────────────
        public int GetActiveEnemyCount() => _activeEnemies.Count;

        public List<Node> GetEnemiesInRadius(Vector2 center, float radius)
        {
            float radiusSq = radius * radius;
            List<Node> result = new();
            foreach (Node enemy in _activeEnemies)
            {
                if (enemy is Node2D n2d && n2d.GlobalPosition.DistanceSquaredTo(center) <= radiusSq)
                    result.Add(enemy);
            }
            return result;
        }

        public Node? GetNearestEnemy(Vector2 position)
        {
            Node? nearest = null;
            float bestDist = float.MaxValue;
            foreach (Node enemy in _activeEnemies)
            {
                if (enemy is not Node2D n2d) continue;
                float dist = n2d.GlobalPosition.DistanceSquaredTo(position);
                if (dist < bestDist) { bestDist = dist; nearest = enemy; }
            }
            return nearest;
        }

        // ── Bulk operations ────────────────────────────────────────────────────
        public void ClearAllEnemies()
        {
            // Iterate over a copy since KillEnemy modifies the list
            List<Node> copy = new(_activeEnemies);
            foreach (Node enemy in copy)
                KillEnemy(enemy, enemy.GetMeta("EnemyType", "unknown").AsString());

            _activeEnemies.Clear();
        }

        public void OnFloorChanged()
        {
            _activeEnemies.Clear();
            GD.Print("[EnemyManager] Enemy list cleared for new floor.");
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private PackedScene? FindSceneByType(string enemyType)
        {
            foreach (PackedScene scene in EnemyScenes)
            {
                if (scene == null) continue;
                string path = scene.ResourcePath.GetFile().GetBaseName().ToLower();
                if (path.Contains(enemyType.ToLower()))
                    return scene;
            }
            return EnemyScenes.Count > 0 ? EnemyScenes[0] : null;
        }
    }
}
