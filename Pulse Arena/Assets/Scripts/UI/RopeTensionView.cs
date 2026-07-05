using Data;
using Game.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class RopeTensionView : MonoBehaviour
    {
        private const float BarWidth = 460f;
        private const float BarHeight = 30f;
        private const float FillPadding = 6f;

        private EnemySlingshot _slingshot;
        private RectTransform _panel;
        private Image _fill;
        private float _tension;
        private Color _backgroundColor = new(0.06f, 0.07f, 0.1f, 0.78f);
        private Color _safeColor = new(1f, 0.82f, 0.3f, 0.95f);
        private Color _dangerColor = new(1f, 0.22f, 0.12f, 1f);

        public static RopeTensionView Create(EnemySlingshot slingshot, UiData ui = null)
        {
            GameObject root = new("RopeTensionView", typeof(RectTransform));
            RopeTensionView view = root.AddComponent<RopeTensionView>();

            if (ui != null)
            {
                view._backgroundColor = ui.TensionBackgroundColor;
                view._safeColor = ui.TensionSafeColor;
                view._dangerColor = ui.TensionDangerColor;
            }

            view.Initialize(slingshot);
            return view;
        }

        private void Initialize(EnemySlingshot slingshot)
        {
            _slingshot = slingshot;

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform root = GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            _panel = CreateRect("TensionPanel", root);
            _panel.anchorMin = new Vector2(0.5f, 0f);
            _panel.anchorMax = new Vector2(0.5f, 0f);
            _panel.pivot = new Vector2(0.5f, 0f);
            _panel.anchoredPosition = new Vector2(0f, 132f);
            _panel.sizeDelta = new Vector2(BarWidth, BarHeight);

            Image background = _panel.gameObject.AddComponent<Image>();
            background.color = _backgroundColor;

            RectTransform fillRect = CreateRect("Fill", _panel);
            fillRect.anchorMin = new Vector2(0f, 0.5f);
            fillRect.anchorMax = new Vector2(0f, 0.5f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = new Vector2(FillPadding, 0f);
            fillRect.sizeDelta = new Vector2(0f, BarHeight - FillPadding * 2f);

            _fill = fillRect.gameObject.AddComponent<Image>();
            _fill.color = _safeColor;

            _slingshot.TensionChanged += OnTensionChanged;
            SetVisible(false);
        }

        private void OnDestroy()
        {
            if (_slingshot != null)
                _slingshot.TensionChanged -= OnTensionChanged;
        }

        private void Update()
        {
            if (_panel == null || !_panel.gameObject.activeSelf)
                return;

            float danger = Mathf.InverseLerp(0.55f, 1f, _tension);
            float pulse = 1f + Mathf.Sin(Time.time * 24f) * 0.06f * danger;
            _panel.localScale = Vector3.one * pulse;
        }

        private void OnTensionChanged(float tension)
        {
            _tension = tension;
            bool visible = tension > 0.01f;
            SetVisible(visible);

            if (!visible)
                return;

            float maxFillWidth = BarWidth - FillPadding * 2f;
            _fill.rectTransform.sizeDelta = new Vector2(maxFillWidth * tension, BarHeight - FillPadding * 2f);
            _fill.color = Color.Lerp(_safeColor, _dangerColor, tension);
        }

        private void SetVisible(bool visible)
        {
            if (_panel != null && _panel.gameObject.activeSelf != visible)
                _panel.gameObject.SetActive(visible);
        }

        private static RectTransform CreateRect(string name, RectTransform parent)
        {
            GameObject child = new(name, typeof(RectTransform));
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }
    }
}
