using Godot;
using DungeonCrawler.Core;

namespace DungeonCrawler.Projectiles
{
    /// <summary>
    /// Projectile that steers toward the nearest valid target.
    /// Extends <see cref="BulletProjectile"/> and overrides _Process to apply homing.
    /// If the current target dies a new one is sought; if none exists it flies straight.
    /// </summary>
    public partial class HomingProjectile : BulletProjectile
    {
        // ── Homing state ───────────────────────────────────────────────────────
        private Node2D? _target;
        private float   _searchCooldown = 0f;   // seconds between target re-searches

        // ── Godot lifecycle ────────────────────────────────────────────────────
        public override void _Ready()
        {
            base._Ready();
            _target = FindNearestTarget();
        }

        public override void _Process(double delta)
        {
            float dt = (float)delta;

            // Periodically look for a fresh target if the current one is gone.
            _searchCooldown -= dt;
            if (_target == null || !IsInstanceValid(_target) || !_target.IsInsideTree())
            {
                if (_searchCooldown <= 0f)
                {
                    _target         = FindNearestTarget();
                    _searchCooldown = 0.25f;
                }
            }

            // Steer velocity toward target.
            if (_target != null && IsInstanceValid(_target) && Data != null)
            {
                Vector2 desired     = (_target.GlobalPosition - GlobalPosition).Normalized()
                                      * Data.Speed;
                float   turnRate    = Data.HomingStrength * dt;
                _velocity           = _velocity.Lerp(desired, Mathf.Clamp(turnRate, 0f, 1f));
                _velocity           = _velocity.Normalized() * Data.Speed;
                Rotation            = _velocity.Angle();
            }

            // Delegate move / lifetime logic to base.
            base._Process(delta);
        }

        // ── Target acquisition ─────────────────────────────────────────────────

        /// <summary>
        /// Finds the nearest node in the appropriate group (enemy or player) that is alive.
        /// </summary>
        public Node2D? FindNearestTarget()
        {
            string group   = Data?.OwnerTag == Constants.TAG_PLAYER
                             ? Constants.TAG_ENEMY
                             : Constants.TAG_PLAYER;

            float  nearest = float.MaxValue;
            Node2D? best   = null;

            foreach (Node node in GetTree().GetNodesInGroup(group))
            {
                if (node is not Node2D n2d) continue;
                if (node is Entity e && !e.IsAlive) continue;

                float dist = GlobalPosition.DistanceTo(n2d.GlobalPosition);
                if (dist < nearest)
                {
                    nearest = dist;
                    best    = n2d;
                }
            }

            if (best != null)
                GD.Print($"[HomingProjectile] Target acquired: {best.Name}");

            return best;
        }
    }
}
