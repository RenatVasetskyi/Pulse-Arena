using UnityEngine;

namespace Game.Enemy
{
    /// <summary>
    /// Swaps an actor's renderer materials to a flat flash colour for a short time on hit,
    /// then restores them. Timer-driven (Tick) so no coroutine / MonoBehaviour is needed.
    /// Reusable for any actor with renderers.
    /// </summary>
    public class HitFlash
    {
        private Renderer[] _renderers;
        private Material[][] _original;
        private Material[][] _flash;
        private Material _flashMaterial;
        private float _duration;
        private float _timer;
        private bool _active;

        public void Initialize(Renderer[] renderers, Color color, float duration)
        {
            _renderers = renderers;
            _duration = duration;
            CreateMaterial(color);
            CacheOriginals();
        }

        public void Play()
        {
            if (_renderers == null || _renderers.Length == 0)
                return;

            _timer = _duration;   // repeated hits extend the flash (same as the old coroutine)

            if (_active)
                return;

            Apply();
            _active = true;
        }

        public void Tick(float deltaTime)
        {
            if (!_active)
                return;

            _timer -= deltaTime;

            if (_timer <= 0f)
                Restore();
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

        private void CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            shader ??= Shader.Find("Sprites/Default");

            _flashMaterial = new Material(shader) { name = "Hit Flash" };

            if (_flashMaterial.HasProperty("_BaseColor"))
                _flashMaterial.SetColor("_BaseColor", color);

            if (_flashMaterial.HasProperty("_Color"))
                _flashMaterial.SetColor("_Color", color);
        }
    }
}
