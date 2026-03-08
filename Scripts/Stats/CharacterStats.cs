using System;
using System.Collections.Generic;
using Godot;
using DungeonCrawler.Core;

namespace DungeonCrawler.Stats
{
    /// <summary>
    /// Godot Node that tracks all base stats and active modifiers for a character.
    /// Attach as a child of any Entity node.
    /// </summary>
    public partial class CharacterStats : Node
    {
        // ── Events ─────────────────────────────────────────────────────────────
        public event Action<StatType, float>?   OnStatChanged;      // (stat, newValue)
        public event Action<StatModifier>?      OnModifierAdded;
        public event Action<StatModifier>?      OnModifierRemoved;

        // ── Base stat storage ──────────────────────────────────────────────────
        private readonly Dictionary<StatType, float> _baseStats = new();
        private readonly List<StatModifier>          _modifiers = new();

        // ── Godot lifecycle ────────────────────────────────────────────────────
        public override void _Ready()
        {
            // Initialise all stats to 0 so GetStat always returns a valid float.
            foreach (StatType st in Enum.GetValues(typeof(StatType)))
                _baseStats.TryAdd(st, 0f);
        }

        public override void _Process(double delta)
        {
            bool anyExpired = false;
            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                _modifiers[i].Update((float)delta);
                if (_modifiers[i].IsExpired)
                {
                    StatModifier expired = _modifiers[i];
                    _modifiers.RemoveAt(i);
                    OnModifierRemoved?.Invoke(expired);
                    GameEvents.RaiseBuffExpired(expired.Id);
                    anyExpired = true;
                }
            }

            if (anyExpired)
                NotifyAllStatChanged();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Returns the computed (final) value of a stat.</summary>
        public float GetStat(StatType stat)
        {
            float baseVal = _baseStats.TryGetValue(stat, out float b) ? b : 0f;

            float flat       = 0f;
            float percentage = 0f;
            float? overrideVal = null;

            foreach (StatModifier mod in _modifiers)
            {
                if (mod.StatType != stat) continue;
                switch (mod.ModifierType)
                {
                    case ModifierType.Flat:       flat       += mod.Value; break;
                    case ModifierType.Percentage: percentage += mod.Value; break;
                    case ModifierType.Override:   overrideVal = mod.Value; break;
                }
            }

            if (overrideVal.HasValue)
                return MathF.Max(0f, overrideVal.Value);

            float result = (baseVal + flat) * (1f + percentage);
            return MathF.Max(0f, result);
        }

        /// <summary>Sets a base stat directly and fires OnStatChanged.</summary>
        public void SetBaseStat(StatType stat, float value)
        {
            _baseStats[stat] = value;
            OnStatChanged?.Invoke(stat, GetStat(stat));
        }

        /// <summary>Retrieves the raw base value without modifiers.</summary>
        public float GetBaseStat(StatType stat) =>
            _baseStats.TryGetValue(stat, out float v) ? v : 0f;

        /// <summary>Adds a modifier and fires events.</summary>
        public void AddModifier(StatModifier modifier)
        {
            if (modifier == null) throw new ArgumentNullException(nameof(modifier));
            _modifiers.Add(modifier);
            OnModifierAdded?.Invoke(modifier);
            OnStatChanged?.Invoke(modifier.StatType, GetStat(modifier.StatType));
            GameEvents.RaiseBuffApplied(modifier.Id);
        }

        /// <summary>Removes the first modifier with the given id.</summary>
        public void RemoveModifier(string id)
        {
            for (int i = 0; i < _modifiers.Count; i++)
            {
                if (_modifiers[i].Id != id) continue;
                StatModifier removed = _modifiers[i];
                _modifiers.RemoveAt(i);
                OnModifierRemoved?.Invoke(removed);
                OnStatChanged?.Invoke(removed.StatType, GetStat(removed.StatType));
                return;
            }
        }

        /// <summary>Removes all modifiers that originate from the given source.</summary>
        public void RemoveModifiersFromSource(string source)
        {
            HashSet<StatType> affected = new();
            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                if (_modifiers[i].Source != source) continue;
                affected.Add(_modifiers[i].StatType);
                OnModifierRemoved?.Invoke(_modifiers[i]);
                _modifiers.RemoveAt(i);
            }
            foreach (StatType st in affected)
                OnStatChanged?.Invoke(st, GetStat(st));
        }

        /// <summary>Returns a snapshot list of all active modifiers (read-only copy).</summary>
        public IReadOnlyList<StatModifier> GetModifiers() => _modifiers.AsReadOnly();

        // ── Private helpers ────────────────────────────────────────────────────
        private void NotifyAllStatChanged()
        {
            foreach (StatType st in _baseStats.Keys)
                OnStatChanged?.Invoke(st, GetStat(st));
        }
    }
}
