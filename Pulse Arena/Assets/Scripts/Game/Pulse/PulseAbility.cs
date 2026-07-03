using System;
using System.Collections;
using Architecture.Services.Interfaces;
using Data;
using Game.Enemy;
using UnityEngine;
using Zenject;

namespace Game.Pulse
{
    public class PulseAbility : MonoBehaviour
    {
        public event Action Used;
        public event Action<float> ChargeChanged;

        private const int RingSegments = 64;

        private IInputService _inputService;
        private PulseData _data;
        private float _charge;
        private float _pushCooldownTimer;
        private float _pullCooldownTimer;
        private Coroutine _visualRoutine;
        private Material _ringMaterial;

        [Inject]
        public void Construct(IInputService inputService, GameSettings gameSettings)
        {
            _inputService = inputService;
            _data = gameSettings.PulseData;
        }

        private void Update()
        {
            TickCooldown();

            if (_inputService.IsPushPressedThisFrame)
                TryPush();

            if (_inputService.IsPullPressedThisFrame)
                TryPull();
        }

        public void AddEnergy(float value)
        {
            _charge = Mathf.Clamp(_charge + value, 0f, _data.MaxCharge);
            ChargeChanged?.Invoke(_charge / _data.MaxCharge);
        }

        private void TryPush()
        {
            if (_pushCooldownTimer > 0f)
                return;

            PushEnemies();
            PlayPushVisual();

            _pushCooldownTimer = _data.Cooldown;
            _charge = 0f;
            ChargeChanged?.Invoke(0f);
            Used?.Invoke();
        }

        private void TryPull()
        {
            if (_pullCooldownTimer > 0f)
                return;

            PullEnemies();
            PlayPullVisual();

            _pullCooldownTimer = _data.PullCooldown;
            Used?.Invoke();
        }

        private void TickCooldown()
        {
            if (_pushCooldownTimer > 0f)
                _pushCooldownTimer -= Time.deltaTime;

            if (_pullCooldownTimer > 0f)
                _pullCooldownTimer -= Time.deltaTime;
        }

        private void PushEnemies()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, _data.Radius, _data.EnemyLayer);

            foreach (Collider hit in hits)
            {
                EnemyController enemy = hit.GetComponentInParent<EnemyController>();

                if (enemy == null)
                    continue;

                Vector3 planarDirection = enemy.transform.position - transform.position;
                planarDirection.y = 0f;

                if (planarDirection.sqrMagnitude <= 0.001f)
                    planarDirection = transform.forward;

                float distance = Mathf.Min(planarDirection.magnitude, _data.Radius);
                float falloff = 1f - Mathf.Clamp01(distance / _data.Radius);
                float forceMultiplier = Mathf.Lerp(_data.MinForceMultiplier, 1f, falloff);

                Vector3 direction = (planarDirection.normalized + Vector3.up * _data.UpwardForceRatio).normalized;
                enemy.Knockback(direction * (_data.Force * forceMultiplier));
            }
        }

        private void PullEnemies()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, _data.PullRadius, _data.EnemyLayer);

            for (int i = 0; i < hits.Length; i++)
            {
                EnemyController enemy = hits[i].GetComponentInParent<EnemyController>();

                if (enemy == null)
                    continue;

                Vector3 radialDirection = enemy.transform.position - transform.position;
                radialDirection.y = 0f;

                if (radialDirection.sqrMagnitude <= 0.001f)
                    radialDirection = Quaternion.Euler(0f, i * 45f, 0f) * transform.forward;

                Vector3 targetPosition = transform.position + radialDirection.normalized * _data.PullStopRadius;
                enemy.PullTo(targetPosition, _data.PullForce, _data.PullUpwardForceRatio, _data.PullStasisDuration);
            }
        }

        private void PlayPushVisual()
        {
            if (_visualRoutine != null)
                StopCoroutine(_visualRoutine);

            _visualRoutine = StartCoroutine(RingVisualRoutine(
                _data.VisualStartRadius,
                _data.Radius,
                _data.VisualColor,
                _data.VisualDuration));
        }

        private void PlayPullVisual()
        {
            if (_visualRoutine != null)
                StopCoroutine(_visualRoutine);

            _visualRoutine = StartCoroutine(RingVisualRoutine(
                _data.PullRadius,
                _data.PullStopRadius,
                _data.PullVisualColor,
                _data.VisualDuration));
        }

        private IEnumerator RingVisualRoutine(float startRadius, float targetRadius, Color color, float duration)
        {
            LineRenderer ring = CreateRing();
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float radius = Mathf.Lerp(startRadius, targetRadius, t);
                float alpha = Mathf.Lerp(color.a, 0f, t);

                DrawRing(ring, radius);
                ring.startColor = WithAlpha(color, alpha);
                ring.endColor = WithAlpha(color, alpha);

                yield return null;
            }

            Destroy(ring.gameObject);
            _visualRoutine = null;
        }

        private LineRenderer CreateRing()
        {
            GameObject ringObject = new("Pulse Shockwave");
            LineRenderer ring = ringObject.AddComponent<LineRenderer>();

            ring.useWorldSpace = true;
            ring.loop = true;
            ring.positionCount = RingSegments;
            ring.widthMultiplier = _data.VisualWidth;
            ring.numCapVertices = 4;
            ring.numCornerVertices = 4;
            ring.material = GetRingMaterial();

            return ring;
        }

        private void DrawRing(LineRenderer ring, float radius)
        {
            Vector3 center = transform.position + Vector3.up * _data.VisualHeight;

            for (int i = 0; i < RingSegments; i++)
            {
                float angle = i / (float)RingSegments * Mathf.PI * 2f;
                Vector3 point = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                ring.SetPosition(i, point);
            }
        }

        private Material GetRingMaterial()
        {
            if (_ringMaterial != null)
                return _ringMaterial;

            Shader shader = Shader.Find("Sprites/Default");
            _ringMaterial = new Material(shader)
            {
                name = "Pulse Shockwave"
            };

            return _ringMaterial;
        }

        private Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
