using System;
using Architecture.Services.Interfaces;
using Data;
using Game.Enemy;
using UnityEngine;
using Zenject;

namespace Game.Combat
{
    public class EnemySlingshot : MonoBehaviour
    {
        private static readonly Collider[] EnemySearchBuffer = new Collider[32];
        private static Material _sharedLineMaterial;

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
        private VfxData _vfx;
        private EnemyController _targetEnemy;
        private EnemyController _grabbedEnemy;
        private LineRenderer _line;
        private LineRenderer _wrapRing;
        private EnemyWrapMetrics _wrapMetrics;
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
        private float _tension;
        private ParticleSystem _snapBurst;
        private HookTargetMarker _targetMarker;
        private bool _releaseRequested;

        [Inject]
        public void Construct(IInputService inputService, GameSettings gameSettings)
        {
            _inputService = inputService;
            _data = gameSettings.SlingshotData;
            _vfx = gameSettings.Vfx;
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

            UpdateLine();
            UpdateWrapRing();
            UpdateTargetMarker();
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

            if (_state != LassoState.Spinning)
                return;

            _chargeTimer = Mathf.Min(_chargeTimer + Time.fixedDeltaTime, _data.ChargeDuration);
            ChargeChanged?.Invoke(GetChargeProgress());
            
            float spinProgress = Mathf.SmoothStep(0f, 1f, GetChargeProgress());
            float weightFactor = GetGrabbedWeightFactor();
            float targetSpinSpeed = Mathf.Lerp(_data.HoldAngularSpeed, _data.MaxHoldAngularSpeed, spinProgress) *
                weightFactor;
            _spinSpeed = Mathf.MoveTowards(_spinSpeed, targetSpinSpeed,
                _data.SpinAcceleration * weightFactor * Time.fixedDeltaTime);
            _holdAngle += _spinSpeed * Time.fixedDeltaTime;

            TickTension();

            if (_tension >= 1f)
            {
                BreakRope();
                return;
            }

            _grabbedEnemy.MoveGrabbed(GetHoldPosition(), _data.HoldFollowSpeed);
        }

        private void TryThrowLasso()
        {
            if (_cooldownTimer > 0f)
                return;

            EnemyController enemy = FindNearestEnemy();

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
            EnsureLine();
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
            _wrapMetrics = GetEnemyWrapMetrics(_grabbedEnemy);
            _holdAngle = Vector3.SignedAngle(Vector3.forward,
                GetPlanarDirectionTo(enemy.transform.position), Vector3.up);
            _spinSpeed = _data.HoldAngularSpeed * GetGrabbedWeightFactor();
            _wrapTimer = 0f;
            _state = LassoState.Wrapping;
            EnsureWrapRing();
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

        private EnemyController FindNearestEnemy()
        {
            return FindNearestEnemy(_data.GrabRadius);
        }

        private EnemyController FindNearestEnemy(float radius)
        {
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, radius, EnemySearchBuffer,
                _data.EnemyLayer);
            EnemyController nearestEnemy = null;
            float nearestSqrDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Rigidbody hitBody = EnemySearchBuffer[i].attachedRigidbody;

                if (hitBody == null || !hitBody.TryGetComponent(out EnemyController enemy))
                    continue;

                if (enemy.IsGrabbed || enemy.Health <= 0)
                    continue;

                float sqrDistance = (enemy.transform.position - transform.position).sqrMagnitude;

                if (sqrDistance >= nearestSqrDistance)
                    continue;

                nearestSqrDistance = sqrDistance;
                nearestEnemy = enemy;
            }

            return nearestEnemy;
        }

        private void UpdateTargetMarker()
        {
            if (_state != LassoState.Idle)
            {
                _targetMarker?.Hide();
                return;
            }

            float searchRadius = _data.GrabRadius * Mathf.Max(1f, _data.MarkerSearchRangeMultiplier);
            EnemyController enemy = FindNearestEnemy(searchRadius);

            if (enemy == null)
            {
                _targetMarker?.Hide();
                return;
            }

            if (_targetMarker == null)
                _targetMarker = HookTargetMarker.Create(transform);

            float sqrDistance = (enemy.transform.position - transform.position).sqrMagnitude;
            bool isGrabbable = _cooldownTimer <= 0f &&
                sqrDistance <= _data.GrabRadius * _data.GrabRadius;
            Color color = isGrabbable ? _data.MarkerActiveColor : _data.MarkerInactiveColor;
            float radius = _data.MarkerRadius * Mathf.Max(0.5f, enemy.TypeData.VisualScale);
            Vector3 center = enemy.transform.position + Vector3.up * _data.MarkerHeight;

            _targetMarker.Show(center, radius, _data.MarkerWidth, color,
                _data.MarkerPulseSpeed, _data.MarkerPulseAmplitude);
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

        private EnemyWrapMetrics GetEnemyWrapMetrics(EnemyController enemy)
        {
            if (enemy == null || !enemy.TryGetRopeBounds(out Bounds bounds))
            {
                return new EnemyWrapMetrics(
                    enemy != null ? enemy.transform.position : Vector3.zero,
                    Mathf.Max(_data.MinWrapRadius, _data.WrapRadius),
                    0.55f,
                    transform.right,
                    transform.forward);
            }

            float horizontalRadius = Mathf.Max(bounds.extents.x, bounds.extents.z);
            float targetRadius = horizontalRadius * _data.WrapRadiusScale + _data.WrapRadiusPadding;
            float maxRadius = Mathf.Max(_data.MinWrapRadius, _data.WrapRadius);
            float radius = Mathf.Clamp(targetRadius, _data.MinWrapRadius, maxRadius);
            float verticalRange = Mathf.Clamp(bounds.size.y * _data.WrapVerticalScale, 0.35f, 0.68f);
            GetWrapAxes(bounds.center, out Vector3 sideAxis, out Vector3 depthAxis);

            return new EnemyWrapMetrics(bounds.center, radius, verticalRange, sideAxis, depthAxis);
        }

        private void GetWrapAxes(Vector3 center, out Vector3 sideAxis, out Vector3 depthAxis)
        {
            depthAxis = transform.position - center;
            depthAxis.y = 0f;

            if (depthAxis.sqrMagnitude <= 0.001f)
                depthAxis = transform.forward;
            else
                depthAxis.Normalize();

            sideAxis = Vector3.Cross(Vector3.up, depthAxis);

            if (sideAxis.sqrMagnitude <= 0.001f)
                sideAxis = transform.right;
            else
                sideAxis.Normalize();
        }

        private Vector3 GetPlanarDirectionTo(Vector3 position)
        {
            Vector3 direction = position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.001f)
                return transform.forward;

            return direction.normalized;
        }

        private void EnsureLine()
        {
            if (_line != null)
                return;

            GameObject lineObject = new("Lasso Rope");
            lineObject.transform.SetParent(transform, false);
            _line = lineObject.AddComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.positionCount = 6;
            _line.widthMultiplier = _data.LineWidth;
            _line.numCapVertices = 5;
            _line.numCornerVertices = 5;
            _line.textureMode = LineTextureMode.Tile;
            _line.material = GetLineMaterial();
        }

        private void EnsureWrapRing()
        {
            if (_wrapRing != null)
                return;

            GameObject ringObject = new("Lasso Wrap Ring");
            ringObject.transform.SetParent(transform, false);
            _wrapRing = ringObject.AddComponent<LineRenderer>();
            _wrapRing.useWorldSpace = true;
            _wrapRing.loop = true;
            _wrapRing.positionCount = 24;
            _wrapRing.widthMultiplier = _data.LineWidth * 0.7f;
            _wrapRing.numCapVertices = 4;
            _wrapRing.numCornerVertices = 4;
            _wrapRing.textureMode = LineTextureMode.Tile;
            _wrapRing.material = GetLineMaterial();
            _wrapRing.enabled = false;
        }

        private void UpdateLine()
        {
            if (_line == null)
                return;

            if (_state == LassoState.Idle)
            {
                HideLine();
                return;
            }

            _line.enabled = true;
            ApplyLineVisuals(_line, 1f);

            if (_state == LassoState.Throwing)
                DrawThrowingRope();
            else
                DrawWrappedRope();
        }

        private void DrawThrowingRope()
        {
            float t = Mathf.Clamp01(_throwTimer / Mathf.Max(_data.ThrowDuration, 0.01f));
            float travelT = Mathf.SmoothStep(0f, 1f, t);
            Vector3 currentEnd = Vector3.Lerp(_lassoStart, _lassoEnd, travelT);

            DrawRope(_lassoStart, currentEnd, _data.ThrowWaveAmplitude);
        }

        private void DrawWrappedRope()
        {
            if (_grabbedEnemy == null)
                return;

            float shake = 1f + GetTensionWarning() * _data.TensionShakeAmplitude;
            DrawRope(GetLassoOrigin(), GetEnemyRopeCenter(_grabbedEnemy), _data.WrapWaveAmplitude * shake);
        }

        private void DrawRope(Vector3 start, Vector3 end, float waveAmplitude)
        {
            Vector3 direction = end - start;
            Vector3 side = Vector3.Cross(Vector3.up, direction);

            if (side.sqrMagnitude <= 0.001f)
                side = transform.right;

            side.Normalize();

            for (int i = 0; i < _line.positionCount; i++)
            {
                float t = i / (float)(_line.positionCount - 1);
                float wave = Mathf.Sin(t * Mathf.PI * _data.RopeWaveCount + Time.time * _data.RopeWaveSpeed) *
                    waveAmplitude;
                _line.SetPosition(i, Vector3.Lerp(start, end, t) + side * wave);
            }
        }

        private void HideLine()
        {
            if (_line != null)
                _line.enabled = false;
        }

        private void UpdateWrapRing()
        {
            if (_wrapRing == null)
                return;

            if ((_state != LassoState.Wrapping && _state != LassoState.Pulling &&
                 _state != LassoState.Spinning) || _grabbedEnemy == null)
            {
                _wrapRing.enabled = false;
                return;
            }

            _wrapRing.enabled = true;
            ApplyLineVisuals(_wrapRing, 0.7f);

            if (_state == LassoState.Wrapping)
                DrawWrappingRing();
            else
                DrawSpinningRing();
        }

        private void DrawWrappingRing()
        {
            float progress = Mathf.Clamp01(_wrapTimer / Mathf.Max(_data.WrapDuration, 0.01f));
            float visibleProgress = Mathf.SmoothStep(0f, 1f, progress);
            int maxPoints = 48;
            int pointCount = Mathf.Max(2, Mathf.RoundToInt(maxPoints * visibleProgress));

            _wrapRing.loop = false;
            _wrapRing.positionCount = pointCount;

            EnemyWrapMetrics metrics = GetCurrentWrapMetrics();
            Vector3 center = metrics.Center;
            float totalAngle = Mathf.PI * 2f * _data.WrapTurns * visibleProgress;
            float verticalRange = metrics.VerticalRange;

            for (int i = 0; i < _wrapRing.positionCount; i++)
            {
                float t = i / (float)(_wrapRing.positionCount - 1);
                float angle = t * totalAngle;
                float height = Mathf.Lerp(-verticalRange * 0.5f, verticalRange * 0.5f, t);
                Vector3 point = center +
                    metrics.SideAxis * (Mathf.Cos(angle) * metrics.Radius) +
                    metrics.DepthAxis * (Mathf.Sin(angle) * metrics.Radius) +
                    Vector3.up * height;
                _wrapRing.SetPosition(i, point);
            }
        }

        private void DrawSpinningRing()
        {
            _wrapRing.loop = true;
            _wrapRing.positionCount = 48;

            EnemyWrapMetrics metrics = GetCurrentWrapMetrics();
            Vector3 center = metrics.Center;
            float twist = Time.time * _data.WrapSpinSpeed;
            float verticalWave = Mathf.Min(metrics.VerticalRange * 0.18f, 0.08f);

            for (int i = 0; i < _wrapRing.positionCount; i++)
            {
                float angle = (i / (float)_wrapRing.positionCount) * Mathf.PI * 2f + twist;
                Vector3 point = center +
                    metrics.SideAxis * (Mathf.Cos(angle) * metrics.Radius) +
                    metrics.DepthAxis * (Mathf.Sin(angle) * metrics.Radius) +
                    Vector3.up * (Mathf.Sin(angle * 2f) * verticalWave);
                _wrapRing.SetPosition(i, point);
            }
        }

        private EnemyWrapMetrics GetCurrentWrapMetrics()
        {
            if (_grabbedEnemy == null)
                return _wrapMetrics;

            EnemyWrapMetrics currentMetrics = GetEnemyWrapMetrics(_grabbedEnemy);
            _wrapMetrics = currentMetrics;
            return currentMetrics;
        }

        private void ResetLasso()
        {
            _targetEnemy = null;
            _grabbedEnemy = null;
            _state = LassoState.Idle;
            _chargeTimer = 0f;
            ChargeChanged?.Invoke(0f);
            SetTension(0f);
            _releaseRequested = false;
            HideLine();

            if (_wrapRing != null)
                _wrapRing.enabled = false;
        }

        private readonly struct EnemyWrapMetrics
        {
            public readonly Vector3 Center;
            public readonly float Radius;
            public readonly float VerticalRange;
            public readonly Vector3 SideAxis;
            public readonly Vector3 DepthAxis;

            public EnemyWrapMetrics(
                Vector3 center,
                float radius,
                float verticalRange,
                Vector3 sideAxis,
                Vector3 depthAxis)
            {
                Center = center;
                Radius = radius;
                VerticalRange = verticalRange;
                SideAxis = sideAxis;
                DepthAxis = depthAxis;
            }
        }

        private Material GetLineMaterial()
        {
            if (_sharedLineMaterial != null)
                return _sharedLineMaterial;

            Shader shader = Shader.Find("Sprites/Default");
            _sharedLineMaterial = new Material(shader)
            {
                name = "Lasso Rope"
            };
            _sharedLineMaterial.mainTexture = CreateRopeTexture();
            _sharedLineMaterial.mainTextureScale = new Vector2(_data.RopeTextureRepeat, 1f);

            return _sharedLineMaterial;
        }

        private Texture2D CreateRopeTexture()
        {
            const int width = 64;
            const int height = 8;
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
            {
                name = "Procedural Lasso Texture",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point
            };

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    bool stripe = ((x + y * 2) / 6) % 2 == 0;
                    Color color = stripe ? _data.RopeBaseColor : _data.RopeStripeColor;
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return texture;
        }

        private void ApplyLineVisuals(LineRenderer renderer, float widthMultiplier)
        {
            float chargeProgress = Mathf.SmoothStep(0f, 1f, GetChargeProgress());
            float tensionWarning = GetTensionWarning();
            Color color = Color.Lerp(_data.LineColor, _data.ChargedLineColor, chargeProgress);
            color = Color.Lerp(color, _data.TensionColor, tensionWarning);
            float chargedWidth = Mathf.Lerp(1f, _data.MaxChargeLineWidthMultiplier, chargeProgress);
            float tensionPulse = 1f + Mathf.Sin(Time.time * _data.TensionPulseSpeed) *
                _data.TensionPulseAmplitude * tensionWarning;

            renderer.startColor = color;
            renderer.endColor = color;
            renderer.widthMultiplier = _data.LineWidth * widthMultiplier * chargedWidth * tensionPulse;
        }

        private float GetChargeProgress()
        {
            return Mathf.Clamp01(_chargeTimer / Mathf.Max(_data.ChargeDuration, 0.01f));
        }

        private void TickTension()
        {
            float weight = _grabbedEnemy != null ? Mathf.Max(0.1f, _grabbedEnemy.TypeData.Weight) : 1f;
            float typeRate = _grabbedEnemy != null
                ? Mathf.Max(0.1f, _grabbedEnemy.TypeData.TensionRateMultiplier)
                : 1f;
            float chargeBoost = 1f + GetChargeProgress() * _data.TensionChargeInfluence;
            float rate = weight * typeRate * chargeBoost / Mathf.Max(0.5f, _data.TensionBreakTime);

            SetTension(Mathf.Min(1f, _tension + rate * Time.fixedDeltaTime));
        }

        private void SetTension(float value)
        {
            if (Mathf.Approximately(_tension, value))
                return;

            _tension = value;
            TensionChanged?.Invoke(_tension);
        }

        private float GetTensionWarning()
        {
            return Mathf.InverseLerp(Mathf.Clamp01(_data.TensionWarningThreshold), 1f, _tension);
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
            PlaySnapBurst(breakPoint);
            RopeBroke?.Invoke();
        }

        private void PlaySnapBurst(Vector3 position)
        {
            EnsureSnapBurst();
            _snapBurst.transform.position = position;
            _snapBurst.Emit(_vfx.SnapBurstCount);
        }

        private void EnsureSnapBurst()
        {
            if (_snapBurst != null)
                return;

            GameObject burstObject = new("Rope Snap Burst");
            burstObject.transform.SetParent(transform, false);
            _snapBurst = burstObject.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = _snapBurst.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(_vfx.SnapBurstLifetimeMin, _vfx.SnapBurstLifetimeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(_vfx.SnapBurstSpeedMin, _vfx.SnapBurstSpeedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(_vfx.SnapBurstSizeMin, _vfx.SnapBurstSizeMax);
            main.startColor = new ParticleSystem.MinMaxGradient(_data.RopeBaseColor, _data.TensionColor);
            main.gravityModifier = _vfx.SnapBurstGravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = _snapBurst.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = _snapBurst.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            ParticleSystemRenderer particleRenderer = burstObject.GetComponent<ParticleSystemRenderer>();
            particleRenderer.material = GetLineMaterial();
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
