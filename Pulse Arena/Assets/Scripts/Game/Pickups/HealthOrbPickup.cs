using System;
using Data;
using DG.Tweening;
using Game.Player;
using UnityEngine;
using Zenject;

namespace Game.Pickups
{
    public class HealthOrbPickup : MonoBehaviour
    {
        private static readonly Color CoreColor = new(0.25f, 1f, 0.38f, 1f);
        private static readonly Color GlowColor = new(0.35f, 1f, 0.58f, 0.72f);

        public event Action<HealthOrbPickup> Collected;

        private PickupData _pickupData;
        private Transform _visualRoot;
        private Transform _core;
        private Transform _innerRing;
        private Transform _outerRing;
        private LineRenderer _beam;
        private Vector3 _startPosition;

        [Inject]
        public void Construct(GameSettings gameSettings)
        {
            _pickupData = gameSettings.PickupData;
        }

        public void Initialize()
        {
            _startPosition = transform.position;
            EnsureCollider();
            CreateVisual();
        }

        private void Update()
        {
            float bobOffset = Mathf.Sin(Time.time * _pickupData.BobSpeed) * _pickupData.BobHeight;
            transform.position = _startPosition + Vector3.up * bobOffset;

            if (_visualRoot != null)
                _visualRoot.Rotate(Vector3.up, _pickupData.RotateSpeed * Time.deltaTime, Space.World);

            if (_innerRing != null)
                _innerRing.Rotate(Vector3.forward, _pickupData.RotateSpeed * 1.3f * Time.deltaTime, Space.Self);

            if (_outerRing != null)
                _outerRing.Rotate(Vector3.right, -_pickupData.RotateSpeed * 0.9f * Time.deltaTime, Space.Self);

            UpdateBeam();
        }

        private bool _collected;

        private void OnTriggerEnter(Collider other)
        {
            if (_collected)
                return;

            PlayerController player = other.GetComponentInParent<PlayerController>();

            if (player == null)
                return;

            if (!player.TryHeal(_pickupData.HealthAmount))
                return;

            _collected = true;
            Collected?.Invoke(this);
            PlayCollect();
        }

        private void PlayCollect()
        {
            SphereCollider sphere = GetComponent<SphereCollider>();

            if (sphere != null)
                sphere.enabled = false;

            if (_visualRoot != null)
                _visualRoot.DOScale(_visualRoot.localScale * 1.8f, 0.22f).SetEase(Ease.OutQuad);

            Destroy(gameObject, 0.24f);
        }

        private void EnsureCollider()
        {
            SphereCollider sphere = GetComponent<SphereCollider>();

            if (sphere == null)
                sphere = gameObject.AddComponent<SphereCollider>();

            sphere.isTrigger = true;
            sphere.radius = 0.72f;
            sphere.center = Vector3.zero;
        }

        private void CreateVisual()
        {
            if (_visualRoot != null)
                return;

            _visualRoot = new GameObject("Health Orb Visual").transform;
            _visualRoot.SetParent(transform, false);

            Material coreMaterial = CreateMaterial(CoreColor, "Health Orb Core");
            Material glowMaterial = CreateMaterial(GlowColor, "Health Orb Glow");

            _core = CreatePrimitive("Core", PrimitiveType.Sphere, _visualRoot,
                Vector3.zero, Vector3.one * 0.52f, coreMaterial);
            CreatePrimitive("Inner Glow", PrimitiveType.Sphere, _visualRoot,
                Vector3.zero, Vector3.one * 0.74f, glowMaterial);

            _innerRing = CreateRing("Inner Ring", _visualRoot, 0.58f, 0.045f, glowMaterial);
            _outerRing = CreateRing("Outer Ring", _visualRoot, 0.72f, 0.035f, glowMaterial);
            _outerRing.localRotation = Quaternion.Euler(90f, 0f, 0f);

            CreateBeam(glowMaterial);
            CreateLight();
        }

        private Transform CreatePrimitive(
            string objectName,
            PrimitiveType primitiveType,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject part = GameObject.CreatePrimitive(primitiveType);
            part.name = objectName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;

            Collider partCollider = part.GetComponent<Collider>();

            if (partCollider != null)
                Destroy(partCollider);

            Renderer renderer = part.GetComponent<Renderer>();

            if (renderer != null)
                renderer.sharedMaterial = material;

            return part.transform;
        }

        private static Transform CreateRing(
            string objectName,
            Transform parent,
            float radius,
            float width,
            Material material)
        {
            const int segments = 64;

            GameObject ringObject = new(objectName);
            ringObject.transform.SetParent(parent, false);

            LineRenderer ring = ringObject.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = segments;
            ring.widthMultiplier = width;
            ring.numCapVertices = 4;
            ring.material = material;

            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }

            return ringObject.transform;
        }

        private void CreateBeam(Material material)
        {
            GameObject beamObject = new("Health Orb Beam");
            beamObject.transform.SetParent(transform, false);

            _beam = beamObject.AddComponent<LineRenderer>();
            _beam.useWorldSpace = true;
            _beam.positionCount = 2;
            _beam.widthCurve = new AnimationCurve(
                new Keyframe(0f, 0.14f),
                new Keyframe(1f, 0.38f));
            _beam.colorGradient = CreateBeamGradient();
            _beam.material = material;
            _beam.numCapVertices = 4;
        }

        private void UpdateBeam()
        {
            if (_beam == null)
                return;

            Vector3 basePosition = _startPosition + Vector3.down * 0.45f;
            _beam.SetPosition(0, basePosition);
            _beam.SetPosition(1, basePosition + Vector3.up * 5.5f);
        }

        private void CreateLight()
        {
            Light light = gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = CoreColor;
            light.range = 4.5f;
            light.intensity = 2.1f;
        }

        private static Material CreateMaterial(Color color, string materialName)
        {
            GameObject template = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            template.SetActive(false);
            template.hideFlags = HideFlags.HideAndDontSave;

            Renderer renderer = template.GetComponent<Renderer>();
            Material material = new(renderer.sharedMaterial)
            {
                name = materialName,
                color = color
            };

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);

            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);

            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", color * 2.2f);

            Destroy(template);

            return material;
        }

        private static Gradient CreateBeamGradient()
        {
            Gradient gradient = new();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(CoreColor, 0f),
                    new GradientColorKey(CoreColor, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.7f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });

            return gradient;
        }
    }
}
