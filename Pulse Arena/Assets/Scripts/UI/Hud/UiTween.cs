using DG.Tweening;
using UnityEngine;

namespace UI.Hud
{
    /// <summary>
    /// Small DOTween helpers for HUD juice. Animations are relative to the element's own scale
    /// (never reset it to one), so elements that were scaled in the prefab keep their size. All
    /// tweens complete-kill any prior tween and SetLink so they die with the object.
    /// </summary>
    public static class UiTween
    {
        public static void Punch(Transform target, float strength = 0.3f, float duration = 0.28f)
        {
            if (target == null)
                return;

            target.DOKill(true);
            target.DOPunchScale(target.localScale * strength, duration, 8, 0.8f).SetLink(target.gameObject);
        }

        public static void Pop(Transform target, float duration = 0.35f)
        {
            if (target == null)
                return;

            target.DOKill(true);
            Vector3 baseScale = target.localScale;
            target.localScale = Vector3.zero;
            target.DOScale(baseScale, duration).SetEase(Ease.OutBack).SetLink(target.gameObject);
        }

        public static void Shake(Transform target, float duration = 0.4f, float strength = 0.5f)
        {
            if (target == null)
                return;

            target.DOKill(true);
            target.DOShakeScale(duration, target.localScale * strength, 10, 90f, true).SetLink(target.gameObject);
        }

        /// <summary>
        /// Reusable "window opens" animation for any popup: the window bounces up from a smaller scale
        /// while the group fades in. Runs on unscaled time so it plays even when the game is paused.
        /// </summary>
        public static void OpenWindow(RectTransform window, CanvasGroup group, Vector3 baseScale)
        {
            if (window != null)
            {
                window.DOKill();
                window.localScale = baseScale * 0.72f;
                window.DOScale(baseScale, 0.32f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(window.gameObject);
            }

            if (group != null)
            {
                group.DOKill();
                group.alpha = 0f;
                group.DOFade(1f, 0.18f).SetUpdate(true).SetLink(group.gameObject);
            }
        }
    }
}
