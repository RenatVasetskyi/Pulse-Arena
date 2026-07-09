using System;
using DG.Tweening;
using Game.Enemy;
using UnityEngine;

namespace Game.Arena
{
    /// <summary>
    ///     A transient arena pit. It grows in at a random spot, stays open for a while, and any enemy flung
    ///     into its trigger gets sucked in (instant ring-out) — after which the pit gulps shut and despawns.
    ///     If nothing falls in, it closes on its own after its lifetime. A NavMeshObstacle on the same object
    ///     carves the navmesh so walking enemies path around it; the player has no EnemyController so it can
    ///     cross safely. Spawned/pooled by <see cref="Game.Spawning.PitSpawner" /> via the pit factory.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Pit : MonoBehaviour
    {
        [SerializeField] private float _growDuration = 0.35f;
        [SerializeField] private float _closeDuration = 0.4f;
        private bool _consumed;
        private Sequence _life;
        private float _suckDown = 4f;
        private float _suckSpeed = 12f;
        private Vector3 _targetScale;

        private Collider _trigger;

        /// <summary>Raised once when the pit is about to be destroyed (eaten or timed out), so the spawner can free its slot.</summary>
        public event Action<Pit> Despawned;

        private void Awake()
        {
            _trigger = GetComponent<Collider>();
        }

        private void OnDestroy()
        {
            _life?.Kill();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_consumed)
                return;

            Rigidbody body = other.attachedRigidbody;

            if (body == null)
                return;

            EnemyController enemy = body.GetComponent<EnemyController>();

            if (enemy != null)
                Consume(body, enemy);
        }

        /// <summary>Grow in, stay open for <paramref name="lifetime" />s, then close and despawn if unused.</summary>
        public void Initialize(float scale, float lifetime, float suckSpeed, float suckDown)
        {
            _consumed = false;
            _suckSpeed = suckSpeed;
            _suckDown = suckDown;
            _targetScale = Vector3.one * scale;
            _trigger.enabled = true;
            transform.localScale = Vector3.zero;

            _life = DOTween.Sequence().SetLink(gameObject);
            _life.Append(transform.DOScale(_targetScale, _growDuration).SetEase(Ease.OutBack));
            _life.AppendInterval(Mathf.Max(0.1f, lifetime));
            _life.Append(transform.DOScale(Vector3.zero, _closeDuration).SetEase(Ease.InBack));
            _life.OnComplete(Despawn);
        }

        private void Consume(Rigidbody body, EnemyController enemy)
        {
            _consumed = true;
            _trigger.enabled = false;

            enemy.FallIntoPit();
            Suck(body);

            _life?.Kill();

            Sequence gulp = DOTween.Sequence().SetLink(gameObject);
            gulp.Append(transform.DOScale(_targetScale * 1.12f, 0.1f).SetEase(Ease.OutQuad));
            gulp.Append(transform.DOScale(Vector3.zero, _closeDuration).SetEase(Ease.InBack));
            gulp.OnComplete(Despawn);
        }

        // Yank the enemy toward the pit center (and down), so it visibly gets sucked in as it shrinks.
        private void Suck(Rigidbody body)
        {
            Vector3 toCenter = transform.position - body.position;
            toCenter.y = 0f;

            Vector3 horizontal = toCenter.sqrMagnitude > 0.001f ? toCenter.normalized : Vector3.zero;
            body.linearVelocity = horizontal * _suckSpeed + Vector3.down * _suckDown;
        }

        private void Despawn()
        {
            Despawned?.Invoke(this);
            Destroy(gameObject);
        }
    }
}