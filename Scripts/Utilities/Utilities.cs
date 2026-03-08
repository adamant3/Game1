using System;
using System.Collections.Generic;
using Godot;

namespace DungeonCrawler.Utilities
{
    /// <summary>
    /// A generic object pool to avoid GC pressure from frequently spawned objects.
    /// T must be a Godot Node.
    /// </summary>
    public class ObjectPool<T> where T : Node, new()
    {
        private readonly Stack<T>  _pool    = new();
        private readonly Node      _parent;
        private readonly Func<T>   _factory;
        private int                _maxSize;

        public ObjectPool(Node parent, Func<T>? factory = null, int initialSize = 10, int maxSize = 100)
        {
            _parent  = parent;
            _factory = factory ?? (() => new T());
            _maxSize = maxSize;

            for (int i = 0; i < initialSize; i++)
            {
                T obj = CreateInstance();
                obj.ProcessMode = Node.ProcessModeEnum.Disabled;
                _pool.Push(obj);
            }
        }

        public T Get()
        {
            T obj;
            if (_pool.Count > 0)
            {
                obj = _pool.Pop();
            }
            else
            {
                obj = CreateInstance();
            }
            obj.ProcessMode = Node.ProcessModeEnum.Inherit;
            return obj;
        }

        public void Return(T obj)
        {
            if (_pool.Count >= _maxSize)
            {
                obj.QueueFree();
                return;
            }
            obj.ProcessMode = Node.ProcessModeEnum.Disabled;
            if (obj is Node2D node2D)
                node2D.Visible = false;
            _pool.Push(obj);
        }

        private T CreateInstance()
        {
            T obj = _factory();
            _parent.AddChild(obj);
            return obj;
        }

        public int PoolSize => _pool.Count;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Math helpers
    // ─────────────────────────────────────────────────────────────────────────
    public static class MathUtils
    {
        /// <summary>Linearly interpolates and clamps between 0 and 1.</summary>
        public static float LerpClamped(float from, float to, float weight) =>
            Mathf.Lerp(from, to, Mathf.Clamp(weight, 0f, 1f));

        /// <summary>Returns a random point inside a circle of given radius.</summary>
        public static Vector2 RandomInsideCircle(float radius)
        {
            float angle = (float)GD.RandRange(0.0, Mathf.Tau);
            float r     = radius * Mathf.Sqrt((float)GD.RandRange(0.0, 1.0));
            return new Vector2(r * Mathf.Cos(angle), r * Mathf.Sin(angle));
        }

        /// <summary>Map a value from one range to another.</summary>
        public static float Remap(float value, float inMin, float inMax, float outMin, float outMax)
        {
            float t = Mathf.InverseLerp(inMin, inMax, value);
            return Mathf.Lerp(outMin, outMax, t);
        }

        /// <summary>Returns true with probability 0..1.</summary>
        public static bool Chance(float probability) => GD.Randf() < probability;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Extension methods
    // ─────────────────────────────────────────────────────────────────────────
    public static class NodeExtensions
    {
        /// <summary>
        /// Returns the first descendant of type T, searching the entire subtree.
        /// </summary>
        public static T? FindFirst<T>(this Node root) where T : Node
        {
            foreach (Node child in root.GetChildren())
            {
                if (child is T match) return match;
                T? found = child.FindFirst<T>();
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>Returns all descendants of type T.</summary>
        public static List<T> FindAll<T>(this Node root) where T : Node
        {
            var results = new List<T>();
            CollectAll(root, results);
            return results;
        }

        private static void CollectAll<T>(Node node, List<T> list) where T : Node
        {
            foreach (Node child in node.GetChildren())
            {
                if (child is T match) list.Add(match);
                CollectAll(child, list);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Timer helper (non-Godot timer, delta-based)
    // ─────────────────────────────────────────────────────────────────────────
    public class DeltaTimer
    {
        private float _duration;
        private float _elapsed;
        private bool  _running;

        public bool  IsFinished  => _elapsed >= _duration;
        public float Progress    => _duration > 0 ? Mathf.Min(1f, _elapsed / _duration) : 1f;
        public float TimeLeft    => Mathf.Max(0f, _duration - _elapsed);

        public DeltaTimer(float duration) { _duration = duration; }

        public void Start()  { _elapsed = 0f; _running = true; }
        public void Stop()   => _running = false;
        public void Reset()  { _elapsed = 0f; }

        public bool Tick(float delta)
        {
            if (!_running || IsFinished) return IsFinished;
            _elapsed += delta;
            return IsFinished;
        }
    }
}
