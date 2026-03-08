using Godot;
using DungeonCrawler.Core;
using DungeonCrawler.Stats;

namespace DungeonCrawler.Player
{
    /// <summary>
    /// Handles movement physics for the player: smooth acceleration, friction, and dodge bursts.
    /// Attach as a child node of the player CharacterBody2D.
    /// </summary>
    public partial class PlayerMovement : Node
    {
        // ── Inspector exports ──────────────────────────────────────────────────
        [Export] public float Acceleration  = 800f;
        [Export] public float Friction      = 600f;
        [Export] public float MaxSpeed      = 200f;
        [Export] public float DodgeSpeed    = 400f;
        [Export] public float DodgeDuration = 0.2f;

        // ── Public state ───────────────────────────────────────────────────────
        /// <summary>Current facing direction (unit vector, updated by UpdateFacingDirection).</summary>
        public Vector2 FacingDirection { get; private set; } = Vector2.Right;

        // ── Private references ─────────────────────────────────────────────────
        private CharacterBody2D? _body;
        private bool   _isDodging      = false;
        private float  _dodgeTimer     = 0f;
        private Vector2 _dodgeDirection = Vector2.Zero;

        // ── Godot lifecycle ────────────────────────────────────────────────────
        public override void _Ready()
        {
            _body = GetParentOrNull<CharacterBody2D>();
            if (_body == null)
                GD.PrintErr("[PlayerMovement] Parent must be a CharacterBody2D.");
        }

        public override void _PhysicsProcess(double delta)
        {
            if (_isDodging)
            {
                _dodgeTimer -= (float)delta;
                _body?.MoveAndSlide();

                if (_dodgeTimer <= 0f)
                {
                    _isDodging = false;
                    GD.Print("[PlayerMovement] Dodge complete");
                }
            }
        }

        // ── Input reading ──────────────────────────────────────────────────────

        /// <summary>
        /// Reads WASD / arrow keys and returns a normalised direction vector.
        /// Returns Vector2.Zero when no movement keys are held.
        /// </summary>
        public Vector2 GetMovementInput()
        {
            Vector2 dir = Vector2.Zero;

            if (Input.IsActionPressed("ui_right") || Input.IsKeyPressed(Key.D)) dir.X += 1f;
            if (Input.IsActionPressed("ui_left")  || Input.IsKeyPressed(Key.A)) dir.X -= 1f;
            if (Input.IsActionPressed("ui_down")  || Input.IsKeyPressed(Key.S)) dir.Y += 1f;
            if (Input.IsActionPressed("ui_up")    || Input.IsKeyPressed(Key.W)) dir.Y -= 1f;

            return dir.Length() > 0f ? dir.Normalized() : Vector2.Zero;
        }

        // ── Movement application ───────────────────────────────────────────────

        /// <summary>
        /// Accelerates toward <paramref name="input"/> direction or applies friction when idle.
        /// Call every physics frame (pass <c>GetPhysicsProcessDeltaTime()</c> as delta).
        /// </summary>
        public void ApplyMovement(float delta, Vector2 input)
        {
            if (_body == null) return;

            if (_isDodging) return; // Movement locked during dodge.

            if (input != Vector2.Zero)
            {
                _body.Velocity = _body.Velocity.MoveToward(
                    input * MaxSpeed,
                    Acceleration * delta);

                FacingDirection = input.Normalized();
            }
            else
            {
                ApplyFriction(delta);
            }

            _body.MoveAndSlide();
        }

        /// <summary>
        /// Decelerates the body to zero using the configured Friction value.
        /// Called automatically from ApplyMovement when there is no input.
        /// </summary>
        public void ApplyFriction(float delta)
        {
            if (_body == null) return;
            _body.Velocity = _body.Velocity.MoveToward(Vector2.Zero, Friction * delta);
        }

        /// <summary>
        /// Launches a dodge burst in <paramref name="direction"/>.
        /// While dodging, normal movement input is ignored.
        /// </summary>
        public void ApplyDodge(Vector2 direction)
        {
            if (_body == null || _isDodging) return;

            _isDodging      = true;
            _dodgeTimer     = DodgeDuration;
            _dodgeDirection = direction.Length() > 0f ? direction.Normalized() : FacingDirection;
            _body.Velocity  = _dodgeDirection * DodgeSpeed;

            GD.Print($"[PlayerMovement] Dodge launched — direction={_dodgeDirection}");
        }

        /// <summary>
        /// Updates <see cref="FacingDirection"/> to point toward a given aim direction.
        /// Rotates any Sprite2D child of the parent to match.
        /// </summary>
        public void UpdateFacingDirection(Vector2 aimDirection)
        {
            if (aimDirection == Vector2.Zero) return;

            FacingDirection = aimDirection.Normalized();

            // Rotate the weapon pivot or sprite to face aim direction.
            if (_body != null)
            {
                var weaponPivot = _body.GetNodeOrNull<Node2D>("WeaponPivot");
                if (weaponPivot != null)
                    weaponPivot.Rotation = FacingDirection.Angle();
            }
        }

        /// <summary>Whether the body is currently in a dodge burst.</summary>
        public bool IsDodging => _isDodging;
    }
}
