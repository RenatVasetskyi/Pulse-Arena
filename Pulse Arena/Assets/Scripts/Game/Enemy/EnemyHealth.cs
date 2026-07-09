using System;
using UnityEngine;

namespace Game.Enemy
{
    /// <summary>
    ///     Owns only the enemy's hit-point number. The controller reacts to Changed/Died
    ///     (health bar, score, death state) — this class knows nothing about them.
    /// </summary>
    public class EnemyHealth
    {
        private int _current;
        private int _max;

        public event Action<int, int> Changed; // (current, max)
        public event Action Died;

        public int Current => _current;
        public int Max => _max;

        public void Kill()
        {
            if (_current <= 0)
                return;

            _current = 0;
            Changed?.Invoke(0, _max);
            Died?.Invoke();
        }

        public void Reset(int maxHealth)
        {
            _max = Mathf.Max(1, maxHealth);
            _current = _max;
            Changed?.Invoke(_current, _max);
        }

        public void TakeDamage(int damage)
        {
            if (_current <= 0)
                return;

            _current = Mathf.Max(0, _current - Mathf.Max(0, damage));
            Changed?.Invoke(_current, _max);

            if (_current <= 0)
                Died?.Invoke();
        }
    }
}