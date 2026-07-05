using Data;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class FloatingScoreText : MonoBehaviour
    {
        private Text _text;
        private Camera _camera;
        private float _timer;
        private float _lifetime = 0.9f;
        private float _riseSpeed = 1.6f;

        public static FloatingScoreText Create(Vector3 position, string value, VfxData vfx = null)
        {
            GameObject root = new("FloatingScoreText", typeof(RectTransform));
            root.transform.position = position;
            root.transform.localScale = Vector3.one * 0.02f;

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 60;

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(160f, 60f);

            GameObject textObject = new("Value", typeof(RectTransform));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(rootRect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textObject.AddComponent<Text>();
            text.text = value;
            text.font = GetDefaultFont();
            text.fontSize = 44;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.92f, 0.4f, 1f);

            FloatingScoreText floating = root.AddComponent<FloatingScoreText>();
            floating._text = text;
            floating._camera = Camera.main;

            if (vfx != null)
            {
                floating._lifetime = vfx.FloatingTextLifetime;
                floating._riseSpeed = vfx.FloatingTextRiseSpeed;
                text.color = vfx.FloatingTextColor;
            }

            return floating;
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            transform.position += Vector3.up * (_riseSpeed * Time.deltaTime);

            if (_camera != null)
                transform.rotation = Quaternion.LookRotation(transform.position - _camera.transform.position);

            if (_text != null)
            {
                Color color = _text.color;
                color.a = 1f - Mathf.SmoothStep(0.35f, 1f, _timer / _lifetime);
                _text.color = color;
            }

            if (_timer >= _lifetime)
                Destroy(gameObject);
        }

        private static Font GetDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
