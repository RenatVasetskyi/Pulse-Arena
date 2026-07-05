using Data;
using UnityEngine;

namespace Game.Visuals
{
    public class EnemyPrimitiveVisual : MonoBehaviour
    {
        private const float MoveThreshold = 0.2f;
        private const float GroundClearance = 0.02f;
        private static readonly Vector3 RootVisualOffset = new(0f, -1.15f, 0f);

        private Rigidbody _rigidbody;
        private Transform _body;
        private Transform _head;
        private Transform _belly;
        private Transform _leftEye;
        private Transform _rightEye;
        private Transform _faceRoot;
        private Transform _spikeRoot;
        private Renderer[] _bottomRenderers;
        private Material _bodyMaterial;
        private Material _bellyMaterial;
        private float _typeScale = 1f;
        private Vector3 _lastPosition;
        private Vector3 _baseLocalPosition;
        private Vector3 _baseScale;
        private Vector3 _headBaseScale;
        private float _hitTimer;
        private float _bounceTimer;
        private float _deathTimer;
        private float _moveSpeed;
        private bool _isGrabbed;
        private bool _isThrown;
        private bool _isDead;

        public static EnemyPrimitiveVisual Create(Transform parent, Rigidbody rigidbody)
        {
            GameObject root = new("EnemyPrimitiveVisual");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = RootVisualOffset;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            EnemyPrimitiveVisual visual = root.AddComponent<EnemyPrimitiveVisual>();
            visual.Initialize(rigidbody);
            visual.Build();
            return visual;
        }

        public void Initialize(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
            _lastPosition = GetMovementReferencePosition();
        }

        public void ResetState()
        {
            _hitTimer = 0f;
            _bounceTimer = 0f;
            _deathTimer = 0f;
            _moveSpeed = 0f;
            _isGrabbed = false;
            _isThrown = false;
            _isDead = false;
            _lastPosition = GetMovementReferencePosition();

            transform.localPosition = _baseLocalPosition;
            transform.localRotation = Quaternion.identity;
            transform.localScale = _baseScale * _typeScale;

            if (_body != null)
                _body.localRotation = Quaternion.identity;

            if (_head != null)
            {
                _head.localRotation = Quaternion.identity;

                if (_headBaseScale.sqrMagnitude > 0f)
                    _head.localScale = _headBaseScale;
            }

            if (_faceRoot != null)
            {
                _faceRoot.localRotation = Quaternion.identity;
                _faceRoot.localScale = Vector3.one;
            }

            if (_spikeRoot != null)
            {
                _spikeRoot.localRotation = Quaternion.identity;
                _spikeRoot.localScale = Vector3.one;
            }

            AlignBottomToCollider();
        }

        public void PlayHit()
        {
            if (_isDead)
                return;

            _hitTimer = 0.16f;
        }

        public void PlayDeath()
        {
            _isDead = true;
            _isGrabbed = false;
            _isThrown = false;
            _deathTimer = 0f;
        }

        public void PlayGroundBounce()
        {
            if (_isDead)
                return;

            _bounceTimer = 0.22f;
        }

        public void SetGrabbed(bool isGrabbed)
        {
            _isGrabbed = isGrabbed;
        }

        public void SetThrown(bool isThrown)
        {
            _isThrown = isThrown;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            _hitTimer = Mathf.Max(0f, _hitTimer - deltaTime);
            _bounceTimer = Mathf.Max(0f, _bounceTimer - deltaTime);
            _deathTimer += _isDead ? deltaTime : 0f;
            TickMoveSpeed(deltaTime);
            Animate(deltaTime);
        }

        private void Build()
        {
            Material bodyMaterial = PrimitiveVisualUtility.CreateMaterial("Enemy Body", new Color(0.42f, 0.2f, 0.72f, 1f));
            Material bellyMaterial = PrimitiveVisualUtility.CreateMaterial("Enemy Belly", new Color(0.58f, 0.42f, 0.9f, 1f));
            _bodyMaterial = bodyMaterial;
            _bellyMaterial = bellyMaterial;
            Material eyeMaterial = PrimitiveVisualUtility.CreateMaterial("Enemy Eye", new Color(1f, 0.92f, 0.72f, 1f));
            Material pupilMaterial = PrimitiveVisualUtility.CreateMaterial("Enemy Pupil", new Color(0.04f, 0.02f, 0.08f, 1f));
            Material spikeMaterial = PrimitiveVisualUtility.CreateMaterial("Enemy Spike", new Color(0.12f, 0.08f, 0.22f, 1f));

            _body = PrimitiveVisualUtility.CreatePart("Body", PrimitiveType.Sphere, transform,
                new Vector3(0f, 0.68f, 0f), Vector3.zero, new Vector3(0.9f, 1.05f, 0.78f), bodyMaterial);
            _head = PrimitiveVisualUtility.CreatePart("Head", PrimitiveType.Sphere, transform,
                new Vector3(0f, 1.34f, 0.08f), Vector3.zero, new Vector3(0.62f, 0.5f, 0.58f), bodyMaterial);
            _belly = PrimitiveVisualUtility.CreatePart("Belly", PrimitiveType.Sphere, transform,
                new Vector3(0f, 0.66f, 0.33f), Vector3.zero, new Vector3(0.5f, 0.6f, 0.28f), bellyMaterial);

            _faceRoot = new GameObject("FaceRoot").transform;
            _faceRoot.SetParent(transform, false);
            _faceRoot.localPosition = new Vector3(0f, 1.34f, 0.08f);

            _leftEye = PrimitiveVisualUtility.CreatePart("LeftEye", PrimitiveType.Sphere, _faceRoot,
                new Vector3(-0.175f, 0.1f, 0.27f), Vector3.zero, new Vector3(0.21f, 0.21f, 0.12f), eyeMaterial);
            _rightEye = PrimitiveVisualUtility.CreatePart("RightEye", PrimitiveType.Sphere, _faceRoot,
                new Vector3(0.175f, 0.1f, 0.27f), Vector3.zero, new Vector3(0.21f, 0.21f, 0.12f), eyeMaterial);
            PrimitiveVisualUtility.CreatePart("LeftPupil", PrimitiveType.Sphere, _leftEye,
                new Vector3(0f, 0f, 0.6f), Vector3.zero, new Vector3(0.42f, 0.42f, 0.22f), pupilMaterial);
            PrimitiveVisualUtility.CreatePart("RightPupil", PrimitiveType.Sphere, _rightEye,
                new Vector3(0f, 0f, 0.6f), Vector3.zero, new Vector3(0.42f, 0.42f, 0.22f), pupilMaterial);

            _spikeRoot = new GameObject("SpikeRoot").transform;
            _spikeRoot.SetParent(transform, false);
            PrimitiveVisualUtility.CreatePart("Spike_Back_1", PrimitiveType.Cube, _spikeRoot,
                new Vector3(0f, 1.52f, -0.34f), new Vector3(42f, 0f, 0f), new Vector3(0.24f, 0.42f, 0.24f), spikeMaterial);
            PrimitiveVisualUtility.CreatePart("Spike_Back_2", PrimitiveType.Cube, _spikeRoot,
                new Vector3(-0.28f, 1.16f, -0.34f), new Vector3(48f, -18f, 0f), new Vector3(0.18f, 0.34f, 0.18f), spikeMaterial);
            PrimitiveVisualUtility.CreatePart("Spike_Back_3", PrimitiveType.Cube, _spikeRoot,
                new Vector3(0.28f, 1.16f, -0.34f), new Vector3(48f, 18f, 0f), new Vector3(0.18f, 0.34f, 0.18f), spikeMaterial);

            _bottomRenderers = GetComponentsInChildren<Renderer>();
            AlignBottomToCollider();
            _baseLocalPosition = transform.localPosition;
            _baseScale = transform.localScale;
            _headBaseScale = _head.localScale;
        }

        public void ApplyTypeStyle(EnemyTypeData type)
        {
            if (type == null)
                return;

            _typeScale = Mathf.Max(0.1f, type.VisualScale);
            transform.localScale = _baseScale * _typeScale;

            if (_spikeRoot != null)
                _spikeRoot.gameObject.SetActive(type.ShowSpikes);

            if (type.OverrideBodyColor)
            {
                ApplyMaterialColor(_bodyMaterial, type.BodyColor);
                ApplyMaterialColor(_bellyMaterial, Color.Lerp(type.BodyColor, Color.white, 0.35f));
            }
        }

        private static void ApplyMaterialColor(Material material, Color color)
        {
            if (material == null)
                return;

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);

            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
        }

        public bool TryGetRopeBounds(out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;

            IncludeRendererBounds(_body, ref bounds, ref hasBounds);
            IncludeRendererBounds(_head, ref bounds, ref hasBounds);
            IncludeRendererBounds(_belly, ref bounds, ref hasBounds);

            return hasBounds;
        }

        private static void IncludeRendererBounds(Transform part, ref Bounds bounds, ref bool hasBounds)
        {
            if (part == null)
                return;

            Renderer partRenderer = part.GetComponent<Renderer>();

            if (partRenderer == null || !partRenderer.enabled)
                return;

            if (!hasBounds)
            {
                bounds = partRenderer.bounds;
                hasBounds = true;
                return;
            }

            bounds.Encapsulate(partRenderer.bounds);
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

        private void TickMoveSpeed(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            Vector3 currentPosition = GetMovementReferencePosition();
            Vector3 delta = currentPosition - _lastPosition;
            delta.y = 0f;
            _moveSpeed = Mathf.Lerp(_moveSpeed, delta.magnitude / deltaTime, deltaTime * 12f);
            _lastPosition = currentPosition;
        }

        private void Animate(float deltaTime)
        {
            if (_isDead)
            {
                AnimateDeath(deltaTime);
                return;
            }

            float moveProgress = Mathf.InverseLerp(0f, 3.8f, _moveSpeed);
            float time = Time.time;
            float wobble = Mathf.Sin(time * Mathf.Lerp(2.6f, 8.5f, moveProgress));
            float squash = 1f + wobble * Mathf.Lerp(0.025f, 0.08f, moveProgress);

            transform.localScale = new Vector3(1f + (1f - squash) * 0.35f, squash, 1f + (1f - squash) * 0.35f) * _typeScale;
            transform.localRotation = Quaternion.Lerp(transform.localRotation,
                Quaternion.Euler(0f, 0f, -wobble * 8f * moveProgress), deltaTime * 12f);

            if (_hitTimer > 0f)
            {
                float hit = Mathf.Sin((_hitTimer / 0.16f) * Mathf.PI);
                transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(1.18f, 0.82f, 1.18f) * _typeScale, hit);
            }

            if (_bounceTimer > 0f)
            {
                float bounce = Mathf.Sin((_bounceTimer / 0.22f) * Mathf.PI);
                transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(1.22f, 0.74f, 1.22f) * _typeScale, bounce);
            }

            transform.localPosition = Vector3.Lerp(transform.localPosition, _baseLocalPosition, deltaTime * 14f);

            if (_isThrown && !_isGrabbed)
                _body.Rotate(Vector3.right, 540f * deltaTime, Space.Self);

            _head.localRotation = Quaternion.Euler(Mathf.Sin(time * 3f) * 4f, 0f, 0f);
            _faceRoot.localRotation = _head.localRotation;
            _spikeRoot.localRotation = Quaternion.Euler(Mathf.Sin(time * 2.4f) * 3f, 0f, 0f);

            if (!_isGrabbed && !_isThrown)
                AlignBottomToCollider();
        }

        private void AnimateDeath(float deltaTime)
        {
            float progress = Mathf.Clamp01(_deathTimer / 0.38f);
            float pop = Mathf.Sin(progress * Mathf.PI);
            Vector3 targetScale = Vector3.Lerp(_baseScale * (1.16f * _typeScale),
                new Vector3(1.25f, 0.08f, 1.25f) * _typeScale, progress);

            transform.localPosition = Vector3.Lerp(transform.localPosition, _baseLocalPosition + Vector3.down * 0.28f,
                deltaTime * 12f);
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale + Vector3.one * (pop * 0.08f),
                deltaTime * 18f);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(0f, 0f, 82f),
                deltaTime * 12f);
            _head.localScale = Vector3.Lerp(_head.localScale, _headBaseScale * 0.65f, deltaTime * 12f);
            _faceRoot.localScale = Vector3.Lerp(_faceRoot.localScale, Vector3.one * 0.65f, deltaTime * 12f);
            _spikeRoot.localScale = Vector3.Lerp(_spikeRoot.localScale, Vector3.one * 0.2f, deltaTime * 14f);
        }

        private Vector3 GetMovementReferencePosition()
        {
            return transform.parent != null ? transform.parent.position : transform.position;
        }
    }
}
