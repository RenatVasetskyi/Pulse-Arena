using System.Collections.Generic;
using Data;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    /// <summary>
    /// World-space "+N" score popup that rises and fades out. Pooled and recycled on expiry (the
    /// pool is cleared on scene unload) so a kill no longer allocates a fresh canvas + text each time.
    /// </summary>
    public class FloatingScoreText : MonoBehaviour
    {
        private static readonly Queue<FloatingScoreText> Pool = new();
        private static bool _sceneHookRegistered;

        private TextMeshProUGUI _text;
        private Camera _camera;
        private Color _baseColor = new(1f, 0.92f, 0.4f, 1f);
        private float _timer;
        private float _lifetime = 0.9f;
        private float _riseSpeed = 1.6f;

        public static FloatingScoreText Create(Vector3 position, string value, VfxData vfx = null)
        {
            FloatingScoreText instance = Rent();
            instance.Play(position, value, vfx);
            return instance;
        }

        private static FloatingScoreText Rent()
        {
            EnsureSceneHook();

            while (Pool.Count > 0)
            {
                FloatingScoreText pooled = Pool.Dequeue();

                if (pooled != null)
                {
                    pooled.gameObject.SetActive(true);
                    return pooled;
                }
            }

            return Build();
        }

        private static FloatingScoreText Build()
        {
            GameObject root = new("FloatingScoreText", typeof(RectTransform));
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

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;
            text.fontSize = 44f;

            FloatingScoreText floating = root.AddComponent<FloatingScoreText>();
            floating._text = text;
            return floating;
        }

        private void Play(Vector3 position, string value, VfxData vfx)
        {
            transform.position = position;
            transform.localScale = Vector3.one * 0.02f;
            _timer = 0f;
            _camera = Camera.main;

            _lifetime = 0.9f;
            _riseSpeed = 1.6f;
            _baseColor = new Color(1f, 0.92f, 0.4f, 1f);

            if (vfx != null)
            {
                _lifetime = vfx.FloatingTextLifetime;
                _riseSpeed = vfx.FloatingTextRiseSpeed;
                _baseColor = vfx.FloatingTextColor;
            }

            if (_text != null)
            {
                _text.text = value;
                _text.color = _baseColor;
            }
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            transform.position += Vector3.up * (_riseSpeed * Time.deltaTime);

            if (_camera != null)
                transform.rotation = Quaternion.LookRotation(transform.position - _camera.transform.position);

            if (_text != null)
            {
                Color color = _baseColor;
                color.a = _baseColor.a * (1f - Mathf.SmoothStep(0.35f, 1f, _timer / _lifetime));
                _text.color = color;
            }

            if (_timer >= _lifetime)
                Release();
        }

        private void Release()
        {
            gameObject.SetActive(false);
            Pool.Enqueue(this);
        }

        private static void EnsureSceneHook()
        {
            if (_sceneHookRegistered)
                return;

            _sceneHookRegistered = true;
            SceneManager.sceneUnloaded += _ => Pool.Clear();
        }
    }
}
