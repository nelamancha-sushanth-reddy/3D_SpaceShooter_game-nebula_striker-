using System.Collections;
using UnityEngine;

namespace NebulaStriker
{
    /// <summary>
    /// Runtime-built visual effects: explosions, sparks, engine flames, Nova Bomb shockwave and boss death sequence.
    /// All particle systems are configured from code — no prefabs required.
    /// </summary>
    public static class FX
    {
        // ------------------------------------------------------------------ coroutine host
        static FXRunner runner;
        static FXRunner Runner
        {
            get
            {
                if (runner == null) runner = new GameObject("FX Runner").AddComponent<FXRunner>();
                return runner;
            }
        }

        // ------------------------------------------------------------------ helpers
        static ParticleSystem MakeSystem(string name, Vector3 pos, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = MaterialLibrary.ParticleMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            var em = ps.emission;
            em.rateOverTime = 0f;
            return ps;
        }

        static Gradient Fade(Color start, Color end)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        static void OneShot(ParticleSystem ps, float duration, int count)
        {
            var main = ps.main;
            main.duration = duration;
            main.loop = false;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var em = ps.emission;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(count, 1, 500)) });
            ps.Play();
        }

        // ------------------------------------------------------------------ effects
        public static void Explosion(Vector3 pos, Color color, float scale)
        {
            scale = Mathf.Max(0.3f, scale);

            // Fireball
            var ps = MakeSystem("Explosion", pos);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f * scale, 13f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(0.8f * scale, 2.4f * scale);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.6f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 300;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.4f * scale;
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0f)));
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(Fade(Color.white, new Color(0.6f, 0.15f, 0.05f)));
            OneShot(ps, 1f, Mathf.RoundToInt(35 * scale));

            // Sparks
            Spark(pos, Color.Lerp(color, Color.white, 0.4f), 1.6f * scale);

            // Flash sphere
            var flash = new GameObject("Flash");
            flash.transform.position = pos;
            ShipFactory.Part(flash.transform, PrimitiveType.Sphere, Vector3.zero, Vector3.zero, Vector3.one, MaterialLibrary.Glow(Color.white, 2.5f, 1f, 0f, 1.5f));
            var f = flash.AddComponent<FlashFX>();
            f.Setup(0.25f, 0.5f * scale, 4.5f * scale, Color.Lerp(color, Color.white, 0.5f));

            // Debris chunks
            if (scale >= 0.8f)
            {
                var mat = MaterialLibrary.Ship(new Color(0.2f, 0.2f, 0.22f), color, color * 0.3f, 2f);
                int n = Mathf.Clamp(Mathf.RoundToInt(5 * scale), 3, 14);
                for (int i = 0; i < n; i++)
                {
                    var d = ShipFactory.Part(null, PrimitiveType.Cube, pos, Random.rotation.eulerAngles, Vector3.one * Random.Range(0.15f, 0.45f) * Mathf.Sqrt(scale), mat);
                    d.transform.position = pos;
                    var db = d.AddComponent<Debris>();
                    db.velocity = Random.insideUnitSphere * 14f * Mathf.Sqrt(scale);
                    db.spin = Random.insideUnitSphere * 540f;
                    db.life = Random.Range(0.8f, 1.6f);
                }
            }
        }

        public static void Spark(Vector3 pos, Color color, float scale = 1f)
        {
            var ps = MakeSystem("Sparks", pos);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(8f * scale, 22f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.white);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.1f;
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.04f;
            r.lengthScale = 1.5f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(Fade(Color.white, color));
            OneShot(ps, 0.5f, Mathf.RoundToInt(14 * scale));
        }

        /// <summary>Looping exhaust attached to a ship. localDir is the direction the flame shoots out.</summary>
        public static ParticleSystem EngineFlame(Transform parent, Vector3 localPos, Vector3 localDir, Color color, float scale)
        {
            var ps = MakeSystem("EngineFlame", Vector3.zero, parent);
            ps.transform.localPosition = localPos;
            ps.transform.localRotation = Quaternion.LookRotation(localDir);
            var main = ps.main;
            main.loop = true;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(10f * scale, 16f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f * scale, 0.6f * scale);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.5f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 120;
            var em = ps.emission;
            em.rateOverTime = 70f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = 6f;
            sh.radius = 0.08f * scale;
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(Fade(Color.white, color));
            ps.Play();
            return ps;
        }

        /// <summary>Nova Bomb: expanding shockwave that heavily damages everything and erases enemy fire.</summary>
        public static void NovaBomb(Vector3 pos)
        {
            Color c = new Color(1f, 0.4f, 0.95f);
            var ring = new GameObject("NovaShockwave");
            ring.transform.position = pos;
            ShipFactory.Part(ring.transform, PrimitiveType.Sphere, Vector3.zero, Vector3.zero, Vector3.one, MaterialLibrary.Glow(Color.white, 2.5f, 1f, 1f, 2f));
            ring.AddComponent<FlashFX>().Setup(0.8f, 2f, 140f, c);

            var inner = new GameObject("NovaCore");
            inner.transform.position = pos;
            ShipFactory.Part(inner.transform, PrimitiveType.Sphere, Vector3.zero, Vector3.zero, Vector3.one, MaterialLibrary.Glow(Color.white, 3f, 1f, 0f, 1f));
            inner.AddComponent<FlashFX>().Setup(0.4f, 1f, 12f, Color.white);

            Spark(pos, c, 4f);
            Sfx.Play(SfxId.Bomb, 1f);
            ChaseCamera.Shake(0.9f);
            if (GameManager.Instance != null) GameManager.Instance.UI.Flash(0.25f, new Color(1f, 0.5f, 1f));

            foreach (var p in Projectile.Active.ToArray())
                if (p != null && p.owner == Team.Enemy) { Spark(p.transform.position, c, 0.5f); Object.Destroy(p.gameObject); }
            foreach (var e in EnemyBase.Active.ToArray())
                if (e != null && !e.IsDead) e.Health.TakeDamage(e is BossEnemy ? 150f : 120f);
        }

        public static void BossDeath(Vector3 pos)
        {
            Runner.StartCoroutine(BossDeathRoutine(pos));
        }

        static IEnumerator BossDeathRoutine(Vector3 pos)
        {
            Explosion(pos, new Color(1f, 0.5f, 0.2f), 3f);
            for (int i = 0; i < 9; i++)
            {
                yield return new WaitForSeconds(0.12f);
                Explosion(pos + Random.insideUnitSphere * 6f, i % 2 == 0 ? new Color(1f, 0.3f, 0.2f) : new Color(1f, 0.8f, 0.3f), Random.Range(1.5f, 2.5f));
                Sfx.Play(SfxId.Explosion, 0.6f, Random.Range(0.6f, 0.9f));
            }
            yield return new WaitForSeconds(0.2f);
            Explosion(pos, Color.white, 6f);
            ChaseCamera.Shake(1.4f);
        }
    }
}
