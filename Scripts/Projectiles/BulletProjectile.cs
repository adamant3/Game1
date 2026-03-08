using System.Collections.Generic;
using Godot;
using DungeonCrawler.Core;
using DungeonCrawler.Stats;

namespace DungeonCrawler.Projectiles
{
    /// <summary>
    /// General-purpose bullet projectile.  Configure via Initialize() before adding to the tree,
    /// or set <see cref="Data"/> before _Ready() is called.
    /// </summary>
    public partial class BulletProjectile : Area2D
    {
        // ── Configuration ──────────────────────────────────────────────────────
        [Export] public ProjectileData? Data;

        // ── Runtime state ──────────────────────────────────────────────────────
        protected Vector2              _velocity;
        protected float                _lifetime;
        protected float                _traveledDistance;
        protected int                  _pierceCount;
        protected readonly List<Node>  _alreadyHit = new();

        // ── Visual ─────────────────────────────────────────────────────────────
        private ColorRect? _visual;

        // ── Godot lifecycle ────────────────────────────────────────────────────
        public override void _Ready()
        {
            Data ??= new ProjectileData();

            AddToGroup(Constants.TAG_PROJECTILE);
            CollisionLayer = Constants.MASK_PROJECTILE;
            CollisionMask  = Data.OwnerTag == Constants.TAG_PLAYER
                ? Constants.MASK_ENEMY  | Constants.MASK_WALL
                : Constants.MASK_PLAYER | Constants.MASK_WALL;

            _lifetime = Data.Lifetime;
            _velocity = _velocity == Vector2.Zero
                ? Vector2.Right * Data.Speed
                : _velocity;                           // Set by Initialize() before _Ready()

            BuildVisual();
            BodyEntered += OnBodyEntered;
        }

        public override void _Process(double delta)
        {
            float dt = (float)delta;

            GlobalPosition    += _velocity * dt;
            _traveledDistance += _velocity.Length() * dt;
            _lifetime          -= dt;

            if (_lifetime <= 0f)
            {
                QueueFree();
                return;
            }

            // Range guard: free when projectile has travelled more than Speed × Lifetime.
            float maxRange = (Data?.Speed ?? 400f) * (Data?.Lifetime ?? 3f);
            if (_traveledDistance > maxRange)
                QueueFree();
        }

        // ── Public initialiser ────────────────────────────────────────────────

        /// <summary>
        /// Configure and position this projectile.  Call before AddChild so _Ready
        /// receives the initialised Data.
        /// </summary>
        public void Initialize(ProjectileData data, Vector2 position, Vector2 direction, string ownerTag)
        {
            Data              = data;
            Data.OwnerTag     = ownerTag;
            GlobalPosition    = position;
            _velocity         = direction.Normalized() * data.Speed;
            _lifetime         = data.Lifetime;
            _pierceCount      = 0;
            _traveledDistance = 0f;
            _alreadyHit.Clear();

            Rotation = direction.Angle();
        }

        // ── Collision ─────────────────────────────────────────────────────────

        private void OnBodyEntered(Node2D body) => HandleBodyEntered(body);

        /// <summary>
        /// Virtual collision handler — override in subclasses to add extra behaviour
        /// (explosion, homing target removal, etc.) before or after base logic.
        /// </summary>
        protected virtual void HandleBodyEntered(Node2D body)
        {
            if (_alreadyHit.Contains(body)) return;

            bool isEnemy  = body.IsInGroup(Constants.TAG_ENEMY);
            bool isPlayer = body.IsInGroup(Constants.TAG_PLAYER);
            bool isWall   = body.IsInGroup(Constants.TAG_WALL) || body is StaticBody2D;

            if (isWall)
            {
                QueueFree();
                return;
            }

            bool ownerIsPlayer = Data?.OwnerTag == Constants.TAG_PLAYER;
            bool shouldDamage  = (ownerIsPlayer && isEnemy) || (!ownerIsPlayer && isPlayer);

            if (!shouldDamage) return;

            _alreadyHit.Add(body);

            if (body is Entity entity)
            {
                entity.TakeDamage(Data?.Damage ?? 1f, DamageType.Physical);

                if (body is Character character)
                    character.ApplyKnockback(_velocity.Normalized(), Data?.Knockback ?? 100f);

                GD.Print($"[BulletProjectile] Hit {body.Name} for {Data?.Damage:F1}");
            }

            // Pierce logic.
            if (Data != null && Data.CanPierce && _pierceCount < Data.PierceCount)
            {
                _pierceCount++;
            }
            else
            {
                QueueFree();
            }
        }

        // ── Private ────────────────────────────────────────────────────────────

        private void BuildVisual()
        {
            float size = Data?.Size ?? 8f;
            _visual = new ColorRect
            {
                Color    = Data?.VisualColor ?? Colors.Yellow,
                Size     = new Vector2(size, size),
                Position = new Vector2(-size * 0.5f, -size * 0.5f)
            };
            AddChild(_visual);

            // Collision shape matching visual size.
            var col = new CollisionShape2D();
            col.Shape = new RectangleShape2D { Size = new Vector2(size, size) };
            AddChild(col);
        }
    }
}
