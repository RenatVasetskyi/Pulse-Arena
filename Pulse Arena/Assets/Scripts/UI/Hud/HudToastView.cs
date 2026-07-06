using System.Collections;
using TMPro;
using UnityEngine;

namespace UI.Hud
{
    /// <summary>
    /// HUD toast: fades a short message in, holds, then fades out (e.g. "Rare Health Orb spawned!").
    /// Assign the CanvasGroup (for the fade) and the TMP label in the prefab. Starts hidden.
    /// </summary>
    public class HudToastView : MonoBehaviour
    {
        private const float FadeDuration = 0.18f;

        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TextMeshProUGUI _label;

        private Coroutine _routine;

        public void Show(string message, float duration)
        {
            if (_label != null)
                _label.text = message;

            if (_routine != null)
                StopCoroutine(_routine);

            _routine = StartCoroutine(ShowRoutine(duration));
        }

        private void Awake()
        {
            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;
        }

        private IEnumerator ShowRoutine(float duration)
        {
            yield return FadeTo(1f, FadeDuration);
            yield return new WaitForSeconds(Mathf.Max(0.1f, duration));
            yield return FadeTo(0f, FadeDuration);
            _routine = null;
        }

        private IEnumerator FadeTo(float target, float duration)
        {
            if (_canvasGroup == null)
                yield break;

            float start = _canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(start, target, elapsed / duration);
                yield return null;
            }

            _canvasGroup.alpha = target;
        }
    }
}
