using Data;
using Game.Combat;
using Game.Common;
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
        // Baked animation "feel" constants — the personality of the character, not per-instance tuning
        // (the designer-facing knobs live in PlayerVisualData). Named so the numbers read intent, not magic.
        private const float ArmRestZAngle = 22f;         // arms' resting Z, mirrored per side
        private const float ThrowArmPitch = 95f;         // extra X-swing on the throwing arm mid-throw
        private const float ThrowArmZKick = 18f;         // extra Z-flare on the throwing arm mid-throw
        private const float HeadWobbleFrequency = 2.2f;
        private const float HeadWobbleAngle = 3f;
        private const float DeathRotationLerpSpeed = 8f;
        private const float UprightLerpSpeed = 10f;

        private static readonly Vector3 HitSquashScale = new(1.12f, 0.9f, 1.12f);

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
            ActorPhysicsUtility.AlignVisualBottomToCollider(transform, _bottomRenderers);
            _baseLocalPosition = transform.localPosition;
            _baseScale = transform.localScale;
        }

        private void Animate(float deltaTime)
        {
            float moveProgress = Mathf.InverseLerp(0f, _visualData.MoveAnimationMaxSpeed, GetPlanarSpeed());

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
            transform.localScale = HitSquashScale * Mathf.Sin(hitProgress * Mathf.PI) +
                                   _baseScale * (1f - Mathf.Sin(hitProgress * Mathf.PI));
        }

        private void ApplyBodyRotation(float deltaTime)
        {
            if (_isDead)
                transform.localRotation = Quaternion.Lerp(transform.localRotation,
                    Quaternion.Euler(0f, 0f, _visualData.DeathRollAngle), deltaTime * DeathRotationLerpSpeed);
            else
                transform.localRotation =
                    Quaternion.Lerp(transform.localRotation, Quaternion.identity, deltaTime * UprightLerpSpeed);
        }

        private void AnimateArms(float moveProgress)
        {
            float armSwing = Mathf.Sin(Time.time * _visualData.ArmSwingFrequency) * _visualData.ArmSwingAngle *
                             moveProgress;
            _leftArm.localRotation = Quaternion.Euler(armSwing, 0f, -ArmRestZAngle);

            float throwProgress = _throwTimer > 0f
                ? Mathf.Sin((_throwTimer / Mathf.Max(0.01f, _visualData.ThrowSwingDuration)) * Mathf.PI)
                : 0f;
            _rightArm.localRotation = Quaternion.Euler(-armSwing - throwProgress * ThrowArmPitch, 0f,
                ArmRestZAngle + throwProgress * ThrowArmZKick);
        }

        private void AnimateHeadAndHat()
        {
            _head.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * HeadWobbleFrequency) * HeadWobbleAngle, 0f, 0f);
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
