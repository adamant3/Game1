using System;
using Godot;
using DungeonCrawler.Generation;

namespace DungeonCrawler.Rooms
{
    /// <summary>
    /// A miscellaneous room offering one of three random special events:
    ///   0 – Healing Spring  : interactable that restores 3 HP
    ///   1 – Secret Stash    : a free chest with a random item
    ///   2 – Challenge Room  : 30-second timed combat with a bonus reward
    /// </summary>
    public partial class MiscRoom : BaseRoom
    {
        // 0=healing spring, 1=stash, 2=challenge.
        private int _eventType = 0;
        private bool _challengeCompleted = false;
        private float _challengeTimer = 0f;
        private const float ChallengeTimeLimitSeconds = 30f;
        private const float ChallengeGracePeriod = 5f;

        [Export] public PackedScene? SpringScene { get; set; }
        [Export] public PackedScene? ChestScene { get; set; }
        [Export] public PackedScene? ChallengeEnemyScene { get; set; }
        [Export] public PackedScene? BonusItemScene { get; set; }

        private static readonly Random _rng = new Random();

        public override void Initialize(RoomData roomData)
        {
            _roomData = roomData;
            RoomType  = Generation.RoomType.Misc;
            SetupRoom();
        }

        public override void SetupRoom()
        {
            _eventType = _rng.Next(0, 3);

            switch (_eventType)
            {
                case 0: SetupHealingSpring();  break;
                case 1: SetupSecretStash();    break;
                case 2: SetupChallengeRoom();  break;
            }

            GD.Print($"[MiscRoom] Event type: {_eventType} ({GetEventName()})");
        }

        public override void OnRoomEntered(Node player)
        {
            if (_eventType == 2)
            {
                // Challenge room: lock doors and start the timer.
                if (!IsCleared)
                    LockDoors();
            }
            else
            {
                // Non-combat events clear immediately.
                if (!IsCleared)
                {
                    IsCleared = true;
                    Core.GameEvents.RaiseRoomCleared(_roomData?.UniqueId ?? Name);
                }
            }

            GD.Print($"[MiscRoom] {GetEventName()} – {GetEventHint()}");
        }

        public override void OnRoomCleared()
        {
            if (_eventType == 2 && !_challengeCompleted)
            {
                _challengeCompleted = true;
                bool inTime = _challengeTimer <= ChallengeTimeLimitSeconds;
                if (inTime)
                {
                    GD.Print("[MiscRoom] Challenge completed in time! Bonus reward granted.");
                    SpawnBonusReward();
                }
                else
                {
                    GD.Print("[MiscRoom] Challenge completed, but time limit exceeded.");
                }
            }
        }

        public override void _Process(double delta)
        {
            base._Process(delta);

            // Advance the challenge timer only during an active challenge.
            if (_eventType == 2 && !IsCleared && HasBeenVisited)
            {
                _challengeTimer += (float)delta;

                // If time runs out without clearing, force-clear (no bonus).
                if (_challengeTimer > ChallengeTimeLimitSeconds + ChallengeGracePeriod && !IsCleared)
                {
                    GD.Print("[MiscRoom] Time limit expired – challenge failed.");
                    TriggerRoomCleared();
                }
            }
        }

        // ── Event setups ───────────────────────────────────────────────────────

        private void SetupHealingSpring()
        {
            OpenDoors();

            if (SpringScene != null)
            {
                var spring = SpringScene.Instantiate<Node2D>();
                var container = _interactableContainer ?? this;
                container.AddChild(spring);
                spring.Position = Vector2.Zero;

                // Connect heal signal if the spring exposes one.
                if (spring.HasSignal("Used"))
                    spring.Connect("Used", new Callable(this, nameof(OnSpringUsed)));
            }
            else
            {
                // Fallback placeholder.
                var rect = new ColorRect
                {
                    Color = new Color(0.0f, 0.8f, 1.0f, 0.7f),
                    Size  = new Vector2(50f, 50f)
                };
                rect.Position = new Vector2(-25f, -25f);
                AddChild(rect);

                var label = new Label { Text = "Healing Spring\n(+3 HP)" };
                label.Position = new Vector2(-40f, -60f);
                AddChild(label);
            }
        }

        private void SetupSecretStash()
        {
            OpenDoors();

            if (ChestScene != null)
            {
                var chest = ChestScene.Instantiate<Node2D>();
                var container = _interactableContainer ?? this;
                container.AddChild(chest);
                chest.Position = Vector2.Zero;
            }
            else
            {
                var rect = new ColorRect
                {
                    Color = new Color(0.9f, 0.7f, 0.1f),
                    Size  = new Vector2(48f, 48f)
                };
                rect.Position = new Vector2(-24f, -24f);
                AddChild(rect);

                var label = new Label { Text = "Secret Stash" };
                label.Position = new Vector2(-32f, -60f);
                AddChild(label);
            }
        }

        private void SetupChallengeRoom()
        {
            // Doors start open; they lock when the player enters.
            OpenDoors();

            int enemyCount = _rng.Next(3, 7);
            if (ChallengeEnemyScene != null)
            {
                for (int i = 0; i < enemyCount; i++)
                {
                    float angle  = (float)(_rng.NextDouble() * Math.PI * 2.0);
                    float radius = (float)(_rng.NextDouble() * 120.0 + 60.0);
                    Vector2 pos  = GlobalPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    SpawnEnemy(ChallengeEnemyScene, pos);
                }
            }

            var timerLabel = new Label { Text = $"CHALLENGE: {ChallengeTimeLimitSeconds:F0}s" };
            timerLabel.Name = "TimerLabel";
            timerLabel.Position = new Vector2(-60f, -160f);
            AddChild(timerLabel);
        }

        private void SpawnBonusReward()
        {
            if (BonusItemScene == null) return;
            var item = BonusItemScene.Instantiate<Node2D>();
            var container = _itemContainer ?? this;
            container.AddChild(item);
            item.GlobalPosition = GlobalPosition;
        }

        // ── Signal handlers ────────────────────────────────────────────────────

        private void OnSpringUsed(Node user)
        {
            GD.Print("[MiscRoom] Healing spring used – restoring 3 HP.");
            if (user.HasMethod("Heal"))
                user.Call("Heal", 3.0f);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private string GetEventName() => _eventType switch
        {
            0 => "Healing Spring",
            1 => "Secret Stash",
            2 => "Challenge Room",
            _ => "Unknown Event"
        };

        private string GetEventHint() => _eventType switch
        {
            0 => "Interact with the spring to restore 3 HP.",
            1 => "A hidden chest – open it for a free item!",
            2 => $"Defeat all enemies within {ChallengeTimeLimitSeconds} seconds for a bonus reward!",
            _ => ""
        };
    }
}
