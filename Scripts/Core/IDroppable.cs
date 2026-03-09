namespace DungeonCrawler.Core
{
    public interface IDroppable
    {
        void OnPickup(Godot.Node collector);
        bool CanPickup(Godot.Node collector);
    }
}
