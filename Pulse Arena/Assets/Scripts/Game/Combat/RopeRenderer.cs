using Data;
using Game.Enemy;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Draws the lasso: the rope line and the wrap ring, via procedural LineRenderers.
    /// The slingshot decides WHAT to draw (game state); this class knows HOW (geometry).
    /// It's fed a RopeFrame each Update and reaches back into nothing.
    /// </summary>
    public class RopeRenderer
    {
        private static Material _sharedMaterial;

        private Transform _owner;
        private SlingshotData _data;
        private LineRenderer _line;
        private LineRenderer _wrapRing;

        public readonly struct RopeFrame
        {
            public readonly bool RopeVisible;
            public readonly bool Throwing;
            public readonly bool RingVisible;
            public readonly bool Wrapping;
            public readonly Vector3 LassoStart;
            public readonly Vector3 LassoEnd;
            public readonly float ThrowTimer;
            public readonly Vector3 WrappedOrigin;
            public readonly Vector3 EnemyRopeCenter;
            public readonly EnemyController GrabbedEnemy;
            public readonly float WrapTimer;
            public readonly float ChargeProgress;
            public readonly float TensionWarning;

            public RopeFrame(bool ropeVisible, bool throwing, bool ringVisible, bool wrapping,
                Vector3 lassoStart, Vector3 lassoEnd, float throwTimer, Vector3 wrappedOrigin,
                Vector3 enemyRopeCenter, EnemyController grabbedEnemy, float wrapTimer,
                float chargeProgress, float tensionWarning)
            {
                RopeVisible = ropeVisible;
                Throwing = throwing;
                RingVisible = ringVisible;
                Wrapping = wrapping;
                LassoStart = lassoStart;
                LassoEnd = lassoEnd;
                ThrowTimer = throwTimer;
                WrappedOrigin = wrappedOrigin;
                EnemyRopeCenter = enemyRopeCenter;
                GrabbedEnemy = grabbedEnemy;
                WrapTimer = wrapTimer;
                ChargeProgress = chargeProgress;
                TensionWarning = tensionWarning;
            }
        }

        public Material Material => GetLineMaterial();

        public void Initialize(Transform owner, SlingshotData data)
        {
            _owner = owner;
            _data = data;
        }

        public void Render(RopeFrame frame)
        {
            EnsureLine();
            UpdateLine(frame);
            EnsureWrapRing();
            UpdateWrapRing(frame);
        }

        public void Hide()
        {
            if (_line != null)
                _line.enabled = false;

            if (_wrapRing != null)
                _wrapRing.enabled = false;
        }

        private void UpdateLine(RopeFrame frame)
        {
            if (!frame.RopeVisible)
            {
                _line.enabled = false;
                return;
            }

            _line.enabled = true;
            ApplyLineVisuals(_line, 1f, frame.ChargeProgress, frame.TensionWarning);

            if (frame.Throwing)
                DrawThrowingRope(frame);
            else
                DrawWrappedRope(frame);
        }

        private void DrawThrowingRope(RopeFrame frame)
        {
            float t = Mathf.Clamp01(frame.ThrowTimer / Mathf.Max(_data.ThrowDuration, 0.01f));
            float travelT = Mathf.SmoothStep(0f, 1f, t);
            Vector3 currentEnd = Vector3.Lerp(frame.LassoStart, frame.LassoEnd, travelT);

            DrawRope(frame.LassoStart, currentEnd, _data.ThrowWaveAmplitude);
        }

        private void DrawWrappedRope(RopeFrame frame)
        {
            if (frame.GrabbedEnemy == null)
                return;

            float shake = 1f + frame.TensionWarning * _data.TensionShakeAmplitude;
            DrawRope(frame.WrappedOrigin, frame.EnemyRopeCenter, _data.WrapWaveAmplitude * shake);
        }

        private void DrawRope(Vector3 start, Vector3 end, float waveAmplitude)
        {
            Vector3 direction = end - start;
            Vector3 side = Vector3.Cross(Vector3.up, direction);

            if (side.sqrMagnitude <= 0.001f)
                side = _owner.right;

            side.Normalize();

            for (int i = 0; i < _line.positionCount; i++)
            {
                float t = i / (float)(_line.positionCount - 1);
                float wave = Mathf.Sin(t * Mathf.PI * _data.RopeWaveCount + Time.time * _data.RopeWaveSpeed) *
                    waveAmplitude;
                _line.SetPosition(i, Vector3.Lerp(start, end, t) + side * wave);
            }
        }

        private void UpdateWrapRing(RopeFrame frame)
        {
            if (!frame.RingVisible)
            {
                _wrapRing.enabled = false;
                return;
            }

            _wrapRing.enabled = true;
            ApplyLineVisuals(_wrapRing, 0.7f, frame.ChargeProgress, frame.TensionWarning);

            if (frame.Wrapping)
                DrawWrappingRing(frame);
            else
                DrawSpinningRing(frame);
        }

        private void DrawWrappingRing(RopeFrame frame)
        {
            float progress = Mathf.Clamp01(frame.WrapTimer / Mathf.Max(_data.WrapDuration, 0.01f));
            float visibleProgress = Mathf.SmoothStep(0f, 1f, progress);
            int maxPoints = 48;
            int pointCount = Mathf.Max(2, Mathf.RoundToInt(maxPoints * visibleProgress));

            _wrapRing.loop = false;
            _wrapRing.positionCount = pointCount;

            EnemyWrapMetrics metrics = GetEnemyWrapMetrics(frame.GrabbedEnemy);
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

        private void DrawSpinningRing(RopeFrame frame)
        {
            _wrapRing.loop = true;
            _wrapRing.positionCount = 48;

            EnemyWrapMetrics metrics = GetEnemyWrapMetrics(frame.GrabbedEnemy);
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

        private EnemyWrapMetrics GetEnemyWrapMetrics(EnemyController enemy)
        {
            if (enemy == null || !enemy.TryGetRopeBounds(out Bounds bounds))
            {
                return new EnemyWrapMetrics(
                    enemy != null ? enemy.transform.position : Vector3.zero,
                    Mathf.Max(_data.MinWrapRadius, _data.WrapRadius),
                    0.55f,
                    _owner.right,
                    _owner.forward);
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
            depthAxis = _owner.position - center;
            depthAxis.y = 0f;

            if (depthAxis.sqrMagnitude <= 0.001f)
                depthAxis = _owner.forward;
            else
                depthAxis.Normalize();

            sideAxis = Vector3.Cross(Vector3.up, depthAxis);

            if (sideAxis.sqrMagnitude <= 0.001f)
                sideAxis = _owner.right;
            else
                sideAxis.Normalize();
        }

        private void ApplyLineVisuals(LineRenderer renderer, float widthMultiplier,
            float rawCharge, float tensionWarning)
        {
            float chargeProgress = Mathf.SmoothStep(0f, 1f, rawCharge);
            Color color = Color.Lerp(_data.LineColor, _data.ChargedLineColor, chargeProgress);
            color = Color.Lerp(color, _data.TensionColor, tensionWarning);
            float chargedWidth = Mathf.Lerp(1f, _data.MaxChargeLineWidthMultiplier, chargeProgress);
            float tensionPulse = 1f + Mathf.Sin(Time.time * _data.TensionPulseSpeed) *
                _data.TensionPulseAmplitude * tensionWarning;

            renderer.startColor = color;
            renderer.endColor = color;
            renderer.widthMultiplier = _data.LineWidth * widthMultiplier * chargedWidth * tensionPulse;
        }

        private void EnsureLine()
        {
            if (_line != null)
                return;

            GameObject lineObject = new("Lasso Rope");
            lineObject.transform.SetParent(_owner, false);
            _line = lineObject.AddComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.positionCount = 6;
            _line.widthMultiplier = _data.LineWidth;
            _line.numCapVertices = 5;
            _line.numCornerVertices = 5;
            _line.textureMode = LineTextureMode.Tile;
            _line.material = GetLineMaterial();
            _line.enabled = false;
        }

        private void EnsureWrapRing()
        {
            if (_wrapRing != null)
                return;

            GameObject ringObject = new("Lasso Wrap Ring");
            ringObject.transform.SetParent(_owner, false);
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

        private Material GetLineMaterial()
        {
            if (_sharedMaterial != null)
                return _sharedMaterial;

            Shader shader = Shader.Find("Sprites/Default");
            _sharedMaterial = new Material(shader)
            {
                name = "Lasso Rope"
            };
            _sharedMaterial.mainTexture = CreateRopeTexture();
            _sharedMaterial.mainTextureScale = new Vector2(_data.RopeTextureRepeat, 1f);

            return _sharedMaterial;
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

        private readonly struct EnemyWrapMetrics
        {
            public readonly Vector3 Center;
            public readonly float Radius;
            public readonly float VerticalRange;
            public readonly Vector3 SideAxis;
            public readonly Vector3 DepthAxis;

            public EnemyWrapMetrics(Vector3 center, float radius, float verticalRange,
                Vector3 sideAxis, Vector3 depthAxis)
            {
                Center = center;
                Radius = radius;
                VerticalRange = verticalRange;
                SideAxis = sideAxis;
                DepthAxis = depthAxis;
            }
        }
    }
}
