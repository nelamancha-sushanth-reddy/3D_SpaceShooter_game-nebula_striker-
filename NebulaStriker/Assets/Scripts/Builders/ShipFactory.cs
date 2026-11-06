using UnityEngine;

namespace NebulaStriker
{
    /// <summary>
    /// Factory that assembles every ship, projectile, asteroid and pickup from Unity primitives
    /// at runtime (no external models needed). Each method returns the gameplay component.
    /// </summary>
    public static class ShipFactory
    {
        public static readonly Color PlayerLaserColor = new Color(0.3f, 0.85f, 1f);
        public static readonly Color EnemyShotColor = new Color(1f, 0.45f, 0.15f);

        // ------------------------------------------------------------------ helpers
        public static GameObject Part(Transform parent, PrimitiveType type, Vector3 pos, Vector3 euler, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = type.ToString();
            var c = go.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = scale;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }

        static GameObject Empty(string name, Transform parent, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go;
        }

        static void AddPhysics(GameObject root, float radius, bool trigger)
        {
            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            var c = root.AddComponent<SphereCollider>();
            c.radius = radius;
            c.isTrigger = trigger;
        }

        static Health AddHealth(GameObject root, Team team, float hp)
        {
            var h = root.AddComponent<Health>();
            h.team = team;
            h.Init(hp);
            return h;
        }

        static void Spin(GameObject go, Vector3 degPerSec) => go.AddComponent<Spinner>().degreesPerSecond = degPerSec;

        // ------------------------------------------------------------------ player
        public static PlayerController CreatePlayer()
        {
            var root = new GameObject("Player");
            root.transform.position = new Vector3(0f, -2f, 0f);
            var model = Empty("Model", root.transform, Vector3.zero).transform;

            var hull = MaterialLibrary.Ship(new Color(0.78f, 0.8f, 0.86f), new Color(0.25f, 0.6f, 1f), Color.black, 2.5f);
            var dark = MaterialLibrary.Ship(new Color(0.16f, 0.18f, 0.23f), new Color(0.2f, 0.45f, 0.9f), Color.black, 3f);
            var accent = MaterialLibrary.Ship(new Color(0.1f, 0.4f, 0.95f), new Color(0.4f, 0.8f, 1f), new Color(0f, 0.12f, 0.3f), 2f);
            var canopy = MaterialLibrary.Ship(new Color(0.02f, 0.06f, 0.12f), new Color(0.5f, 0.95f, 1f), new Color(0.05f, 0.35f, 0.7f), 1.5f);
            var glow = MaterialLibrary.Glow(new Color(0.35f, 0.75f, 1f), 3f, 1f, 0f, 1.2f);
            Color flame = new Color(0.3f, 0.7f, 1f);

            // Fuselage, belly, canopy, spine stripe
            Part(model, PrimitiveType.Capsule, new Vector3(0f, 0f, 0.1f), new Vector3(90f, 0f, 0f), new Vector3(0.95f, 1.9f, 0.65f), hull);
            Part(model, PrimitiveType.Cube, new Vector3(0f, -0.12f, -0.4f), Vector3.zero, new Vector3(1.3f, 0.35f, 2.2f), dark);
            Part(model, PrimitiveType.Sphere, new Vector3(0f, 0.28f, 0.55f), Vector3.zero, new Vector3(0.55f, 0.38f, 1.1f), canopy);
            Part(model, PrimitiveType.Cube, new Vector3(0f, 0.31f, -0.6f), Vector3.zero, new Vector3(0.12f, 0.06f, 1.4f), accent);

            var muzzles = new Transform[2];
            int mi = 0;
            for (int s = -1; s <= 1; s += 2)
            {
                // Swept wings with dihedral and accent stripe
                Part(model, PrimitiveType.Cube, new Vector3(1.35f * s, -0.08f, -0.45f), new Vector3(0f, 14f * s, 5f * s), new Vector3(2.3f, 0.09f, 1.25f), hull);
                Part(model, PrimitiveType.Cube, new Vector3(1.55f * s, -0.02f, -0.6f), new Vector3(0f, 14f * s, 5f * s), new Vector3(1.6f, 0.1f, 0.25f), accent);
                // Wing-tip cannons
                Part(model, PrimitiveType.Cylinder, new Vector3(2.5f * s, -0.12f, 0.05f), new Vector3(90f, 0f, 0f), new Vector3(0.16f, 0.85f, 0.16f), dark);
                Part(model, PrimitiveType.Sphere, new Vector3(2.5f * s, -0.12f, 0.92f), Vector3.zero, Vector3.one * 0.2f, glow);
                muzzles[mi++] = Empty("Muzzle", model, new Vector3(2.5f * s, -0.12f, 1.1f)).transform;
                // Twin canted tail fins
                Part(model, PrimitiveType.Cube, new Vector3(0.45f * s, 0.42f, -1.35f), new Vector3(-25f, 0f, -12f * s), new Vector3(0.08f, 0.7f, 0.9f), hull);
                // Engine nacelles + glowing exhaust
                Part(model, PrimitiveType.Cylinder, new Vector3(0.6f * s, -0.08f, -1.5f), new Vector3(90f, 0f, 0f), new Vector3(0.48f, 0.55f, 0.48f), dark);
                Part(model, PrimitiveType.Sphere, new Vector3(0.6f * s, -0.08f, -2.05f), Vector3.zero, new Vector3(0.42f, 0.42f, 0.2f), glow);
                FX.EngineFlame(model, new Vector3(0.6f * s, -0.08f, -2.1f), Vector3.back, flame, 1f);
            }

            // Aiming reticles (not banked with the model)
            var ret = Empty("Reticles", root.transform, Vector3.zero).transform;
            Reticle(ret, 22f, 1.0f, new Color(0.4f, 1f, 0.6f));
            Reticle(ret, 45f, 1.8f, new Color(0.4f, 1f, 0.6f));

            AddPhysics(root, 1.4f, true);
            AddHealth(root, Team.Player, 100f);
            var weapons = root.AddComponent<PlayerWeapons>();
            weapons.Setup(muzzles);
            return root.AddComponent<PlayerController>();
        }

        static void Reticle(Transform parent, float z, float size, Color c)
        {
            var mat = MaterialLibrary.Glow(c, 1.4f);
            var r = Empty("Reticle", parent, new Vector3(0f, 0f, z)).transform;
            float h = size * 0.5f, t = 0.06f * size, l = size * 0.35f;
            // Four corner brackets
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sy = -1; sy <= 1; sy += 2)
                {
                    Part(r, PrimitiveType.Cube, new Vector3(sx * (h - l * 0.5f), sy * h, 0f), Vector3.zero, new Vector3(l, t, t), mat);
                    Part(r, PrimitiveType.Cube, new Vector3(sx * h, sy * (h - l * 0.5f), 0f), Vector3.zero, new Vector3(t, l, t), mat);
                }
        }

        public static GameObject CreateShieldBubble(Transform parent)
        {
            var go = Empty("Shield", parent, Vector3.zero);
            Part(go.transform, PrimitiveType.Sphere, Vector3.zero, Vector3.zero, new Vector3(6.2f, 3.2f, 5.2f),
                 MaterialLibrary.Glow(new Color(0.3f, 0.7f, 1f, 0.9f), 2f, 1f, 1f, 2.5f));
            return go;
        }

        // ------------------------------------------------------------------ enemies
        public static DroneEnemy CreateDrone(Vector3 pos, float hpMult)
        {
            var root = new GameObject("Drone");
            root.transform.position = pos;
            var model = Empty("Model", root.transform, Vector3.zero).transform;
            model.localRotation = Quaternion.Euler(-65f, 0f, 0f); // tilt the saucer towards the camera
            var spin = Empty("Spin", model, Vector3.zero);
            Spin(spin, new Vector3(0f, 120f, 0f));

            var hull = MaterialLibrary.Ship(new Color(0.35f, 0.08f, 0.1f), new Color(1f, 0.3f, 0.25f), new Color(0.08f, 0f, 0f), 2.2f);
            var metal = MaterialLibrary.Ship(new Color(0.5f, 0.5f, 0.55f), new Color(1f, 0.4f, 0.3f), Color.black, 3f);
            var glow = MaterialLibrary.Glow(new Color(1f, 0.25f, 0.15f), 3f, 1f, 0f, 1.2f);

            Part(spin.transform, PrimitiveType.Sphere, Vector3.zero, Vector3.zero, new Vector3(2.4f, 0.55f, 2.4f), hull);
            Part(spin.transform, PrimitiveType.Cylinder, new Vector3(0f, -0.05f, 0f), Vector3.zero, new Vector3(2.9f, 0.04f, 2.9f), metal);
            Part(spin.transform, PrimitiveType.Sphere, new Vector3(0f, 0.28f, 0f), Vector3.zero, new Vector3(1f, 0.7f, 1f), glow);
            for (int k = 0; k < 4; k++)
            {
                float a = k * Mathf.PI * 0.5f;
                Part(spin.transform, PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * 1.35f, 0f, Mathf.Sin(a) * 1.35f), Vector3.zero, Vector3.one * 0.32f, glow);
            }

            AddPhysics(root, 1.5f, false);
            AddHealth(root, Team.Enemy, 30f * hpMult);
            var e = root.AddComponent<DroneEnemy>();
            e.scoreValue = 100;
            e.speed = Random.Range(13f, 17f);
            e.fxColor = new Color(1f, 0.4f, 0.2f);
            e.contactDamage = 20f;
            e.dropChance = 0.08f;
            return e;
        }

        public static FighterEnemy CreateFighter(Vector3 pos, float hpMult)
        {
            var root = new GameObject("Fighter");
            root.transform.position = pos;
            var model = Empty("Model", root.transform, Vector3.zero).transform;

            var hull = MaterialLibrary.Ship(new Color(0.32f, 0.12f, 0.45f), new Color(0.9f, 0.4f, 1f), new Color(0.04f, 0f, 0.08f), 2.2f);
            var dark = MaterialLibrary.Ship(new Color(0.12f, 0.1f, 0.16f), new Color(0.7f, 0.3f, 1f), Color.black, 3f);
            var glow = MaterialLibrary.Glow(new Color(1f, 0.3f, 0.9f), 3f, 1f, 0f, 1.2f);

            // Nose points towards -Z (towards the player)
            Part(model, PrimitiveType.Capsule, Vector3.zero, new Vector3(90f, 0f, 0f), new Vector3(0.8f, 1.7f, 0.7f), hull);
            Part(model, PrimitiveType.Sphere, new Vector3(0f, 0.22f, -0.9f), Vector3.zero, new Vector3(0.45f, 0.3f, 0.8f), glow);
            var muzzles = new Transform[2];
            int mi = 0;
            for (int s = -1; s <= 1; s += 2)
            {
                Part(model, PrimitiveType.Cube, new Vector3(1.5f * s, 0f, 0.3f), new Vector3(0f, -18f * s, 0f), new Vector3(2.6f, 0.1f, 1f), hull);
                Part(model, PrimitiveType.Cube, new Vector3(2.75f * s, 0f, 0.55f), Vector3.zero, new Vector3(0.1f, 1.2f, 1.4f), dark);
                Part(model, PrimitiveType.Sphere, new Vector3(2.75f * s, 0.62f, 0.55f), Vector3.zero, Vector3.one * 0.22f, glow);
                Part(model, PrimitiveType.Cylinder, new Vector3(1f * s, -0.2f, -0.6f), new Vector3(90f, 0f, 0f), new Vector3(0.18f, 0.6f, 0.18f), dark);
                muzzles[mi++] = Empty("Muzzle", model, new Vector3(1f * s, -0.2f, -1.3f)).transform;
            }
            Part(model, PrimitiveType.Sphere, new Vector3(0f, 0f, 1.75f), Vector3.zero, new Vector3(0.55f, 0.55f, 0.3f), glow);
            FX.EngineFlame(model, new Vector3(0f, 0f, 1.85f), Vector3.forward, new Color(1f, 0.35f, 0.9f), 1f);

            AddPhysics(root, 1.8f, false);
            AddHealth(root, Team.Enemy, 60f * hpMult);
            var e = root.AddComponent<FighterEnemy>();
            e.Setup(muzzles);
            e.scoreValue = 250;
            e.speed = 22f;
            e.fxColor = new Color(0.9f, 0.4f, 1f);
            e.contactDamage = 25f;
            e.dropChance = 0.2f;
            e.explosionScale = 1.3f;
            return e;
        }

        public static KamikazeEnemy CreateKamikaze(Vector3 pos, float hpMult)
        {
            var root = new GameObject("Kamikaze");
            root.transform.position = pos;
            var model = Empty("Model", root.transform, Vector3.zero);
            Spin(model, new Vector3(90f, 140f, 60f));

            var core = MaterialLibrary.Ship(new Color(0.25f, 0.08f, 0.02f), new Color(1f, 0.6f, 0.1f), new Color(0.2f, 0.05f, 0f), 2f);
            var spike = MaterialLibrary.Ship(new Color(0.4f, 0.38f, 0.35f), new Color(1f, 0.5f, 0.1f), Color.black, 3f);
            var glow = MaterialLibrary.Glow(new Color(1f, 0.55f, 0.1f), 2.5f, 1f, 0f, 1f);

            Part(model.transform, PrimitiveType.Sphere, Vector3.zero, Vector3.zero, Vector3.one * 1.3f, core);
            Part(model.transform, PrimitiveType.Sphere, Vector3.zero, Vector3.zero, Vector3.one * 1.9f, glow);
            Vector3[] dirs = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back,
                               new Vector3(1, 1, 1).normalized, new Vector3(-1, -1, 1).normalized,
                               new Vector3(1, -1, -1).normalized, new Vector3(-1, 1, -1).normalized };
            foreach (var d in dirs)
                Part(model.transform, PrimitiveType.Cylinder, d * 0.9f, Quaternion.FromToRotation(Vector3.up, d).eulerAngles, new Vector3(0.16f, 0.5f, 0.16f), spike);

            AddPhysics(root, 1.2f, false);
            AddHealth(root, Team.Enemy, 20f * hpMult);
            var e = root.AddComponent<KamikazeEnemy>();
            e.scoreValue = 150;
            e.speed = 21f;
            e.fxColor = new Color(1f, 0.6f, 0.15f);
            e.contactDamage = 30f;
            e.dropChance = 0.08f;
            return e;
        }

        public static BossEnemy CreateBoss(int wave)
        {
            var root = new GameObject("Dreadnought");
            root.transform.position = new Vector3(0f, 6f, 170f);
            var model = Empty("Model", root.transform, Vector3.zero).transform;

            var armor = MaterialLibrary.Ship(new Color(0.28f, 0.3f, 0.36f), new Color(1f, 0.35f, 0.3f), Color.black, 2.5f);
            var dark = MaterialLibrary.Ship(new Color(0.1f, 0.1f, 0.13f), new Color(0.8f, 0.2f, 0.2f), Color.black, 3f);
            var red = MaterialLibrary.Glow(new Color(1f, 0.2f, 0.1f), 3f, 1f, 0f, 1.2f);
            var orange = MaterialLibrary.Glow(new Color(1f, 0.55f, 0.2f), 3f, 1f, 0f, 1.2f);

            Part(model, PrimitiveType.Sphere, Vector3.zero, Vector3.zero, new Vector3(9f, 3.2f, 7f), armor);
            Part(model, PrimitiveType.Cube, new Vector3(0f, 1.4f, 0.8f), Vector3.zero, new Vector3(4f, 1.2f, 4.5f), armor);
            Part(model, PrimitiveType.Cube, new Vector3(0f, 2.3f, 1.5f), Vector3.zero, new Vector3(1.6f, 0.9f, 1.6f), dark);
            Part(model, PrimitiveType.Cylinder, new Vector3(0f, 0f, -3f), new Vector3(90f, 0f, 0f), new Vector3(3f, 0.3f, 3f), dark);
            Part(model, PrimitiveType.Sphere, new Vector3(0f, 0f, -3.3f), Vector3.zero, new Vector3(2.2f, 2.2f, 1.2f), red);
            var core = Empty("Core", model, new Vector3(0f, 0f, -4f)).transform;

            var cannons = new Transform[2];
            int ci = 0;
            for (int s = -1; s <= 1; s += 2)
            {
                Part(model, PrimitiveType.Cylinder, new Vector3(5.2f * s, -0.4f, 0f), new Vector3(90f, 0f, 0f), new Vector3(2f, 3.8f, 2f), armor);
                Part(model, PrimitiveType.Sphere, new Vector3(5.2f * s, -0.4f, -3.9f), Vector3.zero, new Vector3(1.3f, 1.3f, 0.6f), red);
                Part(model, PrimitiveType.Cylinder, new Vector3(5.2f * s, -0.4f, -4.6f), new Vector3(90f, 0f, 0f), new Vector3(0.5f, 1.2f, 0.5f), dark);
                cannons[ci++] = Empty("Cannon", model, new Vector3(5.2f * s, -0.4f, -5.9f)).transform;
                Part(model, PrimitiveType.Cube, new Vector3(3f * s, 0f, 1.5f), new Vector3(0f, 0f, -8f * s), new Vector3(5f, 0.3f, 3.5f), dark);
                Part(model, PrimitiveType.Cube, new Vector3(1.6f * s, 2f, -0.5f), Vector3.zero, new Vector3(0.3f, 1.2f, 2.5f), dark);
                Part(model, PrimitiveType.Sphere, new Vector3(5.2f * s, -0.4f, 3.9f), Vector3.zero, new Vector3(1.4f, 1.4f, 0.6f), orange);
                FX.EngineFlame(model, new Vector3(5.2f * s, -0.4f, 4.1f), Vector3.forward, new Color(1f, 0.5f, 0.2f), 3f);
            }
            Part(model, PrimitiveType.Sphere, new Vector3(0f, 0f, 3.6f), Vector3.zero, new Vector3(2f, 1.2f, 0.6f), orange);
            FX.EngineFlame(model, new Vector3(0f, 0f, 3.8f), Vector3.forward, new Color(1f, 0.5f, 0.2f), 3.5f);

            int tier = Mathf.Max(1, wave / 5);
            AddPhysics(root, 5.2f, false);
            AddHealth(root, Team.Enemy, 1400f * (1f + (tier - 1) * 0.6f));
            var e = root.AddComponent<BossEnemy>();
            e.Setup(cannons, core);
            e.scoreValue = 5000 * tier;
            e.fxColor = new Color(1f, 0.45f, 0.25f);
            e.contactDamage = 40f;
            e.explosionScale = 4f;
            e.dropChance = 0f;
            return e;
        }

        // ------------------------------------------------------------------ asteroids
        static Mesh[] rockMeshes;

        static Mesh GetRockMesh()
        {
            if (rockMeshes == null || rockMeshes[0] == null)
            {
                rockMeshes = new Mesh[4];
                var tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                var src = tmp.GetComponent<MeshFilter>().sharedMesh;
                for (int i = 0; i < rockMeshes.Length; i++) rockMeshes[i] = DeformRock(src, i * 17.3f + 3.1f);
                Object.DestroyImmediate(tmp);
            }
            return rockMeshes[Random.Range(0, rockMeshes.Length)];
        }

        static Mesh DeformRock(Mesh src, float seed)
        {
            var m = Object.Instantiate(src);
            m.name = "Rock";
            var v = m.vertices;
            for (int i = 0; i < v.Length; i++)
            {
                Vector3 d = v[i].normalized;
                float n = Mathf.PerlinNoise(d.x * 1.7f + seed, d.y * 1.7f + seed * 0.5f) * 0.5f
                        + Mathf.PerlinNoise(d.y * 3.1f + seed, d.z * 3.1f) * 0.3f
                        + Mathf.PerlinNoise(d.z * 6f, d.x * 6f + seed) * 0.15f;
                v[i] = d * (0.5f * (0.65f + n * 0.8f));
            }
            m.vertices = v;
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        public static Asteroid CreateAsteroid(Vector3 pos, float size, Vector3 velocity)
        {
            var root = new GameObject("Asteroid");
            root.transform.position = pos;
            var model = Empty("Model", root.transform, Vector3.zero);
            model.AddComponent<MeshFilter>().sharedMesh = GetRockMesh();
            var mr = model.AddComponent<MeshRenderer>();
            mr.sharedMaterial = MaterialLibrary.Ship(new Color(0.34f, 0.29f, 0.26f), new Color(0.55f, 0.42f, 0.35f), Color.black, 4f);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            model.transform.localScale = new Vector3(Random.Range(0.85f, 1.15f), Random.Range(0.75f, 1.1f), Random.Range(0.85f, 1.2f)) * size;
            model.transform.localRotation = Random.rotation;
            Spin(model, Random.insideUnitSphere * 70f);

            AddPhysics(root, size * 0.45f, false);
            AddHealth(root, Team.Enemy, 12f * size);
            var a = root.AddComponent<Asteroid>();
            a.Setup(velocity, size);
            return a;
        }

        // ------------------------------------------------------------------ projectiles
        public static Projectile CreateShot(Vector3 pos, Vector3 dir, Color color, Team owner, float damage, float speed, bool orb)
        {
            var go = new GameObject(owner == Team.Player ? "PlayerLaser" : "EnemyShot");
            go.transform.position = pos;
            go.transform.rotation = Quaternion.LookRotation(dir);
            var core = MaterialLibrary.Glow(Color.Lerp(color, Color.white, 0.65f), 3f);
            var halo = MaterialLibrary.Glow(color, 2f, 1f, 0f, 1.5f);
            if (orb)
            {
                Part(go.transform, PrimitiveType.Sphere, Vector3.zero, Vector3.zero, Vector3.one * 0.4f, core);
                Part(go.transform, PrimitiveType.Sphere, Vector3.zero, Vector3.zero, new Vector3(1.1f, 1.1f, 1.6f), halo);
            }
            else
            {
                Part(go.transform, PrimitiveType.Capsule, Vector3.zero, new Vector3(90f, 0f, 0f), new Vector3(0.13f, 0.9f, 0.13f), core);
                Part(go.transform, PrimitiveType.Capsule, Vector3.zero, new Vector3(90f, 0f, 0f), new Vector3(0.42f, 1.15f, 0.42f), halo);
            }
            var p = go.AddComponent<Projectile>();
            p.Init(owner, damage, dir.normalized * speed, color, orb ? 0.55f : 0.35f);
            p.life = owner == Team.Player ? 1.8f : 6f;
            return p;
        }

        // ------------------------------------------------------------------ pickups
        public static PowerUp CreatePowerUp(Vector3 pos, PowerUpType type)
        {
            Color c = PowerUp.ColorOf(type);
            var root = new GameObject("PowerUp_" + type);
            root.transform.position = new Vector3(pos.x, pos.y, Mathf.Max(pos.z, 2f));
            var model = Empty("Model", root.transform, Vector3.zero);
            Spin(model, new Vector3(40f, 120f, 0f));
            Part(model.transform, PrimitiveType.Cube, Vector3.zero, new Vector3(45f, 45f, 0f), Vector3.one * 0.8f,
                 MaterialLibrary.Ship(c * 0.4f, c, c * 0.5f, 1.5f));
            Part(model.transform, PrimitiveType.Sphere, Vector3.zero, Vector3.zero, Vector3.one * 1.3f, MaterialLibrary.Glow(c, 2.5f, 1f, 0f, 1.5f));
            Part(model.transform, PrimitiveType.Sphere, Vector3.zero, Vector3.zero, Vector3.one * 2.3f, MaterialLibrary.Glow(c, 1.6f, 1f, 1f, 2f));

            AddPhysics(root, 1.3f, false);
            var pu = root.AddComponent<PowerUp>();
            pu.type = type;
            return pu;
        }
    }
}
