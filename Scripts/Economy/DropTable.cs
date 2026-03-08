using System;
using System.Collections.Generic;

namespace DungeonCrawler.Economy
{
    /// <summary>
    /// Static utility for weighted random item drops.
    /// All tables are registered at startup and queried at runtime.
    /// </summary>
    public static class DropTable
    {
        // ── Data structures ────────────────────────────────────────────────────

        public struct DropEntry
        {
            public string ItemId;
            public float  Weight;

            public DropEntry(string itemId, float weight)
            {
                ItemId = itemId;
                Weight = weight;
            }
        }

        private static readonly Dictionary<string, List<DropEntry>> _tables =
            new Dictionary<string, List<DropEntry>>();

        private static readonly Random _rng = new Random();

        // ── Static constructor – register predefined tables ────────────────────

        static DropTable()
        {
            // Regular enemy drops: 70% coins(1-3), 20% mana, 10% nothing.
            RegisterTable("regular_enemy", new List<DropEntry>
            {
                new DropEntry("coins_small", 70f),
                new DropEntry("mana_pickup",  20f),
                new DropEntry("nothing",      10f)
            });

            // Mini-boss drops: 100% double_coins, 80% consumable, 30% weapon.
            RegisterTable("mini_boss", new List<DropEntry>
            {
                new DropEntry("coins_double",   100f),
                new DropEntry("consumable_rand", 80f),
                new DropEntry("weapon_rand",     30f)
            });

            // Breakable crate drops: 60% coins, 20% health, 10% mana, 10% nothing.
            RegisterTable("crate", new List<DropEntry>
            {
                new DropEntry("coins_small", 60f),
                new DropEntry("health_pickup", 20f),
                new DropEntry("mana_pickup",   10f),
                new DropEntry("nothing",       10f)
            });

            // Chest drops: 50% weapon, 30% item, 20% consumable.
            RegisterTable("chest", new List<DropEntry>
            {
                new DropEntry("weapon_rand",     50f),
                new DropEntry("item_rand",        30f),
                new DropEntry("consumable_rand",  20f)
            });
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Registers or replaces a drop table under the given ID.</summary>
        public static void RegisterTable(string tableId, List<DropEntry> entries)
        {
            _tables[tableId] = entries;
        }

        /// <summary>
        /// Performs a weighted random roll against <paramref name="tableId"/>.
        /// <paramref name="luckMultiplier"/> boosts the weight of non-"nothing" entries.
        /// Returns the winning ItemId, or "nothing" if the table is empty.
        /// </summary>
        public static string RollDrop(string tableId, float luckMultiplier = 1.0f)
        {
            if (!_tables.TryGetValue(tableId, out var table) || table.Count == 0)
                return "nothing";

            // Build an adjusted weight list.
            float totalWeight = 0f;
            var adjusted = new List<(string itemId, float weight)>(table.Count);
            foreach (var entry in table)
            {
                float w = (entry.ItemId == "nothing")
                    ? entry.Weight / luckMultiplier   // luck reduces "nothing" chance
                    : entry.Weight * luckMultiplier;  // luck increases good-drop chance
                adjusted.Add((entry.ItemId, w));
                totalWeight += w;
            }

            float roll = (float)(_rng.NextDouble() * totalWeight);
            float cursor = 0f;
            foreach (var (itemId, weight) in adjusted)
            {
                cursor += weight;
                if (roll < cursor)
                    return itemId;
            }

            // Fallback: return last entry.
            return adjusted[adjusted.Count - 1].itemId;
        }

        /// <summary>Returns the drop table registered for <paramref name="enemyType"/>.</summary>
        public static List<DropEntry> GetEnemyDropTable(string enemyType)
        {
            string key = enemyType.ToLower() switch
            {
                "mini_boss" or "miniboss" or "boss" => "mini_boss",
                "regular"   or "normal"             => "regular_enemy",
                _                                   => "regular_enemy"
            };

            return _tables.TryGetValue(key, out var table)
                ? table
                : new List<DropEntry>();
        }

        /// <summary>
        /// Returns the number of coins to drop for a given enemy type and floor number.
        /// Coins scale with floor: base * (1 + floor * 0.1).
        /// </summary>
        public static int RollCoinAmount(string enemyType, int floor)
        {
            float multiplier = 1f + floor * 0.1f;

            int baseMin, baseMax;
            switch (enemyType.ToLower())
            {
                case "boss":
                case "mini_boss":
                case "miniboss":
                    baseMin = 8;
                    baseMax = 15;
                    break;
                case "elite":
                    baseMin = 3;
                    baseMax = 6;
                    break;
                default:
                    baseMin = 1;
                    baseMax = 3;
                    break;
            }

            int rolled = _rng.Next(baseMin, baseMax + 1);
            return (int)MathF.Round(rolled * multiplier);
        }

        /// <summary>Returns a read-only view of all registered table IDs.</summary>
        public static IReadOnlyCollection<string> GetTableIds() => _tables.Keys;
    }
}
