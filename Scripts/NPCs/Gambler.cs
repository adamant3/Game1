using System;
using System.Collections.Generic;
using Godot;
using DungeonCrawler.Buffs;
using DungeonCrawler.Nerfs;

namespace DungeonCrawler.NPCs
{
    /// <summary>
    /// The Gambler NPC offers the player a random buff or nerf in exchange for coins.
    /// Outcome: 60% chance of a buff, 40% chance of a nerf.
    /// </summary>
    public partial class Gambler : NPCBase
    {
        [Export] public int GambleCost { get; set; } = 5;

        private static readonly Random _rng = new Random();

        // ── Buff / nerf name pools ─────────────────────────────────────────────

        private static readonly string[] BuffNames =
            { "SpeedBuff", "DamageBuff", "CritBuff", "ArmorBuff", "LuckBuff" };

        private static readonly string[] NerfNames =
            { "SlowNerf", "WeakenNerf", "BlindNerf", "CurseNerf" };

        // ── Godot lifecycle ────────────────────────────────────────────────────

        public override void _Ready()
        {
            base._Ready();
            NPCName  = "Gambler";
            Dialogue = new[] { "Feeling lucky? Try your fate!", "Win big or lose big…" };
        }

        // ── NPCBase overrides ──────────────────────────────────────────────────

        public override void Interact(Node interactor)
        {
            OfferGamble(interactor);
        }

        // ── Gamble logic ───────────────────────────────────────────────────────

        /// <summary>Checks if the player can pay, then applies a random outcome.</summary>
        public void OfferGamble(Node player)
        {
            GD.Print($"[Gambler] Gamble costs {GambleCost} coins. Offering gamble to {player.Name}.");

            if (!player.HasMethod("SpendCoins"))
            {
                GD.Print("[Gambler] Player does not expose SpendCoins – aborting.");
                return;
            }

            bool paid = (bool)player.Call("SpendCoins", GambleCost);
            if (!paid)
            {
                GD.Print($"[Gambler] {player.Name} cannot afford the gamble ({GambleCost}g).");
                return;
            }

            bool isBuff = GetGambleOutcome();
            if (isBuff)
            {
                ApplyRandomBuff(player);
                GD.Print("[Gambler] Lucky! A buff was applied.");
            }
            else
            {
                ApplyRandomNerf(player);
                GD.Print("[Gambler] Unlucky! A nerf was applied.");
            }
        }

        /// <summary>Returns true (buff) with 60% probability; false (nerf) with 40%.</summary>
        public bool GetGambleOutcome() => _rng.NextDouble() < 0.60;

        // ── Buff application ───────────────────────────────────────────────────

        public void ApplyRandomBuff(Node player)
        {
            string chosen = BuffNames[_rng.Next(BuffNames.Length)];
            GD.Print($"[Gambler] Applying buff: {chosen}");

            BuffBase? buff = chosen switch
            {
                "SpeedBuff"  => new SpeedBuff(),
                "DamageBuff" => new DamageAuraBuff(),
                "CritBuff"   => new CritBuff(),
                "ArmorBuff"  => new ArmorBuff(),
                "LuckBuff"   => new LuckBuff(),
                _            => new SpeedBuff()
            };

            player.AddChild(buff);
        }

        // ── Nerf application ───────────────────────────────────────────────────

        public void ApplyRandomNerf(Node player)
        {
            string chosen = NerfNames[_rng.Next(NerfNames.Length)];
            GD.Print($"[Gambler] Applying nerf: {chosen}");

            NerfBase? nerf = chosen switch
            {
                "SlowNerf"   => new SlowNerf(),
                "WeakenNerf" => new WeakenNerf(),
                "BlindNerf"  => new BlindNerf(),
                "CurseNerf"  => new CurseNerf(),
                _            => new SlowNerf()
            };

            player.AddChild(nerf);
        }
    }
}
