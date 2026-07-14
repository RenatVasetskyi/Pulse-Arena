using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    ///     A reusable modal confirmation dialog: a dimmed full-screen overlay + a centre panel with a message and
    ///     Confirm/Cancel buttons. Passive — it only shows/hides and relays the two clicks as <see cref="Confirmed" /> /
    ///     <see cref="Cancelled" />; the presenter owns the decision + side effect. The dialog GameObject lives
    ///     INACTIVE in the prefab (so it stays out of the way while editing) — <see cref="Show" /> activates it (which
    ///     also runs Awake -> wires the buttons the first time) and <see cref="Hide" /> deactivates it after the fade.
    ///     Tweens use SetUpdate(true) so it opens even under frozen time.
    /// </summary>
    public class ConfirmDialogView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private RectTransform _panel;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;

        private Vector3 _baseScale = Vector3.one;

        public event Action Confirmed;
        public event Action Cancelled;

        private void Awake()
        {
            if (_panel != null)
                _baseScale = _panel.localScale;

            if (_confirmButton != null)
                _confirmButton.onClick.AddListener(OnConfirm);

            if (_cancelButton != null)
                _cancelButton.onClick.AddListener(OnCancel);
        }

        private void OnDestroy()
        {
            if (_confirmButton != null)
                _confirmButton.onClick.RemoveListener(OnConfirm);

            if (_cancelButton != null)
                _cancelButton.onClick.RemoveListener(OnCancel);
        }

        public void Show()
        {
            gameObject.SetActive(true); // first show also runs Awake -> wires the buttons

            if (_group == null || _panel == null)
                return;

            _group.DOKill();
            _panel.DOKill();
            _group.blocksRaycasts = true;
            _group.interactable = true;
            _group.alpha = 0f;
            _panel.localScale = _baseScale * 0.9f;
            _group.DOFade(1f, 0.18f).SetUpdate(true).SetLink(gameObject);
            _panel.DOScale(_baseScale, 0.25f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
        }

        public void Hide()
        {
            if (_group == null)
            {
                gameObject.SetActive(false);
                return;
            }

            _group.DOKill();
            _group.interactable = false;
            _group.blocksRaycasts = false;
            _group.DOFade(0f, 0.15f).SetUpdate(true).SetLink(gameObject).OnComplete(Deactivate);
        }

        private void Deactivate()
        {
            gameObject.SetActive(false);
        }

        private void OnConfirm()
        {
            Confirmed?.Invoke();
        }

        private void OnCancel()
        {
            Cancelled?.Invoke();
        }
    }
}
