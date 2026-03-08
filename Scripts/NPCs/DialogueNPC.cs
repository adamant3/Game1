using Godot;
using DungeonCrawler.NPCs;

namespace DungeonCrawler.NPCs
{
    /// <summary>
    /// A simple NPC that cycles through scripted dialogue lines.
    /// Optionally gives the player an item on the very first interaction.
    /// </summary>
    public partial class DialogueNPC : NPCBase
    {
        [Export] public string[] DialogueLines { get; set; } = { "Hello, adventurer!" };
        [Export] public bool GivesItemOnFirstMeet { get; set; } = false;
        [Export] public string GiftItemId { get; set; } = "";

        private int  _currentLine = 0;
        private bool _hasGivenItem = false;

        // ── Godot lifecycle ────────────────────────────────────────────────────

        public override void _Ready()
        {
            base._Ready();
            // Sync the base-class Dialogue array so NPCBase dialogue machinery
            // also has access to the lines if needed.
            Dialogue = DialogueLines;
        }

        // ── NPCBase overrides ──────────────────────────────────────────────────

        public override void Interact(Node interactor)
        {
            // Give item on the very first interaction if configured.
            if (GivesItemOnFirstMeet && !_hasGivenItem && GiftItemId.Length > 0)
            {
                _hasGivenItem = true;
                GD.Print($"[DialogueNPC] {NPCName} gives item '{GiftItemId}' to {interactor.Name}.");
                if (interactor.HasMethod("AddItemById"))
                    interactor.Call("AddItemById", GiftItemId);
            }

            AdvanceDialogue(interactor);
        }

        // ── Dialogue helpers ───────────────────────────────────────────────────

        /// <summary>Resets the dialogue back to the first line.</summary>
        public void ResetDialogue()
        {
            _currentLine = 0;
        }

        /// <summary>Moves to the next dialogue line, wrapping around at the end.</summary>
        public void AdvanceDialogue()
        {
            if (DialogueLines.Length == 0) return;

            string line = GetCurrentLine();
            GD.Print($"[{NPCName}]: {line}");

            _currentLine++;
            if (_currentLine >= DialogueLines.Length)
                _currentLine = 0;
        }

        /// <summary>
        /// Overload that accepts an interactor node (matches NPCBase.AdvanceDialogue signature).
        /// </summary>
        public new void AdvanceDialogue(Node interactor)
        {
            AdvanceDialogue();
        }

        /// <summary>Returns the dialogue line the NPC will speak next.</summary>
        public string GetCurrentLine()
        {
            if (DialogueLines.Length == 0) return "";
            return DialogueLines[_currentLine % DialogueLines.Length];
        }
    }
}
