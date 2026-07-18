using System;
using Architecture.Services.Interfaces;
using Game.Player;
using UnityEngine;

namespace Game.Turrets
{
    /// <summary>
    ///     A turret projectile. Flies straight along its fired direction; it damages ONLY the player and is stopped
    ///     by solid walls/boxes (non-trigger colliders on the obstacle mask), but passes through enemies and ground.
    ///     It also despawns after its lifetime if it hits nothing. The prefab needs a trigger collider + a kinematic
    ///     Rigidbody so the trigger fires. Mechanical-pause aware.
    /// </summary>
    public class TurretBullet : MonoBehaviour, IPausable
    {
        private float _age;
        private int _damage;
        private Vector3 _direction;
        private float _lifetime;
        private LayerMask _obstacleMask;
        private bool _paused;
        private IPauseService _pauseService;
        private Action<TurretBullet> _returnToPool;
        private float _speed;

        public void Initialize(Vector3 direction, float speed, int damage, float lifetime, LayerMask obstacleMask,
            IPauseService pauseService)
        {
            _direction = direction;
            _speed = speed;
            _damage = damage;
            _lifetime = lifetime;
            _obstacleMask = obstacleMask;
            _pauseService = pauseService;
            _age = 0f;
            _paused = false; // a pooled bullet must not come back frozen; Register below re-pauses it if already paused
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            _pauseService?.Register(this);
        }

        /// <summary>Wires the return-to-pool action (set by the factory). Without it the bullet self-destroys on despawn.</summary>
        public void SetPoolReturnAction(Action<TurretBullet> returnToPool)
        {
            _returnToPool = returnToPool;
        }

        /// <summary>Reset on pool return: drop the pause registration so an inactive pooled bullet is not tracked.</summary>
        public void PrepareForPool()
        {
            _pauseService?.Unregister(this);
        }

        /// <summary>Mechanical pause: freeze the bullet in flight.</summary>
        public void Pause()
        {
            _paused = true;
        }

        public void Resume()
        {
            _paused = false;
        }

        private void Update()
        {
            if (_paused)
                return;

            transform.position += _direction * (_speed * Time.deltaTime);
            _age += Time.deltaTime;

            if (_age >= _lifetime)
                Despawn();
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();

            if (player != null)
            {
                player.TakeDamage(_damage, transform.position);
                Despawn();
                return;
            }

            // Stopped by a solid wall/box (a non-trigger collider on the obstacle mask); enemies + ground + the
            // pit/pickup triggers are not on that mask (or are triggers), so the bullet flies through them.
            if (!other.isTrigger && (_obstacleMask.value & (1 << other.gameObject.layer)) != 0)
                Despawn();
        }

        private void OnDestroy()
        {
            _pauseService?.Unregister(this);
        }

        private void Despawn()
        {
            if (_returnToPool != null)
            {
                _returnToPool(this);
                return;
            }

            _pauseService?.Unregister(this);
            Destroy(gameObject);
        }
    }
}
