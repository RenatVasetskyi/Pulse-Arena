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
        private float _cooldownTimer;
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

            if (_inputService.IsPulsePressedThisFrame)
                TryUse();
        }

        public void AddEnergy(float value)
        {
            _charge = Mathf.Clamp(_charge + value, 0f, _data.MaxCharge);
            ChargeChanged?.Invoke(_charge / _data.MaxCharge);
        }

        private void TryUse()
        {
            if (_cooldownTimer > 0f)
                return;

            PushEnemies();
            PlayPulseVisual();

            _cooldownTimer = _data.Cooldown;
            _charge = 0f;
            ChargeChanged?.Invoke(0f);
            Used?.Invoke();
        }

        private void TickCooldown()
        {
            if (_cooldownTimer > 0f)
                _cooldownTimer -= Time.deltaTime;
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

        private void PlayPulseVisual()
        {
            if (_visualRoutine != null)
                StopCoroutine(_visualRoutine);

            _visualRoutine = StartCoroutine(PulseVisualRoutine());
        }

        private IEnumerator PulseVisualRoutine()
        {
            LineRenderer ring = CreateRing();
            float elapsed = 0f;

            while (elapsed < _data.VisualDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _data.VisualDuration);
                float radius = Mathf.Lerp(_data.VisualStartRadius, _data.Radius, t);
                float alpha = Mathf.Lerp(_data.VisualColor.a, 0f, t);

                DrawRing(ring, radius);
                ring.startColor = WithAlpha(_data.VisualColor, alpha);
                ring.endColor = WithAlpha(_data.VisualColor, alpha);

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
