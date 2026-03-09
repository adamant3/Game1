using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using DungeonCrawler.Core;

namespace DungeonCrawler.Managers
{
    /// <summary>
    /// Handles serialising/deserialising a run's persistent state to disk.
    /// Singleton autoload.
    /// </summary>
    public partial class SaveManager : Node
    {
        public static SaveManager Instance { get; private set; } = null!;

        public const string SAVE_PATH = "user://save.json";

        // ── Nested SaveData ────────────────────────────────────────────────────
        public class SaveData
        {
            public int CurrentFloor { get; set; } = 1;
            public int Coins { get; set; } = 0;
            public int TotalCoins { get; set; } = 0;
            public float CurrentHealth { get; set; } = 100f;
            public float MaxHealth { get; set; } = 100f;
            public float CurrentMana { get; set; } = 50f;
            public List<string> InventoryItemIds { get; set; } = new();
            public List<string> ActiveBuffIds { get; set; } = new();
            public Dictionary<string, float> StatOverrides { get; set; } = new();
            public int TotalKills { get; set; } = 0;
            public int TotalRoomsCleared { get; set; } = 0;
            public int TotalDeaths { get; set; } = 0;
            public DateTime LastSaved { get; set; } = DateTime.UtcNow;
            public int MasterSeed { get; set; } = 0;
        }

        // ── State ──────────────────────────────────────────────────────────────
        public SaveData CurrentSave { get; private set; } = new SaveData();

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // ── Godot lifecycle ────────────────────────────────────────────────────
        public override void _Ready()
        {
            if (Instance != null && Instance != this) { QueueFree(); return; }
            Instance = this;

            GameEvents.OnFloorChanged += OnFloorChanged;
        }

        public override void _ExitTree()
        {
            GameEvents.OnFloorChanged -= OnFloorChanged;
        }

        // ── Public API ─────────────────────────────────────────────────────────
        public bool HasSave()
        {
            return Godot.FileAccess.FileExists(SAVE_PATH);
        }

        public void Save(GameState state)
        {
            CurrentSave.LastSaved = DateTime.UtcNow;
            if (RNGManager.Instance != null)
                CurrentSave.MasterSeed = RNGManager.Instance.GetSeed();

            string json = SerializeToJson(CurrentSave);

            using Godot.FileAccess file = Godot.FileAccess.Open(SAVE_PATH, Godot.FileAccess.ModeFlags.Write);
            if (file == null)
            {
                GD.PrintErr("[SaveManager] Failed to open save file for writing.");
                return;
            }
            file.StoreString(json);
            GD.Print("[SaveManager] Game saved.");
        }

        public SaveData? Load()
        {
            if (!HasSave()) return null;

            using Godot.FileAccess file = Godot.FileAccess.Open(SAVE_PATH, Godot.FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PrintErr("[SaveManager] Failed to open save file for reading.");
                return null;
            }

            string json = file.GetAsText();
            SaveData? data = DeserializeFromJson(json);
            if (data != null)
                CurrentSave = data;

            return data;
        }

        public void DeleteSave()
        {
            if (HasSave())
            {
                DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SAVE_PATH));
                GD.Print("[SaveManager] Save deleted.");
            }
        }

        public void NewGame()
        {
            CurrentSave = new SaveData();
            if (RNGManager.Instance != null)
            {
                RNGManager.Instance.Initialize(0);
                CurrentSave.MasterSeed = RNGManager.Instance.GetSeed();
            }
            GD.Print("[SaveManager] New game initialised.");
        }

        public string SerializeToJson(SaveData data)
            => JsonSerializer.Serialize(data, _jsonOptions);

        public SaveData? DeserializeFromJson(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<SaveData>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[SaveManager] Deserialize error: {ex.Message}");
                return null;
            }
        }

        // ── Event handlers ─────────────────────────────────────────────────────
        private void OnFloorChanged(int newFloor)
        {
            CurrentSave.CurrentFloor = newFloor;
            Save(GameState.Playing);
        }
    }
}
