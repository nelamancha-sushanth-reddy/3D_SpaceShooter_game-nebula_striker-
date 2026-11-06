using System.Collections;
using UnityEngine;

namespace NebulaStriker
{
    /// <summary>
    /// Drives the wave progression: escalating enemy mixes, formations and a boss every 5th wave.
    /// Also spawns ambient asteroids (also during the menu for a living background).
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        public const float SpawnZ = 135f;
        public static readonly Vector2 SpawnArea = new Vector2(13f, 7f);

        public int CurrentWave { get; private set; }
        public BossEnemy Boss { get; private set; }

        Coroutine run;
        float asteroidTimer = 1f;

        public void BeginRun()
        {
            StopRun();
            CurrentWave = 0;
            run = StartCoroutine(RunWaves());
        }

        public void StopRun()
        {
            if (run != null) StopCoroutine(run);
            run = null;
            Boss = null;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (gm.State != GameState.Playing && gm.State != GameState.Menu) return;

            asteroidTimer -= Time.deltaTime;
            if (asteroidTimer <= 0f)
            {
                asteroidTimer = gm.State == GameState.Menu ? Random.Range(0.6f, 1.4f) : Random.Range(1.4f, 3.2f);
                float size = Random.Range(1.2f, 3.4f);
                var pos = RandomSpawnPos(1.6f);
                var vel = new Vector3(Random.Range(-2f, 2f), Random.Range(-1.2f, 1.2f), -Random.Range(14f, 24f));
                ShipFactory.CreateAsteroid(pos, size, vel);
            }
        }

        public static Vector3 RandomSpawnPos(float spread = 1f)
        {
            return new Vector3(Random.Range(-SpawnArea.x, SpawnArea.x) * spread,
                               Random.Range(-SpawnArea.y, SpawnArea.y) * spread,
                               SpawnZ + Random.Range(0f, 15f));
        }

        IEnumerator RunWaves()
        {
            yield return new WaitForSeconds(1.2f);
            while (true)
            {
                CurrentWave++;
                GameManager.Instance.OnWaveStarted(CurrentWave);
                yield return new WaitForSeconds(2.5f);

                if (CurrentWave % 5 == 0)
                {
                    Boss = ShipFactory.CreateBoss(CurrentWave);
                    while (Boss != null) yield return null;
                    yield return new WaitForSeconds(2.5f);
                }
                else
                {
                    int quota = 6 + CurrentWave * 2;
                    int spawned = 0;
                    float gap = Mathf.Max(0.45f, 1.4f - CurrentWave * 0.08f);
                    while (spawned < quota)
                    {
                        spawned += SpawnGroup();
                        yield return new WaitForSeconds(gap * Random.Range(0.8f, 1.4f));
                    }
                    // Wait for the wave to be cleared (or time out so the pace never stalls)
                    float t = 0f;
                    while (HostileCount() > 0 && t < 20f) { t += Time.deltaTime; yield return null; }
                }
                yield return new WaitForSeconds(1.5f);
            }
        }

        int SpawnGroup()
        {
            int w = CurrentWave;
            float hp = GameManager.Instance.Difficulty;
            float r = Random.value;

            if (w >= 3 && r < 0.2f)
            {
                int n = w >= 6 ? 3 : 2;
                for (int i = 0; i < n; i++) ShipFactory.CreateKamikaze(RandomSpawnPos(), hp);
                return n;
            }
            if (w >= 2 && r < 0.48f)
            {
                ShipFactory.CreateFighter(RandomSpawnPos(0.8f), hp);
                return 1;
            }
            if (r < 0.7f)
            {
                // V formation of drones sharing the same weave pattern
                Vector3 c = RandomSpawnPos(0.6f);
                float phase = Random.value * 6.28f, amp = Random.Range(1.5f, 3.5f), freq = Random.Range(0.9f, 1.6f);
                for (int i = -2; i <= 2; i++)
                {
                    var d = ShipFactory.CreateDrone(c + new Vector3(i * 3.2f, -Mathf.Abs(i) * 1.4f, Mathf.Abs(i) * 5f), hp);
                    d.SetPattern(phase, amp, freq);
                }
                return 5;
            }
            ShipFactory.CreateDrone(RandomSpawnPos(), hp);
            return 1;
        }

        static int HostileCount()
        {
            int n = 0;
            foreach (var e in EnemyBase.Active) if (e != null && !(e is Asteroid)) n++;
            return n;
        }
    }
}
