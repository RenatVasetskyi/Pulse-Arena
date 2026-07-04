using Game.Cameras;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class CameraZoomView : MonoBehaviour
    {
        private IBattleCamera _battleCamera;

        public static CameraZoomView Create(IBattleCamera battleCamera)
        {
            GameObject root = new("CameraZoomView", typeof(RectTransform));
            CameraZoomView view = root.AddComponent<CameraZoomView>();
            view.Initialize(battleCamera);
            return view;
        }

        private void Initialize(IBattleCamera battleCamera)
        {
            _battleCamera = battleCamera;

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 55;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            RectTransform root = GetComponent<RectTransform>();
            Stretch(root);

            RectTransform panel = CreateRect("Panel", root);
            panel.anchorMin = new Vector2(1f, 1f);
            panel.anchorMax = new Vector2(1f, 1f);
            panel.pivot = new Vector2(1f, 1f);
            panel.anchoredPosition = new Vector2(-42f, -42f);
            panel.sizeDelta = new Vector2(164f, 76f);

            Button zoomOut = CreateButton("ZoomOut", panel, "-", OnZoomOutClicked);
            SetRect(zoomOut.GetComponent<RectTransform>(), new Vector2(-88f, 0f), new Vector2(72f, 72f));

            Button zoomIn = CreateButton("ZoomIn", panel, "+", OnZoomInClicked);
            SetRect(zoomIn.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(72f, 72f));
        }

        private void OnZoomInClicked()
        {
            _battleCamera?.ZoomIn();
        }

        private void OnZoomOutClicked()
        {
            _battleCamera?.ZoomOut();
        }

        private static Button CreateButton(string name, RectTransform parent, string label, UnityEngine.Events.UnityAction action)
        {
            Image image = CreateImage(name, parent, new Color(0.06f, 0.08f, 0.12f, 0.78f));
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            Text text = CreateText("Label", image.rectTransform, label, 44, FontStyle.Bold, Color.white);
            text.alignment = TextAnchor.MiddleCenter;
            Stretch(text.rectTransform);

            return button;
        }

        private static Image CreateImage(string name, RectTransform parent, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(string name, RectTransform parent, string value, int size,
            FontStyle style, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Text text = rect.gameObject.AddComponent<Text>();
            text.text = value;
            text.font = GetDefaultFont();
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(12, size / 2);
            text.resizeTextMaxSize = size;
            return text;
        }

        private static RectTransform CreateRect(string name, RectTransform parent)
        {
            GameObject child = new(name, typeof(RectTransform));
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static Font GetDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

    }
}
