using Data;
using DG.Tweening;
using Game.Common;
using UnityEngine;

namespace Game.Visuals
{
    /// <summary>
    ///     Animates the enemy's baked primitive visual. The hierarchy (Body/Head/Belly/eyes/spikes)
    ///     lives in the enemy prefab; this component caches those child transforms, instances the
    ///     body/belly materials for per-enemy type tinting, and drives the wobble/hit/bounce/death.
    ///     Each animation overlay + reset step is a small single-purpose method the per-frame Animate
    ///     coordinator composes in order.
    /// </summary>
    public class EnemyPrimitiveVisual : MonoBehaviour
    {
        // Baked animation "feel" constants — the personality of the blob, not per-instance tuning
        // (the designer-facing knobs live in EnemyVisualData). Named so the numbers read intent, not magic.
        private const float MoveSpeedSmoothing = 12f; // how fast the sampled move-speed follows reality
        private const float SquashLateralRatio = 0.35f; // x/z counter-stretch vs the vertical squash
        private const float WobbleTiltAngle = 8f; // max idle/run Z-tilt
        private const float WobbleRotationLerp = 12f; // how fast the tilt eases in
        private const float SettleLerpSpeed = 14f; // root eases back to its base position
        private const float HeadWobbleFrequency = 3f;
        private const float HeadWobbleAngle = 4f;
        private const float SpikeWobbleFrequency = 2.4f;
        private const float SpikeWobbleAngle = 3f;
        private const float DeathScaleBulge = 1.16f; // brief bulge before the flatten
        private const float DeathSinkDepth = 0.28f; // how far the corpse sinks into the ground
        private const float DeathPopAmount = 0.08f; // squash-and-stretch pop over the death arc
        private const float DeathRollAngle = 82f; // topple-over angle
        private const float DeathBodyLerp = 12f; // position / rotation / part-shrink easing
        private const float DeathScaleLerp = 18f; // body flatten easing
        private const float DeathPartShrink = 0.65f; // head + face shrink target
        private const float DeathSpikeShrink = 0.2f;
        private const float DeathSpikeLerp = 14f;

        private static readonly Vector3 HitSquashScale = new(1.18f, 0.82f, 1.18f);
        private static readonly Vector3 BounceSquashScale = new(1.22f, 0.74f, 1.22f);
        private static readonly Vector3 DeathFlattenScale = new(1.25f, 0.08f, 1.25f);

        private Vector3 _baseLocalPosition;
        private Vector3 _baseScale;
        private Transform _belly;
        private Material _bellyMaterial;
        private Transform _body;
        private Material _bodyMaterial;
        private Renderer[] _bottomRenderers;
        private float _bounceTimer;
        private float _deathTimer;
        private Transform _faceRoot;
        private Transform _head;
        private Vector3 _headBaseScale;
        private float _hitTimer;
        private bool _isDead;
        private bool _isGrabbed;
        private bool _isThrown;
        private Vector3 _lastPosition;
        private Transform _leftEye;
        private float _moveSpeed;
        private bool _paused;
        private Transform _rightEye;
        private Rigidbody _rigidbody;
        private Material _skinSharedMaterial;
        private float _spawnScale = 1f;
        private Tween _spawnTween;
        private Transform _spikeRoot;
        private float _typeScale = 1f;

        private EnemyVisualData _visualData = new();

        public void Initialize(Rigidbody rigidbody, EnemyVisualData visualData = null)
        {
            _rigidbody = rigidbody;

            if (visualData != null)
                _visualData = visualData;

            _lastPosition = GetMovementReferencePosition();
            EnsureParts();
        }

        private void Update()
        {
            if (_paused)
                return;

            float deltaTime = Time.deltaTime;
            _hitTimer = Mathf.Max(0f, _hitTimer - deltaTime);
            _bounceTimer = Mathf.Max(0f, _bounceTimer - deltaTime);
            _deathTimer += _isDead ? deltaTime : 0f;
            TickMoveSpeed(deltaTime);
            Animate(deltaTime);
        }

        /// <summary>Mechanical pause (driven by the controller): freeze procedural animation + the spawn tween.</summary>
        public void SetPaused(bool value)
        {
            _paused = value;

            if (value)
                _spawnTween?.Pause();
            else
                _spawnTween?.Play();
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

            _bounceTimer = _visualData.BounceSquashDuration;
        }

        public void PlayHit()
        {
            if (_isDead)
                return;

            _hitTimer = _visualData.HitSquashDuration;
        }

        public void ResetState()
        {
            ResetRuntimeState();
            ResetRootTransform();
            ResetPartTransforms();
            ActorPhysicsUtility.AlignVisualBottomToCollider(transform, _bottomRenderers);
            PlaySpawnPop();
        }

        public void SetGrabbed(bool isGrabbed)
        {
            _isGrabbed = isGrabbed;
        }

        public void SetThrown(bool isThrown)
        {
            _isThrown = isThrown;
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

        private void ResetRuntimeState()
        {
            _hitTimer = 0f;
            _bounceTimer = 0f;
            _deathTimer = 0f;
            _moveSpeed = 0f;
            _isGrabbed = false;
            _isThrown = false;
            _isDead = false;
            _lastPosition = GetMovementReferencePosition();
        }

        private void ResetRootTransform()
        {
            transform.localPosition = _baseLocalPosition;
            transform.localRotation = Quaternion.identity;
            transform.localScale = _baseScale * _typeScale;
        }

        private void ResetPartTransforms()
        {
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
        }

        private void PlaySpawnPop()
        {
            _spawnTween?.Kill();
            _spawnScale = 0f;
            _spawnTween = DOTween.To(() => _spawnScale, value => _spawnScale = value, 1f, 0.35f)
                .SetEase(Ease.OutBack).SetLink(gameObject);
        }

        private void EnsureParts()
        {
            if (_body != null)
                return;

            CacheParts();
            FinishSetup();
        }

        private void CacheParts()
        {
            CacheChildTransforms();
            InstanceTintMaterials();
            LinkSkinMaterialToBody();
        }

        private void CacheChildTransforms()
        {
            _body = transform.Find("Body");
            _head = transform.Find("Head");
            _belly = transform.Find("Belly");
            _faceRoot = transform.Find("FaceRoot");
            _leftEye = _faceRoot != null ? _faceRoot.Find("LeftEye") : null;
            _rightEye = _faceRoot != null ? _faceRoot.Find("RightEye") : null;
            _spikeRoot = transform.Find("SpikeRoot");
        }

        private void InstanceTintMaterials()
        {
            _skinSharedMaterial = GetSharedMaterial(_body);
            _bodyMaterial = GetInstancedMaterial(_body);
            _bellyMaterial = GetInstancedMaterial(_belly);
        }

        // The head plus every skin part (ears, nose, limbs, feet…) share the body's one skin material asset in
        // the prefab; point them all at the single body material instance so an enemy-type tint recolours the
        // whole creature together, not just the torso.
        private void LinkSkinMaterialToBody()
        {
            if (_bodyMaterial == null || _skinSharedMaterial == null)
                return;

            foreach (Renderer partRenderer in GetComponentsInChildren<Renderer>(true))
            {
                if (partRenderer.sharedMaterial == _skinSharedMaterial)
                    partRenderer.sharedMaterial = _bodyMaterial;
            }
        }

        private static Material GetSharedMaterial(Transform part)
        {
            if (part == null)
                return null;

            Renderer partRenderer = part.GetComponent<Renderer>();
            return partRenderer != null ? partRenderer.sharedMaterial : null;
        }

        private static Material GetInstancedMaterial(Transform part)
        {
            if (part == null)
                return null;

            Renderer partRenderer = part.GetComponent<Renderer>();
            return partRenderer != null ? partRenderer.material : null;
        }

        private void FinishSetup()
        {
            _bottomRenderers = GetComponentsInChildren<Renderer>();
            ActorPhysicsUtility.AlignVisualBottomToCollider(transform, _bottomRenderers);
            _baseLocalPosition = transform.localPosition;
            _baseScale = transform.localScale;
            _headBaseScale = _head != null ? _head.localScale : Vector3.one;
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

        private void TickMoveSpeed(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            Vector3 currentPosition = GetMovementReferencePosition();
            Vector3 delta = currentPosition - _lastPosition;
            delta.y = 0f;
            _moveSpeed = Mathf.Lerp(_moveSpeed, delta.magnitude / deltaTime, deltaTime * MoveSpeedSmoothing);
            _lastPosition = currentPosition;
        }

        private void Animate(float deltaTime)
        {
            if (_isDead)
            {
                AnimateDeath(deltaTime);
                return;
            }

            ApplyIdleWobble(deltaTime);
            ApplyHitSquash();
            ApplyBounceSquash();
            ApplySpawnAndSettle(deltaTime);
            AnimateParts(deltaTime);
        }

        private void ApplyIdleWobble(float deltaTime)
        {
            float moveProgress = Mathf.InverseLerp(0f, _visualData.MoveAnimationMaxSpeed, _moveSpeed);
            float wobble = Mathf.Sin(Time.time * Mathf.Lerp(_visualData.WobbleFrequencyIdle,
                _visualData.WobbleFrequencyRun, moveProgress));
            float squash = 1f + wobble * Mathf.Lerp(_visualData.SquashAmountIdle,
                _visualData.SquashAmountRun, moveProgress);

            transform.localScale =
                new Vector3(1f + (1f - squash) * SquashLateralRatio, squash, 1f + (1f - squash) * SquashLateralRatio) *
                _typeScale;
            transform.localRotation = Quaternion.Lerp(transform.localRotation,
                Quaternion.Euler(0f, 0f, -wobble * WobbleTiltAngle * moveProgress), deltaTime * WobbleRotationLerp);
        }

        private void ApplyHitSquash()
        {
            if (_hitTimer <= 0f)
                return;

            float hit = Mathf.Sin((_hitTimer / Mathf.Max(0.01f, _visualData.HitSquashDuration)) * Mathf.PI);
            transform.localScale =
                Vector3.Lerp(transform.localScale, HitSquashScale * _typeScale, hit);
        }

        private void ApplyBounceSquash()
        {
            if (_bounceTimer <= 0f)
                return;

            float bounce = Mathf.Sin((_bounceTimer / Mathf.Max(0.01f, _visualData.BounceSquashDuration)) * Mathf.PI);
            transform.localScale =
                Vector3.Lerp(transform.localScale, BounceSquashScale * _typeScale, bounce);
        }

        private void ApplySpawnAndSettle(float deltaTime)
        {
            transform.localScale *= _spawnScale;
            transform.localPosition =
                Vector3.Lerp(transform.localPosition, _baseLocalPosition, deltaTime * SettleLerpSpeed);
        }

        private void AnimateParts(float deltaTime)
        {
            float time = Time.time;

            if (_isThrown && !_isGrabbed)
                _body.Rotate(Vector3.right, _visualData.ThrownSpinSpeed * deltaTime, Space.Self);

            _head.localRotation = Quaternion.Euler(Mathf.Sin(time * HeadWobbleFrequency) * HeadWobbleAngle, 0f, 0f);
            _faceRoot.localRotation = _head.localRotation;
            _spikeRoot.localRotation =
                Quaternion.Euler(Mathf.Sin(time * SpikeWobbleFrequency) * SpikeWobbleAngle, 0f, 0f);

            if (!_isGrabbed && !_isThrown)
                ActorPhysicsUtility.AlignVisualBottomToCollider(transform, _bottomRenderers);
        }

        private void AnimateDeath(float deltaTime)
        {
            float progress = Mathf.Clamp01(_deathTimer / Mathf.Max(0.01f, _visualData.DeathPopDuration));
            float pop = Mathf.Sin(progress * Mathf.PI);
            Vector3 targetScale = Vector3.Lerp(_baseScale * (DeathScaleBulge * _typeScale),
                DeathFlattenScale * _typeScale, progress);

            transform.localPosition = Vector3.Lerp(transform.localPosition,
                _baseLocalPosition + Vector3.down * DeathSinkDepth, deltaTime * DeathBodyLerp);
            transform.localScale = Vector3.Lerp(transform.localScale,
                targetScale + Vector3.one * (pop * DeathPopAmount),
                deltaTime * DeathScaleLerp);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(0f, 0f, DeathRollAngle),
                deltaTime * DeathBodyLerp);
            _head.localScale =
                Vector3.Lerp(_head.localScale, _headBaseScale * DeathPartShrink, deltaTime * DeathBodyLerp);
            _faceRoot.localScale = Vector3.Lerp(_faceRoot.localScale, Vector3.one * DeathPartShrink,
                deltaTime * DeathBodyLerp);
            _spikeRoot.localScale = Vector3.Lerp(_spikeRoot.localScale, Vector3.one * DeathSpikeShrink,
                deltaTime * DeathSpikeLerp);
        }

        private Vector3 GetMovementReferencePosition()
        {
            return transform.parent != null ? transform.parent.position : transform.position;
        }
    }
}