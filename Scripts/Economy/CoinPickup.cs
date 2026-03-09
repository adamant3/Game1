using System;
using Godot;
using DungeonCrawler.Core;

namespace DungeonCrawler.Economy
{
    /// <summary>
    /// A coin pickup node that bobs in place and magnetises toward the player
    /// when they get close enough.
    /// Implements IDroppable for the pickup system.
    /// </summary>
    public partial class CoinPickup : Area2D, IDroppable
    {
        [Export] public int Amount { get; set; } = 1;

        // ── Visual ─────────────────────────────────────────────────────────────
        private ColorRect _visual = null!;

        // ── Bob animation ──────────────────────────────────────────────────────
        private float _bobTimer = 0f;
        private const float BobSpeed      = 3.0f;
        private const float BobAmplitude  = 4.0f;

        // ── Magnet behaviour ───────────────────────────────────────────────────
        private float _magnetRange  = 80f;
        private float _magnetSpeed  = 300f;
        private bool  _isBeingMagnetized = false;
        private Node2D? _target;

        // ── Godot lifecycle ────────────────────────────────────────────────────

        public override void _Ready()
        {
            AddToGroup("Pickups");

            // Build a yellow square visual.
            _visual = new ColorRect
            {
                Color = new Color(1f, 0.85f, 0f),
                Name  = "Visual"
            };
            AddChild(_visual);

            // Collision shape for overlap detection.
            var shape = new CircleShape2D { Radius = 12f };
            var collision = new CollisionShape2D { Shape = shape };
            AddChild(collision);

            BodyEntered += OnBodyEntered;

            // Apply sizing based on amount.
            Initialize(Amount);
        }

        public override void _Process(double delta)
        {
            // Bob up and down.
            _bobTimer += (float)delta * BobSpeed;
            _visual.Position = new Vector2(
                _visual.Position.X,
                Mathf.Sin(_bobTimer) * BobAmplitude - _visual.Size.Y / 2f);

            if (_isBeingMagnetized && _target != null && IsInstanceValid(_target))
            {
                Vector2 direction = (_target.GlobalPosition - GlobalPosition).Normalized();
                GlobalPosition  += direction * _magnetSpeed * (float)delta;
            }
            else
            {
                CheckMagnetRange();
            }
        }

        // ── IDroppable ─────────────────────────────────────────────────────────

        public void OnPickup(Godot.Node collector)
        {
            GD.Print($"[CoinPickup] {Amount} coin(s) picked up by {collector.Name}.");
            // AddCoins is responsible for updating the coin total and raising events.
            if (collector.HasMethod("AddCoins"))
                collector.Call("AddCoins", Amount);
            QueueFree();
        }

        public bool CanPickup(Godot.Node collector) => collector.IsInGroup(Constants.TAG_PLAYER);

        // ── Initialisation ─────────────────────────────────────────────────────

        /// <summary>Sets the coin amount and adjusts the visual size accordingly.</summary>
        public void Initialize(int amount)
        {
            Amount = amount;

            Vector2 size;
            if (amount <= 5)
                size = new Vector2(14f, 14f);
            else if (amount <= 10)
                size = new Vector2(20f, 20f);
            else
                size = new Vector2(28f, 28f);

            _visual.Size     = size;
            _visual.Position = new Vector2(-size.X / 2f, -size.Y / 2f);
        }

        // ── Collision callbacks ────────────────────────────────────────────────

        private void OnBodyEntered(Node2D body)
        {
            if (CanPickup(body))
                OnPickup(body);
        }

        // ── Magnet ─────────────────────────────────────────────────────────────

        private void CheckMagnetRange()
        {
            var players = GetTree().GetNodesInGroup(Constants.TAG_PLAYER);
            foreach (Node node in players)
            {
                if (node is not Node2D player2D) continue;
                if (GlobalPosition.DistanceTo(player2D.GlobalPosition) <= _magnetRange)
                {
                    _isBeingMagnetized = true;
                    _target = player2D;
                    return;
                }
            }
            _isBeingMagnetized = false;
            _target = null;
        }
    }
}
