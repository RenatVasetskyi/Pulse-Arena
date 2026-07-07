using System;
using Architecture.Services.Interfaces;
using Data;
using UnityEngine;

namespace Architecture.Services
{
    /// <summary>
    /// Charges the ultimate meter one notch per kill (it listens to the combo service, which fires on every
    /// kill). Fills after <see cref="SuperData.KillsToCharge"/> kills; <see cref="TryConsume"/> spends it.
    /// </summary>
    public class SuperMeterService : ISuperMeterService, IDisposable
    {
        private readonly IComboService _comboService;
        private readonly int _killsToCharge;

        private int _kills;
        private bool _wasFull;

        public SuperMeterService(GameSettings gameSettings, IComboService comboService)
        {
            _comboService = comboService;
            _killsToCharge = Mathf.Max(1, gameSettings.SuperData.KillsToCharge);
            _comboService.ComboChanged += OnComboChanged;
        }

        public event Action<float> ChargeChanged;
        public event Action Ready;

        public float Charge01 => Mathf.Clamp01((float)_kills / _killsToCharge);
        public bool IsFull => _kills >= _killsToCharge;

        // The combo service fires ComboChanged on every kill (count >= 1) and on reset (count 0).
        private void OnComboChanged(int combo)
        {
            if (combo <= 0)
                return;

            AddKill();
        }

        private void AddKill()
        {
            if (IsFull)
                return;

            _kills++;
            ChargeChanged?.Invoke(Charge01);

            if (IsFull && !_wasFull)
            {
                _wasFull = true;
                Ready?.Invoke();
            }
        }

        public bool TryConsume()
        {
            if (!IsFull)
                return false;

            _kills = 0;
            _wasFull = false;
            ChargeChanged?.Invoke(0f);
            return true;
        }

        public void Reset()
        {
            _kills = 0;
            _wasFull = false;
            ChargeChanged?.Invoke(0f);
        }

        public void Dispose()
        {
            if (_comboService != null)
                _comboService.ComboChanged -= OnComboChanged;
        }
    }
}
