using System;

namespace DungeonCrawler.Stats
{
    public enum ModifierType { Flat, Percentage, Override }

    [Serializable]
    public class StatModifier
    {
        public string       Id           { get; private set; }
        public StatType     StatType     { get; private set; }
        public float        Value        { get; private set; }
        public ModifierType ModifierType { get; private set; }
        /// <summary>Lifetime in seconds. -1 = permanent.</summary>
        public float        Duration     { get; private set; }
        public string       Source       { get; private set; }

        public bool IsExpired => Duration >= 0 && _timeRemaining <= 0f;

        private float _timeRemaining;

        public StatModifier(string id, StatType statType, float value,
                            ModifierType modType, float duration = -1f, string source = "")
        {
            Id           = id;
            StatType     = statType;
            Value        = value;
            ModifierType = modType;
            Duration     = duration;
            _timeRemaining = duration;
            Source       = source;
        }

        /// <summary>Tick down time-limited modifiers. Call every frame.</summary>
        public void Update(float delta)
        {
            if (Duration >= 0f)
                _timeRemaining -= delta;
        }

        public float GetTimeRemaining() => _timeRemaining;

        public override string ToString() =>
            $"[StatModifier id={Id} stat={StatType} val={Value} type={ModifierType} src={Source} ttl={_timeRemaining:F2}]";
    }
}
