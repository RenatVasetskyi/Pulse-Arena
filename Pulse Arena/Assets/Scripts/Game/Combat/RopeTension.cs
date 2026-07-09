using System;
using Data;
using Game.Combat.Interfaces;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    ///     Pure tension simulation for the spun rope. Each fixed step it accrues tension at a rate scaled by the
    ///     grabbed enemy's weight and type-rate and the current charge, and reports break/warning. No Unity object
    ///     dependencies — the slingshot feeds it plain numbers — so it is directly unit-testable.
    /// </summary>
    public class RopeTension : IRopeTension
    {
        private SlingshotData _data;
        private float _tension;

        public event Action<float> Changed;
        public bool IsBroken => _tension >= 1f;

        public float Value => _tension;
        public float Warning => Mathf.InverseLerp(Mathf.Clamp01(_data.TensionWarningThreshold), 1f, _tension);

        public void Initialize(SlingshotData data)
        {
            _data = data;
        }

        public void Reset()
        {
            Set(0f);
        }

        public void Tick(float weight, float typeRate, float chargeProgress, float deltaTime)
        {
            float safeWeight = Mathf.Max(0.1f, weight);
            float safeTypeRate = Mathf.Max(0.1f, typeRate);
            float chargeBoost = 1f + chargeProgress * _data.TensionChargeInfluence;
            float rate = safeWeight * safeTypeRate * chargeBoost / Mathf.Max(0.5f, _data.TensionBreakTime);

            Set(Mathf.Min(1f, _tension + rate * deltaTime));
        }

        private void Set(float value)
        {
            if (Mathf.Approximately(_tension, value))
                return;

            _tension = value;
            Changed?.Invoke(_tension);
        }
    }
}