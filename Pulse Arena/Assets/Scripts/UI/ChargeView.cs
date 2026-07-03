using Game.Pulse;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ChargeView : MonoBehaviour
    {
        [SerializeField] private PulseAbility _pulseAbility;
        [SerializeField] private Slider _chargeSlider;

        private void OnEnable()
        {
            if (_pulseAbility != null)
                _pulseAbility.ChargeChanged += UpdateView;
        }

        private void OnDisable()
        {
            if (_pulseAbility != null)
                _pulseAbility.ChargeChanged -= UpdateView;
        }

        private void UpdateView(float normalizedCharge)
        {
            if (_chargeSlider != null)
                _chargeSlider.value = normalizedCharge;
        }
    }
}
