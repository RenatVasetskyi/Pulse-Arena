using System;
using Architecture.Services.Interfaces;
using Data;
using UnityEngine;

namespace Architecture.Services
{
    /// <summary>
    ///     Kill-chain combo. A kill within <see cref="ComboData.Window" /> of the previous one extends the
    ///     chain (multiplier = combo count, capped). Expiry is checked lazily on the next kill, so it costs
    ///     nothing between kills; the HUD fades itself out. <see cref="IPausable" />: the window is measured in
    ///     <c>Time.time</c>, which keeps advancing under a mechanical pause, so a pause shifts the last-kill
    ///     timestamp forward by its own duration — otherwise a long pause would silently expire the chain.
    /// </summary>
    public class ComboService : IComboService, IPausable
    {
        private readonly int _maxMultiplier;
        private readonly float _window;
        private int _combo;

        private float _lastKillTime;
        private float _pauseStartTime;

        public event Action<int> ComboChanged;

        public int Combo => _combo;
        public int Multiplier => Mathf.Clamp(_combo, 1, _maxMultiplier);

        public ComboService(GameSettings gameSettings, IPauseService pauseService)
        {
            ComboData data = gameSettings.ComboData;
            _window = data != null ? Mathf.Max(0.1f, data.Window) : 2.5f;
            _maxMultiplier = data != null ? Mathf.Max(1, data.MaxMultiplier) : 8;
            pauseService.Register(this);
        }

        public void Pause()
        {
            _pauseStartTime = Time.time;
        }

        public void Resume()
        {
            _lastKillTime += Time.time - _pauseStartTime; // don't let the paused span eat into the combo window
        }

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