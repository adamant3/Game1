using Godot;
using DungeonCrawler.Core;

namespace DungeonCrawler.Managers
{
    /// <summary>
    /// Manages audio: background music and sound effects.
    /// Attach as an AutoLoad or child of Main.
    /// </summary>
    public partial class AudioManager : Node
    {
        private AudioStreamPlayer? _musicPlayer;
        private float              _masterVolume = 1.0f;
        private float              _sfxVolume    = 1.0f;

        public override void _Ready()
        {
            _musicPlayer = new AudioStreamPlayer { Name = "MusicPlayer" };
            AddChild(_musicPlayer);

            // Subscribe to game events for contextual audio.
            GameEvents.OnPlayerDied    += OnPlayerDied;
            GameEvents.OnRoomCleared   += OnRoomCleared;
            GameEvents.OnEnemyDied     += _ => PlaySFX("enemy_died");
            GameEvents.OnItemPickedUp  += _ => PlaySFX("item_pickup");
        }

        public override void _ExitTree()
        {
            GameEvents.OnPlayerDied  -= OnPlayerDied;
            GameEvents.OnRoomCleared -= OnRoomCleared;
        }

        // ── Music ──────────────────────────────────────────────────────────────
        public void PlayMusic(AudioStream stream, bool loop = true)
        {
            if (_musicPlayer == null) return;
            _musicPlayer.Stream    = stream;
            _musicPlayer.VolumeDb  = GD.Linear2Db(_masterVolume);
            _musicPlayer.Play();
        }

        public void StopMusic() => _musicPlayer?.Stop();

        public void SetMasterVolume(float normalised)
        {
            _masterVolume             = Mathf.Clamp(normalised, 0f, 1f);
            if (_musicPlayer != null)
                _musicPlayer.VolumeDb = GD.Linear2Db(_masterVolume);
        }

        // ── SFX ───────────────────────────────────────────────────────────────
        public void PlaySFX(string sfxName)
        {
            // Placeholder — in a real project load from res://Audio/SFX/{sfxName}.wav
            GD.Print($"[AudioManager] SFX: {sfxName}");
        }

        // ── Event callbacks ────────────────────────────────────────────────────
        private void OnPlayerDied()         => PlaySFX("player_died");
        private void OnRoomCleared(string _) => PlaySFX("room_cleared");
    }
}
