using System;
using System.Collections.Generic;
using Godot;

namespace DungeonCrawler.Managers
{
    /// <summary>
    /// Centralised RNG manager. Provides separate, reproducible streams for
    /// different game systems so that changing one system doesn't affect others.
    /// Autoload singleton.
    /// </summary>
    public partial class RNGManager : Node
    {
        public static RNGManager Instance { get; private set; } = null!;

        private int _masterSeed;
        private readonly Dictionary<string, Random> _namedRNGs = new();
        private Random _globalRng = new();
        private bool _isSeeded = false;

        // ── Godot lifecycle ────────────────────────────────────────────────────
        public override void _Ready()
        {
            if (Instance != null && Instance != this) { QueueFree(); return; }
            Instance = this;

            // Create default streams
            foreach (string name in new[] { "global", "dungeon", "items", "enemies", "drops" })
                CreateStream(name);

            if (!_isSeeded)
                Initialize(0);
        }

        // ── Initialisation ─────────────────────────────────────────────────────
        public void Initialize(int seed = 0)
        {
            _masterSeed = seed == 0 ? (int)(Time.GetTicksMsec() & 0x7FFFFFFF) : seed;
            _isSeeded = true;
            _globalRng = new Random(_masterSeed);

            // Re-seed every named stream deterministically from the master seed
            int idx = 0;
            foreach (string key in _namedRNGs.Keys)
            {
                _namedRNGs[key] = new Random(_masterSeed ^ (idx * 2654435761));
                idx++;
            }

            GD.Print($"[RNGManager] Initialized with seed {_masterSeed}");
        }

        // ── Stream management ──────────────────────────────────────────────────
        public void CreateStream(string name, int? seed = null)
        {
            int s = seed ?? (_masterSeed ^ (name.GetHashCode() & 0x7FFFFFFF));
            _namedRNGs[name] = new Random(s);
        }

        private Random GetStream(string stream)
        {
            if (_namedRNGs.TryGetValue(stream, out Random? rng)) return rng;
            CreateStream(stream);
            return _namedRNGs[stream];
        }

        // ── Public RNG API ─────────────────────────────────────────────────────
        /// <summary>Returns an integer in [min, max) (exclusive upper bound).</summary>
        public int RollInt(int min, int max, string stream = "global")
            => GetStream(stream).Next(min, max);

        /// <summary>Returns a float in [min, max).</summary>
        public float RollFloat(float min = 0f, float max = 1f, string stream = "global")
            => (float)(GetStream(stream).NextDouble() * (max - min) + min);

        /// <summary>Returns true if a random float in [0,1) is less than <paramref name="chance"/>.</summary>
        public bool RollChance(float chance, string stream = "global")
            => (float)GetStream(stream).NextDouble() < chance;

        /// <summary>Picks a uniformly random element from a list.</summary>
        public T PickRandom<T>(List<T> list, string stream = "global")
        {
            if (list == null || list.Count == 0)
                throw new ArgumentException("List must be non-empty.", nameof(list));
            return list[GetStream(stream).Next(list.Count)];
        }

        /// <summary>Picks an element using weighted probabilities.</summary>
        public T PickWeighted<T>(Dictionary<T, float> weightedChoices, string stream = "global") where T : notnull
        {
            if (weightedChoices == null || weightedChoices.Count == 0)
                throw new ArgumentException("Weighted choices must be non-empty.", nameof(weightedChoices));

            float total = 0f;
            foreach (float w in weightedChoices.Values) total += w;

            float roll = RollFloat(0f, total, stream);
            float cumulative = 0f;
            foreach (KeyValuePair<T, float> kvp in weightedChoices)
            {
                cumulative += kvp.Value;
                if (roll < cumulative) return kvp.Key;
            }

            // Fallback: return last key
            T last = default!;
            foreach (T key in weightedChoices.Keys) last = key;
            return last;
        }

        // ── Seed management ────────────────────────────────────────────────────
        public int GetSeed() => _masterSeed;

        public void SetSeed(int seed)
        {
            Initialize(seed);
        }
    }
}
