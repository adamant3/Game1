using Godot;
using DungeonCrawler.Core;
using DungeonCrawler.Player;
using DungeonCrawler.Dungeon;
using DungeonCrawler.Weapons;

namespace DungeonCrawler.Managers
{
    public enum GameState { MainMenu, Playing, Paused, GameOver, Victory }

    /// <summary>
    /// Top-level game orchestrator. Lives in the Main scene.
    /// Manages game state, connects events, handles win/lose conditions.
    /// </summary>
    public partial class GameManager : Node
    {
        // ── Signals ────────────────────────────────────────────────────────────
        [Signal] public delegate void GameStateChangedEventHandler(int newState);

        // ── Exports (wired in scene) ───────────────────────────────────────────
        [Export] public NodePath? PlayerPath      { get; set; }
        [Export] public NodePath? DungeonPath     { get; set; }
        [Export] public NodePath? HUDPath         { get; set; }

        // ── State ──────────────────────────────────────────────────────────────
        public GameState State { get; private set; } = GameState.MainMenu;

        private PlayerController? _player;
        private DungeonManager?   _dungeon;

        // ── Godot lifecycle ────────────────────────────────────────────────────
        public override void _Ready()
        {
            if (PlayerPath  != null) _player  = GetNodeOrNull<PlayerController>(PlayerPath);
            if (DungeonPath != null) _dungeon  = GetNodeOrNull<DungeonManager>(DungeonPath);

            ConnectEvents();
            StartGame();
        }

        public override void _Process(double delta)
        {
            if (Input.IsActionJustPressed("pause") && State == GameState.Playing)
                PauseGame();
            else if (Input.IsActionJustPressed("pause") && State == GameState.Paused)
                ResumeGame();
        }

        // ── Game flow ──────────────────────────────────────────────────────────
        public void StartGame()
        {
            GameEvents.ResetAll();
            ConnectEvents();

            if (_player == null)
                _player = CreatePlayer();

            if (_dungeon == null)
                _dungeon = CreateDungeon();

            SetState(GameState.Playing);
            GD.Print("[GameManager] Game started.");
        }

        public void PauseGame()
        {
            GetTree().Paused = true;
            SetState(GameState.Paused);
        }

        public void ResumeGame()
        {
            GetTree().Paused = false;
            SetState(GameState.Playing);
        }

        public void RestartGame()
        {
            GetTree().ReloadCurrentScene();
        }

        private void SetState(GameState newState)
        {
            State = newState;
            EmitSignal(SignalName.GameStateChanged, (int)newState);
            GD.Print($"[GameManager] State → {newState}");
        }

        // ── Event wiring ───────────────────────────────────────────────────────
        private void ConnectEvents()
        {
            GameEvents.OnPlayerDied    += OnPlayerDied;
            GameEvents.OnFloorCompleted += OnFloorCompleted;
        }

        private void OnPlayerDied()
        {
            GD.Print("[GameManager] Player died — Game Over.");
            SetState(GameState.GameOver);
        }

        private void OnFloorCompleted(int floor)
        {
            if (floor >= Constants.MAX_FLOOR)
            {
                GD.Print("[GameManager] Final floor cleared — Victory!");
                SetState(GameState.Victory);
            }
            else
            {
                GD.Print($"[GameManager] Floor {floor} completed. Advancing...");
                _dungeon?.AdvanceFloor();
            }
        }

        // ── Factory helpers ────────────────────────────────────────────────────
        private PlayerController CreatePlayer()
        {
            var player  = new PlayerController { Name = "Player" };
            var weapon  = new BasicGun();
            AddChild(player);
            player.EquipWeapon(weapon);
            GD.Print("[GameManager] Player created.");
            return player;
        }

        private DungeonManager CreateDungeon()
        {
            var dungeon = new DungeonManager { Name = "DungeonManager" };
            AddChild(dungeon);
            GD.Print("[GameManager] DungeonManager created.");
            return dungeon;
        }
    }
}
