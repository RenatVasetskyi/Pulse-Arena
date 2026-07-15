using Game.Combat.Interfaces;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    ///     The rope-snap particle burst. Spawns the authored one-shot burst prefab on first use, then triggers it
    ///     at the break point. The whole effect — look, material AND particle count (an emission burst at t=0) —
    ///     lives on the asset, so it previews correctly in the inspector; this configures nothing.
    /// </summary>
    public class SnapBurstEffect : ISnapBurstEffect
    {
        private ParticleSystem _burst;
        private GameObject _burstPrefab;
        private Transform _parent;

        public void Initialize(Transform parent, GameObject burstPrefab)
        {
            _parent = parent;
            _burstPrefab = burstPrefab;
        }

        // Restart, don't Play(): Play() rewinds and re-fires the authored t=0 burst only when the system is playing
        // or stopped — on a PAUSED one it silently takes the resume branch (no burst, and it un-pauses the effect
        // behind IPauseService's back). Stop-then-Play is unambiguous from every state.
        public void Play(Vector3 position)
        {
            EnsureBurst();

            if (_burst == null)
                return;

            _burst.transform.position = position;
            _burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _burst.Play(true);
        }

        public void PauseEffect()
        {
            if (_burst != null)
                _burst.Pause(true);
        }

        // Guarded on isPaused: Play(true) on an already-playing system rewinds to t=0 and re-fires the burst, so a
        // resume without a matching pause would visibly re-bang the effect.
        public void ResumeEffect()
        {
            if (_burst != null && _burst.isPaused)
                _burst.Play(true);
        }

        // Spawns the authored burst prefab once and reuses it; everything about the effect is baked on the asset.
        private void EnsureBurst()
        {
            if (_burst != null || _burstPrefab == null)
                return;

            _burst = Object.Instantiate(_burstPrefab, _parent, false).GetComponent<ParticleSystem>();
        }
    }
}
