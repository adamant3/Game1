namespace DungeonCrawler.Core
{
    public interface IInteractable
    {
        string InteractionPrompt { get; }
        bool CanInteract { get; }
        void Interact(Godot.Node interactor);
    }
}
