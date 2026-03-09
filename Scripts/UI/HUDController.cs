using Godot;
using DungeonCrawler.Core;
using DungeonCrawler.Player;

namespace DungeonCrawler.UI
{
    /// <summary>
    /// Heads-Up Display: health bar, mana bar, coins, floor counter.
    /// Expects child nodes: HealthBar (ProgressBar), ManaBar, CoinsLabel, FloorLabel.
    /// </summary>
    public partial class HUDController : CanvasLayer
    {
        // ── Node references ────────────────────────────────────────────────────
        private ProgressBar? _healthBar;
        private ProgressBar? _manaBar;
        private Label?       _coinsLabel;
        private Label?       _floorLabel;
        private Label?       _messageLabel;
        private float        _messageDuration = 0f;

        // ── Godot lifecycle ────────────────────────────────────────────────────
        public override void _Ready()
        {
            _healthBar    = GetNodeOrNull<ProgressBar>("HealthBar");
            _manaBar      = GetNodeOrNull<ProgressBar>("ManaBar");
            _coinsLabel   = GetNodeOrNull<Label>("CoinsLabel");
            _floorLabel   = GetNodeOrNull<Label>("FloorLabel");
            _messageLabel = GetNodeOrNull<Label>("MessageLabel");

            // Subscribe to events.
            GameEvents.OnPlayerHealthChanged += UpdateHealth;
            GameEvents.OnPlayerManaChanged   += UpdateMana;
            GameEvents.OnPlayerCoinsChanged  += UpdateCoins;
            GameEvents.OnFloorChanged        += UpdateFloor;
            GameEvents.OnRoomCleared         += _ => ShowMessage("Room Cleared!", 2f);
            GameEvents.OnFloorCompleted      += f => ShowMessage($"Floor {f} Complete!", 3f);
            GameEvents.OnPlayerDied          += OnPlayerDied;

            // Default values.
            UpdateHealth(Constants.PLAYER_BASE_HEALTH, Constants.PLAYER_BASE_HEALTH);
            UpdateMana(Constants.PLAYER_BASE_MANA, Constants.PLAYER_BASE_MANA);
            UpdateCoins(0);
            UpdateFloor(1);
        }

        public override void _ExitTree()
        {
            GameEvents.OnPlayerHealthChanged -= UpdateHealth;
            GameEvents.OnPlayerManaChanged   -= UpdateMana;
            GameEvents.OnPlayerCoinsChanged  -= UpdateCoins;
            GameEvents.OnFloorChanged        -= UpdateFloor;
        }

        public override void _Process(double delta)
        {
            if (_messageDuration > 0f)
            {
                _messageDuration -= (float)delta;
                if (_messageDuration <= 0f && _messageLabel != null)
                    _messageLabel.Visible = false;
            }
        }

        // ── Update methods ─────────────────────────────────────────────────────
        private void UpdateHealth(float current, float max)
        {
            if (_healthBar == null) return;
            _healthBar.MaxValue = max;
            _healthBar.Value    = current;
        }

        private void UpdateMana(float current, float max)
        {
            if (_manaBar == null) return;
            _manaBar.MaxValue = max;
            _manaBar.Value    = current;
        }

        private void UpdateCoins(int coins)
        {
            if (_coinsLabel != null)
                _coinsLabel.Text = $"Coins: {coins}";
        }

        private void UpdateFloor(int floor)
        {
            if (_floorLabel != null)
                _floorLabel.Text = $"Floor: {floor}";
        }

        private void ShowMessage(string text, float duration)
        {
            if (_messageLabel == null) return;
            _messageLabel.Text    = text;
            _messageLabel.Visible = true;
            _messageDuration      = duration;
        }

        private void OnPlayerDied()
        {
            ShowMessage("YOU DIED", 999f);
        }
    }
}
