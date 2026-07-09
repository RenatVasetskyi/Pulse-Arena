using System;
using Game.Common.Interfaces;
using UnityEngine;

namespace Game.Common
{
    /// <summary>
    ///     Shared hit-point model for both actors — the player and every enemy (replaces the near-identical
    ///     PlayerHealth + EnemyHealth twins). Pure C# (no Rigidbody, no MonoBehaviour) so the rules are
    ///     unit-testable in isolation. Invulnerability is opt-in: pass a non-zero <c>hitInvulnerability</c> to
    ///     <see cref="Initialize" /> (the player's post-hit + dash i-frames); enemies pass 0 and never gain
    ///     frames. Death is exposed BOTH ways so each owner picks what fits — enemies react to
    ///     <see cref="Died" />; the player ignores it and drives its own death from <see cref="IsDepleted" />
    ///     because it also dies from ring-out off the arena.
    /// </summary>
    public class ActorHealth : IActorHealth
    {
        private int _current;
        private float _hitInvulnerability;
        private float _invulnerabilityTimer;
        private bool _isDead;
        private int _max;

        public event Action<int, int> Changed; // (current, max)
        public event Action Died;

        public int Current => _current;
        public bool IsDepleted => _current <= 0;
        public bool IsInvulnerable => _invulnerabilityTimer > 0f;
        public int Max => _max;

        public void GrantInvulnerability(float seconds)
        {
            _invulnerabilityTimer = Mathf.Max(_invulnerabilityTimer, seconds);
        }

        public void Initialize(int maxHealth, float hitInvulnerability = 0f)
        {
            _max = Mathf.Max(1, maxHealth);
            _hitInvulnerability = hitInvulnerability;
            _current = _max;
            _invulnerabilityTimer = 0f;
            _isDead = false;
            Changed?.Invoke(_current, _max);
        }

        public void Kill()
        {
            if (_isDead)
                return;

            _current = 0;
            _isDead = true;
            Changed?.Invoke(_current, _max);
            Died?.Invoke();
        }

        public bool TakeDamage(int amount)
        {
            if (_isDead || _invulnerabilityTimer > 0f)
                return false;

            _current = Mathf.Max(0, _current - Mathf.Max(0, amount));
            _invulnerabilityTimer = _hitInvulnerability;
            Changed?.Invoke(_current, _max);

            if (_current <= 0)
            {
                _isDead = true;
                Died?.Invoke();
            }

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
