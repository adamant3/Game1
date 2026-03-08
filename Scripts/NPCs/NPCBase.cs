using Godot;
using DungeonCrawler.Core;
using DungeonCrawler.Player;

namespace DungeonCrawler.NPCs
{
    public enum NPCType { Merchant, QuestGiver, Blacksmith, Healer }

    /// <summary>
    /// Base class for all friendly NPCs. Implements interactable interface.
    /// </summary>
    public abstract partial class NPCBase : CharacterBody2D, IInteractable
    {
        [Export] public string   NPCName { get; set; } = "NPC";
        [Export] public NPCType  Type    { get; set; } = NPCType.Merchant;
        [Export] public string[] Dialogue { get; set; } = { "Hello, adventurer!" };

        private int  _dialogueIndex = 0;
        private bool _inDialogue    = false;

        // ── IInteractable ──────────────────────────────────────────────────────
        public virtual string InteractionPrompt => $"Talk to {NPCName}";
        public virtual bool   CanInteract       => !_inDialogue;

        public virtual void Interact(Node interactor)
        {
            if (_inDialogue) return;
            StartDialogue(interactor);
        }

        // ── Dialogue ───────────────────────────────────────────────────────────
        private void StartDialogue(Node interactor)
        {
            _inDialogue    = true;
            _dialogueIndex = 0;
            GameEvents.RaiseDialogueStarted(NPCName);
            ShowNextLine(interactor);
        }

        private void ShowNextLine(Node interactor)
        {
            if (_dialogueIndex >= Dialogue.Length)
            {
                EndDialogue(interactor);
                return;
            }
            GD.Print($"[{NPCName}]: {Dialogue[_dialogueIndex]}");
            _dialogueIndex++;
        }

        public void AdvanceDialogue(Node interactor)
        {
            if (!_inDialogue) return;
            ShowNextLine(interactor);
        }

        protected virtual void EndDialogue(Node interactor)
        {
            _inDialogue = false;
            GameEvents.RaiseDialogueEnded();
            OnDialogueEnded(interactor);
        }

        protected virtual void OnDialogueEnded(Node interactor) { }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Merchant NPC — opens the shop
    // ─────────────────────────────────────────────────────────────────────────
    public partial class MerchantNPC : NPCBase
    {
        public MerchantNPC()
        {
            NPCName  = "Merchant";
            Type     = NPCType.Merchant;
            Dialogue = new[] { "Welcome! Check my wares.", "Buy something or leave." };
        }

        protected override void OnDialogueEnded(Node interactor)
        {
            // Signal the shop system to open.
            GameEvents.RaiseShopOpened();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Healer NPC — restores player HP for coins
    // ─────────────────────────────────────────────────────────────────────────
    public partial class HealerNPC : NPCBase
    {
        [Export] public int   HealCost   { get; set; } = 5;
        [Export] public float HealAmount { get; set; } = 5f;

        public HealerNPC()
        {
            NPCName  = "Healer";
            Type     = NPCType.Healer;
            Dialogue = new[] { "I can heal you for 5 coins." };
        }

        protected override void OnDialogueEnded(Node interactor)
        {
            if (interactor is not PlayerController player) return;
            if (!player.SpendCoins(HealCost))
            {
                GD.Print("[Healer] Not enough coins.");
                return;
            }
            player.Heal(HealAmount);
            GD.Print($"[Healer] Healed player for {HealAmount} HP.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Blacksmith NPC — upgrades weapons for coins
    // ─────────────────────────────────────────────────────────────────────────
    public partial class BlacksmithNPC : NPCBase
    {
        [Export] public int   UpgradeCost { get; set; } = 15;

        public BlacksmithNPC()
        {
            NPCName  = "Blacksmith";
            Type     = NPCType.Blacksmith;
            Dialogue = new[] { "I can upgrade your weapon for 15 coins." };
        }

        protected override void OnDialogueEnded(Node interactor)
        {
            if (interactor is not PlayerController player) return;
            if (!player.SpendCoins(UpgradeCost))
            {
                GD.Print("[Blacksmith] Not enough coins.");
                return;
            }

            // Increase equipped weapon damage multiplier.
            if (player.EquippedWeapon != null)
            {
                player.EquippedWeapon.BaseDamage *= 1.25f;
                GD.Print($"[Blacksmith] Upgraded {player.EquippedWeapon.WeaponName}. New damage: {player.EquippedWeapon.BaseDamage:F1}");
            }
        }
    }
}
