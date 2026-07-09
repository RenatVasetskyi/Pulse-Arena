using System.Collections;
using Architecture.Services.Interfaces;
using UnityEngine;

namespace Architecture.Services
{
    /// <summary>
    ///     Bullet-time on a coroutine driven by real time: drop Time.timeScale (and fixedDeltaTime, to keep
    ///     physics smooth), hold, then ease back to normal. <see cref="Stop" /> cancels and restores at once.
    /// </summary>
    public class SlowMoService : ISlowMoService
    {
        private const float DefaultFixedDelta = 0.02f;

        private readonly ICoroutineRunner _runner;
        private Coroutine _routine;

        public SlowMoService(ICoroutineRunner runner)
        {
            _runner = runner;
        }

        public void Stop()
        {
            if (_routine != null)
            {
                _runner.StopCoroutine(_routine);
                _routine = null;
            }

            Time.timeScale = 1f;
            Time.fixedDeltaTime = DefaultFixedDelta;
        }

        public void Trigger(float scale, float duration)
        {
            Stop();
            _routine = _runner.StartCoroutine(Run(Mathf.Clamp(scale, 0.05f, 1f), Mathf.Max(0.01f, duration)));
        }

        private IEnumerator Run(float scale, float duration)
        {
            Time.timeScale = scale;
            Time.fixedDeltaTime = DefaultFixedDelta * scale;

            float held = 0f;
            while (held < duration)
            {
                held += Time.unscaledDeltaTime;
                yield return null;
            }

            const float easeBack = 0.12f;
            float elapsed = 0f;
            while (elapsed < easeBack)
            {
                elapsed += Time.unscaledDeltaTime;
                Time.timeScale = Mathf.Lerp(scale, 1f, elapsed / easeBack);
                Time.fixedDeltaTime = DefaultFixedDelta * Time.timeScale;
                yield return null;
            }

            Time.timeScale = 1f;
            Time.fixedDeltaTime = DefaultFixedDelta;
            _routine = null;
        }
    }
}