using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Hud
{
    /// <summary>
    ///     On-screen dash/ultimate button for touch. A bright Radial360 fill (<see cref="_radialFill" />) sweeps over
    ///     the always-visible faint button as the ability recharges, and gates the <see cref="Button" />'s
    ///     <c>interactable</c> flag. A tap on a ready button is reported frame-accurately (Time.frameCount) for the
    ///     input layer to poll.
    /// </summary>
    public class AbilityButton : MonoBehaviour, IPointerDownHandler
    {
        private const float ReadyThreshold = 0.999f; // float charge rarely lands exactly on 1 — treat near-full as ready

        [SerializeField] private Button _button;
        [SerializeField] private Image _radialFill;

        private int _pressedFrame = -1;

        public bool PressedThisFrame => _pressedFrame == Time.frameCount;

        private void Awake()
        {
            SetCharge(0f); // start with the fill empty (only the faint ghost showing) + non-interactable
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_button != null && !_button.interactable)
                return;

            _pressedFrame = Time.frameCount;
        }

        public void SetCharge(float charge01)
        {
            if (_radialFill != null)
                _radialFill.fillAmount = Mathf.Clamp01(charge01);

            if (_button != null)
                _button.interactable = charge01 >= ReadyThreshold;
        }
    }
}
