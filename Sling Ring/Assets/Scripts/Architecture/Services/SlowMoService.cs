using System.Collections;
using Architecture.Services.Interfaces;
using UnityEngine;

namespace Architecture.Services
{
    /// <summary>
    ///     Bullet-time on a coroutine driven by real time: drop Time.timeScale (and fixedDeltaTime, to keep
    ///     physics smooth), hold, then ease back to normal. <see cref="Stop" /> cancels and restores at once.
    ///     <see cref="Pause" />/<see cref="Resume" /> suspend an in-progress dip so a mechanical pause does not
    ///     lose it — the dip continues from the exact same point on resume.
    /// </summary>
    public class SlowMoService : ISlowMoService
    {
        private const float DefaultFixedDelta = 0.02f;
        private const float EaseBack = 0.12f;

        private readonly ICoroutineRunner _runner;

        private bool _active;      // a dip is in progress (hold or ease phase)
        private float _duration;
        private bool _easing;      // moved from hold into the ease-back phase
        private float _easeElapsed;
        private float _held;
        private Coroutine _routine;
        private float _scale = 1f;

        // pause cache — the exact scaled values to restore on resume
        private float _pausedFixedDelta = DefaultFixedDelta;
        private bool _pausedActive;
        private float _pausedTimeScale = 1f;

        public SlowMoService(ICoroutineRunner runner)
        {
            _runner = runner;
        }

        public void Stop()
        {
            StopRoutine();
            _active = false;
            _pausedActive = false;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = DefaultFixedDelta;
        }

        public void Trigger(float scale, float duration)
        {
            StopRoutine();
            _scale = Mathf.Clamp(scale, 0.05f, 1f);
            _duration = Mathf.Max(0.01f, duration);
            _held = 0f;
            _easeElapsed = 0f;
            _easing = false;
            _active = true;
            Time.timeScale = _scale;
            Time.fixedDeltaTime = DefaultFixedDelta * _scale;
            _routine = _runner.StartCoroutine(Run());
        }

        /// <summary>Suspend an in-progress dip; caches the exact scaled time so it isn't lost while paused.</summary>
        public void Pause()
        {
            _pausedActive = _active;

            if (!_active)
                return;

            _pausedTimeScale = Time.timeScale;
            _pausedFixedDelta = Time.fixedDeltaTime;
            StopRoutine(); // stop easing timeScale back while paused; gameplay is frozen by per-object gates
        }

        /// <summary>Continue the suspended dip from the exact point it was paused at.</summary>
        public void Resume()
        {
            if (!_pausedActive)
                return;

            _pausedActive = false;
            Time.timeScale = _pausedTimeScale;
            Time.fixedDeltaTime = _pausedFixedDelta;

            if (_active)
                _routine = _runner.StartCoroutine(Run()); // continues from cached _held/_easeElapsed/_easing
        }

        private IEnumerator Run()
        {
            while (!_easing && _held < _duration)
            {
                _held += Time.unscaledDeltaTime;
                yield return null;
            }

            _easing = true;

            while (_easeElapsed < EaseBack)
            {
                _easeElapsed += Time.unscaledDeltaTime;
                Time.timeScale = Mathf.Lerp(_scale, 1f, _easeElapsed / EaseBack);
                Time.fixedDeltaTime = DefaultFixedDelta * Time.timeScale;
                yield return null;
            }

            Time.timeScale = 1f;
            Time.fixedDeltaTime = DefaultFixedDelta;
            _active = false;
            _routine = null;
        }

        private void StopRoutine()
        {
            if (_routine == null)
                return;

            _runner.StopCoroutine(_routine);
            _routine = null;
        }
    }
}
