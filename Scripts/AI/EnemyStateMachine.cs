using System.Collections.Generic;
using Godot;
using DungeonCrawler.Enemies;

namespace DungeonCrawler.AI
{
    public enum EnemyStateType { Idle, Patrol, Chase, Attack, Retreat, Dead }

    // ─────────────────────────────────────────────────────────────────────────
    // State machine
    // ─────────────────────────────────────────────────────────────────────────
    public class EnemyStateMachine
    {
        private readonly EnemyBase                           _enemy;
        private readonly Dictionary<EnemyStateType, IEnemyState> _states;
        private IEnemyState?                                 _current;
        public  EnemyStateType                               CurrentType { get; private set; }

        public EnemyStateMachine(EnemyBase enemy)
        {
            _enemy = enemy;
            _states = new Dictionary<EnemyStateType, IEnemyState>
            {
                [EnemyStateType.Idle]    = new IdleState(),
                [EnemyStateType.Patrol]  = new PatrolState(),
                [EnemyStateType.Chase]   = new ChaseState(),
                [EnemyStateType.Attack]  = new AttackState(),
                [EnemyStateType.Retreat] = new RetreatState(),
                [EnemyStateType.Dead]    = new DeadState(),
            };
        }

        public void TransitionTo(EnemyStateType type)
        {
            _current?.OnExit(_enemy);
            CurrentType = type;
            _current    = _states[type];
            _current.OnEnter(_enemy);
        }

        public void Update(float delta)
        {
            if (_current == null) return;
            EnemyStateType? next = _current.Update(_enemy, delta);
            if (next.HasValue && next.Value != CurrentType)
                TransitionTo(next.Value);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // State interface
    // ─────────────────────────────────────────────────────────────────────────
    public interface IEnemyState
    {
        void             OnEnter(EnemyBase enemy);
        EnemyStateType?  Update(EnemyBase enemy, float delta);
        void             OnExit(EnemyBase enemy);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Idle: wait a moment, then patrol
    // ─────────────────────────────────────────────────────────────────────────
    public class IdleState : IEnemyState
    {
        private float _timer;
        private float _idleDuration = 1.5f;

        public void OnEnter(EnemyBase enemy)
        {
            _timer = _idleDuration;
            enemy.StopMoving();
        }

        public EnemyStateType? Update(EnemyBase enemy, float delta)
        {
            if (!enemy.IsAlive) return EnemyStateType.Dead;
            if (enemy.CanSeePlayer()) return EnemyStateType.Chase;

            _timer -= delta;
            if (_timer <= 0f) return EnemyStateType.Patrol;
            return null;
        }

        public void OnExit(EnemyBase enemy) { }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Patrol: wander a random direction for a few seconds
    // ─────────────────────────────────────────────────────────────────────────
    public class PatrolState : IEnemyState
    {
        private float   _timer;
        private Vector2 _dir;
        private float   _patrolDuration = 2.5f;

        public void OnEnter(EnemyBase enemy)
        {
            _timer = _patrolDuration;
            float angle = (float)GD.RandRange(0.0, Mathf.Tau);
            _dir        = Vector2.Right.Rotated(angle);
        }

        public EnemyStateType? Update(EnemyBase enemy, float delta)
        {
            if (!enemy.IsAlive) return EnemyStateType.Dead;
            if (enemy.CanSeePlayer()) return EnemyStateType.Chase;

            enemy.MoveToward(enemy.GlobalPosition + _dir * 100f, delta);

            _timer -= delta;
            if (_timer <= 0f) return EnemyStateType.Idle;
            return null;
        }

        public void OnExit(EnemyBase enemy) => enemy.StopMoving();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Chase: move toward player
    // ─────────────────────────────────────────────────────────────────────────
    public class ChaseState : IEnemyState
    {
        public void OnEnter(EnemyBase enemy) { }

        public EnemyStateType? Update(EnemyBase enemy, float delta)
        {
            if (!enemy.IsAlive) return EnemyStateType.Dead;
            if (!enemy.CanSeePlayer()) return EnemyStateType.Idle;

            // Special: ranged enemies retreat if too close.
            if (enemy is RangedEnemy ranged && ranged.ShouldRetreat())
                return EnemyStateType.Retreat;

            if (enemy.IsInAttackRange()) return EnemyStateType.Attack;

            enemy.MoveToward(enemy.GetPlayerPosition(), delta);
            return null;
        }

        public void OnExit(EnemyBase enemy) => enemy.StopMoving();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Attack: stand still and attack
    // ─────────────────────────────────────────────────────────────────────────
    public class AttackState : IEnemyState
    {
        public void OnEnter(EnemyBase enemy) => enemy.StopMoving();

        public EnemyStateType? Update(EnemyBase enemy, float delta)
        {
            if (!enemy.IsAlive) return EnemyStateType.Dead;
            if (!enemy.CanSeePlayer()) return EnemyStateType.Idle;

            // Tank: trigger charge in attack state.
            if (enemy is TankEnemy tank && tank.CanCharge())
            {
                tank.BeginCharge();
                return EnemyStateType.Chase;
            }

            if (enemy.CanAttack())
                enemy.PerformAttack();

            if (!enemy.IsInAttackRange()) return EnemyStateType.Chase;
            return null;
        }

        public void OnExit(EnemyBase enemy) { }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Retreat: ranged enemy backs away
    // ─────────────────────────────────────────────────────────────────────────
    public class RetreatState : IEnemyState
    {
        private float _timer = 1.0f;

        public void OnEnter(EnemyBase enemy) { _timer = 1.0f; }

        public EnemyStateType? Update(EnemyBase enemy, float delta)
        {
            if (!enemy.IsAlive) return EnemyStateType.Dead;

            if (enemy is RangedEnemy ranged)
            {
                Vector2 dir = ranged.GetRetreatDirection();
                enemy.MoveToward(enemy.GlobalPosition + dir * 100f, delta);

                if (enemy.CanAttack())
                    enemy.PerformAttack();
            }

            _timer -= delta;
            if (_timer <= 0f) return EnemyStateType.Chase;
            return null;
        }

        public void OnExit(EnemyBase enemy) => enemy.StopMoving();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Dead: terminal state
    // ─────────────────────────────────────────────────────────────────────────
    public class DeadState : IEnemyState
    {
        public void            OnEnter(EnemyBase enemy)                    => enemy.StopMoving();
        public EnemyStateType? Update(EnemyBase enemy, float delta)        => null;
        public void            OnExit(EnemyBase enemy)                     { }
    }
}
