using System;
using Godot;
using DungeonCrawler.Core;
using DungeonCrawler.Stats;

namespace DungeonCrawler.Buffs
{
    /// <summary>
    /// A temporary positive effect applied to a character.
    /// Wraps one or more StatModifiers and manages their lifecycle.
    /// </summary>
    public partial class BuffBase : Node
    {
        public string BuffId     { get; protected set; } = Guid.NewGuid().ToString();
        public string BuffName   { get; protected set; } = "Buff";
        public float  Duration   { get; protected set; } = 5f;
        protected CharacterStats? _stats;
        private float _timeLeft;
        private bool  _applied = false;

        public override void _Ready()
        {
            _stats   = GetParent().GetNodeOrNull<CharacterStats>("CharacterStats");
            _timeLeft = Duration;
            if (_stats != null)
                ApplyModifiers(_stats);
            _applied = true;
            GameEvents.RaiseBuffApplied(BuffId);
        }

        public override void _Process(double delta)
        {
            if (!_applied) return;
            _timeLeft -= (float)delta;
            if (_timeLeft <= 0f)
                Expire();
        }

        protected virtual void ApplyModifiers(CharacterStats stats) { }
        protected virtual void RemoveModifiers(CharacterStats stats) =>
            stats.RemoveModifiersFromSource(BuffId);

        private void Expire()
        {
            if (_stats != null) RemoveModifiers(_stats);
            GameEvents.RaiseBuffExpired(BuffId);
            _applied = false;
            QueueFree();
        }

        public float GetTimeLeft() => _timeLeft;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Concrete Buffs
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Speed boost buff (+30% speed, 8s).</summary>
    public partial class SpeedBuff : BuffBase
    {
        public SpeedBuff() { BuffName = "Speed Boost"; Duration = 8f; }

        protected override void ApplyModifiers(CharacterStats stats)
        {
            stats.AddModifier(new StatModifier(
                BuffId, StatType.Speed, 0.30f, ModifierType.Percentage, Duration, BuffId));
        }
    }

    /// <summary>Damage aura (+50% damage, 10s).</summary>
    public partial class DamageAuraBuff : BuffBase
    {
        public DamageAuraBuff() { BuffName = "Damage Aura"; Duration = 10f; }

        protected override void ApplyModifiers(CharacterStats stats)
        {
            stats.AddModifier(new StatModifier(
                BuffId, StatType.Damage, 0.50f, ModifierType.Percentage, Duration, BuffId));
        }
    }

    /// <summary>Shield buff (+5 armor, 6s).</summary>
    public partial class ArmorBuff : BuffBase
    {
        public ArmorBuff() { BuffName = "Iron Skin"; Duration = 6f; }

        protected override void ApplyModifiers(CharacterStats stats)
        {
            stats.AddModifier(new StatModifier(
                BuffId, StatType.Armor, 5f, ModifierType.Flat, Duration, BuffId));
        }
    }

    /// <summary>Crit surge (+20% crit chance, 5s).</summary>
    public partial class CritSurgeBuff : BuffBase
    {
        public CritSurgeBuff() { BuffName = "Crit Surge"; Duration = 5f; }

        protected override void ApplyModifiers(CharacterStats stats)
        {
            stats.AddModifier(new StatModifier(
                BuffId, StatType.CritChance, 0.20f, ModifierType.Flat, Duration, BuffId));
        }
    }
}
