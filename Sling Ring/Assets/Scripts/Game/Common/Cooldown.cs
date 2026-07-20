namespace Game.Common
{
    /// <summary>
    ///     A tiny value-type countdown timer wrapping the "float _t; if (_t > 0f) _t -= dt;" pattern.
    ///     A struct on purpose: copied by value into helpers, no allocation, and resetting a whole bag of
    ///     them is just field assignment.
    /// </summary>
    public struct Cooldown
    {
        private float _t;

        /// <summary>Seconds left. 0 (or below) means expired.</summary>
        public float Remaining => _t;

        public bool IsActive => _t > 0f;

        public void Set(float seconds)
        {
            _t = seconds;
        }

        /// <summary>Keeps the larger of the current and requested time.</summary>
        public void SetMax(float seconds)
        {
            if (seconds > _t)
                _t = seconds;
        }

        public void Clear()
        {
            _t = 0f;
        }

        public void Tick(float deltaTime)
        {
            if (_t > 0f)
                _t -= deltaTime;
        }
    }
}