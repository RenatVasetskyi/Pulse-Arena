using UnityEngine;

namespace Game.Combat
{
    public static class ProjectileVfxUtility
    {
        private static readonly Color CoreColor = new(1f, 0.92f, 0.34f, 1f);
        private static readonly Color TrailColor = new(1f, 0.34f, 0.05f, 0.75f);
        private static readonly Color ImpactColor = new(1f, 0.16f, 0.05f, 0.85f);

        public static void CreateOrbitVisual(Transform parent)
        {
            GameObject core = CreateCore(parent, "Orbit Core", CoreColor);
            core.transform.localScale = Vector3.one * 0.55f;

            Light light = core.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = CoreColor;
            light.range = 2f;
            light.intensity = 1.4f;
        }

        public static void CreateProjectileVisual(Transform parent)
        {
            GameObject core = CreateCore(parent, "Projectile Core", CoreColor);
            core.transform.localScale = Vector3.one * 0.65f;

            TrailRenderer trail = parent.gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.18f;
            trail.minVertexDistance = 0.05f;
            trail.widthCurve = new AnimationCurve(
                new Keyframe(0f, 0.42f),
                new Keyframe(1f, 0.02f));
            trail.colorGradient = CreateGradient(CoreColor, TrailColor);
            trail.material = CreateMaterial(TrailColor);
            trail.numCapVertices = 4;
            trail.numCornerVertices = 4;
        }

        public static void CreateImpactVisual(Transform parent)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ring.name = "Impact Pulse";
            ring.transform.SetParent(parent, false);
            ring.transform.localScale = Vector3.one * 0.85f;
            Object.Destroy(ring.GetComponent<Collider>());

            MeshRenderer renderer = ring.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateMaterial(ImpactColor);

            ParticleSystem particles = parent.gameObject.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particles.main;
            main.playOnAwake = false;
            main.duration = 0.18f;
            main.loop = false;
            main.startLifetime = 0.18f;
            main.startSpeed = 1.8f;
            main.startSize = 0.12f;
            main.startColor = ImpactColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.None;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 12)
            });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            ParticleSystemRenderer particleRenderer = parent.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = CreateMaterial(ImpactColor);
        }

        private static GameObject CreateCore(Transform parent, string name, Color color)
        {
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = name;
            core.transform.SetParent(parent, false);
            Object.Destroy(core.GetComponent<Collider>());

            MeshRenderer renderer = core.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateMaterial(color);

            return core;
        }

        private static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            shader ??= Shader.Find("Sprites/Default");

            Material material = new(shader)
            {
                name = "Runtime Projectile Glow"
            };

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);

            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);

            return material;
        }

        private static Gradient CreateGradient(Color start, Color end)
        {
            Gradient gradient = new();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(start, 0f),
                    new GradientColorKey(end, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(start.a, 0f),
                    new GradientAlphaKey(0f, 1f)
                });

            return gradient;
        }
    }
}
