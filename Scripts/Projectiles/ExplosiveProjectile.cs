using System.Collections.Generic;
using Godot;
using DungeonCrawler.Core;

namespace DungeonCrawler.Projectiles
{
    /// <summary>
    /// Bullet projectile that explodes on impact.
    /// Overrides HandleBodyEntered to trigger an AOE explosion after the base hit.
    /// </summary>
    public partial class ExplosiveProjectile : BulletProjectile
    {
        private bool _hasExploded = false;

        // ── Collision override ─────────────────────────────────────────────────

        protected override void HandleBodyEntered(Node2D body)
        {
            // Apply normal hit logic first.
            base.HandleBodyEntered(body);

            if (!_hasExploded)
                Explode();
        }

        // ── Explosion ─────────────────────────────────────────────────────────

        /// <summary>
        /// Instant AOE damage check in ExplosionRadius, then shows a fading visual circle.
        /// </summary>
        public void Explode()
        {
            if (_hasExploded) return;
            _hasExploded = true;

            float radius = Data?.ExplosionRadius ?? 80f;
            float damage = Data?.ExplosionDamage ?? 10f;
            string ownerTag = Data?.OwnerTag ?? Constants.TAG_PLAYER;

            GD.Print($"[ExplosiveProjectile] Exploding at {GlobalPosition} — r={radius} dmg={damage}");

            // Damage all valid targets in radius.
            string targetGroup = ownerTag == Constants.TAG_PLAYER
                ? Constants.TAG_ENEMY
                : Constants.TAG_PLAYER;

            var alreadyDamaged = new HashSet<Node>();
            foreach (Node node in GetTree().GetNodesInGroup(targetGroup))
            {
                if (node is not Node2D n2d) continue;
                if (GlobalPosition.DistanceTo(n2d.GlobalPosition) > radius) continue;
                if (alreadyDamaged.Contains(node)) continue;

                alreadyDamaged.Add(node);
                if (n2d is Entity entity)
                    entity.TakeDamage(damage, DamageType.Physical);

                GD.Print($"[ExplosiveProjectile] Explosion hit {n2d.Name}");
            }

            ShowExplosionVisual(radius);
            QueueFree();
        }

        // ── Visual ─────────────────────────────────────────────────────────────

        private void ShowExplosionVisual(float radius)
        {
            // Spawn a temporary node at the explosion position that fades out.
            var fx = new ExplosionFX();
            fx.GlobalPosition = GlobalPosition;
            fx.Radius         = radius;
            fx.Color          = Data?.VisualColor ?? Colors.OrangeRed;

            GetTree().Root.AddChild(fx);
        }

        // ── Nested visual helper ───────────────────────────────────────────────

        /// <summary>Temporary scene node that renders the explosion circle and fades out.</summary>
        private partial class ExplosionFX : Node2D
        {
            public float Radius    { get; set; } = 80f;
            public Color Color     { get; set; } = Colors.OrangeRed;
            public float FadeDuration              = 0.3f;

            private float   _timer;
            private ColorRect? _circle;

            public override void _Ready()
            {
                _timer = FadeDuration;

                // Use a ColorRect sized to the diameter as a placeholder circle visual.
                float diameter = Radius * 2f;
                _circle = new ColorRect
                {
                    Color    = Color,
                    Size     = new Vector2(diameter, diameter),
                    Position = new Vector2(-Radius, -Radius)
                };
                AddChild(_circle);
            }

            public override void _Process(double delta)
            {
                _timer -= (float)delta;
                float alpha = Mathf.Clamp(_timer / FadeDuration, 0f, 1f);

                if (_circle != null)
                    _circle.Modulate = new Color(1f, 1f, 1f, alpha);

                if (_timer <= 0f)
                    QueueFree();
            }
        }
    }
}
