using Game.Enemy.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    ///     World-space segmented HP bar that billboards to the camera. Canvas, background and one disabled
    ///     segment template live on the prefab; segments are cloned from that template at runtime because
    ///     pooled enemies respawn with different max HP (2 -&gt; 2 pips, 3 -&gt; 3, 4 -&gt; 4).
    /// </summary>
    public class WorldHealthBar : MonoBehaviour, IWorldHealthBar
    {
        private const float SegmentSpacing = 0.04f;

        [SerializeField] private RectTransform _segmentsParent;
        [SerializeField] private Image _segmentTemplate;
        [SerializeField] private Image _background;
        [SerializeField] private Color _aliveColor = new(1f, 0.2f, 0.12f, 1f);
        [SerializeField] private Color _emptyColor = new(0.12f, 0.12f, 0.15f, 0.76f);
        [SerializeField] private Color _backgroundColor = new(0.02f, 0.025f, 0.035f, 0.72f);
        private Camera _camera;

        private Image[] _segments;

        public void Initialize(int maxHealth, float height)
        {
            transform.localPosition = Vector3.up * height;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            if (_background != null)
                _background.color = _backgroundColor;

            SetHealth(maxHealth, maxHealth);
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                _camera = Camera.main;

                if (_camera == null)
                    return;
            }

            transform.rotation = _camera.transform.rotation;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void SetHealth(int health, int maxHealth)
        {
            int count = Mathf.Max(1, maxHealth);

            if (_segments == null || _segments.Length != count)
                RebuildSegments(count);

            for (int i = 0; i < _segments.Length; i++)
                _segments[i].color = i < health ? _aliveColor : _emptyColor;
        }

        private void RebuildSegments(int count)
        {
            DestroyExistingSegments();

            _segments = new Image[count];

            if (_segmentTemplate == null || _segmentsParent == null)
                return;

            float totalWidth = 1.05f;
            float segmentWidth = (totalWidth - SegmentSpacing * (count - 1)) / count;
            float startX = -totalWidth * 0.5f;

            for (int i = 0; i < count; i++)
                _segments[i] = CreateSegment(i, startX, segmentWidth);
        }

        private void DestroyExistingSegments()
        {
            if (_segments == null)
                return;

            foreach (Image segment in _segments)
                if (segment != null)
                    Destroy(segment.gameObject);
        }

        private Image CreateSegment(int index, float startX, float segmentWidth)
        {
            Image segment = Instantiate(_segmentTemplate, _segmentsParent);
            segment.gameObject.SetActive(true);
            segment.gameObject.name = $"HP_{index + 1}";
            segment.color = _aliveColor;

            RectTransform rect = segment.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(startX + index * (segmentWidth + SegmentSpacing), 0f);
            rect.sizeDelta = new Vector2(segmentWidth, 0.1f);

            return segment;
        }
    }
}