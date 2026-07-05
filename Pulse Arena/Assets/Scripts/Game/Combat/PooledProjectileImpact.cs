using System;
using UnityEngine;

namespace Game.Combat
{
    public class PooledProjectileImpact : MonoBehaviour
    {
        private Action<PooledProjectileImpact> _releaseAction;
        private float _lifetime;
        private bool _isPlaying;

        public void Initialize(float lifetime, Action<PooledProjectileImpact> releaseAction)
        {
            StopParticles();
            _lifetime = lifetime;
            _releaseAction = releaseAction;
            _isPlaying = true;
            PlayParticles();
        }

        public void ResetForPool()
        {
            StopParticles();
            _lifetime = 0f;
            _releaseAction = null;
            _isPlaying = false;
        }

        private void PlayParticles()
        {
            ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>(true);

            foreach (ParticleSystem particle in particles)
                particle.Play(true);
        }

        private void StopParticles()
        {
            ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>(true);

            foreach (ParticleSystem particle in particles)
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void Update()
        {
            if (!_isPlaying)
                return;

            _lifetime -= Time.deltaTime;

            if (_lifetime <= 0f)
                _releaseAction?.Invoke(this);
        }
    }
}
