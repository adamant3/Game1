using System;
using System.Collections.Generic;
using Godot;
using DungeonCrawler.Core;
using DungeonCrawler.Stats;

namespace DungeonCrawler.Weapons
{
    /// <summary>Flavour of melee weapon — controls speed, damage, and knockback defaults.</summary>
    public enum MeleeWeaponType { Sword, Hammer, Dagger }

    /// <summary>
    /// Melee weapon that performs a cone sweep on Fire().
    /// Subtype (Sword / Hammer / Dagger) is configured via <see cref="MeleeType"/>.
    /// </summary>
    public partial class MeleeWeapon : WeaponBase
    {
        // ── Inspector exports ──────────────────────────────────────────────────
        [Export] public float          SwingArc      = 120f;   // degrees
        [Export] public float          SwingDuration = 0.25f;  // seconds
        [Export] public MeleeWeaponType MeleeType    = MeleeWeaponType.Sword;

        // ── Hit detection ──────────────────────────────────────────────────────
        private Area2D?            _hitArea;
        private CollisionShape2D?  _hitShape;
        private bool               _isSwinging           = false;
        private float              _swingTimer            = 0f;
        private float              _swingDamageMultiplier = 1f;
        private readonly List<Node> _hitEnemies           = new();

        // ── Godot lifecycle ────────────────────────────────────────────────────
        public override void _Ready()
        {
            base._Ready();
            ConfigureForType();
            BuildHitArea();
        }

        public override void _Process(double delta)
        {
            base._Process(delta);

            if (!_isSwinging) return;

            _swingTimer -= (float)delta;
            CheckHits();

            if (_swingTimer <= 0f)
            {
                _isSwinging = false;
                _hitEnemies.Clear();
                if (_hitArea != null)
                    _hitArea.Monitoring = false;
                GD.Print("[MeleeWeapon] Swing ended");
            }
        }

        // ── WeaponBase override ────────────────────────────────────────────────

        /// <summary>
        /// Called by WeaponBase.Fire(); starts a melee swing (ignores projectile origin/stats
        /// since melee is positional and uses BaseDamage + stat scaling directly).
        /// </summary>
        protected override void SpawnProjectiles(
            Vector2 origin, Vector2 direction,
            float damage, bool isCrit, float critMul,
            CharacterStats stats)
        {
            float mul = isCrit ? critMul : 1f;
            TriggerSwing(direction, mul);
        }

        // ── Public melee API ───────────────────────────────────────────────────

        /// <summary>
        /// Convenience wrapper: trigger primary swing in <paramref name="direction"/>.
        /// </summary>
        public void Fire(Vector2 direction) => TriggerSwing(direction, 1f);

        /// <summary>
        /// Heavy alternate attack: 2× damage, uses the same swing duration multiplied by 1.5.
        /// </summary>
        public void AltFire(Vector2 direction)
        {
            if (_isSwinging) return;

            // Temporarily lengthen swing for the heavy attack.
            float original = SwingDuration;
            SwingDuration = original * 1.5f;
            TriggerSwing(direction, 2f);
            SwingDuration = original;
        }

        // ── Hit checking ───────────────────────────────────────────────────────

        /// <summary>
        /// Called every frame during a swing.  Damages any enemy in the hit area
        /// that has not already been struck this swing.
        /// </summary>
        public void CheckHits()
        {
            if (_hitArea == null || !_isSwinging) return;

            foreach (Node2D body in _hitArea.GetOverlappingBodies())
            {
                if (_hitEnemies.Contains(body)) continue;
                if (!body.IsInGroup(Constants.TAG_ENEMY)) continue;

                _hitEnemies.Add(body);

                float damage = BaseDamage * _swingDamageMultiplier;
                if (body is Entity entity)
                    entity.TakeDamage(damage, DamageType.Physical);

                if (body is Character character)
                    character.ApplyKnockback(
                        (body.GlobalPosition - GlobalPosition).Normalized(),
                        KnockbackForce);

                GD.Print($"[MeleeWeapon] Hit {body.Name} for {damage:F1} ({MeleeType})");
            }
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private void TriggerSwing(Vector2 direction, float damageMultiplier = 1f)
        {
            if (_isSwinging) return;

            _swingDamageMultiplier = damageMultiplier;
            _swingTimer            = SwingDuration;
            _isSwinging            = true;
            _hitEnemies.Clear();

            if (_hitArea != null)
            {
                // Position the hit area forward in the swing direction.
                float reach = MathF.Max(BaseDamage * 5f, 40f);
                _hitArea.Position   = direction.Normalized() * reach * 0.5f;
                _hitArea.Rotation   = direction.Angle();
                _hitArea.Monitoring = true;
            }

            GD.Print($"[MeleeWeapon] Swing started ({MeleeType}) dir={direction} mul={damageMultiplier}");
        }

        private void ConfigureForType()
        {
            switch (MeleeType)
            {
                case MeleeWeaponType.Sword:
                    WeaponName     = "Sword";
                    BaseDamage     = 5f;
                    FireRate       = 0.4f;
                    KnockbackForce = 100f;
                    SwingDuration  = 0.25f;
                    SwingArc       = 120f;
                    break;

                case MeleeWeaponType.Hammer:
                    WeaponName     = "Hammer";
                    BaseDamage     = 12f;
                    FireRate       = 1.0f;
                    KnockbackForce = 300f;
                    SwingDuration  = 0.5f;
                    SwingArc       = 90f;
                    break;

                case MeleeWeaponType.Dagger:
                    WeaponName     = "Dagger";
                    BaseDamage     = 3f;
                    FireRate       = 0.2f;
                    KnockbackForce = 50f;
                    SwingDuration  = 0.15f;
                    SwingArc       = 80f;
                    break;
            }
        }

        private void BuildHitArea()
        {
            _hitArea = new Area2D { Name = "MeleeHitArea" };
            _hitArea.CollisionLayer = 0;
            _hitArea.CollisionMask  = Constants.MASK_ENEMY;
            _hitArea.Monitoring     = false;

            _hitShape = new CollisionShape2D();
            _hitShape.Shape = new CapsuleShape2D
            {
                Radius = 14f,
                Height = MathF.Max(BaseDamage * 6f, 48f)
            };

            _hitArea.AddChild(_hitShape);
            AddChild(_hitArea);
        }
    }
}
