using Data;
using Game.Combat;
using UnityEngine;

namespace Game.Visuals
{
    public class PlayerPrimitiveVisual : MonoBehaviour
    {
        private const float GroundClearance = 0.02f;

        private PlayerVisualData _visualData = new();
        private Rigidbody _rigidbody;
        private EnemySlingshot _slingshot;
        private Transform _body;
        private Transform _head;
        private Transform _leftArm;
        private Transform _rightArm;
        private Transform _hatRoot;
        private Transform _lassoOrigin;
        private Renderer[] _bottomRenderers;
        private Vector3 _baseLocalPosition;
        private Vector3 _baseScale;
        private float _throwTimer;
        private float _hitTimer;
        private bool _isDead;

        public Transform LassoOrigin => _lassoOrigin;

        public static PlayerPrimitiveVisual Create(Transform parent, Rigidbody rigidbody, EnemySlingshot slingshot,
            PlayerVisualData visualData)
        {
            GameObject root = new("PlayerPrimitiveVisual");
            root.transform.SetParent(parent, false);
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            PlayerPrimitiveVisual visual = root.AddComponent<PlayerPrimitiveVisual>();
            visual.Initialize(rigidbody, slingshot, visualData);
            root.transform.localPosition = visual._visualData.RootOffset;
            visual.Build();
            return visual;
        }

        public void Initialize(Rigidbody rigidbody, EnemySlingshot slingshot, PlayerVisualData visualData = null)
        {
            _rigidbody = rigidbody;

            if (visualData != null)
                _visualData = visualData;

            if (_slingshot != null)
                _slingshot.LassoThrown -= PlayThrow;

            _slingshot = slingshot;

            if (_slingshot != null)
                _slingshot.LassoThrown += PlayThrow;
        }

        public void PlayHit()
        {
            _hitTimer = _visualData.HitSquashDuration;
        }

        public void PlayDeath()
        {
            _isDead = true;
        }

        private void OnDestroy()
        {
            if (_slingshot != null)
                _slingshot.LassoThrown -= PlayThrow;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            _throwTimer = Mathf.Max(0f, _throwTimer - deltaTime);
            _hitTimer = Mathf.Max(0f, _hitTimer - deltaTime);

            Animate(deltaTime);
        }

        private void PlayThrow()
        {
            _throwTimer = _visualData.ThrowSwingDuration;
        }

        private void Build()
        {
            Material bodyMaterial = PrimitiveVisualUtility.CreateMaterial("Player Body", _visualData.BodyColor);
            Material headMaterial = PrimitiveVisualUtility.CreateMaterial("Player Head", _visualData.HeadColor);
            Material darkMaterial = PrimitiveVisualUtility.CreateMaterial("Player Dark", _visualData.DarkColor);
            Material accentMaterial = PrimitiveVisualUtility.CreateMaterial("Player Accent", _visualData.AccentColor);

            _body = PrimitiveVisualUtility.CreatePart("Body", PrimitiveType.Capsule, transform,
                new Vector3(0f, 0.62f, 0f), Vector3.zero, new Vector3(0.72f, 0.84f, 0.72f), bodyMaterial);
            _head = PrimitiveVisualUtility.CreatePart("Head", PrimitiveType.Sphere, transform,
                new Vector3(0f, 1.44f, 0f), Vector3.zero, new Vector3(0.58f, 0.58f, 0.58f), headMaterial);

            _leftArm = PrimitiveVisualUtility.CreatePart("LeftArm", PrimitiveType.Capsule, transform,
                new Vector3(-0.48f, 0.86f, 0f), new Vector3(0f, 0f, -22f),
                new Vector3(0.22f, 0.48f, 0.22f), bodyMaterial);
            _rightArm = PrimitiveVisualUtility.CreatePart("RightArm", PrimitiveType.Capsule, transform,
                new Vector3(0.48f, 0.88f, 0f), new Vector3(0f, 0f, 22f),
                new Vector3(0.22f, 0.52f, 0.22f), bodyMaterial);

            _hatRoot = new GameObject("HatRoot").transform;
            _hatRoot.SetParent(transform, false);
            _hatRoot.localPosition = new Vector3(0f, 1.72f, 0f);
            PrimitiveVisualUtility.CreatePart("HatBrim", PrimitiveType.Cylinder, _hatRoot,
                Vector3.zero, Vector3.zero, new Vector3(0.78f, 0.08f, 0.78f), darkMaterial);
            PrimitiveVisualUtility.CreatePart("HatTop", PrimitiveType.Cylinder, _hatRoot,
                new Vector3(0f, 0.12f, 0f), Vector3.zero, new Vector3(0.43f, 0.28f, 0.43f), darkMaterial);
            PrimitiveVisualUtility.CreatePart("HatBand", PrimitiveType.Cylinder, _hatRoot,
                new Vector3(0f, 0.09f, 0f), Vector3.zero, new Vector3(0.46f, 0.05f, 0.46f), accentMaterial);

            _lassoOrigin = new GameObject("LassoOrigin").transform;
            _lassoOrigin.SetParent(_rightArm, false);
            _lassoOrigin.localPosition = new Vector3(0f, -0.62f, 0.08f);

            _bottomRenderers = GetComponentsInChildren<Renderer>();
            AlignBottomToCollider();
            _baseLocalPosition = transform.localPosition;
            _baseScale = transform.localScale;
        }

        private void AlignBottomToCollider()
        {
            if (transform.parent == null)
                return;

            CapsuleCollider capsule = transform.parent.GetComponent<CapsuleCollider>();

            if (capsule == null)
                return;

            if (!TryGetRendererBottom(out float rendererBottom))
                return;

            float actorBottom = GetCapsuleBottom(capsule);
            float worldDelta = actorBottom + GroundClearance - rendererBottom;
            float parentScaleY = Mathf.Max(0.001f, Mathf.Abs(transform.parent.lossyScale.y));

            transform.localPosition += Vector3.up * (worldDelta / parentScaleY);
        }

        private bool TryGetRendererBottom(out float bottom)
        {
            Renderer[] renderers = _bottomRenderers ?? GetComponentsInChildren<Renderer>();
            bottom = float.MaxValue;
            bool hasRenderer = false;

            foreach (Renderer visualRenderer in renderers)
            {
                if (visualRenderer == null)
                    continue;

                bottom = Mathf.Min(bottom, visualRenderer.bounds.min.y);
                hasRenderer = true;
            }

            return hasRenderer;
        }

        private static float GetCapsuleBottom(CapsuleCollider capsule)
        {
            Transform capsuleTransform = capsule.transform;
            float scaledHeight = capsule.height * Mathf.Abs(capsuleTransform.lossyScale.y);
            float scaledCenterY = capsule.center.y * capsuleTransform.lossyScale.y;

            return capsuleTransform.position.y + scaledCenterY - scaledHeight * 0.5f;
        }

        private void Animate(float deltaTime)
        {
            float speed = GetPlanarSpeed();
            float moveProgress = Mathf.InverseLerp(0f, 4.5f, speed);
            float time = Time.time;
            float bob = Mathf.Sin(time * Mathf.Lerp(_visualData.BobFrequencyIdle, _visualData.BobFrequencyRun,
                moveProgress)) * Mathf.Lerp(_visualData.BobAmplitudeIdle, _visualData.BobAmplitudeRun, moveProgress);

            transform.localPosition = _baseLocalPosition + Vector3.up * bob;
            transform.localScale = _baseScale;

            if (_hitTimer > 0f)
            {
                float hitProgress = _hitTimer / Mathf.Max(0.01f, _visualData.HitSquashDuration);
                transform.localScale = new Vector3(1.12f, 0.9f, 1.12f) * Mathf.Sin(hitProgress * Mathf.PI) +
                                       _baseScale * (1f - Mathf.Sin(hitProgress * Mathf.PI));
            }

            if (_isDead)
                transform.localRotation = Quaternion.Lerp(transform.localRotation,
                    Quaternion.Euler(0f, 0f, _visualData.DeathRollAngle), deltaTime * 8f);
            else
                transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.identity, deltaTime * 10f);

            float armSwing = Mathf.Sin(time * _visualData.ArmSwingFrequency) * _visualData.ArmSwingAngle * moveProgress;
            _leftArm.localRotation = Quaternion.Euler(armSwing, 0f, -22f);

            float throwProgress = _throwTimer > 0f
                ? Mathf.Sin((_throwTimer / Mathf.Max(0.01f, _visualData.ThrowSwingDuration)) * Mathf.PI)
                : 0f;
            _rightArm.localRotation = Quaternion.Euler(-armSwing - throwProgress * 95f, 0f, 22f + throwProgress * 18f);
            _head.localRotation = Quaternion.Euler(Mathf.Sin(time * 2.2f) * 3f, 0f, 0f);
            _hatRoot.localRotation = _head.localRotation;

            if (_isDead)
                return;
        }

        private float GetPlanarSpeed()
        {
            if (_rigidbody == null)
                return 0f;

            Vector3 velocity = _rigidbody.linearVelocity;
            velocity.y = 0f;
            return velocity.magnitude <= _visualData.MoveThreshold ? 0f : velocity.magnitude;
        }
    }
}
