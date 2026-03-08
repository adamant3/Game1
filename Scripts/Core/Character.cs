using System.Collections;
using Godot;
using DungeonCrawler.Stats;

namespace DungeonCrawler.Core
{
    /// <summary>
    /// Extends Entity with locomotion, facing direction, and hit-flash visuals.
    /// All player/enemy classes derive from this.
    /// </summary>
    public abstract partial class Character : Entity
    {
        // ── Inspector exports ──────────────────────────────────────────────────
        [Export] public float MoveSpeed  { get; set; } = Constants.PLAYER_BASE_SPEED;

        // ── State ──────────────────────────────────────────────────────────────
        public  Vector2 FacingDirection { get; protected set; } = Vector2.Right;
        protected bool  _isMovementLocked = false;

        // ── Visual flash fields ────────────────────────────────────────────────
        private Sprite2D?   _sprite;
        private bool        _isFlashing      = false;
        private float       _flashTimer      = 0f;
        private float       _flashInterval   = 0.07f;
        private int         _flashCount      = 6;
        private int         _flashesLeft     = 0;
        private Color       _originalColor   = Colors.White;

        // ── Godot lifecycle ────────────────────────────────────────────────────
        protected override void OnReady()
        {
            base.OnReady();
            _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
            if (_sprite != null)
                _originalColor = _sprite.Modulate;
        }

        public override void _Process(double delta)
        {
            TickFlash((float)delta);
        }

        // ── Abstract interface ─────────────────────────────────────────────────
        /// <summary>Subclasses read input / AI decisions and set Velocity here.</summary>
        public abstract void Move(float delta);

        // ── Locomotion helpers ─────────────────────────────────────────────────
        public void ApplyMovement(Vector2 direction, float? speedOverride = null)
        {
            if (_isMovementLocked)
            {
                Velocity = Vector2.Zero;
                MoveAndSlide();
                return;
            }

            float speed = speedOverride ?? Stats.GetStat(StatType.Speed);
            Velocity    = direction.Normalized() * speed;

            if (direction != Vector2.Zero)
                FacingDirection = direction.Normalized();

            MoveAndSlide();
        }

        public void ApplyKnockback(Vector2 direction, float force)
        {
            Velocity = direction.Normalized() * force;
            MoveAndSlide();
        }

        protected void LockMovement(float duration)
        {
            _isMovementLocked = true;
            var timer = GetTree().CreateTimer(duration);
            timer.Timeout += () => _isMovementLocked = false;
        }

        // ── Hit-flash visual ───────────────────────────────────────────────────
        protected override void OnDamageReceived(float amount, DamageType type)
        {
            StartHitFlash();
        }

        private void StartHitFlash()
        {
            if (_sprite == null) return;
            _isFlashing  = true;
            _flashTimer  = 0f;
            _flashesLeft = _flashCount;
        }

        private void TickFlash(float delta)
        {
            if (!_isFlashing || _sprite == null) return;

            _flashTimer -= delta;
            if (_flashTimer <= 0f)
            {
                _flashTimer   = _flashInterval;
                _flashesLeft--;

                // Toggle between white-tinted and original each interval.
                bool showWhite  = (_flashesLeft % 2) == 0;
                _sprite.Modulate = showWhite ? Colors.White : _originalColor;

                if (_flashesLeft <= 0)
                {
                    _isFlashing      = false;
                    _sprite.Modulate = _originalColor;
                }
            }
        }

        // ── Utility ────────────────────────────────────────────────────────────
        /// <summary>
        /// Face the given world position (updates FacingDirection).
        /// </summary>
        protected void FaceToward(Vector2 worldPos)
        {
            Vector2 dir = (worldPos - GlobalPosition);
            if (dir.LengthSquared() > 0.01f)
                FacingDirection = dir.Normalized();
        }
    }
}
