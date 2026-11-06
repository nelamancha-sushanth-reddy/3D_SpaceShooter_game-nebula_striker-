using UnityEngine;

namespace NebulaStriker
{
    /// <summary>
    /// Builds the parallax environment around the nebula sky: hyperspace star streaks,
    /// drifting space dust, and a distant ringed gas giant with a glowing atmosphere.
    /// </summary>
    public static class SpaceEnvironment
    {
        public static void Build()
        {
            var root = new GameObject("Environment").transform;
            BuildStreaks(root);
            BuildDust(root);
            BuildPlanet(root);
        }

        static ParticleSystem MakeLooping(string name, Transform parent, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = MaterialLibrary.ParticleMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.duration = 5f;
            main.startSpeed = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            return ps;
        }

        static void BuildStreaks(Transform parent)
        {
            var ps = MakeLooping("StarStreaks", parent, new Vector3(0f, 0f, 230f));
            var main = ps.main;
            main.startLifetime = 1.8f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.24f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 0.85f, 1f, 0.9f), new Color(1f, 0.9f, 0.95f, 0.6f));
            main.maxParticles = 800;
            var em = ps.emission;
            em.rateOverTime = 150f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(170f, 110f, 10f);
            var vol = ps.velocityOverLifetime;
            vol.enabled = true;
            vol.space = ParticleSystemSimulationSpace.World;
            vol.x = new ParticleSystem.MinMaxCurve(0f);
            vol.y = new ParticleSystem.MinMaxCurve(0f);
            vol.z = new ParticleSystem.MinMaxCurve(-165f);
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.03f;
            r.lengthScale = 1f;
            ps.Play();
        }

        static void BuildDust(Transform parent)
        {
            var ps = MakeLooping("SpaceDust", parent, new Vector3(0f, 0f, 230f));
            var main = ps.main;
            main.startLifetime = 6f;
            main.startSize = new ParticleSystem.MinMaxCurve(1f, 3.5f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.6f, 0.4f, 1f, 0.12f), new Color(0.3f, 0.6f, 1f, 0.08f));
            main.maxParticles = 300;
            var em = ps.emission;
            em.rateOverTime = 28f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(220f, 140f, 10f);
            var vol = ps.velocityOverLifetime;
            vol.enabled = true;
            vol.space = ParticleSystemSimulationSpace.World;
            vol.x = new ParticleSystem.MinMaxCurve(0f);
            vol.y = new ParticleSystem.MinMaxCurve(0f);
            vol.z = new ParticleSystem.MinMaxCurve(-45f);
            ps.Play();
        }

        static void BuildPlanet(Transform parent)
        {
            var planet = new GameObject("GasGiant").transform;
            planet.SetParent(parent, false);
            planet.position = new Vector3(330f, -150f, 1100f);
            planet.rotation = Quaternion.Euler(12f, 0f, 18f);

            var surface = MaterialLibrary.Ship(new Color(0.25f, 0.32f, 0.6f), new Color(0.5f, 0.7f, 1f), new Color(0.02f, 0.03f, 0.08f), 2.2f);
            var body = ShipFactory.Part(planet, PrimitiveType.Sphere, Vector3.zero, Vector3.zero, Vector3.one * 420f, surface);
            body.AddComponent<Spinner>().degreesPerSecond = new Vector3(0f, 1.5f, 0f);
            ShipFactory.Part(planet, PrimitiveType.Sphere, Vector3.zero, Vector3.zero, Vector3.one * 450f,
                             MaterialLibrary.Glow(new Color(0.4f, 0.6f, 1f), 1.6f, 1f, 1f, 3f));
            // Rings
            ShipFactory.Part(planet, PrimitiveType.Cylinder, Vector3.zero, Vector3.zero, new Vector3(820f, 0.2f, 820f),
                             MaterialLibrary.Glow(new Color(0.7f, 0.6f, 0.9f, 0.18f), 1f));
            ShipFactory.Part(planet, PrimitiveType.Cylinder, Vector3.zero, Vector3.zero, new Vector3(680f, 0.3f, 680f),
                             MaterialLibrary.Glow(new Color(0.3f, 0.5f, 0.9f, 0.15f), 1f));

            // Small moon
            var moon = MaterialLibrary.Ship(new Color(0.45f, 0.42f, 0.4f), new Color(0.8f, 0.7f, 0.9f), Color.black, 3f);
            ShipFactory.Part(parent, PrimitiveType.Sphere, new Vector3(-260f, 120f, 900f), Vector3.zero, Vector3.one * 60f, moon);
        }
    }
}
