using System;
using Architecture.Services.Interfaces;
using Data;
using Game.Combat.Interfaces;
using Game.Enemy;
using UnityEngine;
using Zenject;

namespace Game.Combat
{
    /// <summary>
    /// The lasso orchestrator: runs the grab state machine (Idle → Throwing → Wrapping → Pulling → Spinning),
    /// the spin/pull/launch physics in FixedUpdate, and assembles the rope frame each frame. The distinct
    /// concerns are delegated to helpers — <see cref="IRopeTension"/> (tension math), <see cref="IEnemyTargetFinder"/>
    /// (grab target search), <see cref="ISnapBurstEffect"/> (break VFX) and <see cref="IHookTargetMarkerPresenter"/>
    /// (target highlight) — leaving only the core combat-feel logic here.
    /// </summary>
    public class EnemySlingshot : MonoBehaviour
    {
        private enum LassoState
        {
            Idle,
            Throwing,
            Wrapping,
            Pulling,
            Spinning
        }

        public event Action LassoThrown;
        public event Action EnemyGrabbed;
        public event Action<float> ChargeChanged;
        public event Action<float> EnemyLaunched;
        public event Action<float> TensionChanged;
        public event Action RopeBroke;

        [SerializeField] private Transform _lassoOrigin;

        private IInputService _inputService;
        private SlingshotData _data;
        private EnemyController _targetEnemy;
        private EnemyController _grabbedEnemy;
        private readonly RopeRenderer _rope = new();
        private readonly IRopeTension _tension = new RopeTension();
        private readonly IEnemyTargetFinder _finder = new EnemyTargetFinder();
        private readonly ISnapBurstEffect _snapBurst = new SnapBurstEffect();
        private readonly IHookTargetMarkerPresenter _marker = new HookTargetMarkerPresenter();
        private LassoState _state;
        private Vector3 _lassoStart;
        private Vector3 _lassoEnd;
        private Vector3 _pullStartPosition;
        private float _throwTimer;
        private float _wrapTimer;
        private float _pullTimer;
        private float _chargeTimer;
        private float _holdAngle;
        private float _spinSpeed;
        private float _cooldownTimer;
        private bool _releaseRequested;

        [Inject]
        public void Construct(IInputService inputService, GameSettings gameSettings)
        {
            _inputService = inputService;
            _data = gameSettings.SlingshotData;
            _rope.Initialize(transform, _data);
            _finder.Initialize(transform, _data);
            _tension.Initialize(_data);
            _tension.Changed += OnTensionChanged;
            _snapBurst.Initialize(transform, gameSettings.Vfx, _data, () => _rope.Material);
            _marker.Initialize(transform, _data, gameSettings.Prefabs.HookTargetMarkerPrefab, _finder);
        }

        private void OnTensionChanged(float value)
        {
            TensionChanged?.Invoke(value);
        }

        private void Update()
        {
            TickCooldown();

            if (_state == LassoState.Idle && _inputService.IsSlingshotPressedThisFrame)
                TryThrowLasso();

            if (_state == LassoState.Throwing || _state == LassoState.Wrapping ||
                _state == LassoState.Pulling)
            {
                if (_inputService.IsSlingshotReleasedThisFrame)
                    _releaseRequested = true;
            }

            if (_state == LassoState.Throwing)
                TickThrow();

            if (_state == LassoState.Wrapping)
                TickWrap();

            if (_state == LassoState.Spinning &&
                (_releaseRequested || _inputService.IsSlingshotReleasedThisFrame ||
                 _inputService.IsOrbitBurstPressedThisFrame))
            {
                LaunchGrabbedEnemy();
            }

            _rope.Render(BuildRopeFrame());
            _marker.Tick(_state == LassoState.Idle, _cooldownTimer > 0f);
        }

        private RopeRenderer.RopeFrame BuildRopeFrame()
        {
            bool ringVisible = (_state == LassoState.Wrapping || _state == LassoState.Pulling ||
                                _state == LassoState.Spinning) && _grabbedEnemy != null;

            return new RopeRenderer.RopeFrame(
                ropeVisible: _state != LassoState.Idle,
                throwing: _state == LassoState.Throwing,
                ringVisible: ringVisible,
                wrapping: _state == LassoState.Wrapping,
                lassoStart: _lassoStart,
                lassoEnd: _lassoEnd,
                throwTimer: _throwTimer,
                wrappedOrigin: GetLassoOrigin(),
                enemyRopeCenter: _grabbedEnemy != null ? GetEnemyRopeCenter(_grabbedEnemy) : Vector3.zero,
                grabbedEnemy: _grabbedEnemy,
                wrapTimer: _wrapTimer,
                chargeProgress: GetChargeProgress(),
                tensionWarning: _tension.Warning);
        }

        private void FixedUpdate()
        {
            if (_grabbedEnemy == null)
                return;

            if (_state == LassoState.Pulling)
            {
                TickPullToHold();
                return;
            }

            if (_state == LassoState.Spinning)
                TickSpin();
        }

        private void TickSpin()
        {
            AdvanceCharge();
            UpdateSpinSpeed();

            if (TickTensionAndCheckBreak())
                return;

            FollowHoldPosition();
        }

        private void AdvanceCharge()
        {
            _chargeTimer = Mathf.Min(_chargeTimer + Time.fixedDeltaTime, _data.ChargeDuration);
            ChargeChanged?.Invoke(GetChargeProgress());
        }

        private void UpdateSpinSpeed()
        {
            float spinProgress = Mathf.SmoothStep(0f, 1f, GetChargeProgress());
            float weightFactor = GetGrabbedWeightFactor();
            float targetSpinSpeed = Mathf.Lerp(_data.HoldAngularSpeed, _data.MaxHoldAngularSpeed, spinProgress) *
                weightFactor;
            _spinSpeed = Mathf.MoveTowards(_spinSpeed, targetSpinSpeed,
                _data.SpinAcceleration * weightFactor * Time.fixedDeltaTime);
            _holdAngle += _spinSpeed * Time.fixedDeltaTime;
        }

        // Returns true if the rope broke this step (BreakRope already ran) so the spin tick stops.
        private bool TickTensionAndCheckBreak()
        {
            _tension.Tick(_grabbedEnemy.TypeData.Weight, _grabbedEnemy.TypeData.TensionRateMultiplier,
                GetChargeProgress(), Time.fixedDeltaTime);

            if (_tension.IsBroken)
            {
                BreakRope();
                return true;
            }

            return false;
        }

        private void FollowHoldPosition()
        {
            _grabbedEnemy.MoveGrabbed(GetHoldPosition(), _data.HoldFollowSpeed);
        }

        private void TryThrowLasso()
        {
            if (_cooldownTimer > 0f)
                return;

            EnemyController enemy = _finder.FindNearest(_data.GrabRadius);

            if (enemy == null)
                return;

            _targetEnemy = enemy;
            _lassoStart = GetLassoOrigin();
            _lassoEnd = GetEnemyRopeCenter(enemy);
            _throwTimer = 0f;
            _wrapTimer = 0f;
            _pullTimer = 0f;
            _chargeTimer = 0f;
            _releaseRequested = false;
            _state = LassoState.Throwing;
            LassoThrown?.Invoke();
        }

        public void SetLassoOrigin(Transform lassoOrigin)
        {
            _lassoOrigin = lassoOrigin;
        }

        private void TickThrow()
        {
            if (_targetEnemy == null)
            {
                ResetLasso();
                return;
            }

            _throwTimer += Time.deltaTime;
            _lassoEnd = GetEnemyRopeCenter(_targetEnemy);

            if (_throwTimer < _data.ThrowDuration)
                return;

            StartWrappingEnemy(_targetEnemy);
        }

        private void StartWrappingEnemy(EnemyController enemy)
        {
            _grabbedEnemy = enemy;
            _targetEnemy = null;
            _grabbedEnemy.Grab();
            _holdAngle = Vector3.SignedAngle(Vector3.forward,
                GetPlanarDirectionTo(enemy.transform.position), Vector3.up);
            _spinSpeed = _data.HoldAngularSpeed * GetGrabbedWeightFactor();
            _wrapTimer = 0f;
            _state = LassoState.Wrapping;
        }

        private void TickWrap()
        {
            if (_grabbedEnemy == null)
            {
                ResetLasso();
                return;
            }

            _wrapTimer += Time.deltaTime;
            _grabbedEnemy.MoveGrabbed(_grabbedEnemy.transform.position, _data.HoldFollowSpeed);

            if (_wrapTimer < _data.WrapDuration)
                return;

            CompleteWrap();
        }

        private void CompleteWrap()
        {
            _pullStartPosition = _grabbedEnemy.transform.position;
            _pullTimer = 0f;
            _state = LassoState.Pulling;
        }

        private void TickPullToHold()
        {
            _pullTimer += Time.fixedDeltaTime;

            float progress = Mathf.Clamp01(_pullTimer / Mathf.Max(_data.PullToHoldDuration, 0.01f));
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            Vector3 targetPosition = Vector3.Lerp(_pullStartPosition, GetHoldPosition(), easedProgress);
            targetPosition += Vector3.up * (Mathf.Sin(easedProgress * Mathf.PI) * _data.PullToHoldArcHeight);

            _grabbedEnemy.MoveGrabbed(targetPosition, _data.HoldFollowSpeed);

            if (progress >= 1f)
                StartSpin();
        }

        private void StartSpin()
        {
            _chargeTimer = 0f;
            _state = LassoState.Spinning;
            _releaseRequested = _releaseRequested || !_inputService.IsSlingshotHeld;
            EnemyGrabbed?.Invoke();
        }

        private void LaunchGrabbedEnemy()
        {
            Vector3 launchDirection = GetLaunchDirection();
            float launchProgress = Mathf.SmoothStep(0f, 1f, GetChargeProgress());
            float chargeProgress = GetChargeProgress();
            float launchForce = _data.LaunchForce *
                Mathf.Lerp(1f, _data.MaxChargeLaunchMultiplier, launchProgress) *
                GetGrabbedLaunchMultiplier();
            Vector3 velocity = launchDirection.normalized * launchForce;
            velocity.y = -Mathf.Lerp(_data.LaunchDownwardVelocity,
                _data.LaunchDownwardVelocity * 1.25f, launchProgress);

            _grabbedEnemy.Launch(velocity, _data.LaunchDuration);
            ResetLasso();
            _cooldownTimer = _data.Cooldown;
            EnemyLaunched?.Invoke(chargeProgress);
        }

        private Vector3 GetLaunchDirection()
        {
            Vector3 orbitDirection = GetHoldPosition() - transform.position;
            orbitDirection.y = 0f;

            if (orbitDirection.sqrMagnitude <= 0.001f)
                return transform.forward;

            Vector3 tangent = Vector3.Cross(Vector3.up, orbitDirection.normalized);
            return tangent.normalized;
        }

        private Vector3 GetHoldPosition()
        {
            Vector3 direction = Quaternion.Euler(0f, _holdAngle, 0f) * Vector3.forward;
            return transform.position + Vector3.up * _data.HoldHeight + direction * _data.HoldRadius;
        }

        private Vector3 GetLassoOrigin()
        {
            if (_lassoOrigin != null)
                return _lassoOrigin.position;

            return transform.position + Vector3.up * _data.HoldHeight;
        }

        private Vector3 GetEnemyRopeCenter(EnemyController enemy)
        {
            if (enemy == null)
                return Vector3.zero;

            if (!enemy.TryGetRopeBounds(out Bounds bounds))
                return enemy.transform.position;

            return bounds.center;
        }

        private Vector3 GetPlanarDirectionTo(Vector3 position)
        {
            Vector3 direction = position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.001f)
                return transform.forward;

            return direction.normalized;
        }

        private void ResetLasso()
        {
            _targetEnemy = null;
            _grabbedEnemy = null;
            _state = LassoState.Idle;
            _chargeTimer = 0f;
            ChargeChanged?.Invoke(0f);
            _tension.Reset();
            _releaseRequested = false;
            _rope.Hide();
        }

        private float GetChargeProgress()
        {
            return Mathf.Clamp01(_chargeTimer / Mathf.Max(_data.ChargeDuration, 0.01f));
        }

        private void BreakRope()
        {
            EnemyController enemy = _grabbedEnemy;
            Vector3 breakPoint = enemy != null
                ? Vector3.Lerp(GetLassoOrigin(), GetEnemyRopeCenter(enemy), 0.5f)
                : GetLassoOrigin();

            if (enemy != null)
            {
                Vector3 dropDirection = GetPlanarDirectionTo(enemy.transform.position);
                enemy.Knockback(dropDirection * _data.BreakDropForce +
                    Vector3.up * (_data.BreakDropForce * 0.25f));
            }

            ResetLasso();
            _cooldownTimer = _data.Cooldown * _data.BreakCooldownMultiplier;
            _snapBurst.Play(breakPoint);
            RopeBroke?.Invoke();
        }

        private float GetGrabbedWeightFactor()
        {
            if (_grabbedEnemy == null)
                return 1f;

            float weight = Mathf.Max(0.1f, _grabbedEnemy.TypeData.Weight);
            return Mathf.Clamp(1f / weight, _data.WeightFactorMin, _data.WeightFactorMax);
        }

        private float GetGrabbedLaunchMultiplier()
        {
            return _grabbedEnemy != null ? _grabbedEnemy.TypeData.LaunchVelocityMultiplier : 1f;
        }

        private void TickCooldown()
        {
            if (_cooldownTimer > 0f)
                _cooldownTimer -= Time.deltaTime;
        }
    }
}
