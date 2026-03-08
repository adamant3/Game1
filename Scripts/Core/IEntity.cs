namespace DungeonCrawler.Core
{
    public interface IEntity
    {
        string EntityId { get; }
        bool IsAlive { get; }
        void TakeDamage(float amount, DamageType damageType = DamageType.Physical);
        void Die();
    }

    public enum DamageType
    {
        Physical,
        Magical,
        True
    }
}
