using Godot;

namespace DungeonCrawler.Projectiles
{
    /// <summary>
    /// Godot Resource holding all configurable data for any projectile.
    /// Pass an instance to a projectile node's Initialize() method.
    /// </summary>
    [GlobalClass]
    public partial class ProjectileData : Resource
    {
        // ── Motion ─────────────────────────────────────────────────────────────
        [Export] public float Speed    { get; set; } = 400f;
        [Export] public float Lifetime { get; set; } = 3f;

        // ── Combat ─────────────────────────────────────────────────────────────
        [Export] public float Damage    { get; set; } = 3f;
        [Export] public float Knockback { get; set; } = 100f;

        // ── Visuals ────────────────────────────────────────────────────────────
        [Export] public float Size        { get; set; } = 8f;
        [Export] public Color VisualColor { get; set; } = Colors.Yellow;

        // ── Pierce ────────────────────────────────────────────────────────────
        /// <summary>If true the projectile can pass through multiple enemies.</summary>
        [Export] public bool CanPierce  { get; set; } = false;
        /// <summary>Number of targets the projectile may pass through (only used when CanPierce = true).</summary>
        [Export] public int  PierceCount { get; set; } = 1;

        // ── Homing ────────────────────────────────────────────────────────────
        [Export] public bool  IsHoming       { get; set; } = false;
        [Export] public float HomingStrength { get; set; } = 3f;  // steering speed (deg/s scale)

        // ── Explosion ─────────────────────────────────────────────────────────
        [Export] public bool  ExplodesOnImpact  { get; set; } = false;
        [Export] public float ExplosionRadius   { get; set; } = 80f;
        [Export] public float ExplosionDamage   { get; set; } = 10f;

        // ── Ownership ─────────────────────────────────────────────────────────
        /// <summary>"player" or "enemy" — controls which collision mask is applied.</summary>
        [Export] public string OwnerTag { get; set; } = "player";
    }
}
