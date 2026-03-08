using System;
using Godot;
using DungeonCrawler.Core;
using DungeonCrawler.Stats;

namespace DungeonCrawler.Projectiles
{
    /// <summary>
    /// A single projectile in flight. Configure via properties before AddChild.
    /// </summary>
    public partial class Projectile : Area2D
    {
        // ── Configuration (set before adding to tree) ──────────────────────────
        public float   Speed         { get; set; } = Constants.DEFAULT_PROJECTILE_SPEED;
        public float   Damage        { get; set; } = Constants.PLAYER_BASE_DAMAGE;
        public float   Lifetime      { get; set; } = Constants.DEFAULT_PROJECTILE_LIFETIME;
        public Vector2 Direction      { get; set; } = Vector2.Right;
        public bool    IsPlayerOwned  { get; set; } = true;
        public bool    IsCrit         { get; set; } = false;
        public float   CritMultiplier { get; set; } = Constants.BASE_CRIT_MULTIPLIER;
        public float   Knockback      { get; set; } = Constants.KNOCKBACK_FORCE;
        public int     Piercing       { get; set; } = 0; // 0 = no pierce, N = pierce N targets
        public float   ProjectileScale { get; set; } = 1f;

        // ── Visuals ────────────────────────────────────────────────────────────
        [Export] public Color ProjectileColor { get; set; } = Colors.Yellow;

        // ── Private state ──────────────────────────────────────────────────────
        private float   _lifeTimer     = 0f;
        private int     _pierceCount   = 0;
        private ColorRect? _visual;

        // ── Godot lifecycle ────────────────────────────────────────────────────
        public override void _Ready()
        {
            AddToGroup(Constants.TAG_PROJECTILE);
            CollisionLayer = Constants.MASK_PROJECTILE;
            CollisionMask  = IsPlayerOwned
                ? Constants.MASK_ENEMY | Constants.MASK_WALL
                : Constants.MASK_PLAYER | Constants.MASK_WALL;

            BodyEntered += OnBodyEntered;

            // Simple visual rectangle as placeholder for sprite.
            _visual = new ColorRect
            {
                Color    = IsCrit ? Colors.OrangeRed : ProjectileColor,
                Size     = new Vector2(12f * ProjectileScale, 6f * ProjectileScale),
                Position = new Vector2(-6f * ProjectileScale, -3f * ProjectileScale)
            };
            AddChild(_visual);

            // Auto-orient to direction.
            Rotation = Direction.Angle();
        }

        public override void _PhysicsProcess(double delta)
        {
            _lifeTimer += (float)delta;
            if (_lifeTimer >= Lifetime)
            {
                QueueFree();
                return;
            }

            Position += Direction * Speed * (float)delta;
        }

        // ── Collision ──────────────────────────────────────────────────────────
        private void OnBodyEntered(Node2D body)
        {
            if (body is Entity entity)
            {
                float dmg = IsCrit ? Damage * CritMultiplier : Damage;
                entity.TakeDamage(dmg, DamageType.Physical);

                // Apply knockback if the entity is a Character.
                if (body is Core.Character character)
                    character.ApplyKnockback(Direction, Knockback);

                if (_pierceCount >= Piercing)
                    QueueFree();
                else
                    _pierceCount++;
            }
            else if (body.IsInGroup(Constants.TAG_WALL) || body is StaticBody2D)
            {
                QueueFree();
            }
        }
    }
}
