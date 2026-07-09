using System;
using Data;
using Game.Combat.Interfaces;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    ///     The rope-snap particle burst. Lazily builds a procedural one-shot <see cref="ParticleSystem" /> from
    ///     <see cref="VfxData" /> (sharing the rope material) on first use, then emits a burst at the break point.
    /// </summary>
    public class SnapBurstEffect : ISnapBurstEffect
    {
        private ParticleSystem _burst;
        private SlingshotData _data;
        private Func<Material> _materialProvider;
        private Transform _parent;
        private VfxData _vfx;

        public void Initialize(Transform parent, VfxData vfx, SlingshotData data, Func<Material> materialProvider)
        {
            _parent = parent;
            _vfx = vfx;
            _data = data;
            _materialProvider = materialProvider;
        }

        public void Play(Vector3 position)
        {
            EnsureBurst();
            _burst.transform.position = position;
            _burst.Emit(_vfx.SnapBurstCount);
        }

        private void EnsureBurst()
        {
            if (_burst != null)
                return;

            GameObject burstObject = new("Rope Snap Burst");
            burstObject.transform.SetParent(_parent, false);
            _burst = burstObject.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = _burst.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(_vfx.SnapBurstLifetimeMin, _vfx.SnapBurstLifetimeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(_vfx.SnapBurstSpeedMin, _vfx.SnapBurstSpeedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(_vfx.SnapBurstSizeMin, _vfx.SnapBurstSizeMax);
            main.startColor = new ParticleSystem.MinMaxGradient(_data.RopeBaseColor, _data.TensionColor);
            main.gravityModifier = _vfx.SnapBurstGravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = _burst.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = _burst.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            ParticleSystemRenderer particleRenderer = burstObject.GetComponent<ParticleSystemRenderer>();
            particleRenderer.material = _materialProvider();
        }
    }
}