using Godot;
using DungeonCrawler.Managers;

namespace DungeonCrawler.UI
{
    /// <summary>Main menu screen. Connects to GameManager to start the game.</summary>
    public partial class MainMenu : Control
    {
        private Button? _startButton;
        private Button? _quitButton;
        private Label?  _titleLabel;

        public override void _Ready()
        {
            _startButton = GetNodeOrNull<Button>("VBoxContainer/StartButton");
            _quitButton  = GetNodeOrNull<Button>("VBoxContainer/QuitButton");
            _titleLabel  = GetNodeOrNull<Label>("VBoxContainer/TitleLabel");

            if (_startButton != null) _startButton.Pressed += OnStartPressed;
            if (_quitButton  != null) _quitButton.Pressed  += OnQuitPressed;

            if (_titleLabel != null) _titleLabel.Text = "Dungeon Crawler";
        }

        private void OnStartPressed()
        {
            // Load the gameplay scene.
            GetTree().ChangeSceneToFile("res://Scenes/Game.tscn");
        }

        private void OnQuitPressed()
        {
            GetTree().Quit();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Game Over screen
    // ─────────────────────────────────────────────────────────────────────────
    public partial class GameOverScreen : Control
    {
        private Button? _restartButton;
        private Button? _menuButton;

        public override void _Ready()
        {
            Visible = false;
            _restartButton = GetNodeOrNull<Button>("VBoxContainer/RestartButton");
            _menuButton    = GetNodeOrNull<Button>("VBoxContainer/MenuButton");

            if (_restartButton != null) _restartButton.Pressed += OnRestartPressed;
            if (_menuButton    != null) _menuButton.Pressed    += OnMenuPressed;

            Core.GameEvents.OnPlayerDied += () => Visible = true;
        }

        private void OnRestartPressed() =>
            GetTree().ReloadCurrentScene();

        private void OnMenuPressed() =>
            GetTree().ChangeSceneToFile("res://Scenes/Main.tscn");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Pause menu
    // ─────────────────────────────────────────────────────────────────────────
    public partial class PauseMenu : Control
    {
        private Button?      _resumeButton;
        private Button?      _quitButton;
        private GameManager? _gameManager;

        public override void _Ready()
        {
            Visible      = false;
            _gameManager = GetTree().Root.GetNodeOrNull<GameManager>("Main/GameManager");

            _resumeButton = GetNodeOrNull<Button>("VBoxContainer/ResumeButton");
            _quitButton   = GetNodeOrNull<Button>("VBoxContainer/QuitButton");

            if (_resumeButton != null) _resumeButton.Pressed += () => { _gameManager?.ResumeGame(); Visible = false; };
            if (_quitButton   != null) _quitButton.Pressed   += () => GetTree().ChangeSceneToFile("res://Scenes/Main.tscn");

            Core.GameEvents.OnPlayerDied += () => Visible = false;
        }

        public override void _Input(InputEvent @event)
        {
            if (@event.IsActionPressed("pause"))
            {
                Visible = !Visible;
                if (Visible)
                    _gameManager?.PauseGame();
                else
                    _gameManager?.ResumeGame();
            }
        }
    }
}
