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
        private enum LassoState
        {
            Idle,
            Throwing,
            Wrapping,
            Pulling,
            Spinning
        }

        public event Action EnemyGrabbed;
        public event Action<float> ChargeChanged;
        public event Action<float> EnemyLaunched;

        private IInputService _inputService;
        private SlingshotData _data;
        private EnemyController _targetEnemy;
        private EnemyController _grabbedEnemy;
        private LineRenderer _line;
        private LineRenderer _wrapRing;
        private Material _lineMaterial;
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
            float targetSpinSpeed = Mathf.Lerp(_data.HoldAngularSpeed, _data.MaxHoldAngularSpeed, spinProgress);
            _spinSpeed = Mathf.MoveTowards(_spinSpeed, targetSpinSpeed, _data.SpinAcceleration * Time.fixedDeltaTime);
            _holdAngle += _spinSpeed * Time.fixedDeltaTime;

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
            _spinSpeed = _data.HoldAngularSpeed;
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
            Collider[] hits = Physics.OverlapSphere(transform.position, _data.GrabRadius, _data.EnemyLayer);
            EnemyController nearestEnemy = null;
            float nearestSqrDistance = float.MaxValue;

            foreach (Collider hit in hits)
            {
                EnemyController enemy = hit.GetComponentInParent<EnemyController>();

                if (enemy == null || enemy.IsGrabbed)
                    continue;

                float sqrDistance = (enemy.transform.position - transform.position).sqrMagnitude;

                if (sqrDistance >= nearestSqrDistance)
                    continue;

                nearestSqrDistance = sqrDistance;
                nearestEnemy = enemy;
            }

            return nearestEnemy;
        }

        private void LaunchGrabbedEnemy()
        {
            Vector3 launchDirection = GetLaunchDirection();
            float launchProgress = Mathf.SmoothStep(0f, 1f, GetChargeProgress());
            float chargeProgress = GetChargeProgress();
            float launchForce = _data.LaunchForce *
                Mathf.Lerp(1f, _data.MaxChargeLaunchMultiplier, launchProgress);
            Vector3 velocity = (launchDirection + Vector3.up * _data.LaunchUpwardRatio).normalized *
                launchForce;

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
            return transform.position + Vector3.up * _data.HoldHeight;
        }

        private Vector3 GetEnemyRopeCenter(EnemyController enemy)
        {
            if (enemy == null)
                return Vector3.zero;

            Collider[] colliders = enemy.GetComponentsInChildren<Collider>();
            bool hasBounds = false;
            Bounds bounds = new(enemy.transform.position, Vector3.zero);

            foreach (Collider enemyCollider in colliders)
            {
                if (enemyCollider == null || enemyCollider.isTrigger)
                    continue;

                if (!hasBounds)
                {
                    bounds = enemyCollider.bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(enemyCollider.bounds);
            }

            if (!hasBounds)
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

            DrawRope(GetLassoOrigin(), GetEnemyRopeCenter(_grabbedEnemy), _data.WrapWaveAmplitude);
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

            Vector3 center = GetEnemyRopeCenter(_grabbedEnemy);
            float totalAngle = Mathf.PI * 2f * _data.WrapTurns * visibleProgress;
            float verticalRange = 0.55f;

            for (int i = 0; i < _wrapRing.positionCount; i++)
            {
                float t = i / (float)(_wrapRing.positionCount - 1);
                float angle = t * totalAngle;
                float height = Mathf.Lerp(-verticalRange * 0.5f, verticalRange * 0.5f, t);
                Vector3 point = center + new Vector3(Mathf.Cos(angle) * _data.WrapRadius, height,
                    Mathf.Sin(angle) * _data.WrapRadius);
                _wrapRing.SetPosition(i, point);
            }
        }

        private void DrawSpinningRing()
        {
            _wrapRing.loop = true;
            _wrapRing.positionCount = 48;

            Vector3 center = GetEnemyRopeCenter(_grabbedEnemy);
            float twist = Time.time * _data.WrapSpinSpeed;

            for (int i = 0; i < _wrapRing.positionCount; i++)
            {
                float angle = (i / (float)_wrapRing.positionCount) * Mathf.PI * 2f + twist;
                Vector3 point = center + new Vector3(Mathf.Cos(angle) * _data.WrapRadius,
                    Mathf.Sin(angle * 2f) * 0.08f, Mathf.Sin(angle) * _data.WrapRadius);
                _wrapRing.SetPosition(i, point);
            }
        }

        private void ResetLasso()
        {
            _targetEnemy = null;
            _grabbedEnemy = null;
            _state = LassoState.Idle;
            _chargeTimer = 0f;
            ChargeChanged?.Invoke(0f);
            _releaseRequested = false;
            HideLine();

            if (_wrapRing != null)
                _wrapRing.enabled = false;
        }

        private Material GetLineMaterial()
        {
            if (_lineMaterial != null)
                return _lineMaterial;

            Shader shader = Shader.Find("Sprites/Default");
            _lineMaterial = new Material(shader)
            {
                name = "Lasso Rope"
            };
            _lineMaterial.mainTexture = CreateRopeTexture();
            _lineMaterial.mainTextureScale = new Vector2(_data.RopeTextureRepeat, 1f);

            return _lineMaterial;
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
            Color color = Color.Lerp(_data.LineColor, _data.ChargedLineColor, chargeProgress);
            float chargedWidth = Mathf.Lerp(1f, _data.MaxChargeLineWidthMultiplier, chargeProgress);

            renderer.startColor = color;
            renderer.endColor = color;
            renderer.widthMultiplier = _data.LineWidth * widthMultiplier * chargedWidth;
        }

        private float GetChargeProgress()
        {
            return Mathf.Clamp01(_chargeTimer / Mathf.Max(_data.ChargeDuration, 0.01f));
        }

        private void TickCooldown()
        {
            if (_cooldownTimer > 0f)
                _cooldownTimer -= Time.deltaTime;
        }
    }
}
