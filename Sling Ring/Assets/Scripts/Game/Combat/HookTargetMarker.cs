using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    ///     Ground ring shown under a lasso-able enemy. The visual (LineRenderer + material) lives on the
    ///     prefab; this component only drives the ring geometry, colour and pulse each frame.
    /// </summary>
    public class HookTargetMarker : MonoBehaviour
    {
        private const int PointCount = 28;

        [SerializeField] private LineRenderer _ring;

        public void Hide()
        {
            if (_ring != null)
                _ring.enabled = false;
        }

        public void Show(Vector3 center, float radius, float width, Color color,
            float pulseSpeed, float pulseAmplitude)
        {
            if (_ring == null)
                return;

            _ring.enabled = true;
            _ring.widthMultiplier = width;
            _ring.startColor = color;
            _ring.endColor = color;

            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude;
            float pulsedRadius = radius * pulse;

            for (int i = 0; i < PointCount; i++)
            {
                float angle = (i / (float)PointCount) * Mathf.PI * 2f;
                _ring.SetPosition(i,
                    center + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * pulsedRadius);
            }
        }
    }
}