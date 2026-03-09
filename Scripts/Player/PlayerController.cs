using Godot;
using DungeonCrawler.Core;
using DungeonCrawler.Stats;
using DungeonCrawler.Weapons;
using DungeonCrawler.Inventory;

namespace DungeonCrawler.Player
{
    /// <summary>
    /// The player character. Handles input, shooting, item interaction, and stats display.
    /// </summary>
    public partial class PlayerController : Character
    {
        // ── Signals ────────────────────────────────────────────────────────────
        [Signal] public delegate void PlayerDiedEventHandler();
        [Signal] public delegate void HealthChangedEventHandler(float current, float max);
        [Signal] public delegate void ManaChangedEventHandler(float current, float max);
        [Signal] public delegate void CoinsChangedEventHandler(int coins);

        // ── Exports ────────────────────────────────────────────────────────────
        [Export] public float ManaRegen { get; set; } = Constants.PLAYER_MANA_REGEN;

        // ── State ──────────────────────────────────────────────────────────────
        public  int    Coins          { get; private set; } = 0;
        private float  _shootCooldown  = 0f;
        private bool   _isDodging      = false;
        private float  _dodgeDuration  = 0.25f;
        private float  _dodgeSpeed     = 500f;
        private float  _dodgeCooldown  = 0.8f;
        private float  _dodgeCooldownTimer = 0f;
        private Vector2 _dodgeDirection = Vector2.Zero;
        private float  _dodgeTimer     = 0f;

        // ── Weapon slot ────────────────────────────────────────────────────────
        private WeaponBase? _equippedWeapon;
        public  WeaponBase? EquippedWeapon => _equippedWeapon;

        // ── Inventory ──────────────────────────────────────────────────────────
        public PlayerInventory Inventory { get; private set; } = null!;

        // ── Interaction ────────────────────────────────────────────────────────
        private Area2D?       _interactionArea;
        private IInteractable? _nearestInteractable;

        // ── Godot lifecycle ────────────────────────────────────────────────────
        protected override void OnReady()
        {
            base.OnReady();
            AddToGroup(Constants.TAG_PLAYER);

            Inventory = GetNodeOrNull<PlayerInventory>("Inventory")
                     ?? CreateInventoryNode();

            _interactionArea = GetNodeOrNull<Area2D>("InteractionArea");
            if (_interactionArea != null)
            {
                _interactionArea.BodyEntered += OnBodyEnteredInteraction;
                _interactionArea.BodyExited  += OnBodyExitedInteraction;
            }

            Stats.OnStatChanged += OnStatChangedHandler;
        }

        protected override void InitialiseStats()
        {
            Stats.SetBaseStat(StatType.MaxHealth, Constants.PLAYER_BASE_HEALTH);
            Stats.SetBaseStat(StatType.Health,    Constants.PLAYER_BASE_HEALTH);
            Stats.SetBaseStat(StatType.Speed,     Constants.PLAYER_BASE_SPEED);
            Stats.SetBaseStat(StatType.Damage,    Constants.PLAYER_BASE_DAMAGE);
            Stats.SetBaseStat(StatType.MaxMana,   Constants.PLAYER_BASE_MANA);
            Stats.SetBaseStat(StatType.Mana,      Constants.PLAYER_BASE_MANA);
            Stats.SetBaseStat(StatType.CritChance, Constants.BASE_CRIT_CHANCE);
            Stats.SetBaseStat(StatType.CritDamage, Constants.BASE_CRIT_MULTIPLIER);
            Stats.SetBaseStat(StatType.DodgeChance, Constants.BASE_DODGE_CHANCE);
        }

        protected override void SetupCollision()
        {
            CollisionLayer = Constants.MASK_PLAYER;
            CollisionMask  = Constants.MASK_WALL | Constants.MASK_ENEMY;
        }

        protected override void OnPhysicsProcess(float delta)
        {
            HandleDodge(delta);
            if (!_isDodging)
            {
                Move(delta);
                HandleShooting(delta);
                HandleInteraction();
            }
            RegenMana(delta);
            if (_dodgeCooldownTimer > 0f)
                _dodgeCooldownTimer -= delta;
        }

        // ── ICharacter: Move ───────────────────────────────────────────────────
        public override void Move(float delta)
        {
            Vector2 dir = Vector2.Zero;
            if (Input.IsActionPressed("move_right")) dir.X += 1f;
            if (Input.IsActionPressed("move_left"))  dir.X -= 1f;
            if (Input.IsActionPressed("move_down"))  dir.Y += 1f;
            if (Input.IsActionPressed("move_up"))    dir.Y -= 1f;

            ApplyMovement(dir);
        }

        // ── Shooting ───────────────────────────────────────────────────────────
        private void HandleShooting(float delta)
        {
            if (_shootCooldown > 0f)
            {
                _shootCooldown -= delta;
                return;
            }

            Vector2 shootDir = Vector2.Zero;
            if (Input.IsActionPressed("shoot_right")) shootDir.X += 1f;
            if (Input.IsActionPressed("shoot_left"))  shootDir.X -= 1f;
            if (Input.IsActionPressed("shoot_down"))  shootDir.Y += 1f;
            if (Input.IsActionPressed("shoot_up"))    shootDir.Y -= 1f;

            if (shootDir == Vector2.Zero && Input.IsActionPressed("shoot_mouse"))
            {
                Vector2 mouseWorld = GetGlobalMousePosition();
                shootDir = (mouseWorld - GlobalPosition).Normalized();
            }

            if (shootDir == Vector2.Zero) return;

            FacingDirection = shootDir.Normalized();
            Shoot(shootDir.Normalized());
            float attackSpeed = Stats.GetStat(StatType.AttackSpeed);
            float baseDelay   = _equippedWeapon?.FireRate ?? 0.4f;
            _shootCooldown    = attackSpeed > 0f ? baseDelay / attackSpeed : baseDelay;
        }

        private void Shoot(Vector2 direction)
        {
            if (_equippedWeapon == null) return;
            _equippedWeapon.Fire(GlobalPosition, direction, Stats);
        }

        // ── Dodge ──────────────────────────────────────────────────────────────
        private void HandleDodge(float delta)
        {
            if (_isDodging)
            {
                _dodgeTimer -= delta;
                Velocity     = _dodgeDirection * _dodgeSpeed;
                MoveAndSlide();
                if (_dodgeTimer <= 0f)
                {
                    _isDodging       = false;
                    _isInvincible    = false;
                    _invincibilityTimer = 0f;
                }
                return;
            }

            if (Input.IsActionJustPressed("dodge") && _dodgeCooldownTimer <= 0f)
            {
                Vector2 dir = Vector2.Zero;
                if (Input.IsActionPressed("move_right")) dir.X += 1f;
                if (Input.IsActionPressed("move_left"))  dir.X -= 1f;
                if (Input.IsActionPressed("move_down"))  dir.Y += 1f;
                if (Input.IsActionPressed("move_up"))    dir.Y -= 1f;

                if (dir == Vector2.Zero) dir = FacingDirection;

                _dodgeDirection       = dir.Normalized();
                _isDodging            = true;
                _dodgeTimer           = _dodgeDuration;
                _dodgeCooldownTimer   = _dodgeCooldown;
                StartInvincibility(_dodgeDuration);
            }
        }

        // ── Interaction ────────────────────────────────────────────────────────
        private void HandleInteraction()
        {
            if (_nearestInteractable == null) return;
            if (!Input.IsActionJustPressed("interact")) return;
            if (_nearestInteractable.CanInteract)
                _nearestInteractable.Interact(this);
        }

        private void OnBodyEnteredInteraction(Node2D body)
        {
            if (body is IInteractable interactable)
                _nearestInteractable = interactable;
        }

        private void OnBodyExitedInteraction(Node2D body)
        {
            if (body is IInteractable interactable && interactable == _nearestInteractable)
                _nearestInteractable = null;
        }

        // ── Mana regen ─────────────────────────────────────────────────────────
        private void RegenMana(float delta)
        {
            float maxMana = Stats.GetStat(StatType.MaxMana);
            float curMana = Stats.GetStat(StatType.Mana);
            if (curMana < maxMana)
            {
                float regen = ManaRegen * delta;
                Stats.SetBaseStat(StatType.Mana, Mathf.Min(maxMana, curMana + regen));
            }
        }

        // ── Currency ───────────────────────────────────────────────────────────
        public void AddCoins(int amount)
        {
            Coins += amount;
            EmitSignal(SignalName.CoinsChanged, Coins);
            GameEvents.RaisePlayerCoinsChanged(Coins);
        }

        public bool SpendCoins(int amount)
        {
            if (Coins < amount) return false;
            Coins -= amount;
            EmitSignal(SignalName.CoinsChanged, Coins);
            GameEvents.RaisePlayerCoinsChanged(Coins);
            return true;
        }

        // ── Weapon management ───────────────────────────────────────────────────
        public void EquipWeapon(WeaponBase weapon)
        {
            _equippedWeapon?.QueueFree();
            _equippedWeapon = weapon;
            if (!weapon.IsInsideTree())
                AddChild(weapon);
            GameEvents.RaiseWeaponEquipped(weapon.WeaponId);
        }

        // ── Death ──────────────────────────────────────────────────────────────
        protected override void OnDeath()
        {
            GD.Print("[Player] Player died!");
            IsAlive = false;
            EmitSignal(SignalName.PlayerDied);
            GameEvents.RaisePlayerDied();
            // Don't QueueFree immediately — let GameManager handle the death screen.
        }

        // ── Stat changed callback ──────────────────────────────────────────────
        private void OnStatChangedHandler(StatType stat, float newVal)
        {
            switch (stat)
            {
                case StatType.Health:
                    float maxHp = Stats.GetStat(StatType.MaxHealth);
                    EmitSignal(SignalName.HealthChanged, newVal, maxHp);
                    GameEvents.RaisePlayerHealthChanged(newVal, maxHp);
                    break;
                case StatType.MaxHealth:
                    float curHp = Stats.GetStat(StatType.Health);
                    EmitSignal(SignalName.HealthChanged, curHp, newVal);
                    GameEvents.RaisePlayerHealthChanged(curHp, newVal);
                    break;
                case StatType.Mana:
                    float maxMana = Stats.GetStat(StatType.MaxMana);
                    EmitSignal(SignalName.ManaChanged, newVal, maxMana);
                    GameEvents.RaisePlayerManaChanged(newVal, maxMana);
                    break;
                case StatType.MaxMana:
                    float curMana = Stats.GetStat(StatType.Mana);
                    EmitSignal(SignalName.ManaChanged, curMana, newVal);
                    GameEvents.RaisePlayerManaChanged(curMana, newVal);
                    break;
            }
        }

        // ── Private helpers ────────────────────────────────────────────────────
        private PlayerInventory CreateInventoryNode()
        {
            var inv = new PlayerInventory { Name = "Inventory" };
            AddChild(inv);
            return inv;
        }
    }
}
