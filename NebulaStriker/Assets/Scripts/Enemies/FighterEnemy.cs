using UnityEngine;

namespace NebulaStriker
{
    /// <summary>
    /// Gunship: flies in, holds position at mid-range, strafes to track the player and fires
    /// aimed plasma bolts, then breaks off past the player after a while.
    /// </summary>
    public class FighterEnemy : EnemyBase
    {
        Transform[] muzzles;
        float holdZ, fireTimer, leaveTime, phase;
        int muzzleIndex;
        Vector3 lastPos;

        public void Setup(Transform[] m) => muzzles = m;

        protected override void Awake()
        {
            base.Awake();
            holdZ = Random.Range(45f, 70f);
            fireTimer = Random.Range(1.2f, 2.2f);
            leaveTime = Random.Range(9f, 14f);
            phase = Random.value * 6.28f;
        }

        protected override void Tick(float dt)
        {
            Vector3 p = transform.position;
            lastPos = p;
            Transform player = Player;

            if (age < leaveTime)
            {
                p.z = Mathf.MoveTowards(p.z, holdZ, speed * dt);
                float tx = (player != null ? player.position.x : 0f) + Mathf.Sin(age * 0.8f + phase) * 7f;
                float ty = (player != null ? player.position.y : 0f) * 0.6f + Mathf.Cos(age * 0.6f + phase) * 3f;
                p.x = Mathf.Lerp(p.x, tx, dt * 0.9f);
                p.y = Mathf.Lerp(p.y, ty, dt * 0.9f);

                if (player != null && p.z < holdZ + 12f)
                {
                    fireTimer -= dt;
                    if (fireTimer <= 0f)
                    {
                        float diff = GameManager.Instance.Difficulty;
                        Vector3 from = muzzles != null && muzzles.Length > 0 ? muzzles[muzzleIndex++ % muzzles.Length].position : p;
                        FireAt(from, player.position, 30f + diff * 3f, 12f);
                        fireTimer = Random.Range(1.1f, 2.0f) / Mathf.Min(diff, 1.8f);
                    }
                }
            }
            else
            {
                p.z -= speed * 1.8f * dt;
            }

            transform.position = p;
            float lateral = (p.x - lastPos.x) / Mathf.Max(dt, 0.0001f);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, 0f, lateral * 6f), dt * 6f);
        }
    }
}
