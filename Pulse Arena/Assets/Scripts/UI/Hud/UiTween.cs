using System;
using DG.Tweening;
using UnityEngine;

namespace UI.Hud
{
    /// <summary>
    ///     Small DOTween helpers for HUD juice. Animations are relative to the element's own scale
    ///     (never reset it to one), so elements that were scaled in the prefab keep their size. All
    ///     tweens complete-kill any prior tween and SetLink so they die with the object.
    /// </summary>
    public static class UiTween
    {
        private const float CloseDuration = 0.14f;

        /// <summary>
        ///     Reusable "window opens" animation for any popup: the window bounces up from a smaller scale
        ///     while the group fades in. Runs on unscaled time so it plays even when the game is paused.
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

        /// <summary>
        ///     Reusable "window closes" animation — the quick mirror of <see cref="OpenWindow" />: the window
        ///     shrinks with a short anticipation while the group fades out, then <paramref name="onComplete" />
        ///     runs (deactivate / destroy). Fast and on unscaled time so it plays while the game is paused.
        /// </summary>
        public static void CloseWindow(RectTransform window, CanvasGroup group, Action onComplete = null)
        {
            Tween driver = null;

            if (window != null)
            {
                window.DOKill();
                driver = window.DOScale(window.localScale * 0.7f, CloseDuration)
                    .SetEase(Ease.InBack).SetUpdate(true).SetLink(window.gameObject);
            }

            if (group != null)
            {
                group.DOKill();
                group.interactable = false;
                group.blocksRaycasts = false;
                Tween fade = group.DOFade(0f, CloseDuration).SetEase(Ease.InQuad)
                    .SetUpdate(true).SetLink(group.gameObject);

                if (driver == null)
                    driver = fade;
            }

            if (onComplete == null)
                return;

            if (driver != null)
                driver.OnComplete(() => onComplete());
            else
                onComplete();
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

        public static void Punch(Transform target, float strength = 0.3f, float duration = 0.28f)
        {
            if (target == null)
                return;

            target.DOKill(true);
            target.DOPunchScale(target.localScale * strength, duration, 8, 0.8f).SetLink(target.gameObject);
        }

        public static void Shake(Transform target, float duration = 0.4f, float strength = 0.5f)
        {
            if (target == null)
                return;

            target.DOKill(true);
            target.DOShakeScale(duration, target.localScale * strength, 10, 90f, true).SetLink(target.gameObject);
        }
    }
}