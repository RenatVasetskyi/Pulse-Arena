using System;
using Architecture.Services.Interfaces;
using DG.Tweening;
using Game.Enemy;
using UnityEngine;
using Zenject;

namespace Game.Arena
{
    /// <summary>
    ///     A transient, pooled arena pit: grows in, stays open for its lifetime, then closes. An enemy flung into
    ///     its trigger is sucked in (instant ring-out) and the pit gulps shut. A NavMeshObstacle carves the navmesh
    ///     so walking enemies path around it. Spawned by <see cref="Game.Spawning.PitSpawner" /> via the pit factory.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Pit : MonoBehaviour, IPausable
    {
        [SerializeField] private float _growDuration = 0.35f;
        [SerializeField] private float _closeDuration = 0.4f;
        [SerializeField] private Collider _trigger;
        [SerializeField] private Transform _vortex;
        private bool _consumed;
        private Sequence _gulp;
        private Sequence _life;
        private IPauseService _pauseService;
        private float _suckDown = 4f;
        private Vector3 _targetScale;
        private Tween _vortexSpin;
        private Action<Pit> _returnToPool;

        /// <summary>Raised once when the pit is about to be destroyed (eaten or timed out), so the spawner can free its slot.</summary>
        public event Action<Pit> Despawned;

        [Inject]
        public void Construct(IPauseService pauseService)
        {
            _pauseService = pauseService;
        }

        /// <summary>Mechanical pause: freeze the grow/close (and gulp) tweens at their exact position.</summary>
        public void Pause()
        {
            _life?.Pause();
            _gulp?.Pause();
            _vortexSpin?.Pause();
        }

        public void Resume()
        {
            _life?.Play();
            _gulp?.Play();
            _vortexSpin?.Play();
        }

        /// <summary>Grow in, stay open for <paramref name="lifetime" />s, then close and despawn if unused.</summary>
        public void Initialize(float scale, float lifetime, float suckDown)
        {
            _consumed = false;
            _pauseService?.Register(this);
            _suckDown = suckDown;
            _targetScale = Vector3.one * scale;
            _trigger.enabled = true;
            transform.localScale = Vector3.zero;

            _life = DOTween.Sequence().SetLink(gameObject);
            _life.Append(transform.DOScale(_targetScale, _growDuration).SetEase(Ease.OutBack));
            _life.AppendInterval(Mathf.Max(0.1f, lifetime));
            _life.Append(transform.DOScale(Vector3.zero, _closeDuration).SetEase(Ease.InBack));
            _life.OnComplete(Despawn);

            StartVortexSpin();
        }

        /// <summary>Wires the return-to-pool action (set by the factory). Without it the pit self-destroys on despawn.</summary>
        public void SetPoolReturnAction(Action<Pit> returnToPool)
        {
            _returnToPool = returnToPool;
        }

        /// <summary>Reset on pool return: drop the pause registration and kill every tween so an inactive pit is inert.</summary>
        public void PrepareForPool()
        {
            _pauseService?.Unregister(this);
            _life?.Kill();
            _gulp?.Kill();
            _vortexSpin?.Kill();
        }

        // Endlessly rotate the glowing vortex so the pit reads as actively sucking (paused with the pit).
        private void StartVortexSpin()
        {
            if (_vortex == null)
                return;

            _vortexSpin?.Kill();
            _vortexSpin = _vortex.DOLocalRotate(new Vector3(0f, 360f, 0f), 3f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear).SetLoops(-1).SetLink(gameObject);
        }

        private void OnDestroy()
        {
            _pauseService?.Unregister(this);
            _life?.Kill();
            _vortexSpin?.Kill();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_consumed)
                return;

            Rigidbody body = other.attachedRigidbody;

            if (body == null)
                return;

            if (body.TryGetComponent(out EnemyController enemy))
                Consume(enemy);
        }

        private void Consume(EnemyController enemy)
        {
            _consumed = true;
            _trigger.enabled = false;

            // The enemy owns the suck-down physics; we just hand it the pit center + rate so its ringout
            // converges to the hole instead of launching sideways.
            enemy.FallIntoPit(transform.position, _suckDown);

            _life?.Kill();

            _gulp = DOTween.Sequence().SetLink(gameObject);
            _gulp.Append(transform.DOScale(_targetScale * 1.12f, 0.1f).SetEase(Ease.OutQuad));
            _gulp.Append(transform.DOScale(Vector3.zero, _closeDuration).SetEase(Ease.InBack));
            _gulp.OnComplete(Despawn);
        }

        private void Despawn()
        {
            Despawned?.Invoke(this);

            if (_returnToPool != null)
            {
                _returnToPool(this);
                return;
            }

            Destroy(gameObject);
        }
    }
}