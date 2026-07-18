using UnityEngine;

namespace Game.Common
{
    /// <summary>
    ///     Swaps an actor's renderer materials to an authored flat flash material for a short time on hit,
    ///     then restores them. Timer-driven (Tick) so no coroutine / MonoBehaviour is needed.
    ///     Reusable for any actor with renderers.
    ///     The flash material is a shared asset (<c>FeelData.HitFlashMaterial</c>) and is NEVER mutated here —
    ///     it is only assigned through <c>sharedMaterials</c>, so no per-actor copy is ever instantiated.
    /// </summary>
    public class HitFlash
    {
        private bool _active;
        private float _duration;
        private Material[][] _flash;
        private Material _flashMaterial;
        private Material[][] _original;
        private Renderer[] _renderers;
        private float _timer;

        public void Initialize(Renderer[] renderers, Material flashMaterial, float duration)
        {
            _renderers = renderers;
            _flashMaterial = flashMaterial;
            _duration = duration;
            CacheOriginals();
        }

        public void Play()
        {
            if (_renderers == null || _renderers.Length == 0)
                return;

            // Bail on an unassigned material: EnsureFlashArrays would fill each renderer's array with nulls, and
            // Apply's `_flash[i] != null` only tests the ARRAY — so the actor would go invisible for the flash.
            if (_flashMaterial == null)
                return;

            _timer = _duration; // repeated hits extend the flash

            if (_active)
                return;

            Apply();
            _active = true;
        }

        public void Restore()
        {
            _active = false;

            if (_renderers == null || _original == null)
                return;

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null || i >= _original.Length || _original[i] == null)
                    continue;

                _renderers[i].sharedMaterials = _original[i];
            }
        }

        public void Tick(float deltaTime)
        {
            if (!_active)
                return;

            _timer -= deltaTime;

            if (_timer <= 0f)
                Restore();
        }

        private void Apply()
        {
            EnsureFlashArrays();

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null && _flash[i] != null)
                    _renderers[i].sharedMaterials = _flash[i];
            }
        }

        private void CacheOriginals()
        {
            _original = new Material[_renderers.Length][];

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                    _original[i] = _renderers[i].sharedMaterials;
            }
        }

        private void EnsureFlashArrays()
        {
            if (_flash != null)
                return;

            _flash = new Material[_renderers.Length][];

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null)
                    continue;

                Material[] mats = new Material[_renderers[i].sharedMaterials.Length];

                for (int j = 0; j < mats.Length; j++)
                    mats[j] = _flashMaterial;

                _flash[i] = mats;
            }
        }
    }
}