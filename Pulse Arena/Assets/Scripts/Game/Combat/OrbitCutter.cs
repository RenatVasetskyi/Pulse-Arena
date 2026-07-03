using System.Collections.Generic;
using Architecture.Services.Interfaces;
using Data;
using Game.Enemy;
using UnityEngine;
using Zenject;

namespace Game.Combat
{
    public class OrbitCutter : MonoBehaviour
    {
        private readonly Dictionary<EnemyController, float> _hitTimers = new();

        private IInputService _inputService;
        private IScoreService _scoreService;
        private OrbitCutterData _data;
        private EnemyData _enemyData;
        private Transform _visualRoot;
        private Transform _coreVisual;
        private Transform _bladeVisual;
        private float _angle;
        private float _burstTimer;

        [Inject]
        public void Construct(IInputService inputService, IScoreService scoreService, GameSettings gameSettings)
        {
            _inputService = inputService;
            _scoreService = scoreService;
            _data = gameSettings.OrbitCutterData;
            _enemyData = gameSettings.EnemyData;
        }

        private void Start()
        {
            CreateVisual();
        }

        private void Update()
        {
            TickTimers();

            if (_inputService.IsOrbitBurstPressedThisFrame)
                _burstTimer = 0.18f;

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
            Collider[] hits = Physics.OverlapSphere(GetBladePosition(), hitRadius, _data.EnemyLayer);

            foreach (Collider hit in hits)
            {
                EnemyController enemy = hit.GetComponentInParent<EnemyController>();

                if (enemy == null || enemy.IsGrabbed || _hitTimers.ContainsKey(enemy))
                    continue;

                Vector3 direction = enemy.transform.position - transform.position;
                direction.y = 0f;

                if (direction.sqrMagnitude <= 0.001f)
                    direction = transform.forward;

                float force = _data.KnockbackForce * (_burstTimer > 0f ? 1.8f : 1f);
                int damage = _data.Damage * (_burstTimer > 0f ? 2 : 1);

                enemy.Knockback((direction.normalized + Vector3.up * 0.25f).normalized * force);

                if (enemy.TakeDamage(damage))
                    _scoreService.Add(_enemyData.ScoreReward);

                _hitTimers[enemy] = _data.HitCooldown;
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

            _bladeVisual = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            _bladeVisual.name = "Cutter Blade";
            _bladeVisual.SetParent(_coreVisual, false);
            _bladeVisual.localPosition = Vector3.back * (_data.BladeLength * 0.45f);
            _bladeVisual.localScale = new Vector3(_data.BladeWidth, _data.BladeWidth, _data.BladeLength);
            Destroy(_bladeVisual.GetComponent<Collider>());

            Material coreMaterial = CreateMaterial(_data.CoreColor, "Orbit Cutter Core");
            Material trailMaterial = CreateMaterial(_data.TrailColor, "Orbit Cutter Trail");
            _coreVisual.GetComponent<Renderer>().sharedMaterial = coreMaterial;
            _bladeVisual.GetComponent<Renderer>().sharedMaterial = trailMaterial;
        }

        private void UpdateVisual()
        {
            if (_visualRoot == null)
                return;

            Vector3 direction = GetOrbitDirection();
            _visualRoot.position = GetBladePosition();
            _visualRoot.rotation = Quaternion.LookRotation(direction);

            float burstScale = _burstTimer > 0f ? 1.35f : 1f;
            _coreVisual.localScale = Vector3.one * (_data.VisualScale * burstScale);
        }

        private Vector3 GetBladePosition()
        {
            return transform.position + Vector3.up * _data.Height + GetOrbitDirection() * _data.Radius;
        }

        private Vector3 GetOrbitDirection()
        {
            return Quaternion.Euler(0f, _angle, 0f) * Vector3.forward;
        }

        private Material CreateMaterial(Color color, string materialName)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            shader ??= Shader.Find("Sprites/Default");

            Material material = new(shader)
            {
                name = materialName
            };

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);

            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);

            return material;
        }
    }
}
