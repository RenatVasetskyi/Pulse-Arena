using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Hud
{
    /// <summary>
    ///     On-screen ability button (dash / ultimate) for touch. The ability's charge maps straight onto the
    ///     <see cref="Button" /> component's <c>interactable</c> flag — greyed + non-responsive while charging,
    ///     active (and tappable) once ready — so the button's own disabled/pressed transition carries the whole
    ///     visual state (no custom fill or dim). A tap on an active button is reported frame-accurately
    ///     (Time.frameCount) for the input layer to poll; a tap while inactive is ignored.
    /// </summary>
    public class AbilityButton : MonoBehaviour, IPointerDownHandler
    {
        private const float ReadyThreshold = 0.999f; // float charge rarely lands exactly on 1 — treat near-full as ready

        [SerializeField] private Button _button;

        private int _pressedFrame = -1;

        public bool PressedThisFrame => _pressedFrame == Time.frameCount;

        private void Awake()
        {
            if (_button == null)
                _button = GetComponent<Button>();

            if (_button != null)
                _button.interactable = false; // start inactive until the first charge update flips it
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_button != null && !_button.interactable)
                return;

            _pressedFrame = Time.frameCount;
        }

        /// <summary>Ready (near-full) → interactable; still charging → disabled. Keeps the raw 0..1 charge so the HUD call sites stay unchanged.</summary>
        public void SetCharge(float charge01)
        {
            if (_button != null)
                _button.interactable = charge01 >= ReadyThreshold;
        }
    }
}
