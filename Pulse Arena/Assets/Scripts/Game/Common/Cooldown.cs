namespace Game.Common
{
    /// <summary>
    ///     A tiny value-type countdown timer. Wraps the "float _t; if (_t > 0f) _t -= dt;" pattern that
    ///     used to be repeated for every enemy cooldown. <see cref="Remaining" /> reads the raw seconds left
    ///     (so callers that used to read a bare float field keep byte-identical behaviour); <see cref="Set" />
    ///     overwrites it, <see cref="SetMax" /> keeps whichever value is larger (the ground-bounce keep-alive
    ///     used Mathf.Max), and <see cref="Tick" /> subtracts elapsed time only while the timer is running.
    ///     It is a struct on purpose: copied by value into helpers, no allocation, and resetting a whole
    ///     bag of them is just field assignment.
    /// </summary>
    public struct Cooldown
    {
        private float _t;

        /// <summary>Seconds left. 0 (or below) means expired. Matches the old raw float read.</summary>
        public float Remaining => _t;

        /// <summary>True while the timer still has time on it (&gt; 0), matching the old "_t &gt; 0f" checks.</summary>
        public bool IsActive => _t > 0f;

        /// <summary>Overwrites the remaining time (matches "_timer = seconds").</summary>
        public void Set(float seconds) => _t = seconds;

        /// <summary>Keeps the larger of the current and requested time (matches "Mathf.Max(_timer, seconds)").</summary>
        public void SetMax(float seconds)
        {
            if (seconds > _t)
                _t = seconds;
        }

        /// <summary>Zeroes the timer (matches "_timer = 0f").</summary>
        public void Clear() => _t = 0f;

        /// <summary>Subtracts elapsed time while running (matches "if (_t &gt; 0f) _t -= dt").</summary>
        public void Tick(float deltaTime)
        {
            if (_t > 0f)
                _t -= deltaTime;
        }
    }
}