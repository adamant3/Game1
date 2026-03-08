using System;
using Godot;
using DungeonCrawler.Core;
using DungeonCrawler.Stats;

namespace DungeonCrawler.Items
{
    /// <summary>
    /// Abstract base for all collectible items (passive or active).
    /// Derives from Node2D so it can be placed in a scene as a pickup.
    /// </summary>
    public abstract partial class ItemBase : Node2D, IInteractable, IDroppable
    {
        // ── Identity ───────────────────────────────────────────────────────────
        public string ItemId   { get; protected set; } = Guid.NewGuid().ToString();
        public string ItemName { get; protected set; } = "Unknown Item";
        public string ItemDesc { get; protected set; } = "";
        public bool   IsActive { get; protected set; } = false; // passive by default

        // ── IInteractable ──────────────────────────────────────────────────────
        public virtual string InteractionPrompt => $"Pick up {ItemName}";
        public virtual bool   CanInteract       => true;
        public virtual void   Interact(Node interactor) => OnPickup(interactor);

        // ── IDroppable ─────────────────────────────────────────────────────────
        public virtual bool CanPickup(Node collector) => true;

        public virtual void OnPickup(Node collector)
        {
            if (!CanPickup(collector)) return;
            GD.Print($"[Item] {ItemName} picked up by {collector.Name}");
            QueueFree();
        }

        // ── Item lifecycle ─────────────────────────────────────────────────────
        /// <summary>Called when the item enters the player's inventory.</summary>
        public abstract void OnCollected(Node owner);

        /// <summary>Called when the item is removed from inventory.</summary>
        public abstract void OnRemoved(Node owner);

        /// <summary>Called when the player activates this item (active items only).</summary>
        public virtual void UseActive(Node owner) { }

        // ── Helper: get CharacterStats from owner ──────────────────────────────
        protected static Stats.CharacterStats? GetStats(Node owner) =>
            owner.GetNodeOrNull<Stats.CharacterStats>("CharacterStats");
    }
}
