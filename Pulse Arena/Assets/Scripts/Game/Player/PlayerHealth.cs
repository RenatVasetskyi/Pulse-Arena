using System;
using Game.Player.Interfaces;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    ///     Owns only the player's hit points + invulnerability window (post-hit i-frames and dash dodge frames).
    ///     Pure C# — no Rigidbody, no MonoBehaviour — so the rules are unit-testable. Mirrors
    ///     <see cref="Game.Enemy.EnemyHealth" />; death itself is left to the controller because the player dies from
    ///     two sources (HP depletion here + ring-out off the arena).
    /// </summary>
    public class PlayerHealth : IPlayerHealth
    {
        private int _current;
        private float _hitInvulnerability;
        private float _invulnerabilityTimer;
        private bool _isDead;
        private int _max;

        public event Action<int, int> Changed;

        public int Current => _current;
        public bool IsDepleted => _current <= 0;
        public bool IsInvulnerable => _invulnerabilityTimer > 0f;
        public int Max => _max;

        public void GrantInvulnerability(float seconds)
        {
            _invulnerabilityTimer = Mathf.Max(_invulnerabilityTimer, seconds);
        }

        public void Initialize(int maxHealth, float hitInvulnerability)
        {
            _max = Mathf.Max(1, maxHealth);
            _hitInvulnerability = hitInvulnerability;
            _current = _max;
            _invulnerabilityTimer = 0f;
            _isDead = false;
        }

        public void Kill()
        {
            if (_isDead)
                return;

            _current = 0;
            _isDead = true;
            Changed?.Invoke(_current, _max);
        }

        public bool TakeDamage(int amount)
        {
            if (_isDead || _invulnerabilityTimer > 0f)
                return false;

            _current = Mathf.Max(0, _current - Mathf.Max(0, amount));
            _invulnerabilityTimer = _hitInvulnerability;
            Changed?.Invoke(_current, _max);
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (_invulnerabilityTimer > 0f)
                _invulnerabilityTimer -= deltaTime;
        }

        public bool TryHeal(int amount)
        {
            if (_isDead || _current >= _max)
                return false;

            _current = Mathf.Min(_max, _current + Mathf.Max(0, amount));
            Changed?.Invoke(_current, _max);
            return true;
        }
    }
}