using Godot;
using DungeonCrawler.Core;
using DungeonCrawler.Stats;
using DungeonCrawler.Player;
using DungeonCrawler.Projectiles;

namespace DungeonCrawler.Enemies
{
    /// <summary>Basic melee enemy that charges toward the player.</summary>
    public partial class MeleeEnemy : EnemyBase
    {
        public MeleeEnemy()
        {
            AttackRadius   = 36f;
            DetectionRadius = 200f;
        }

        protected override void InitialiseStats()
        {
            base.InitialiseStats();
            Stats.SetBaseStat(StatType.Speed, Constants.ENEMY_BASE_SPEED * 1.1f);
        }
    }

    /// <summary>Ranged enemy that keeps distance and fires projectiles.</summary>
    public partial class RangedEnemy : EnemyBase
    {
        [Export] public float ShootRange { get; set; } = 220f;
        [Export] public float RetreatRange { get; set; } = 80f;
        private float _shootCooldown = 0f;
        private float _shootRate     = 1.5f;

        public RangedEnemy()
        {
            AttackRadius   = 220f;
            DetectionRadius = 280f;
        }

        protected override void InitialiseStats()
        {
            base.InitialiseStats();
            Stats.SetBaseStat(StatType.Speed, Constants.ENEMY_BASE_SPEED * 0.7f);
            Stats.SetBaseStat(StatType.Damage, Constants.ENEMY_BASE_DAMAGE * 1.5f);
        }

        protected override void OnAttack(PlayerController player)
        {
            if (_shootCooldown > 0f) return;
            _shootCooldown = _shootRate;

            Vector2 dir = (player.GlobalPosition - GlobalPosition).Normalized();
            float   dmg = Stats.GetStat(StatType.Damage);

            var p = new Projectile
            {
                GlobalPosition = GlobalPosition,
                Direction      = dir,
                Speed          = 280f,
                Damage         = dmg,
                Lifetime       = 3f,
                IsPlayerOwned  = false,
                ProjectileColor = Colors.Red
            };
            GetTree().Root.AddChild(p);
        }

        protected override void OnPhysicsProcess(float delta)
        {
            base.OnPhysicsProcess(delta);
            if (_shootCooldown > 0f) _shootCooldown -= delta;
        }

        public bool ShouldRetreat() =>
            _player != null &&
            GlobalPosition.DistanceTo(_player.GlobalPosition) < RetreatRange;

        public Vector2 GetRetreatDirection()
        {
            if (_player == null) return Vector2.Zero;
            return (GlobalPosition - _player.GlobalPosition).Normalized();
        }
    }

    /// <summary>Slow, tanky enemy that charges the player for big damage.</summary>
    public partial class TankEnemy : EnemyBase
    {
        private bool _isCharging = false;
        private float _chargeTimer = 0f;
        private Vector2 _chargeDirection = Vector2.Zero;
        private float _chargeSpeed = 420f;
        private float _chargeDuration = 0.5f;
        private float _chargeCooldown = 3f;
        private float _chargeCooldownTimer = 0f;

        public TankEnemy()
        {
            AttackRadius    = 50f;
            DetectionRadius = 160f;
        }

        protected override void InitialiseStats()
        {
            base.InitialiseStats();
            float hp = Constants.ENEMY_BASE_HEALTH * 4f;
            Stats.SetBaseStat(StatType.MaxHealth, hp);
            Stats.SetBaseStat(StatType.Health,    hp);
            Stats.SetBaseStat(StatType.Speed,     Constants.ENEMY_BASE_SPEED * 0.5f);
            Stats.SetBaseStat(StatType.Damage,    Constants.ENEMY_BASE_DAMAGE * 2.5f);
            Stats.SetBaseStat(StatType.Armor,     3f);
        }

        protected override void OnPhysicsProcess(float delta)
        {
            if (_isCharging)
            {
                _chargeTimer -= delta;
                ApplyMovement(_chargeDirection, (float?)_chargeSpeed);
                if (_chargeTimer <= 0f)
                {
                    _isCharging = false;
                    _chargeCooldownTimer = _chargeCooldown;
                }
                return;
            }

            if (_chargeCooldownTimer > 0f) _chargeCooldownTimer -= delta;
            base.OnPhysicsProcess(delta);
        }

        public void BeginCharge()
        {
            if (_isCharging || _chargeCooldownTimer > 0f || _player == null) return;
            _chargeDirection = (_player.GlobalPosition - GlobalPosition).Normalized();
            _isCharging      = true;
            _chargeTimer     = _chargeDuration;
        }

        public bool CanCharge() => !_isCharging && _chargeCooldownTimer <= 0f;
    }

    /// <summary>Boss enemy with multiple phases and special attacks.</summary>
    public partial class BossEnemy : EnemyBase
    {
        private int   _phase           = 1;
        private float _enrageThreshold = 0.5f; // HP ratio
        private float _burstCooldown   = 0f;

        public BossEnemy()
        {
            IsBoss         = true;
            AttackRadius   = 350f;
            DetectionRadius = 500f;
        }

        protected override void InitialiseStats()
        {
            base.InitialiseStats();
            Stats.SetBaseStat(StatType.Speed, Constants.ENEMY_BASE_SPEED * 0.8f);
        }

        protected override void OnPhysicsProcess(float delta)
        {
            // Check for phase transition.
            float hpRatio = Stats.GetStat(StatType.Health) / Stats.GetStat(StatType.MaxHealth);
            if (_phase == 1 && hpRatio < _enrageThreshold)
            {
                _phase = 2;
                EnterPhase2();
            }

            if (_burstCooldown > 0f) _burstCooldown -= delta;
            base.OnPhysicsProcess(delta);
        }

        private void EnterPhase2()
        {
            GD.Print("[Boss] Entering Phase 2!");
            Stats.SetBaseStat(StatType.Speed, Constants.ENEMY_BASE_SPEED * 1.4f);
        }

        protected override void OnAttack(PlayerController player)
        {
            if (_phase == 2 && _burstCooldown <= 0f)
            {
                FireBurst(player);
                _burstCooldown = 2.5f;
            }
            else
            {
                base.OnAttack(player);
            }
        }

        private void FireBurst(PlayerController player)
        {
            int count = 8;
            for (int i = 0; i < count; i++)
            {
                float angle = i * (Mathf.Tau / count);
                Vector2 dir = Vector2.Right.Rotated(angle);
                var p = new Projectile
                {
                    GlobalPosition = GlobalPosition,
                    Direction      = dir,
                    Speed          = 260f,
                    Damage         = Stats.GetStat(StatType.Damage),
                    Lifetime       = 4f,
                    IsPlayerOwned  = false,
                    ProjectileColor = Colors.Purple
                };
                GetTree().Root.AddChild(p);
            }
        }
    }
}
