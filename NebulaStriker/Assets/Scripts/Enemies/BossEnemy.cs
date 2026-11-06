using System.Collections;
using UnityEngine;

namespace NebulaStriker
{
    /// <summary>
    /// Dreadnought boss (every 5th wave). Enters, then cycles three attack patterns:
    /// radial plasma fan, aimed burst from the side cannons, and summoning escorts.
    /// Below 50% HP it becomes enraged (faster attacks, denser fans).
    /// </summary>
    public class BossEnemy : EnemyBase
    {
        public const string DisplayName = "DREADNOUGHT";
        Transform[] cannons;
        Transform core;
        bool entered;
        float attackTimer = 2f;
        int pattern;
        float holdZ = 62f;

        public void Setup(Transform[] cannonPoints, Transform corePoint)
        {
            cannons = cannonPoints;
            core = corePoint;
        }

        bool Enraged => health.Percent < 0.5f;

        protected override void Tick(float dt)
        {
            Vector3 p = transform.position;
            if (!entered)
            {
                p.z = Mathf.MoveTowards(p.z, holdZ, 18f * dt);
                p.y = Mathf.Lerp(p.y, 1.5f, dt);
                if (Mathf.Abs(p.z - holdZ) < 0.1f) entered = true;
                transform.position = p;
                return;
            }

            float t = age;
            p.x = Mathf.Lerp(p.x, Mathf.Sin(t * 0.45f) * 9f, dt * 1.5f);
            p.y = Mathf.Lerp(p.y, 1.5f + Mathf.Sin(t * 0.33f) * 3f, dt * 1.5f);
            transform.position = p;
            transform.rotation = Quaternion.Euler(Mathf.Sin(t * 0.7f) * 4f, 0f, Mathf.Sin(t * 0.45f) * -6f);

            attackTimer -= dt;
            if (attackTimer <= 0f)
            {
                DoAttack(pattern);
                pattern = (pattern + 1) % 3;
                attackTimer = Enraged ? 1.5f : 2.4f;
            }
        }

        void DoAttack(int id)
        {
            switch (id)
            {
                case 0: RadialFan(); break;
                case 1: StartCoroutine(AimedBurst()); break;
                case 2: SummonEscorts(); break;
            }
        }

        void RadialFan()
        {
            int count = Enraged ? 22 : 14;
            Vector3 from = core != null ? core.position : transform.position;
            float offset = Random.value * Mathf.PI;
            for (int i = 0; i < count; i++)
            {
                float a = offset + i * Mathf.PI * 2f / count;
                Vector3 dir = new Vector3(Mathf.Cos(a) * 0.32f, Mathf.Sin(a) * 0.2f, -1f).normalized;
                ShipFactory.CreateShot(from, dir, new Color(1f, 0.3f, 0.5f), Team.Enemy, 14f, 26f, true);
            }
            Sfx.Play(SfxId.EnemyShot, 0.6f, 0.6f);
        }

        IEnumerator AimedBurst()
        {
            int shots = Enraged ? 8 : 5;
            for (int i = 0; i < shots; i++)
            {
                Transform player = Player;
                if (player == null) yield break;
                foreach (var c in cannons) if (c != null) FireAt(c.position, player.position, 40f, 15f);
                yield return new WaitForSeconds(0.18f);
            }
        }

        void SummonEscorts()
        {
            float hp = GameManager.Instance.Difficulty;
            int n = Enraged ? 3 : 2;
            for (int i = 0; i < n; i++)
            {
                Vector3 pos = transform.position + new Vector3(Random.Range(-8f, 8f), Random.Range(-3f, 3f), -6f);
                if (Random.value < 0.5f) ShipFactory.CreateKamikaze(pos, hp);
                else ShipFactory.CreateDrone(pos, hp);
            }
        }

        public override void OnRammed() => health.TakeDamage(60f);

        protected override void Die()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(scoreValue);
                GameManager.Instance.OnBossDefeated();
            }
            FX.BossDeath(transform.position);
            Sfx.Play(SfxId.BigExplosion, 1f, 0.8f);
            ChaseCamera.Shake(1.5f);
            ShipFactory.CreatePowerUp(transform.position + Vector3.left * 3f, PowerUpType.Health);
            ShipFactory.CreatePowerUp(transform.position + Vector3.right * 3f, PowerUp.RandomType());
            Destroy(gameObject);
        }
    }
}
