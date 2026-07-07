using System;
using Architecture.Services.Interfaces;
using Data;
using UnityEngine;

namespace Architecture.Services
{
    /// <summary>
    /// Kill-chain combo. A kill within <see cref="ComboData.Window"/> of the previous one extends the
    /// chain (multiplier = combo count, capped). Expiry is checked lazily on the next kill, so it costs
    /// nothing between kills; the HUD fades itself out. Uses scaled time so pause preserves the chain.
    /// </summary>
    public class ComboService : IComboService
    {
        private readonly float _window;
        private readonly int _maxMultiplier;

        private float _lastKillTime;
        private int _combo;

        public ComboService(GameSettings gameSettings)
        {
            ComboData data = gameSettings.ComboData;
            _window = data != null ? Mathf.Max(0.1f, data.Window) : 2.5f;
            _maxMultiplier = data != null ? Mathf.Max(1, data.MaxMultiplier) : 8;
        }

        public event Action<int> ComboChanged;

        public int Combo => _combo;
        public int Multiplier => Mathf.Clamp(_combo, 1, _maxMultiplier);

        public int RegisterKill()
        {
            if (_combo > 0 && Time.time - _lastKillTime > _window)
                _combo = 0;

            _combo++;
            _lastKillTime = Time.time;
            ComboChanged?.Invoke(_combo);
            return Multiplier;
        }

        public void Reset()
        {
            if (_combo == 0)
                return;

            _combo = 0;
            ComboChanged?.Invoke(0);
        }
    }
}
