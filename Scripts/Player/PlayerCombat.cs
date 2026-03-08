using System;
using System.Collections.Generic;
using Godot;
using DungeonCrawler.Core;
using DungeonCrawler.Stats;
using DungeonCrawler.Weapons;

namespace DungeonCrawler.Player
{
    /// <summary>
    /// Handles all combat logic for the player: primary/secondary attacks, dodging,
    /// blocking, hit reception, melee sweeps, and damage calculation.
    /// Attach as a child node of the player CharacterBody2D.
    /// </summary>
    public partial class PlayerCombat : Node
    {
        // ── Inspector exports ──────────────────────────────────────────────────
        [Export] public float MeleeAttackRange  = 60f;
        [Export] public float MeleeAttackAngle  = 90f;   // degrees of the melee cone
        [Export] public float InvincibilityTime = 0.5f;

        // ── Events ─────────────────────────────────────────────────────────────
        public event Action?         OnDodgeStarted;
        public event Action?         OnDodgeEnded;
        public event Action<Vector2>? OnAttackPerformed;
        public event Action?         OnBlockStarted;
        public event Action?         OnBlockEnded;

        // ── Public properties ──────────────────────────────────────────────────
        public bool        IsInvincible          => _invincibilityTimer > 0f;
        public bool        IsBlocking            => _isBlocking;
        public bool        IsDodging             => _isDodging;
        public WeaponBase? CurrentWeaponPrimary   { get; private set; }
        public WeaponBase? CurrentWeaponSecondary { get; private set; }

        // ── Timers ─────────────────────────────────────────────────────────────
        private float _primaryCooldownTimer   = 0f;
        private float _secondaryCooldownTimer = 0f;
        private float _invincibilityTimer     = 0f;
        private float _dodgeCooldownTimer     = 0f;

        // ── State flags ────────────────────────────────────────────────────────
        private bool _isAttacking = false;
        private bool _isBlocking  = false;
        private bool _isDodging   = false;

        // ── Invincibility flash ────────────────────────────────────────────────
        private float _flashTimer   = 0f;
        private bool  _flashVisible = true;

        // ── Melee sweep area ───────────────────────────────────────────────────
        private Area2D? _meleeDamageArea;

        // ── Cached references ──────────────────────────────────────────────────
        private Entity?         _ownerEntity;
        private CharacterStats? _stats;

        // ── Godot lifecycle ────────────────────────────────────────────────────
        public override void _Ready()
        {
            _ownerEntity = GetParentOrNull<Entity>();
            // Prefer a dedicated PlayerStats child; fall back to the Entity's own stats node.
            _stats = GetParentOrNull<Node>()?.GetNodeOrNull<PlayerStats>("PlayerStats")
                  ?? _ownerEntity?.Stats;

            BuildMeleeArea();
        }

        public override void _Process(double delta)
        {
            float dt = (float)delta;

            if (_primaryCooldownTimer   > 0f) _primaryCooldownTimer   -= dt;
            if (_secondaryCooldownTimer > 0f) _secondaryCooldownTimer -= dt;
            if (_dodgeCooldownTimer     > 0f) _dodgeCooldownTimer     -= dt;

            if (_invincibilityTimer > 0f)
            {
                _invincibilityTimer -= dt;
                TickInvincibilityFlash(dt);
            }
        }

        // ── Weapon slots ───────────────────────────────────────────────────────
        /// <summary>Equip primary and optional secondary weapon.</summary>
        public void SetWeapons(WeaponBase? primary, WeaponBase? secondary = null)
        {
            CurrentWeaponPrimary   = primary;
            CurrentWeaponSecondary = secondary;
        }

        // ── Attack methods ─────────────────────────────────────────────────────

        /// <summary>
        /// Attempt primary attack in <paramref name="direction"/>.
        /// Uses equipped primary weapon; falls back to bare-hands melee.
        /// Returns true if an attack was launched.
        /// </summary>
        public bool TryPrimaryAttack(Vector2 direction)
        {
            if (_primaryCooldownTimer > 0f || _isBlocking || _isDodging)
                return false;

            OnAttackPerformed?.Invoke(direction);

            if (CurrentWeaponPrimary != null)
            {
                var stats = GetStats();
                if (stats == null) return false;

                Vector2 origin = _ownerEntity?.GlobalPosition ?? Vector2.Zero;
                bool fired = CurrentWeaponPrimary.Fire(origin, direction.Normalized(), stats);
                if (fired)
                {
                    _isAttacking = true;
                    _primaryCooldownTimer = CurrentWeaponPrimary.FireRate;
                    GD.Print($"[PlayerCombat] Primary attack fired ({CurrentWeaponPrimary.WeaponName})");
                    return true;
                }
            }
            else
            {
                // No weapon — bare-hands melee swing.
                PerformMeleeAttack(direction);
                _primaryCooldownTimer = 0.4f;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempt secondary attack / alternate fire.
        /// Falls back to block if no secondary weapon is equipped.
        /// Returns true if action was taken.
        /// </summary>
        public bool TrySecondaryAttack(Vector2 direction)
        {
            if (_secondaryCooldownTimer > 0f || _isDodging)
                return false;

            if (CurrentWeaponSecondary != null)
            {
                var stats = GetStats();
                if (stats == null) return false;

                Vector2 origin = _ownerEntity?.GlobalPosition ?? Vector2.Zero;
                bool fired = CurrentWeaponSecondary.Fire(origin, direction.Normalized(), stats);
                if (fired)
                {
                    _secondaryCooldownTimer = CurrentWeaponSecondary.FireRate;
                    GD.Print($"[PlayerCombat] Secondary attack fired ({CurrentWeaponSecondary.WeaponName})");
                    return true;
                }
            }
            else
            {
                if (!_isBlocking)
                    StartBlock();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Release secondary / stop blocking.
        /// </summary>
        public void ReleaseSecondary()
        {
            if (_isBlocking)
                StopBlock();
        }

        // ── Dodge ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Attempt a dodge roll in <paramref name="direction"/>.
        /// Grants invincibility frames and applies a speed burst.
        /// Returns true if dodge was started.
        /// </summary>
        public bool TryDodge(Vector2 direction)
        {
            if (_dodgeCooldownTimer > 0f || _isDodging)
                return false;

            if (direction == Vector2.Zero)
                direction = Vector2.Right;

            _isDodging          = true;
            _dodgeCooldownTimer = 0.8f;
            _invincibilityTimer = 0.3f;

            if (_ownerEntity is Character character)
                character.ApplyMovement(direction.Normalized(), 500f);

            OnDodgeStarted?.Invoke();
            GD.Print("[PlayerCombat] Dodge started");

            // Auto-end dodge after duration.
            GetTree().CreateTimer(0.2f).Timeout += EndDodge;
            return true;
        }

        private void EndDodge()
        {
            _isDodging = false;
            OnDodgeEnded?.Invoke();
            GD.Print("[PlayerCombat] Dodge ended");
        }

        // ── Block ──────────────────────────────────────────────────────────────

        private void StartBlock()
        {
            _isBlocking = true;
            OnBlockStarted?.Invoke();
            GD.Print("[PlayerCombat] Block started");
        }

        private void StopBlock()
        {
            _isBlocking = false;
            OnBlockEnded?.Invoke();
            GD.Print("[PlayerCombat] Block ended");
        }

        // ── Hit reception ──────────────────────────────────────────────────────

        /// <summary>
        /// Called when the player should receive <paramref name="damage"/>.
        /// Applies blocking/armor reduction, then forwards to Entity.TakeDamage.
        /// Starts invincibility frames.
        /// </summary>
        public void OnHit(float damage, DamageType type)
        {
            if (IsInvincible) return;

            float reduced = damage;
            var   stats   = GetStats();

            if (stats != null)
            {
                if (_isBlocking)
                {
                    // Blocking halves incoming damage after shielding reduction.
                    float shieldPct = stats.GetStat(StatType.Shielding) / 100f;
                    reduced *= (1f - shieldPct) * 0.5f;
                }

                reduced = type switch
                {
                    DamageType.Physical => MathF.Max(0f, reduced - stats.GetStat(StatType.Armor)),
                    DamageType.Magical  => MathF.Max(0f, reduced - stats.GetStat(StatType.Shielding)),
                    _                   => reduced
                };
            }

            _ownerEntity?.TakeDamage(reduced, type);
            _invincibilityTimer = InvincibilityTime;
            GD.Print($"[PlayerCombat] Player hit for {reduced:F1} ({type}). Invincibility started.");
        }

        // ── Melee sweep ────────────────────────────────────────────────────────

        /// <summary>
        /// Performs an immediate melee cone check against all enemies in range.
        /// </summary>
        public void PerformMeleeAttack(Vector2 direction)
        {
            Vector2 origin = _ownerEntity?.GlobalPosition ?? Vector2.Zero;
            float halfAngle = MathF.PI * MeleeAttackAngle / 360f;
            float baseDmg   = GetStats()?.GetBaseStat(StatType.Damage) ?? 3f;
            float finalDmg  = CalculateDamage(baseDmg);

            GD.Print($"[PlayerCombat] Melee sweep — direction={direction} damage={finalDmg:F1}");

            foreach (Node node in GetTree().GetNodesInGroup(Constants.TAG_ENEMY))
            {
                if (node is not Node2D enemyNode) continue;
                Vector2 toEnemy = enemyNode.GlobalPosition - origin;
                if (toEnemy.Length() > MeleeAttackRange) continue;

                float angle = direction.Normalized().AngleTo(toEnemy.Normalized());
                if (MathF.Abs(angle) > halfAngle) continue;

                if (enemyNode is Entity entity)
                    entity.TakeDamage(finalDmg, DamageType.Physical);

                if (enemyNode is Character ch)
                    ch.ApplyKnockback(toEnemy.Normalized(), Constants.KNOCKBACK_FORCE);

                GD.Print($"[PlayerCombat] Melee hit: {enemyNode.Name}");
            }

            _isAttacking = false;
        }

        // ── Damage calculation ─────────────────────────────────────────────────

        /// <summary>
        /// Scales <paramref name="baseDamage"/> by the Damage stat and optionally a crit.
        /// </summary>
        public float CalculateDamage(float baseDamage)
        {
            var stats = GetStats();
            if (stats == null) return baseDamage;

            float dmg = baseDamage + stats.GetStat(StatType.Damage);
            if (IsCriticalHit())
            {
                dmg *= stats.GetStat(StatType.CritDamage);
                GD.Print($"[PlayerCombat] Critical hit! Damage: {dmg:F1}");
            }

            return dmg;
        }

        /// <summary>Random crit check against the CritChance stat.</summary>
        public bool IsCriticalHit()
        {
            float chance = GetStats()?.GetStat(StatType.CritChance) ?? 0.05f;
            return GD.Randf() < chance;
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private CharacterStats? GetStats() => _stats ?? _ownerEntity?.Stats;

        private void BuildMeleeArea()
        {
            _meleeDamageArea = new Area2D { Name = "MeleeDamageArea" };
            _meleeDamageArea.CollisionLayer = 0;
            _meleeDamageArea.CollisionMask  = Constants.MASK_ENEMY;
            _meleeDamageArea.Monitoring     = false;

            var shape = new CollisionShape2D();
            shape.Shape = new CapsuleShape2D { Radius = 10f, Height = 30f };
            _meleeDamageArea.AddChild(shape);

            AddChild(_meleeDamageArea);
        }

        private void TickInvincibilityFlash(float dt)
        {
            _flashTimer += dt;
            if (_flashTimer < 0.08f) return;

            _flashTimer   = 0f;
            _flashVisible = !_flashVisible;

            var sprite = GetParentOrNull<Node2D>()?.GetNodeOrNull<Sprite2D>("Sprite2D");
            if (sprite != null)
                sprite.Visible = _flashVisible;

            if (_invincibilityTimer <= 0f && sprite != null)
                sprite.Visible = true;
        }
    }
}
