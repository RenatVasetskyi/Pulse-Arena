using Data;
using Game.Combat;
using UnityEngine;

namespace Game.Visuals
{
    /// <summary>
    ///     Animates the player's baked primitive visual. The hierarchy (Body/Head/arms/hat/LassoOrigin)
    ///     lives in the player prefab; this component caches those child transforms and drives the
    ///     idle bob, arm swing, throw, hit-squash and death roll. Each animation layer is its own
    ///     single-purpose method the per-frame Animate coordinator composes in order.
    /// </summary>
    public class PlayerPrimitiveVisual : MonoBehaviour
    {
        private const float GroundClearance = 0.02f;
        private Vector3 _baseLocalPosition;
        private Vector3 _baseScale;
        private Transform _body;
        private Renderer[] _bottomRenderers;
        private Transform _hatRoot;
        private Transform _head;
        private float _hitTimer;
        private bool _isDead;
        private Transform _lassoOrigin;
        private Transform _leftArm;
        private Transform _rightArm;
        private Rigidbody _rigidbody;
        private EnemySlingshot _slingshot;
        private float _throwTimer;

        private PlayerVisualData _visualData = new();

        public Transform LassoOrigin => _lassoOrigin;

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            _throwTimer = Mathf.Max(0f, _throwTimer - deltaTime);
            _hitTimer = Mathf.Max(0f, _hitTimer - deltaTime);

            Animate(deltaTime);
        }

        private void OnDestroy()
        {
            if (_slingshot != null)
                _slingshot.LassoThrown -= PlayThrow;
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

            EnsureParts();
        }

        public void PlayDeath()
        {
            _isDead = true;
        }

        public void PlayHit()
        {
            _hitTimer = _visualData.HitSquashDuration;
        }

        private void PlayThrow()
        {
            _throwTimer = _visualData.ThrowSwingDuration;
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
            _body = transform.Find("Body");
            _head = transform.Find("Head");
            _leftArm = transform.Find("LeftArm");
            _rightArm = transform.Find("RightArm");
            _hatRoot = transform.Find("HatRoot");
            _lassoOrigin = _rightArm != null ? _rightArm.Find("LassoOrigin") : null;
        }

        private void FinishSetup()
        {
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
            float moveProgress = Mathf.InverseLerp(0f, 4.5f, GetPlanarSpeed());

            ApplyIdleBob(moveProgress);
            ApplyHitSquash();
            ApplyBodyRotation(deltaTime);
            AnimateArms(moveProgress);
            AnimateHeadAndHat();
        }

        private void ApplyIdleBob(float moveProgress)
        {
            float bob = Mathf.Sin(Time.time * Mathf.Lerp(_visualData.BobFrequencyIdle, _visualData.BobFrequencyRun,
                moveProgress)) * Mathf.Lerp(_visualData.BobAmplitudeIdle, _visualData.BobAmplitudeRun, moveProgress);

            transform.localPosition = _baseLocalPosition + Vector3.up * bob;
            transform.localScale = _baseScale;
        }

        private void ApplyHitSquash()
        {
            if (_hitTimer <= 0f)
                return;

            float hitProgress = _hitTimer / Mathf.Max(0.01f, _visualData.HitSquashDuration);
            transform.localScale = new Vector3(1.12f, 0.9f, 1.12f) * Mathf.Sin(hitProgress * Mathf.PI) +
                                   _baseScale * (1f - Mathf.Sin(hitProgress * Mathf.PI));
        }

        private void ApplyBodyRotation(float deltaTime)
        {
            if (_isDead)
                transform.localRotation = Quaternion.Lerp(transform.localRotation,
                    Quaternion.Euler(0f, 0f, _visualData.DeathRollAngle), deltaTime * 8f);
            else
                transform.localRotation =
                    Quaternion.Lerp(transform.localRotation, Quaternion.identity, deltaTime * 10f);
        }

        private void AnimateArms(float moveProgress)
        {
            float armSwing = Mathf.Sin(Time.time * _visualData.ArmSwingFrequency) * _visualData.ArmSwingAngle *
                             moveProgress;
            _leftArm.localRotation = Quaternion.Euler(armSwing, 0f, -22f);

            float throwProgress = _throwTimer > 0f
                ? Mathf.Sin((_throwTimer / Mathf.Max(0.01f, _visualData.ThrowSwingDuration)) * Mathf.PI)
                : 0f;
            _rightArm.localRotation = Quaternion.Euler(-armSwing - throwProgress * 95f, 0f, 22f + throwProgress * 18f);
        }

        private void AnimateHeadAndHat()
        {
            _head.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 2.2f) * 3f, 0f, 0f);
            _hatRoot.localRotation = _head.localRotation;
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