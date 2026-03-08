namespace DungeonCrawler.Core
{
    public static class Constants
    {
        // ── Collision Layers (bit index, 1-based) ──────────────────────────────
        public const uint LAYER_PLAYER     = 1;
        public const uint LAYER_ENEMY      = 2;
        public const uint LAYER_WALL       = 3;
        public const uint LAYER_ITEM       = 4;
        public const uint LAYER_PROJECTILE = 5;

        // Bitmask helpers for CollisionMask / CollisionLayer
        public const uint MASK_PLAYER     = 1 << (int)(LAYER_PLAYER     - 1);
        public const uint MASK_ENEMY      = 1 << (int)(LAYER_ENEMY      - 1);
        public const uint MASK_WALL       = 1 << (int)(LAYER_WALL       - 1);
        public const uint MASK_ITEM       = 1 << (int)(LAYER_ITEM       - 1);
        public const uint MASK_PROJECTILE = 1 << (int)(LAYER_PROJECTILE - 1);

        // ── Tags ───────────────────────────────────────────────────────────────
        public const string TAG_PLAYER     = "player";
        public const string TAG_ENEMY      = "enemy";
        public const string TAG_WALL       = "wall";
        public const string TAG_ITEM       = "item";
        public const string TAG_PROJECTILE = "projectile";
        public const string TAG_NPC        = "npc";
        public const string TAG_CHEST      = "chest";
        public const string TAG_DOOR       = "door";

        // ── Game Balance ───────────────────────────────────────────────────────
        public const float PLAYER_BASE_HEALTH       = 10f;
        public const float PLAYER_BASE_SPEED        = 200f;
        public const float PLAYER_BASE_DAMAGE       = 3f;
        public const float PLAYER_BASE_MANA         = 100f;
        public const float PLAYER_MANA_REGEN        = 5f;   // per second
        public const float BASE_CRIT_CHANCE         = 0.05f;
        public const float BASE_CRIT_MULTIPLIER     = 2.0f;
        public const float BASE_DODGE_CHANCE        = 0.0f;
        public const float PLAYER_INVINCIBILITY_TIME = 0.5f; // seconds after hit
        public const float KNOCKBACK_FORCE          = 150f;
        public const float ENEMY_BASE_HEALTH        = 5f;
        public const float ENEMY_BASE_SPEED         = 80f;
        public const float ENEMY_BASE_DAMAGE        = 1f;
        public const float BOSS_HEALTH_MULTIPLIER   = 10f;
        public const float BOSS_DAMAGE_MULTIPLIER   = 2f;

        // ── Room ───────────────────────────────────────────────────────────────
        public const int ROOM_WIDTH   = 25;
        public const int ROOM_HEIGHT  = 25;
        public const int TILE_SIZE    = 32;
        public const int MIN_ROOMS    = 8;
        public const int MAX_ROOMS    = 15;
        public const int BOSS_FLOOR_INTERVAL = 5; // every N floors spawn a boss room

        // ── Economy ────────────────────────────────────────────────────────────
        public const int STARTING_COINS        = 0;
        public const int COINS_PER_ENEMY       = 1;
        public const int COINS_PER_ELITE_ENEMY = 3;
        public const int COINS_PER_BOSS        = 10;
        public const int SHOP_ITEM_COST_MIN    = 5;
        public const int SHOP_ITEM_COST_MAX    = 25;
        public const int CHEST_COIN_MIN        = 3;
        public const int CHEST_COIN_MAX        = 8;

        // ── Projectiles ────────────────────────────────────────────────────────
        public const float DEFAULT_PROJECTILE_SPEED    = 400f;
        public const float DEFAULT_PROJECTILE_LIFETIME = 3f;

        // ── Dungeon Generation ─────────────────────────────────────────────────
        public const int MAX_FLOOR         = 10;
        public const float ITEM_ROOM_CHANCE = 0.15f;
        public const float SHOP_ROOM_CHANCE = 0.10f;

        // ── Interaction ────────────────────────────────────────────────────────
        public const float INTERACTION_RADIUS = 48f;
    }
}
