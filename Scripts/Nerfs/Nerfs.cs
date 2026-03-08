using System;
using Godot;
using DungeonCrawler.Core;
using DungeonCrawler.Stats;

namespace DungeonCrawler.Nerfs
{
    /// <summary>
    /// A temporary negative effect applied to a character.
    /// Mirrors BuffBase but registered as a nerf for the event system.
    /// </summary>
    public partial class NerfBase : Node
    {
        public string NerfId   { get; protected set; } = Guid.NewGuid().ToString();
        public string NerfName { get; protected set; } = "Nerf";
        public float  Duration { get; protected set; } = 4f;
        protected CharacterStats? _stats;
        private float _timeLeft;
        private bool  _applied = false;

        public override void _Ready()
        {
            _stats    = GetParent().GetNodeOrNull<CharacterStats>("CharacterStats");
            _timeLeft  = Duration;
            if (_stats != null)
                ApplyModifiers(_stats);
            _applied = true;
            GameEvents.RaiseNerfApplied(NerfId);
        }

        public override void _Process(double delta)
        {
            if (!_applied) return;
            _timeLeft -= (float)delta;
            if (_timeLeft <= 0f) Expire();
        }

        protected virtual void ApplyModifiers(CharacterStats stats) { }
        protected virtual void RemoveModifiers(CharacterStats stats) =>
            stats.RemoveModifiersFromSource(NerfId);

        private void Expire()
        {
            if (_stats != null) RemoveModifiers(_stats);
            _applied = false;
            QueueFree();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Concrete Nerfs
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Slow nerf: -40% speed for 3 seconds.</summary>
    public partial class SlowNerf : NerfBase
    {
        public SlowNerf() { NerfName = "Slowed"; Duration = 3f; }

        protected override void ApplyModifiers(CharacterStats stats)
        {
            stats.AddModifier(new StatModifier(
                NerfId, StatType.Speed, -0.40f, ModifierType.Percentage, Duration, NerfId));
        }
    }

    /// <summary>Weaken nerf: -30% damage for 5 seconds.</summary>
    public partial class WeakenNerf : NerfBase
    {
        public WeakenNerf() { NerfName = "Weakened"; Duration = 5f; }

        protected override void ApplyModifiers(CharacterStats stats)
        {
            stats.AddModifier(new StatModifier(
                NerfId, StatType.Damage, -0.30f, ModifierType.Percentage, Duration, NerfId));
        }
    }

    /// <summary>Blind nerf: attack speed halved for 4 seconds.</summary>
    public partial class BlindNerf : NerfBase
    {
        public BlindNerf() { NerfName = "Blinded"; Duration = 4f; }

        protected override void ApplyModifiers(CharacterStats stats)
        {
            // AttackSpeed base 1.0; -0.5 flat brings it to 0.5x.
            stats.AddModifier(new StatModifier(
                NerfId, StatType.AttackSpeed, -0.50f, ModifierType.Flat, Duration, NerfId));
        }
    }

    /// <summary>Curse nerf: -20% luck and -15% damage for 6 seconds.</summary>
    public partial class CurseNerf : NerfBase
    {
        public CurseNerf() { NerfName = "Cursed"; Duration = 6f; }

        protected override void ApplyModifiers(CharacterStats stats)
        {
            stats.AddModifier(new StatModifier(
                NerfId, StatType.Luck,   -0.20f, ModifierType.Flat,       Duration, NerfId));
            stats.AddModifier(new StatModifier(
                NerfId, StatType.Damage, -0.15f, ModifierType.Percentage, Duration, NerfId));
        }
    }
}
