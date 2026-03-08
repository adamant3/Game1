using System;
using Godot;
using DungeonCrawler.Core;
using DungeonCrawler.Stats;

namespace DungeonCrawler.Weapons
{
    /// <summary>Style of gun — controls pellet count, spread, fire rate, and other defaults.</summary>
    public enum GunType { Pistol, Shotgun, AutoRifle, Sniper }

    /// <summary>
    /// Ranged weapon covering all gun archetypes.
    /// Subtype behaviour is selected via <see cref="GunStyle"/>.
    /// </summary>
    public partial class GunWeapon : WeaponBase
    {
        // ── Inspector exports ──────────────────────────────────────────────────
        [Export] public GunType GunStyle      { get; set; } = GunType.Pistol;
        [Export] public int     PelletsPerShot { get; set; } = 1;
        [Export] public float   SpreadAngle    { get; set; } = 0f;   // degrees
        [Export] public bool    IsAutomatic    { get; set; } = false;
        [Export] public float   ReloadTime     { get; set; } = 1.0f;

        // ── Events ─────────────────────────────────────────────────────────────
        public event Action?       OnReloadStarted;
        public event Action?       OnReloadFinished;
        public event Action<int>?  OnAmmoChanged;
        public event Action?       OnOutOfAmmo;

        // ── Ammo state ─────────────────────────────────────────────────────────
        public int  MaxAmmo     { get; private set; } = -1;  // -1 = infinite
        public int  CurrentAmmo { get; private set; } = -1;

        // ── Reload state ───────────────────────────────────────────────────────
        private bool  _isReloading  = false;
        private float _reloadTimer  = 0f;

        // ── Cached per-shot stats ──────────────────────────────────────────────
        private float  _lastDamage  = 0f;
        private bool   _lastIsCrit  = false;
        private float  _lastCritMul = 1f;

        // ── Godot lifecycle ────────────────────────────────────────────────────
        public override void _Ready()
        {
            base._Ready();
            ConfigureForGunType();
        }

        public override void _Process(double delta)
        {
            base._Process(delta);

            if (_isReloading)
            {
                _reloadTimer -= (float)delta;
                if (_reloadTimer <= 0f)
                    FinishReload();
            }
        }

        // ── WeaponBase override ────────────────────────────────────────────────

        protected override void SpawnProjectiles(
            Vector2 origin, Vector2 direction,
            float damage, bool isCrit, float critMul,
            CharacterStats stats)
        {
            if (_isReloading) return;

            // Infinite ammo if MaxAmmo == -1.
            if (MaxAmmo != -1)
            {
                if (CurrentAmmo <= 0)
                {
                    OnOutOfAmmo?.Invoke();
                    GD.Print("[GunWeapon] Out of ammo — starting reload");
                    Reload();
                    return;
                }
                CurrentAmmo--;
                OnAmmoChanged?.Invoke(CurrentAmmo);
            }

            _lastDamage  = damage;
            _lastIsCrit  = isCrit;
            _lastCritMul = critMul;

            for (int i = 0; i < PelletsPerShot; i++)
            {
                float spreadRad = SpreadAngle > 0f
                    ? Mathf.DegToRad((float)GD.RandRange(-SpreadAngle * 0.5, SpreadAngle * 0.5))
                    : 0f;
                SpawnProjectile(origin, direction.Rotated(spreadRad), spreadRad);
            }

            GD.Print($"[GunWeapon] Fired ({GunStyle}) — {PelletsPerShot} pellet(s), ammo={CurrentAmmo}");
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Convenience wrapper used by PlayerCombat.</summary>
        public void Fire(Vector2 direction)
        {
            // Routed through WeaponBase.Fire in the normal flow; this is for direct calls.
            if (_isReloading) return;
            SpawnProjectile(
                GetParentOrNull<Node2D>()?.GlobalPosition ?? Vector2.Zero,
                direction.Normalized(),
                0f);
        }

        /// <summary>Aimed / scoped shot: fires a single pellet with halved spread.</summary>
        public void AltFire(Vector2 direction)
        {
            if (_isReloading) return;

            float savedSpread = SpreadAngle;
            float savedPellets = PelletsPerShot;

            SpreadAngle    = savedSpread * 0.2f;
            PelletsPerShot = 1;

            SpawnProjectile(
                GetParentOrNull<Node2D>()?.GlobalPosition ?? Vector2.Zero,
                direction.Normalized(),
                0f);

            SpreadAngle    = savedSpread;
            PelletsPerShot = (int)savedPellets;

            GD.Print("[GunWeapon] Aimed shot fired");
        }

        /// <summary>Begin reloading. Does nothing if already reloading or ammo is full.</summary>
        public void Reload()
        {
            if (_isReloading || MaxAmmo == -1 || CurrentAmmo == MaxAmmo) return;

            _isReloading = true;
            _reloadTimer = ReloadTime;
            OnReloadStarted?.Invoke();
            GD.Print($"[GunWeapon] Reload started ({ReloadTime:F1}s)");
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private void SpawnProjectile(Vector2 origin, Vector2 direction, float spread)
        {
            var p = CreateProjectile(origin, direction, _lastDamage, _lastIsCrit, _lastCritMul);
            p.ProjectileColor = Colors.Yellow;
            AddProjectileToScene(p);
        }

        private void FinishReload()
        {
            _isReloading = false;
            CurrentAmmo  = MaxAmmo;
            OnAmmoChanged?.Invoke(CurrentAmmo);
            OnReloadFinished?.Invoke();
            GD.Print("[GunWeapon] Reload complete");
        }

        private void ConfigureForGunType()
        {
            switch (GunStyle)
            {
                case GunType.Pistol:
                    WeaponName     = "Pistol";
                    BaseDamage     = 4f;
                    FireRate       = 0.4f;
                    PelletsPerShot = 1;
                    SpreadAngle    = 0f;
                    IsAutomatic    = false;
                    MaxAmmo        = 12;
                    CurrentAmmo    = 12;
                    ReloadTime     = 1.0f;
                    ProjectileSpeed = 450f;
                    KnockbackForce  = 100f;
                    break;

                case GunType.Shotgun:
                    WeaponName     = "Shotgun";
                    BaseDamage     = 3f;
                    FireRate       = 0.9f;
                    PelletsPerShot = 6;
                    SpreadAngle    = 20f;
                    IsAutomatic    = false;
                    MaxAmmo        = 6;
                    CurrentAmmo    = 6;
                    ReloadTime     = 1.8f;
                    ProjectileSpeed = 350f;
                    KnockbackForce  = 200f;
                    break;

                case GunType.AutoRifle:
                    WeaponName     = "Auto Rifle";
                    BaseDamage     = 2f;
                    FireRate       = 0.1f;
                    PelletsPerShot = 1;
                    SpreadAngle    = 4f;
                    IsAutomatic    = true;
                    MaxAmmo        = 30;
                    CurrentAmmo    = 30;
                    ReloadTime     = 1.5f;
                    ProjectileSpeed = 500f;
                    KnockbackForce  = 60f;
                    break;

                case GunType.Sniper:
                    WeaponName     = "Sniper";
                    BaseDamage     = 15f;
                    FireRate       = 1.5f;
                    PelletsPerShot = 1;
                    SpreadAngle    = 0f;
                    IsAutomatic    = false;
                    MaxAmmo        = 5;
                    CurrentAmmo    = 5;
                    ReloadTime     = 2.5f;
                    ProjectileSpeed = 700f;
                    KnockbackForce  = 250f;
                    PierceCount    = 3;
                    break;
            }
        }
    }
}
