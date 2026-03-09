using Godot;
using DungeonCrawler.Core;
using DungeonCrawler.Stats;

namespace DungeonCrawler.Items
{
    // ─────────────────────────────────────────────────────────────────────────
    // Passive Items
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Permanently increases max HP by 2.</summary>
    public partial class HealthUpItem : ItemBase
    {
        private const float Bonus = 2f;
        public HealthUpItem()
        {
            ItemName = "Iron Heart";
            ItemDesc = "+2 Max Health";
        }

        public override void OnCollected(Node owner)
        {
            var stats = GetStats(owner);
            if (stats == null) return;
            var mod = new StatModifier($"iron_heart_{ItemId}", StatType.MaxHealth, Bonus, ModifierType.Flat, -1f, ItemId);
            stats.AddModifier(mod);
            // Also heal the bonus amount.
            float curHp = stats.GetStat(StatType.Health);
            stats.SetBaseStat(StatType.Health, curHp + Bonus);
        }

        public override void OnRemoved(Node owner)
        {
            GetStats(owner)?.RemoveModifiersFromSource(ItemId);
        }
    }

    /// <summary>Increases movement speed by 15%.</summary>
    public partial class SpeedBoostItem : ItemBase
    {
        public SpeedBoostItem()
        {
            ItemName = "Hermes Boots";
            ItemDesc = "+15% Speed";
        }

        public override void OnCollected(Node owner)
        {
            var mod = new StatModifier($"hermes_{ItemId}", StatType.Speed, 0.15f, ModifierType.Percentage, -1f, ItemId);
            GetStats(owner)?.AddModifier(mod);
        }

        public override void OnRemoved(Node owner) =>
            GetStats(owner)?.RemoveModifiersFromSource(ItemId);
    }

    /// <summary>Increases damage dealt by +3 flat.</summary>
    public partial class DamageUpItem : ItemBase
    {
        private const float Bonus = 3f;
        public DamageUpItem()
        {
            ItemName = "Spiked Collar";
            ItemDesc = "+3 Damage";
        }

        public override void OnCollected(Node owner)
        {
            var mod = new StatModifier($"spike_{ItemId}", StatType.Damage, Bonus, ModifierType.Flat, -1f, ItemId);
            GetStats(owner)?.AddModifier(mod);
        }

        public override void OnRemoved(Node owner) =>
            GetStats(owner)?.RemoveModifiersFromSource(ItemId);
    }

    /// <summary>Increases crit chance by +10%.</summary>
    public partial class CritEyeItem : ItemBase
    {
        public CritEyeItem()
        {
            ItemName = "Eagle Eye";
            ItemDesc = "+10% Crit Chance";
        }

        public override void OnCollected(Node owner)
        {
            var mod = new StatModifier($"eye_{ItemId}", StatType.CritChance, 0.10f, ModifierType.Flat, -1f, ItemId);
            GetStats(owner)?.AddModifier(mod);
        }

        public override void OnRemoved(Node owner) =>
            GetStats(owner)?.RemoveModifiersFromSource(ItemId);
    }

    /// <summary>Adds a luck bonus that improves shop and chest rolls.</summary>
    public partial class LuckyCharmItem : ItemBase
    {
        public LuckyCharmItem()
        {
            ItemName = "Lucky Clover";
            ItemDesc = "+5 Luck";
        }

        public override void OnCollected(Node owner)
        {
            var mod = new StatModifier($"clover_{ItemId}", StatType.Luck, 5f, ModifierType.Flat, -1f, ItemId);
            GetStats(owner)?.AddModifier(mod);
        }

        public override void OnRemoved(Node owner) =>
            GetStats(owner)?.RemoveModifiersFromSource(ItemId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Active Items
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Active: instantly restores 4 HP. Single use.</summary>
    public partial class HealthPotionItem : ItemBase
    {
        private const float HealAmount = 4f;
        private bool _used = false;

        public HealthPotionItem()
        {
            ItemName = "Health Potion";
            ItemDesc = "Restore 4 HP";
            IsActive = true;
        }

        public override void OnCollected(Node owner) { }
        public override void OnRemoved(Node owner)   { }

        public override void UseActive(Node owner)
        {
            if (_used) return;
            var stats = GetStats(owner);
            if (stats == null) return;
            float maxHp = stats.GetStat(StatType.MaxHealth);
            float curHp = stats.GetStat(StatType.Health);
            stats.SetBaseStat(StatType.Health, Mathf.Min(maxHp, curHp + HealAmount));
            GD.Print($"[Item] Health Potion used. Restored {HealAmount} HP.");
            _used = true;
            GameEvents.RaiseConsumableUsed(ItemId);
        }
    }

    /// <summary>Active: grants 3s of invincibility. Cooldown 10s.</summary>
    public partial class ShieldBubbleItem : ItemBase
    {
        private float _cooldown   = 10f;
        private float _coolTimer  = 0f;

        public ShieldBubbleItem()
        {
            ItemName = "Shield Bubble";
            ItemDesc = "3s invincibility (10s cooldown)";
            IsActive = true;
        }

        public override void _Process(double delta)
        {
            if (_coolTimer > 0f) _coolTimer -= (float)delta;
        }

        public override void OnCollected(Node owner) { }
        public override void OnRemoved(Node owner)   { }

        public override void UseActive(Node owner)
        {
            if (_coolTimer > 0f)
            {
                GD.Print($"[Item] Shield Bubble on cooldown ({_coolTimer:F1}s).");
                return;
            }
            if (owner is Core.Entity entity)
                entity.StartInvincibility(3f);
            _coolTimer = _cooldown;
            GameEvents.RaiseConsumableUsed(ItemId);
        }
    }
}
