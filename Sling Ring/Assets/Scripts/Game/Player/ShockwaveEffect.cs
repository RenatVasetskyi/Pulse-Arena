using Game.Player.Interfaces;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    ///     The ultimate's shockwave: a flat ring of soft particles kicked outward across the ground at the
    ///     player's feet when the ultimate unleashes. Spawns the authored ring prefab once and triggers it per
    ///     activation; the whole effect lives on the asset (an emission burst at t=0), so it previews in the inspector.
    /// </summary>
    public class ShockwaveEffect : IShockwaveEffect
    {
        private const float GroundClearance = 0.1f; // lift the ring just off the floor to avoid z-fighting

        private Transform _parent;
        private ParticleSystem _ring;
        private GameObject _ringPrefab;

        public void Initialize(Transform parent, GameObject ringPrefab)
        {
            _parent = parent;
            _ringPrefab = ringPrefab;
        }

        // Restart, don't Play(): Play() rewinds and re-fires the authored t=0 burst only when the system is playing
        // or stopped — on a PAUSED one it silently takes the resume branch (no burst, and it un-pauses the effect
        // behind IPauseService's back). Stop-then-Play is unambiguous from every state.
        public void Play(Vector3 position)
        {
            EnsureRing();

            if (_ring == null)
                return;

            _ring.transform.position = position + Vector3.up * GroundClearance;
            _ring.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _ring.Play(true);
        }

        private void EnsureRing()
        {
            if (_ring != null || _ringPrefab == null)
                return;

            _ring = Object.Instantiate(_ringPrefab, _parent, false).GetComponent<ParticleSystem>();
        }
    }
}
