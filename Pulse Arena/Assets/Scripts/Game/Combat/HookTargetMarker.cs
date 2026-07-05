using UnityEngine;

namespace Game.Combat
{
    public class HookTargetMarker : MonoBehaviour
    {
        private const int PointCount = 28;

        private LineRenderer _ring;
        private Material _material;

        public static HookTargetMarker Create(Transform parent)
        {
            GameObject root = new("Hook Target Marker");
            root.transform.SetParent(parent, false);

            HookTargetMarker marker = root.AddComponent<HookTargetMarker>();
            marker.Build();
            return marker;
        }

        public void Show(Vector3 center, float radius, float width, Color color)
        {
            _ring.enabled = true;
            _ring.widthMultiplier = width;
            _ring.startColor = color;
            _ring.endColor = color;

            float pulse = 1f + Mathf.Sin(Time.time * 7f) * 0.06f;
            float pulsedRadius = radius * pulse;

            for (int i = 0; i < PointCount; i++)
            {
                float angle = (i / (float)PointCount) * Mathf.PI * 2f;
                _ring.SetPosition(i,
                    center + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * pulsedRadius);
            }
        }

        public void Hide()
        {
            if (_ring != null)
                _ring.enabled = false;
        }

        private void Build()
        {
            Shader shader = Shader.Find("Sprites/Default");
            _material = new Material(shader)
            {
                name = "Hook Target Marker"
            };

            GameObject ringObject = new("Ring");
            ringObject.transform.SetParent(transform, false);

            _ring = ringObject.AddComponent<LineRenderer>();
            _ring.useWorldSpace = true;
            _ring.loop = true;
            _ring.positionCount = PointCount;
            _ring.numCapVertices = 2;
            _ring.numCornerVertices = 2;
            _ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _ring.receiveShadows = false;
            _ring.material = _material;
            _ring.enabled = false;
        }
    }
}
