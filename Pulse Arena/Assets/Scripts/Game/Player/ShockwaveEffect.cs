using Data;
using Game.Player.Interfaces;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    ///     The ultimate's shockwave: a flat, expanding ring of soft particles kicked outward across the ground
    ///     at the player's feet the instant the ultimate unleashes. Lazily builds one procedural
    ///     <see cref="ParticleSystem" /> (a horizontal Circle emitter) from <see cref="SuperData" /> and fires a
    ///     one-shot burst per activation. Renders with the always-present <c>Sprites/Default</c> shader plus a
    ///     generated soft-dot texture, so it needs no material asset and stays reliable under URP. The shared
    ///     material/texture are built once (static) so replays across matches never leak.
    /// </summary>
    public class ShockwaveEffect : IShockwaveEffect
    {
        private const int DotTextureSize = 64;
        private const float GroundClearance = 0.1f; // lift the ring just off the floor to avoid z-fighting

        private static Material _sharedMaterial;

        private SuperData _data;
        private Transform _parent;
        private ParticleSystem _ring;

        public void Initialize(Transform parent, SuperData data)
        {
            _parent = parent;
            _data = data;
        }

        public void Play(Vector3 position)
        {
            if (_data == null)
                return;

            EnsureRing();
            _ring.transform.position = position + Vector3.up * GroundClearance;
            _ring.Emit(_data.ShockwaveParticleCount);
        }

        private void EnsureRing()
        {
            if (_ring != null)
                return;

            GameObject ringObject = new("Ultimate Shockwave");
            ringObject.transform.SetParent(_parent, false);
            _ring = ringObject.AddComponent<ParticleSystem>();

            ConfigureMain();
            ConfigureEmissionAndShape();
            ConfigureFades();
            ConfigureRenderer(ringObject);
        }

        private void ConfigureMain()
        {
            ParticleSystem.MainModule main = _ring.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = _data.ShockwaveLifetime;
            main.startSpeed = _data.ShockwaveSpeed;
            main.startSize = _data.ShockwaveStartSize;
            main.startColor = _data.ShockwaveColor;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
        }

        private void ConfigureEmissionAndShape()
        {
            ParticleSystem.EmissionModule emission = _ring.emission;
            emission.enabled = false; // manual one-shot Emit per activation

            ParticleSystem.ShapeModule shape = _ring.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = _data.ShockwaveStartRadius;
            shape.radiusThickness = 0f; // emit on the ring edge → a clean expanding ring, not a filled disc
            shape.arc = 360f;
            shape.rotation = new Vector3(90f, 0f, 0f); // lay the circle flat on the ground (XZ plane)
        }

        private void ConfigureFades()
        {
            ParticleSystem.ColorOverLifetimeModule color = _ring.colorOverLifetime;
            color.enabled = true;
            Gradient gradient = new();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;

            ParticleSystem.SizeOverLifetimeModule size = _ring.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.25f));
        }

        private void ConfigureRenderer(GameObject ringObject)
        {
            ParticleSystemRenderer particleRenderer = ringObject.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard; // lie flat → a ground ripple, not upright sparks
            particleRenderer.sharedMaterial = GetSharedMaterial();
        }

        private static Material GetSharedMaterial()
        {
            if (_sharedMaterial != null)
                return _sharedMaterial;

            _sharedMaterial = new Material(Shader.Find("Sprites/Default")) { mainTexture = BuildDotTexture() };
            return _sharedMaterial;
        }

        // A soft round dot (white core, radial alpha falloff) so the ring reads as glowing pulses, not hard squares.
        private static Texture2D BuildDotTexture()
        {
            Texture2D texture = new(DotTextureSize, DotTextureSize, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp
            };
            float center = (DotTextureSize - 1) * 0.5f;

            for (int y = 0; y < DotTextureSize; y++)
            {
                for (int x = 0; x < DotTextureSize; x++)
                {
                    float distance = new Vector2(x - center, y - center).magnitude / center;
                    float alpha = Mathf.Clamp01(1f - distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
                }
            }

            texture.Apply();
            return texture;
        }
    }
}
