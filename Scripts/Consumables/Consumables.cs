using System;
using Godot;
using DungeonCrawler.Core;
using DungeonCrawler.Stats;

namespace DungeonCrawler.Consumables
{
    /// <summary>Base class for one-shot consumable items (potions, bombs, etc.).</summary>
    public abstract partial class ConsumableBase : Node2D, IInteractable, IDroppable
    {
        public string ConsumableId   { get; protected set; } = Guid.NewGuid().ToString();
        public string ConsumableName { get; protected set; } = "Consumable";
        public string ConsumableDesc { get; protected set; } = "";
        public int    Stack          { get; protected set; } = 1;

        // ── IInteractable ──────────────────────────────────────────────────────
        public virtual string InteractionPrompt => $"Pick up {ConsumableName}";
        public virtual bool   CanInteract       => true;
        public virtual void   Interact(Node interactor) => OnPickup(interactor);

        // ── IDroppable ─────────────────────────────────────────────────────────
        public virtual bool CanPickup(Node collector) => true;
        public virtual void OnPickup(Node collector)
        {
            if (!CanPickup(collector)) return;
            Apply(collector);
            GameEvents.RaiseConsumableUsed(ConsumableId);
            QueueFree();
        }

        protected abstract void Apply(Node target);

        protected static CharacterStats? GetStats(Node target) =>
            target.GetNodeOrNull<CharacterStats>("CharacterStats");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Concrete consumables
    // ─────────────────────────────────────────────────────────────────────────

    public partial class SmallHealthPotion : ConsumableBase
    {
        public SmallHealthPotion()
        {
            ConsumableName = "Small Health Potion";
            ConsumableDesc = "Restores 2 HP";
        }

        protected override void Apply(Node target)
        {
            var stats = GetStats(target);
            if (stats == null) return;
            float max = stats.GetStat(StatType.MaxHealth);
            float cur = stats.GetStat(StatType.Health);
            stats.SetBaseStat(StatType.Health, Mathf.Min(max, cur + 2f));
            GD.Print("[Consumable] Small Health Potion used. +2 HP");
        }
    }

    public partial class LargeHealthPotion : ConsumableBase
    {
        public LargeHealthPotion()
        {
            ConsumableName = "Large Health Potion";
            ConsumableDesc = "Restores 5 HP";
        }

        protected override void Apply(Node target)
        {
            var stats = GetStats(target);
            if (stats == null) return;
            float max = stats.GetStat(StatType.MaxHealth);
            float cur = stats.GetStat(StatType.Health);
            stats.SetBaseStat(StatType.Health, Mathf.Min(max, cur + 5f));
            GD.Print("[Consumable] Large Health Potion used. +5 HP");
        }
    }

    public partial class ManaPotion : ConsumableBase
    {
        public ManaPotion()
        {
            ConsumableName = "Mana Potion";
            ConsumableDesc = "Restores 50 Mana";
        }

        protected override void Apply(Node target)
        {
            var stats = GetStats(target);
            if (stats == null) return;
            float max = stats.GetStat(StatType.MaxMana);
            float cur = stats.GetStat(StatType.Mana);
            stats.SetBaseStat(StatType.Mana, Mathf.Min(max, cur + 50f));
            GD.Print("[Consumable] Mana Potion used. +50 Mana");
        }
    }

    public partial class BombConsumable : ConsumableBase
    {
        [Export] public float ExplosionRadius { get; set; } = 100f;
        [Export] public float ExplosionDamage { get; set; } = 8f;

        public BombConsumable()
        {
            ConsumableName = "Bomb";
            ConsumableDesc = $"Explodes for {ExplosionDamage} dmg in {ExplosionRadius} radius";
        }

        protected override void Apply(Node target)
        {
            // Damage all enemies in radius.
            var spaceState = GetWorld2D()?.DirectSpaceState;
            if (spaceState == null) return;

            var query = new PhysicsShapeQueryParameters2D
            {
                Shape         = new CircleShape2D { Radius = ExplosionRadius },
                Transform     = new Transform2D(0f, GlobalPosition),
                CollisionMask = Constants.MASK_ENEMY
            };

            foreach (var result in spaceState.IntersectShape(query))
            {
                if (result.TryGetValue("collider", out var colliderVar) &&
                    colliderVar.AsGodotObject() is Entity entity)
                {
                    entity.TakeDamage(ExplosionDamage, DamageType.Magical);
                }
            }
            GD.Print("[Consumable] Bomb exploded!");
        }
    }
}
