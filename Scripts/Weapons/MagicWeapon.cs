using System;
using Godot;
using DungeonCrawler.Core;
using DungeonCrawler.Stats;
using DungeonCrawler.Projectiles;

namespace DungeonCrawler.Weapons
{
    /// <summary>Type of magic spell this weapon casts.</summary>
    public enum MagicSpellType { Bolt, Orb, Nova, Beam, Summon }

    /// <summary>
    /// Magic weapon — spends mana to cast spells.
    /// The active spell type is controlled by <see cref="SpellType"/>.
    /// </summary>
    public partial class MagicWeapon : WeaponBase
    {
        // ── Inspector exports ──────────────────────────────────────────────────
        [Export] public MagicSpellType SpellType { get; set; } = MagicSpellType.Bolt;
        [Export] public float          ManaCost  { get; set; } = 10f;

        // ── Events ─────────────────────────────────────────────────────────────
        public event Action<MagicSpellType>? OnSpellCast;
        public event Action?                 OnOutOfMana;

        // ── Cached per-cast values ─────────────────────────────────────────────
        private CharacterStats? _cachedStats;
        private float  _cachedDamage  = 0f;
        private bool   _cachedIsCrit  = false;
        private float  _cachedCritMul = 1f;

        // ── Godot lifecycle ────────────────────────────────────────────────────
        public override void _Ready()
        {
            base._Ready();
            ConfigureForSpellType();
        }

        // ── WeaponBase override ────────────────────────────────────────────────

        protected override void SpawnProjectiles(
            Vector2 origin, Vector2 direction,
            float damage, bool isCrit, float critMul,
            CharacterStats stats)
        {
            // Check mana before casting.
            float currentMana = stats.GetStat(StatType.Mana);
            if (currentMana < ManaCost)
            {
                OnOutOfMana?.Invoke();
                GD.Print("[MagicWeapon] Not enough mana to cast");
                return;
            }

            // Deduct mana.
            stats.SetBaseStat(StatType.Mana, currentMana - ManaCost);

            _cachedStats  = stats;
            _cachedDamage  = damage;
            _cachedIsCrit  = isCrit;
            _cachedCritMul = critMul;

            switch (SpellType)
            {
                case MagicSpellType.Bolt:   CastBolt(origin, direction);  break;
                case MagicSpellType.Orb:    CastOrb(origin, direction);   break;
                case MagicSpellType.Nova:   CastNova(origin);             break;
                case MagicSpellType.Beam:   CastBeam(origin, direction);  break;
                case MagicSpellType.Summon: CastSummon(origin, direction); break;
            }

            OnSpellCast?.Invoke(SpellType);
            GD.Print($"[MagicWeapon] Cast {SpellType} — mana remaining: {stats.GetStat(StatType.Mana):F0}");
        }

        // ── Convenience wrappers ───────────────────────────────────────────────

        /// <summary>Direct fire call used by PlayerCombat.</summary>
        public void Fire(Vector2 direction)
        {
            var origin = GetParentOrNull<Node2D>()?.GlobalPosition ?? Vector2.Zero;
            if (_cachedStats != null)
                SpawnProjectiles(origin, direction, _cachedDamage, _cachedIsCrit, _cachedCritMul, _cachedStats);
        }

        // ── Spell implementations ──────────────────────────────────────────────

        /// <summary>Fast single projectile, moderate damage.</summary>
        public void CastBolt(Vector2 origin, Vector2 direction)
        {
            var p = CreateProjectile(origin, direction.Normalized(),
                _cachedDamage, _cachedIsCrit, _cachedCritMul);
            p.ProjectileColor = Colors.Cyan;
            p.Speed           = ProjectileSpeed * 1.3f;
            AddProjectileToScene(p);
        }

        /// <summary>Slow large projectile, high damage.</summary>
        public void CastOrb(Vector2 origin, Vector2 direction)
        {
            var p = CreateProjectile(origin, direction.Normalized(),
                _cachedDamage * 2.5f, _cachedIsCrit, _cachedCritMul);
            p.ProjectileColor  = Colors.Purple;
            p.Speed            = ProjectileSpeed * 0.5f;
            p.ProjectileScale  = 2.5f;
            p.Lifetime        *= 1.5f;
            AddProjectileToScene(p);
        }

        /// <summary>Burst in all directions, low damage per projectile.</summary>
        public void CastNova(Vector2 origin)
        {
            int   count = 12;
            float step  = MathF.Tau / count;

            for (int i = 0; i < count; i++)
            {
                Vector2 dir = Vector2.Right.Rotated(step * i);
                var p       = CreateProjectile(origin, dir,
                    _cachedDamage * 0.4f, _cachedIsCrit, _cachedCritMul);
                p.ProjectileColor = Colors.OrangeRed;
                p.Lifetime        = 0.6f;
                AddProjectileToScene(p);
            }
        }

        /// <summary>
        /// Instant raycast beam — damages every enemy along the line.
        /// Implemented as a series of overlapping hit checks along the ray.
        /// </summary>
        public void CastBeam(Vector2 origin, Vector2 direction)
        {
            float range  = 600f;
            float step   = 24f;
            float damage = _cachedDamage * 0.6f;

            Vector2 dir = direction.Normalized();
            var     hit  = new System.Collections.Generic.HashSet<Node>();

            for (float dist = step; dist <= range; dist += step)
            {
                Vector2 point = origin + dir * dist;
                foreach (Node node in GetTree().GetNodesInGroup(Constants.TAG_ENEMY))
                {
                    if (node is not Node2D en)     continue;
                    if (hit.Contains(en))          continue;
                    if (en.GlobalPosition.DistanceTo(point) > 20f) continue;

                    hit.Add(en);
                    if (en is Entity entity)
                        entity.TakeDamage(damage, DamageType.Magical);
                }
            }

            GD.Print($"[MagicWeapon] Beam hit {hit.Count} target(s)");
        }

        /// <summary>
        /// Summon spell — for now spawns three slow homing-style projectiles.
        /// </summary>
        public void CastSummon(Vector2 origin, Vector2 direction)
        {
            float[] angles = { -20f, 0f, 20f };
            foreach (float a in angles)
            {
                Vector2 dir = direction.Normalized().Rotated(Mathf.DegToRad(a));
                var p = CreateProjectile(origin, dir,
                    _cachedDamage * 0.8f, _cachedIsCrit, _cachedCritMul);
                p.ProjectileColor = Colors.LimeGreen;
                p.Speed           = ProjectileSpeed * 0.6f;
                p.Lifetime       *= 2f;
                AddProjectileToScene(p);
            }
        }

        // ── Private ────────────────────────────────────────────────────────────

        private void ConfigureForSpellType()
        {
            switch (SpellType)
            {
                case MagicSpellType.Bolt:
                    WeaponName      = "Magic Bolt";
                    BaseDamage      = 8f;
                    FireRate        = 0.4f;
                    ManaCost        = 8f;
                    ProjectileSpeed = 500f;
                    break;

                case MagicSpellType.Orb:
                    WeaponName      = "Magic Orb";
                    BaseDamage      = 14f;
                    FireRate        = 1.0f;
                    ManaCost        = 20f;
                    ProjectileSpeed = 220f;
                    break;

                case MagicSpellType.Nova:
                    WeaponName      = "Nova Burst";
                    BaseDamage      = 5f;
                    FireRate        = 0.8f;
                    ManaCost        = 15f;
                    ProjectileSpeed = 280f;
                    break;

                case MagicSpellType.Beam:
                    WeaponName      = "Arcane Beam";
                    BaseDamage      = 6f;
                    FireRate        = 0.15f;
                    ManaCost        = 4f;
                    ProjectileSpeed = 0f;  // Not used — beam is instant.
                    break;

                case MagicSpellType.Summon:
                    WeaponName      = "Summon";
                    BaseDamage      = 4f;
                    FireRate        = 1.5f;
                    ManaCost        = 25f;
                    ProjectileSpeed = 200f;
                    break;
            }
        }
    }
}
