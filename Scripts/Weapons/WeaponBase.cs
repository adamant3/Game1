using System;
using Godot;
using DungeonCrawler.Core;
using DungeonCrawler.Stats;
using DungeonCrawler.Projectiles;

namespace DungeonCrawler.Weapons
{
    /// <summary>
    /// Abstract base class for all weapons.
    /// Handles fire-rate gating and crit rolling; subclasses implement SpawnProjectiles().
    /// </summary>
    public abstract partial class WeaponBase : Node2D
    {
        // ── Identity ───────────────────────────────────────────────────────────
        public string WeaponId   { get; protected set; } = Guid.NewGuid().ToString();
        public string WeaponName { get; protected set; } = "Unknown Weapon";

        // ── Configuration ──────────────────────────────────────────────────────
        [Export] public float FireRate     { get; set; } = 0.4f;  // seconds between shots
        [Export] public float BaseDamage   { get; set; } = Constants.PLAYER_BASE_DAMAGE;
        [Export] public float ProjectileSpeed { get; set; } = Constants.DEFAULT_PROJECTILE_SPEED;
        [Export] public float ProjectileLifetime { get; set; } = Constants.DEFAULT_PROJECTILE_LIFETIME;
        [Export] public float KnockbackForce { get; set; } = Constants.KNOCKBACK_FORCE;
        [Export] public int   PierceCount  { get; set; } = 0;

        // ── State ──────────────────────────────────────────────────────────────
        private float _cooldown = 0f;

        // ── Public API ─────────────────────────────────────────────────────────
        /// <summary>
        /// Called by the owning character every frame when the fire button is held.
        /// stats = owner's CharacterStats for damage/crit scaling.
        /// </summary>
        public bool Fire(Vector2 origin, Vector2 direction, CharacterStats stats)
        {
            if (_cooldown > 0f) return false;

            float damage = BaseDamage + stats.GetStat(StatType.Damage);
            bool  isCrit = RollCrit(stats.GetStat(StatType.CritChance));
            float critMul = stats.GetStat(StatType.CritDamage);
            if (critMul <= 0f) critMul = Constants.BASE_CRIT_MULTIPLIER;

            SpawnProjectiles(origin, direction, damage, isCrit, critMul, stats);

            float attackSpeed = stats.GetStat(StatType.AttackSpeed);
            _cooldown = attackSpeed > 0f ? FireRate / attackSpeed : FireRate;
            return true;
        }

        public override void _Process(double delta)
        {
            if (_cooldown > 0f)
                _cooldown -= (float)delta;
        }

        // ── Abstract ───────────────────────────────────────────────────────────
        /// <summary>Spawn one or more projectile nodes into the scene.</summary>
        protected abstract void SpawnProjectiles(
            Vector2 origin, Vector2 direction,
            float damage, bool isCrit, float critMul,
            CharacterStats stats);

        // ── Helpers ────────────────────────────────────────────────────────────
        protected static bool RollCrit(float critChance) =>
            GD.Randf() < critChance;

        protected Projectile CreateProjectile(
            Vector2 origin, Vector2 direction,
            float damage, bool isCrit, float critMul, bool isPlayerOwned = true)
        {
            var p = new Projectile
            {
                GlobalPosition = origin,
                Direction      = direction,
                Speed          = ProjectileSpeed,
                Damage         = damage,
                Lifetime       = ProjectileLifetime,
                IsPlayerOwned  = isPlayerOwned,
                IsCrit         = isCrit,
                CritMultiplier = critMul,
                Knockback      = KnockbackForce,
                Piercing       = PierceCount
            };
            return p;
        }

        protected void AddProjectileToScene(Projectile p)
        {
            // Add to the root so it isn't parented to the character (avoids transform issues).
            GetTree().Root.AddChild(p);
        }
    }
}
