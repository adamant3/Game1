using Godot;

namespace DungeonCrawler.Weapons
{
    /// <summary>Broad category of a weapon.</summary>
    public enum WeaponType { Melee, Gun, Magic, Thrown }

    /// <summary>
    /// Godot Resource holding all configurable data for a weapon.
    /// Create instances via the Inspector or GD.Load&lt;WeaponData&gt;.
    /// </summary>
    [GlobalClass]
    public partial class WeaponData : Resource
    {
        // ── Identity ───────────────────────────────────────────────────────────
        [Export] public string WeaponName        { get; set; } = "Unnamed Weapon";
        [Export] public string WeaponDescription { get; set; } = "";

        // ── Category ───────────────────────────────────────────────────────────
        [Export] public WeaponType WeaponType { get; set; } = WeaponType.Melee;

        // ── Combat stats ───────────────────────────────────────────────────────
        [Export] public float BaseDamage     { get; set; } = 3f;
        [Export] public float AttackSpeed    { get; set; } = 1f;
        [Export] public float CritChance     { get; set; } = 0.05f;
        [Export] public float CritMultiplier { get; set; } = 2f;

        // ── Range / projectile ─────────────────────────────────────────────────
        /// <summary>Melee range in pixels, or maximum travel distance for projectiles.</summary>
        [Export] public float Range          { get; set; } = 60f;
        [Export] public float ProjectileSpeed { get; set; } = 400f;

        // ── Ammo ───────────────────────────────────────────────────────────────
        /// <summary>Maximum ammo capacity. -1 means infinite ammo.</summary>
        [Export] public int MaxAmmo     { get; set; } = -1;
        /// <summary>Current ammo count. -1 means infinite.</summary>
        [Export] public int CurrentAmmo { get; set; } = -1;

        // ── Mana cost ──────────────────────────────────────────────────────────
        [Export] public float ManaCost { get; set; } = 0f;

        // ── Alternate fire ─────────────────────────────────────────────────────
        [Export] public bool  HasAlternateFire  { get; set; } = false;
        [Export] public float AltFireDamage     { get; set; } = 0f;
        [Export] public float AltFireManaCost   { get; set; } = 0f;

        // ── Physics ────────────────────────────────────────────────────────────
        [Export] public float Knockback { get; set; } = 150f;

        // ── Visuals (placeholder until proper sprites are added) ───────────────
        [Export] public Color ProjectileColor { get; set; } = Colors.Yellow;
        [Export] public float ProjectileSize  { get; set; } = 8f;
    }
}
