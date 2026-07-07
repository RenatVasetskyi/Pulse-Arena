using System.Collections.Generic;
using Data;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    /// <summary>
    /// World-space "+N" score popup that rises and fades out. The canvas + text live on the prefab;
    /// instances are pooled and recycled on expiry (the pool is cleared on scene unload) so a kill no
    /// longer allocates a fresh canvas + text each time.
    /// </summary>
    public class FloatingScoreText : MonoBehaviour
    {
        private static readonly Queue<FloatingScoreText> Pool = new();
        private static bool _sceneHookRegistered;

        [SerializeField] private TextMeshProUGUI _text;

        private Camera _camera;
        private Color _baseColor = new(1f, 0.92f, 0.4f, 1f);
        private float _timer;
        private float _lifetime = 0.9f;
        private float _riseSpeed = 1.6f;

        public static FloatingScoreText Create(GameObject prefab, Vector3 position, string value, VfxData vfx = null)
        {
            FloatingScoreText instance = Rent(prefab);

            if (instance == null)
                return null;

            instance.Play(position, value, vfx);
            return instance;
        }

        private static FloatingScoreText Rent(GameObject prefab)
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

            return Build(prefab);
        }

        private static FloatingScoreText Build(GameObject prefab)
        {
            if (prefab == null)
                return null;

            GameObject root = Instantiate(prefab);
            return root.GetComponent<FloatingScoreText>();
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
