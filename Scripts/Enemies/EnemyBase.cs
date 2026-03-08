using Godot;
using DungeonCrawler.Core;
using DungeonCrawler.Stats;
using DungeonCrawler.Player;
using DungeonCrawler.AI;

namespace DungeonCrawler.Enemies
{
    /// <summary>
    /// Base class for all enemies. Wires up AI state machine and stats.
    /// </summary>
    public abstract partial class EnemyBase : Character
    {
        // ── Exports ────────────────────────────────────────────────────────────
        [Export] public float DetectionRadius   { get; set; } = 180f;
        [Export] public float AttackRadius      { get; set; } = 40f;
        [Export] public int   CoinDrop          { get; set; } = Constants.COINS_PER_ENEMY;
        [Export] public bool  IsElite           { get; set; } = false;
        [Export] public bool  IsBoss            { get; set; } = false;

        // ── State ──────────────────────────────────────────────────────────────
        public  EnemyStateMachine? StateMachine { get; private set; }
        protected PlayerController? _player;
        private float _attackCooldown = 0f;
        private float _attackRate     = 1.0f; // seconds between attacks

        // ── Signals ────────────────────────────────────────────────────────────
        [Signal] public delegate void EnemyDiedEventHandler(Node enemy);

        // ── Godot lifecycle ────────────────────────────────────────────────────
        protected override void OnReady()
        {
            base.OnReady();
            AddToGroup(Constants.TAG_ENEMY);

            _player = GetTree().GetFirstNodeInGroup(Constants.TAG_PLAYER) as PlayerController;

            StateMachine = new EnemyStateMachine(this);
            StateMachine.TransitionTo(EnemyStateType.Idle);
        }

        protected override void InitialiseStats()
        {
            float hpMul  = IsBoss ? Constants.BOSS_HEALTH_MULTIPLIER : (IsElite ? 3f : 1f);
            float dmgMul = IsBoss ? Constants.BOSS_DAMAGE_MULTIPLIER : (IsElite ? 1.5f : 1f);

            Stats.SetBaseStat(StatType.MaxHealth, Constants.ENEMY_BASE_HEALTH * hpMul);
            Stats.SetBaseStat(StatType.Health,    Constants.ENEMY_BASE_HEALTH * hpMul);
            Stats.SetBaseStat(StatType.Speed,     Constants.ENEMY_BASE_SPEED);
            Stats.SetBaseStat(StatType.Damage,    Constants.ENEMY_BASE_DAMAGE * dmgMul);
        }

        protected override void SetupCollision()
        {
            CollisionLayer = Constants.MASK_ENEMY;
            CollisionMask  = Constants.MASK_WALL | Constants.MASK_PLAYER;
        }

        protected override void OnPhysicsProcess(float delta)
        {
            StateMachine?.Update(delta);
            if (_attackCooldown > 0f) _attackCooldown -= delta;
        }

        // ── Movement (called by AI states) ─────────────────────────────────────
        public override void Move(float delta) { /* AI drives movement via ApplyMovement */ }

        public void MoveToward(Vector2 target, float delta)
        {
            Vector2 dir = (target - GlobalPosition).Normalized();
            ApplyMovement(dir);
        }

        public void StopMoving() => ApplyMovement(Vector2.Zero);

        // ── Attack ─────────────────────────────────────────────────────────────
        public bool CanAttack() => _attackCooldown <= 0f;

        public void PerformAttack()
        {
            if (_player == null || !CanAttack()) return;
            _attackCooldown = _attackRate;
            OnAttack(_player);
        }

        protected virtual void OnAttack(PlayerController player)
        {
            float dmg = Stats.GetStat(StatType.Damage);
            player.TakeDamage(dmg, DamageType.Physical);
        }

        // ── Sensing ────────────────────────────────────────────────────────────
        public bool CanSeePlayer()
        {
            if (_player == null || !_player.IsAlive) return false;
            return GlobalPosition.DistanceTo(_player.GlobalPosition) <= DetectionRadius;
        }

        public bool IsInAttackRange()
        {
            if (_player == null) return false;
            return GlobalPosition.DistanceTo(_player.GlobalPosition) <= AttackRadius;
        }

        public Vector2 GetPlayerPosition() =>
            _player?.GlobalPosition ?? GlobalPosition;

        // ── Death ──────────────────────────────────────────────────────────────
        protected override void OnDeath()
        {
            GD.Print($"[Enemy] {Name} died.");
            EmitSignal(SignalName.EnemyDied, this);
            GameEvents.RaiseEnemyDied(this);

            // Drop coins.
            int coins = IsElite ? Constants.COINS_PER_ELITE_ENEMY
                      : IsBoss  ? Constants.COINS_PER_BOSS
                      :           CoinDrop;
            DropCoins(coins);

            QueueFree();
        }

        protected virtual void DropCoins(int amount)
        {
            if (_player == null) return;
            _player.AddCoins(amount);
            GD.Print($"[Enemy] Dropped {amount} coins.");
        }
    }
}
