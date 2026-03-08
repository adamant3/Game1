using Godot;
using DungeonCrawler.Stats;

namespace DungeonCrawler.Player
{
    /// <summary>
    /// Specialised CharacterStats node pre-configured with player base values.
    /// Attach as a child of the player scene (alongside or replacing the generic CharacterStats node).
    /// </summary>
    public partial class PlayerStats : CharacterStats
    {
        // ── Convenience properties ─────────────────────────────────────────────
        public bool IsMaxHealth => GetStat(StatType.Health) >= GetStat(StatType.MaxHealth);
        public bool IsMaxMana   => GetStat(StatType.Mana)   >= GetStat(StatType.MaxMana);

        // ── Godot lifecycle ────────────────────────────────────────────────────
        public override void _Ready()
        {
            base._Ready();
            InitializePlayerStats();
        }

        // ── Initialise base stats ──────────────────────────────────────────────
        private void InitializePlayerStats()
        {
            SetBaseStat(StatType.MaxHealth,   10f);
            SetBaseStat(StatType.Health,      10f);
            SetBaseStat(StatType.Speed,       200f);
            SetBaseStat(StatType.Shielding,   0f);
            SetBaseStat(StatType.Luck,        0f);
            SetBaseStat(StatType.CritChance,  0.05f);
            SetBaseStat(StatType.CritDamage,  2.0f);
            SetBaseStat(StatType.Damage,      3.0f);
            SetBaseStat(StatType.Mana,        100f);
            SetBaseStat(StatType.MaxMana,     100f);
            SetBaseStat(StatType.AttackSpeed, 1.0f);
            SetBaseStat(StatType.DodgeChance, 0.0f);
            SetBaseStat(StatType.Armor,       0f);

            GD.Print("[PlayerStats] Base stats initialised.");
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Permanently increases a stat by a flat amount (modifier never expires).
        /// </summary>
        public void AddPermanentUpgrade(StatType stat, float value)
        {
            var modifier = new StatModifier(
                $"perm_{stat}_{GD.Randi()}",
                stat,
                value,
                ModifierType.Flat,
                -1f,
                "permanent_upgrade");

            AddModifier(modifier);
            GD.Print($"[PlayerStats] Permanent upgrade: {stat} +{value}");
        }

        /// <summary>
        /// Applies a timed flat modifier identified by buffId.
        /// If a buff with the same id already exists it is replaced.
        /// </summary>
        public void ApplyBuff(string buffId, StatType stat, float value, float duration)
        {
            RemoveModifier(buffId);

            var modifier = new StatModifier(
                buffId,
                stat,
                value,
                ModifierType.Flat,
                duration,
                "buff");

            AddModifier(modifier);
            GD.Print($"[PlayerStats] Buff applied: {buffId} ({stat} +{value} for {duration:F1}s)");
        }

        /// <summary>
        /// Returns a drop-rate multiplier driven by the Luck stat (0–100 scale).
        /// At Luck 0 the result is 1.0; at Luck 100 the result is 1.5.
        /// </summary>
        public float GetLuckMultiplier() => 1.0f + GetStat(StatType.Luck) / 200f;
    }
}
