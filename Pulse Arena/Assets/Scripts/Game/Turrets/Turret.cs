using System;
using Architecture.Services.Interfaces;
using Data;
using DG.Tweening;
using UnityEngine;
using Zenject;

namespace Game.Turrets
{
    /// <summary>
    ///     A stationary hazard turret. It swivels its head to track the player and fires a bullet at a fixed
    ///     cadence; the bullets damage only the player. After its lifetime it collapses (a crumble animation) and
    ///     despawns, freeing the spawner's slot. Placed by <see cref="TurretSpawner" />; the model plus the Head
    ///     (aim pivot) and Muzzle (bullet spawn) transforms live on the prefab. Mechanical-pause aware.
    /// </summary>
    public class Turret : MonoBehaviour, IPausable
    {
        private const float AimHeightOffset = 0.6f; // aim at the player's torso, not their feet
        private const float DestructDroopAngle = -45f; // the barrel droops before the turret collapses
        private const float DestructDroopDuration = 0.18f;
        private const float DestructCollapseDuration = 0.4f; // crumble-to-nothing + sink
        private const float DestructSinkDepth = 0.35f;

        [SerializeField] private Transform _head;
        [SerializeField] private Transform _muzzle;
        private TurretData _data;
        private bool _destructing;
        private Sequence _destructSeq;
        private ITurretFactory _factory;
        private float _fireTimer;
        private float _lifeTimer;
        private bool _paused;
        private IPauseService _pauseService;
        private Transform _target;

        /// <summary>Raised once when the turret is about to be destroyed, so the spawner can free its slot.</summary>
        public event Action<Turret> Despawned;

        [Inject]
        public void Construct(ITurretFactory factory, GameSettings settings, IPauseService pauseService)
        {
            _factory = factory;
            _data = settings.TurretData;
            _pauseService = pauseService;
        }

        public void Initialize(Transform target)
        {
            _target = target;
            _fireTimer = _data.FireInterval;
            _lifeTimer = _data.Lifetime;
            _pauseService?.Register(this);
        }

        /// <summary>Mechanical pause: freeze the aim, the fire + life timers, and the collapse tween.</summary>
        public void Pause()
        {
            _paused = true;
            _destructSeq?.Pause();
        }

        public void Resume()
        {
            _paused = false;
            _destructSeq?.Play();
        }

        private void Update()
        {
            if (_paused || _destructing || _target == null)
                return;

            TickLife();

            if (_destructing)
                return;

            AimAtTarget();
            TickFire();
        }

        private void OnDestroy()
        {
            _destructSeq?.Kill();
            _pauseService?.Unregister(this);
        }

        private void TickLife()
        {
            _lifeTimer -= Time.deltaTime;

            if (_lifeTimer <= 0f)
                StartDestruct();
        }

        // The barrel droops, then the whole turret crumbles down + sinks, then it despawns.
        private void StartDestruct()
        {
            _destructing = true;
            _destructSeq?.Kill();
            _destructSeq = DOTween.Sequence().SetLink(gameObject);

            if (_head != null)
                _destructSeq.Append(_head.DOLocalRotate(new Vector3(DestructDroopAngle, 0f, 0f), DestructDroopDuration)
                    .SetEase(Ease.OutQuad));

            _destructSeq.Append(transform.DOScale(Vector3.zero, DestructCollapseDuration).SetEase(Ease.InBack));
            _destructSeq.Join(transform.DOMoveY(transform.position.y - DestructSinkDepth, DestructCollapseDuration)
                .SetEase(Ease.InQuad));
            _destructSeq.OnComplete(Despawn);
        }

        private void Despawn()
        {
            Despawned?.Invoke(this);
            Destroy(gameObject);
        }

        // Yaw the head toward the player (level barrel — no pitch), eased so it tracks smoothly.
        private void AimAtTarget()
        {
            Vector3 toTarget = _target.position - _head.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude < 0.0001f)
                return;

            Quaternion desired = Quaternion.LookRotation(toTarget, Vector3.up);
            _head.rotation = Quaternion.Slerp(_head.rotation, desired, Time.deltaTime * _data.AimSpeed);
        }

        private void TickFire()
        {
            _fireTimer -= Time.deltaTime;

            if (_fireTimer > 0f)
                return;

            _fireTimer = _data.FireInterval;
            Fire();
        }

        private void Fire()
        {
            Vector3 aimPoint = _target.position + Vector3.up * AimHeightOffset;
            Vector3 direction = aimPoint - _muzzle.position;

            if (direction.sqrMagnitude < 0.0001f)
                direction = _muzzle.forward;

            _factory.CreateBullet(_muzzle.position, direction.normalized);
        }
    }
}
