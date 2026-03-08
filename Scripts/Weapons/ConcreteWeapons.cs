using Godot;
using DungeonCrawler.Core;
using DungeonCrawler.Stats;

namespace DungeonCrawler.Weapons
{
    /// <summary>Standard single-shot pistol-style weapon.</summary>
    public partial class BasicGun : WeaponBase
    {
        public BasicGun()
        {
            WeaponName    = "Basic Gun";
            FireRate      = 0.35f;
            BaseDamage    = 2f;
            ProjectileSpeed = 420f;
        }

        protected override void SpawnProjectiles(
            Vector2 origin, Vector2 direction,
            float damage, bool isCrit, float critMul,
            CharacterStats stats)
        {
            var p = CreateProjectile(origin, direction, damage, isCrit, critMul);
            AddProjectileToScene(p);
        }
    }

    /// <summary>Fires three projectiles in a spread pattern.</summary>
    public partial class ShotgunWeapon : WeaponBase
    {
        [Export] public int   PelletCount { get; set; } = 3;
        [Export] public float SpreadAngle { get; set; } = 20f; // degrees

        public ShotgunWeapon()
        {
            WeaponName    = "Shotgun";
            FireRate      = 0.9f;
            BaseDamage    = 3f;
            ProjectileSpeed = 350f;
        }

        protected override void SpawnProjectiles(
            Vector2 origin, Vector2 direction,
            float damage, bool isCrit, float critMul,
            CharacterStats stats)
        {
            float halfSpread = SpreadAngle / 2f;
            float step       = PelletCount > 1 ? SpreadAngle / (PelletCount - 1) : 0f;

            for (int i = 0; i < PelletCount; i++)
            {
                float   angleDeg = -halfSpread + step * i;
                Vector2 dir      = direction.Rotated(Mathf.DegToRad(angleDeg));
                var     p        = CreateProjectile(origin, dir, damage, isCrit, critMul);
                p.Speed         *= GD.Randf() * 0.15f + 0.9f; // slight speed variance
                AddProjectileToScene(p);
            }
        }
    }

    /// <summary>Fast-firing automatic rifle.</summary>
    public partial class AutoRifle : WeaponBase
    {
        public AutoRifle()
        {
            WeaponName    = "Auto Rifle";
            FireRate      = 0.12f;
            BaseDamage    = 1.5f;
            ProjectileSpeed = 480f;
        }

        protected override void SpawnProjectiles(
            Vector2 origin, Vector2 direction,
            float damage, bool isCrit, float critMul,
            CharacterStats stats)
        {
            // Small random spread to simulate auto inaccuracy.
            float jitter = Mathf.DegToRad((float)GD.RandRange(-3.0, 3.0));
            Vector2 dir  = direction.Rotated(jitter);
            var p        = CreateProjectile(origin, dir, damage, isCrit, critMul);
            AddProjectileToScene(p);
        }
    }

    /// <summary>Slow, high-damage sniper shot with high pierce.</summary>
    public partial class SniperRifle : WeaponBase
    {
        public SniperRifle()
        {
            WeaponName    = "Sniper Rifle";
            FireRate      = 1.4f;
            BaseDamage    = 12f;
            ProjectileSpeed = 800f;
            PierceCount   = 3;
        }

        protected override void SpawnProjectiles(
            Vector2 origin, Vector2 direction,
            float damage, bool isCrit, float critMul,
            CharacterStats stats)
        {
            var p = CreateProjectile(origin, direction, damage, isCrit, critMul);
            p.ProjectileScale = 1.6f;
            AddProjectileToScene(p);
        }
    }
}
