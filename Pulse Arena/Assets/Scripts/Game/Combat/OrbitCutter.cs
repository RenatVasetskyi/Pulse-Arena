using System.Collections.Generic;
using System;
using Architecture.Services.Interfaces;
using Data;
using Game.Enemy;
using UnityEngine;
using Zenject;

namespace Game.Combat
{
    public class OrbitCutter : MonoBehaviour
    {
        private const float BladeStartOffset = 0.22f;

        public event Action BurstUsed;

        private readonly Dictionary<EnemyController, float> _hitTimers = new();

        private IInputService _inputService;
        private OrbitCutterData _data;
        private Transform _visualRoot;
        private Transform _coreVisual;
        private LineRenderer _bladeLine;
        private LineRenderer _bladeGlowLine;
        private TrailRenderer _motionTrail;
        private ParticleSystem _bladeParticles;
        private Light _coreLight;
        private float _angle;
        private float _burstTimer;

        [Inject]
        public void Construct(IInputService inputService, GameSettings gameSettings)
        {
            _inputService = inputService;
            _data = gameSettings.OrbitCutterData;
        }

        private void Start()
        {
            CreateVisual();
        }

        private void Update()
        {
            TickTimers();

            if (_inputService.IsOrbitBurstPressedThisFrame)
            {
                _burstTimer = 0.18f;
                BurstUsed?.Invoke();
            }

            _angle += _data.AngularSpeed * Time.deltaTime;
            UpdateVisual();
            HitEnemies();
        }

        private void TickTimers()
        {
            List<EnemyController> keys = new(_hitTimers.Keys);

            foreach (EnemyController enemy in keys)
            {
                if (enemy == null)
                {
                    _hitTimers.Remove(enemy);
                    continue;
                }

                _hitTimers[enemy] -= Time.deltaTime;

                if (_hitTimers[enemy] <= 0f)
                    _hitTimers.Remove(enemy);
            }

            if (_burstTimer > 0f)
                _burstTimer -= Time.deltaTime;
        }

        private void HitEnemies()
        {
            float hitRadius = _data.HitRadius * (_burstTimer > 0f ? 1.55f : 1f);
            int layerMask = _data.EnemyLayer.value == 0 ? Physics.DefaultRaycastLayers : _data.EnemyLayer.value;
            HashSet<EnemyController> enemies = new();

            CollectEnemiesAlongBlade(hitRadius, layerMask, enemies);

            if (enemies.Count == 0 && _data.EnemyLayer.value != 0)
                CollectEnemiesAlongBlade(hitRadius, Physics.DefaultRaycastLayers, enemies);

            CollectEnemiesByBladeDistance(hitRadius, enemies);

            foreach (EnemyController enemy in enemies)
            {
                if (enemy == null || enemy.IsGrabbed || _hitTimers.ContainsKey(enemy))
                    continue;

                Vector3 direction = enemy.transform.position - transform.position;
                direction.y = 0f;

                if (direction.sqrMagnitude <= 0.001f)
                    direction = transform.forward;

                float force = _data.KnockbackForce * (_burstTimer > 0f ? 1.8f : 1f);
                int damage = _data.Damage * (_burstTimer > 0f ? 2 : 1);

                enemy.Knockback(direction.normalized * force);

                enemy.TakeDamage(damage);

                _hitTimers[enemy] = _data.HitCooldown;
            }
        }

        private void CollectEnemiesAlongBlade(float hitRadius, int layerMask, HashSet<EnemyController> enemies)
        {
            Vector3 bladeRoot = GetBladeStartPosition();
            Vector3 bladeDirection = GetBladeDirection();
            float bladeLength = GetEffectiveBladeLength();
            int sampleCount = Mathf.Max(3, Mathf.CeilToInt(bladeLength / Mathf.Max(hitRadius, 0.1f)));

            for (int i = 0; i <= sampleCount; i++)
            {
                float t = i / (float)sampleCount;
                Vector3 samplePosition = bladeRoot + bladeDirection * (bladeLength * t);
                Collider[] hits = Physics.OverlapSphere(samplePosition, hitRadius, layerMask, QueryTriggerInteraction.Collide);

                foreach (Collider hit in hits)
                {
                    EnemyController enemy = hit.GetComponentInParent<EnemyController>();

                    if (enemy != null)
                        enemies.Add(enemy);
                }
            }
        }

        private void CollectEnemiesByBladeDistance(float hitRadius, HashSet<EnemyController> enemies)
        {
            Vector3 bladeRoot = GetBladeStartPosition();
            Vector3 bladeTip = GetBladeTipPosition();
            float hitDistance = hitRadius + 0.55f;
            float maxVerticalOffset = hitRadius + 1.1f;
            float sqrHitDistance = hitDistance * hitDistance;
            EnemyController[] activeEnemies = FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude);

            foreach (EnemyController enemy in activeEnemies)
            {
                if (enemy == null)
                    continue;

                Vector3 enemyCenter = enemy.transform.position + Vector3.up * _data.Height;

                if (Mathf.Abs(enemyCenter.y - bladeRoot.y) > maxVerticalOffset)
                    continue;

                if (GetSqrDistanceToBlade(enemyCenter, bladeRoot, bladeTip) <= sqrHitDistance)
                    enemies.Add(enemy);
            }
        }

        private void CreateVisual()
        {
            _visualRoot = new GameObject("Orbit Cutter").transform;
            _visualRoot.SetParent(transform, false);

            _coreVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            _coreVisual.name = "Cutter Core";
            _coreVisual.SetParent(_visualRoot, false);
            _coreVisual.localScale = Vector3.one * _data.VisualScale;
            Destroy(_coreVisual.GetComponent<Collider>());

            Material coreMaterial = CreateMaterial(_data.CoreColor, "Orbit Cutter Core");
            Material trailMaterial = CreateMaterial(_data.TrailColor, "Orbit Cutter Trail");
            _coreVisual.GetComponent<Renderer>().sharedMaterial = coreMaterial;

            CreateBladeLines(trailMaterial);
            CreateMotionTrail(trailMaterial);
            CreateBladeParticles(trailMaterial);
            CreateCoreLight();
        }

        private void UpdateVisual()
        {
            if (_visualRoot == null)
                return;

            Vector3 direction = GetBladeDirection();
            _visualRoot.position = GetBladeRootPosition();
            _visualRoot.rotation = Quaternion.LookRotation(direction);

            float burstScale = _burstTimer > 0f ? 1.35f : 1f;
            _coreVisual.localScale = Vector3.one * (_data.VisualScale * burstScale);

            float bladeScale = _burstTimer > 0f ? 1.35f : 1f;
            _bladeLine.startWidth = _data.BladeWidth * 0.18f * bladeScale;
            _bladeLine.endWidth = _data.BladeWidth * 1.45f * bladeScale;
            _bladeGlowLine.startWidth = _data.BladeWidth * 0.45f * bladeScale;
            _bladeGlowLine.endWidth = _data.BladeWidth * 2.4f * bladeScale;
            _coreLight.intensity = _burstTimer > 0f ? 3.2f : 1.8f;

            ParticleSystem.EmissionModule emission = _bladeParticles.emission;
            emission.rateOverTime = _burstTimer > 0f ? 150f : 80f;
        }

        private void CreateBladeLines(Material trailMaterial)
        {
            _bladeGlowLine = CreateLineRenderer("Cutter Blade Glow", trailMaterial);
            _bladeGlowLine.startWidth = _data.BladeWidth * 0.45f;
            _bladeGlowLine.endWidth = _data.BladeWidth * 2.4f;
            _bladeGlowLine.colorGradient = CreateGradient(
                WithAlpha(_data.TrailColor, 0.05f),
                WithAlpha(_data.TrailColor, 0.34f));

            _bladeLine = CreateLineRenderer("Cutter Blade Core", trailMaterial);
            _bladeLine.startWidth = _data.BladeWidth * 0.18f;
            _bladeLine.endWidth = _data.BladeWidth * 1.45f;
            _bladeLine.colorGradient = CreateGradient(
                WithAlpha(_data.TrailColor, 0.2f),
                WithAlpha(Color.white, 0.95f));
        }

        private LineRenderer CreateLineRenderer(string objectName, Material material)
        {
            GameObject lineObject = new(objectName);
            lineObject.transform.SetParent(_visualRoot, false);

            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.SetPosition(0, Vector3.forward * BladeStartOffset);
            line.SetPosition(1, Vector3.forward * _data.BladeLength);
            line.material = material;
            line.numCapVertices = 6;
            line.numCornerVertices = 6;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;

            return line;
        }

        private void CreateMotionTrail(Material trailMaterial)
        {
            _motionTrail = _visualRoot.gameObject.AddComponent<TrailRenderer>();
            _motionTrail.time = 0.07f;
            _motionTrail.minVertexDistance = 0.04f;
            _motionTrail.widthCurve = new AnimationCurve(
                new Keyframe(0f, _data.BladeWidth * 0.55f),
                new Keyframe(1f, 0f));
            _motionTrail.colorGradient = CreateGradient(
                WithAlpha(_data.CoreColor, 0.3f),
                WithAlpha(_data.TrailColor, 0f));
            _motionTrail.material = trailMaterial;
            _motionTrail.numCapVertices = 4;
            _motionTrail.numCornerVertices = 4;
        }

        private void CreateBladeParticles(Material particleMaterial)
        {
            GameObject particlesObject = new("Cutter Blade Particles");
            particlesObject.transform.SetParent(_visualRoot, false);

            _bladeParticles = particlesObject.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = _bladeParticles.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.11f);
            main.startColor = new ParticleSystem.MinMaxGradient(_data.CoreColor, _data.TrailColor);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 120;

            ParticleSystem.EmissionModule emission = _bladeParticles.emission;
            emission.rateOverTime = 80f;

            ParticleSystem.ShapeModule shape = _bladeParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.position = Vector3.forward * (BladeStartOffset + GetEffectiveBladeLength() * 0.5f);
            shape.scale = new Vector3(_data.BladeWidth * 1.4f, _data.BladeWidth * 1.4f, GetEffectiveBladeLength());

            ParticleSystemRenderer particleRenderer = particlesObject.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = particleMaterial;
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.sortingFudge = 1f;
        }

        private void CreateCoreLight()
        {
            _coreLight = _coreVisual.gameObject.AddComponent<Light>();
            _coreLight.type = LightType.Point;
            _coreLight.color = _data.CoreColor;
            _coreLight.range = 2.2f;
            _coreLight.intensity = 1.8f;
        }

        private Vector3 GetBladeRootPosition()
        {
            return transform.position + Vector3.up * _data.Height + GetBladeDirection() * _data.Radius;
        }

        private Vector3 GetBladeTipPosition()
        {
            return GetBladeRootPosition() + GetBladeDirection() * _data.BladeLength;
        }

        private Vector3 GetBladeStartPosition()
        {
            return GetBladeRootPosition() + GetBladeDirection() * BladeStartOffset;
        }

        private float GetEffectiveBladeLength()
        {
            return Mathf.Max(0.1f, _data.BladeLength - BladeStartOffset);
        }

        private Vector3 GetOrbitDirection()
        {
            return Quaternion.Euler(0f, _angle, 0f) * Vector3.forward;
        }

        private Vector3 GetBladeDirection()
        {
            return Quaternion.Euler(0f, _data.BladeAngleOffset, 0f) * GetOrbitDirection();
        }

        private float GetSqrDistanceToBlade(Vector3 point, Vector3 bladeRoot, Vector3 bladeTip)
        {
            Vector3 segment = bladeTip - bladeRoot;
            float segmentLengthSqr = segment.sqrMagnitude;

            if (segmentLengthSqr <= 0.001f)
                return (point - bladeRoot).sqrMagnitude;

            float t = Vector3.Dot(point - bladeRoot, segment) / segmentLengthSqr;
            t = Mathf.Clamp01(t);
            Vector3 closestPoint = bladeRoot + segment * t;

            point.y = 0f;
            closestPoint.y = 0f;

            return (point - closestPoint).sqrMagnitude;
        }

        private Material CreateMaterial(Color color, string materialName)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            shader ??= Shader.Find("Universal Render Pipeline/Unlit");
            shader ??= Shader.Find("Sprites/Default");

            Material material = new(shader)
            {
                name = materialName
            };

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);

            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);

            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", color * 2.5f);

            return material;
        }

        private Gradient CreateGradient(Color start, Color end)
        {
            Gradient gradient = new();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(start, 0f),
                    new GradientColorKey(end, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(start.a, 0f),
                    new GradientAlphaKey(end.a, 1f)
                });

            return gradient;
        }

        private Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
