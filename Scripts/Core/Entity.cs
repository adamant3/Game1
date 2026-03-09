using System;
using Godot;
using DungeonCrawler.Stats;

namespace DungeonCrawler.Core
{
    /// <summary>
    /// Abstract base for every living entity in the dungeon.
    /// Extends CharacterBody2D so it participates in Godot's physics.
    /// </summary>
    public abstract partial class Entity : CharacterBody2D, IEntity
    {
        // ── IEntity ────────────────────────────────────────────────────────────
        public string EntityId { get; private set; } = Guid.NewGuid().ToString();
        public bool   IsAlive  { get; protected set; } = true;

        // ── Stats ──────────────────────────────────────────────────────────────
        public CharacterStats Stats { get; private set; } = null!;

        // ── Invincibility frames ───────────────────────────────────────────────
        protected bool  _isInvincible       = false;
        protected float _invincibilityTimer  = 0f;

        // ── Godot lifecycle ────────────────────────────────────────────────────
        public override void _Ready()
        {
            // Create and attach the stats node if one wasn't added in the scene.
            Stats = GetNodeOrNull<CharacterStats>("CharacterStats")
                 ?? CreateStatsNode();

            InitialiseStats();
            SetupCollision();
            OnReady();
        }

        public override void _PhysicsProcess(double delta)
        {
            if (!IsAlive) return;

            // Tick invincibility window.
            if (_isInvincible)
            {
                _invincibilityTimer -= (float)delta;
                if (_invincibilityTimer <= 0f)
                    _isInvincible = false;
            }

            OnPhysicsProcess((float)delta);
        }

        // ── IEntity implementation ─────────────────────────────────────────────
        public virtual void TakeDamage(float amount, DamageType damageType = DamageType.Physical)
        {
            if (!IsAlive || _isInvincible) return;

            float armor    = Stats.GetStat(StatType.Armor);
            float shielding = Stats.GetStat(StatType.Shielding);

            float reduced = damageType switch
            {
                DamageType.True     => amount,
                DamageType.Physical => MathF.Max(0f, amount - armor),
                DamageType.Magical  => MathF.Max(0f, amount - shielding),
                _                   => amount
            };

            float currentHp = Stats.GetStat(StatType.Health);
            float newHp     = MathF.Max(0f, currentHp - reduced);
            Stats.SetBaseStat(StatType.Health, newHp);

            GD.Print($"[Entity] {Name} took {reduced:F1} {damageType} dmg | HP {currentHp:F1} → {newHp:F1}");

            OnDamageReceived(reduced, damageType);

            if (newHp <= 0f)
                Die();
        }

        public virtual void Die()
        {
            if (!IsAlive) return;
            IsAlive = false;
            OnDeath();
        }

        // ── Virtuals for subclasses ────────────────────────────────────────────
        protected virtual void OnReady()           { }
        protected virtual void OnPhysicsProcess(float delta) { }
        protected virtual void InitialiseStats()   { }
        protected virtual void SetupCollision()    { }
        protected virtual void OnDamageReceived(float amount, DamageType type) { }
        protected virtual void OnDeath()
        {
            GD.Print($"[Entity] {Name} died.");
            QueueFree();
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        public void StartInvincibility(float duration)
        {
            _isInvincible      = true;
            _invincibilityTimer = duration;
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;
            float maxHp  = Stats.GetStat(StatType.MaxHealth);
            float curHp  = Stats.GetStat(StatType.Health);
            float newHp  = MathF.Min(maxHp, curHp + amount);
            Stats.SetBaseStat(StatType.Health, newHp);
        }

        // ── Private ────────────────────────────────────────────────────────────
        private CharacterStats CreateStatsNode()
        {
            var node = new CharacterStats { Name = "CharacterStats" };
            AddChild(node);
            return node;
        }
    }
}
