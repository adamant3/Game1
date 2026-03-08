using System;
using Godot;

namespace DungeonCrawler.Core
{
    /// <summary>
    /// Global event bus. Subscribe/unsubscribe from anywhere; fire via Raise* methods.
    /// All events are cleared when a new run begins (call ResetAll()).
    /// </summary>
    public static class GameEvents
    {
        // ── Player ─────────────────────────────────────────────────────────────
        public static event Action? OnPlayerDied;
        public static event Action<float, float>? OnPlayerHealthChanged;   // (current, max)
        public static event Action<float, float>? OnPlayerManaChanged;     // (current, max)
        public static event Action<int>?          OnPlayerCoinsChanged;    // (total coins)

        // ── Rooms / Floors ─────────────────────────────────────────────────────
        public static event Action<string>? OnRoomCleared;   // (roomId)
        public static event Action<string>? OnRoomEntered;   // (roomId)
        public static event Action<int>?    OnFloorCompleted; // (floorNumber)
        public static event Action<int>?    OnFloorChanged;   // (newFloor)

        // ── Enemies ────────────────────────────────────────────────────────────
        public static event Action<Node>?   OnEnemyDied;     // (enemyNode)
        public static event Action<Node>?   OnEnemySpawned;  // (enemyNode)

        // ── Items / Weapons / Consumables ──────────────────────────────────────
        public static event Action<string>? OnItemPickedUp;    // (itemId)
        public static event Action<string>? OnWeaponEquipped;  // (weaponId)
        public static event Action<string>? OnConsumableUsed;  // (consumableId)

        // ── Dungeon ────────────────────────────────────────────────────────────
        public static event Action?      OnDungeonGenerated;

        // ── Dialogue ───────────────────────────────────────────────────────────
        public static event Action<string>? OnDialogueStarted; // (dialogueId)
        public static event Action?         OnDialogueEnded;

        // ── Shop ───────────────────────────────────────────────────────────────
        public static event Action? OnShopOpened;
        public static event Action? OnShopClosed;

        // ── Buffs / Nerfs ──────────────────────────────────────────────────────
        public static event Action<string>? OnBuffApplied;   // (buffId)
        public static event Action<string>? OnBuffExpired;   // (buffId)
        public static event Action<string>? OnNerfApplied;   // (nerfId)

        // ── Raise helpers ──────────────────────────────────────────────────────
        public static void RaisePlayerDied()                              => OnPlayerDied?.Invoke();
        public static void RaisePlayerHealthChanged(float cur, float max) => OnPlayerHealthChanged?.Invoke(cur, max);
        public static void RaisePlayerManaChanged(float cur, float max)   => OnPlayerManaChanged?.Invoke(cur, max);
        public static void RaisePlayerCoinsChanged(int coins)             => OnPlayerCoinsChanged?.Invoke(coins);

        public static void RaiseRoomCleared(string roomId)    => OnRoomCleared?.Invoke(roomId);
        public static void RaiseRoomEntered(string roomId)    => OnRoomEntered?.Invoke(roomId);
        public static void RaiseFloorCompleted(int floor)     => OnFloorCompleted?.Invoke(floor);
        public static void RaiseFloorChanged(int floor)       => OnFloorChanged?.Invoke(floor);

        public static void RaiseEnemyDied(Node enemy)         => OnEnemyDied?.Invoke(enemy);
        public static void RaiseEnemySpawned(Node enemy)      => OnEnemySpawned?.Invoke(enemy);

        public static void RaiseItemPickedUp(string itemId)   => OnItemPickedUp?.Invoke(itemId);
        public static void RaiseWeaponEquipped(string id)     => OnWeaponEquipped?.Invoke(id);
        public static void RaiseConsumableUsed(string id)     => OnConsumableUsed?.Invoke(id);

        public static void RaiseDungeonGenerated()            => OnDungeonGenerated?.Invoke();

        public static void RaiseDialogueStarted(string id)   => OnDialogueStarted?.Invoke(id);
        public static void RaiseDialogueEnded()               => OnDialogueEnded?.Invoke();

        public static void RaiseShopOpened()                  => OnShopOpened?.Invoke();
        public static void RaiseShopClosed()                  => OnShopClosed?.Invoke();

        public static void RaiseBuffApplied(string buffId)    => OnBuffApplied?.Invoke(buffId);
        public static void RaiseBuffExpired(string buffId)    => OnBuffExpired?.Invoke(buffId);
        public static void RaiseNerfApplied(string nerfId)    => OnNerfApplied?.Invoke(nerfId);

        /// <summary>Removes all subscribers — call at the start of every new run.</summary>
        public static void ResetAll()
        {
            OnPlayerDied           = null;
            OnPlayerHealthChanged  = null;
            OnPlayerManaChanged    = null;
            OnPlayerCoinsChanged   = null;
            OnRoomCleared          = null;
            OnRoomEntered          = null;
            OnFloorCompleted       = null;
            OnFloorChanged         = null;
            OnEnemyDied            = null;
            OnEnemySpawned         = null;
            OnItemPickedUp         = null;
            OnWeaponEquipped       = null;
            OnConsumableUsed       = null;
            OnDungeonGenerated     = null;
            OnDialogueStarted      = null;
            OnDialogueEnded        = null;
            OnShopOpened           = null;
            OnShopClosed           = null;
            OnBuffApplied          = null;
            OnBuffExpired          = null;
            OnNerfApplied          = null;
        }
    }
}
