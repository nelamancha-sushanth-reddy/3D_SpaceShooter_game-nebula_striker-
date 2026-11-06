using System.Collections.Generic;
using UnityEngine;

namespace NebulaStriker
{
    /// <summary>
    /// Laser bolt / plasma orb. Uses a sphere-cast along its path each frame (no tunnelling at high speed)
    /// and damages the first Health of the opposite team it touches.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        public static readonly List<Projectile> Active = new List<Projectile>();
        static readonly RaycastHit[] hitBuffer = new RaycastHit[16];

        public Team owner;
        public float damage;
        public Vector3 velocity;
        public Color color;
        public float radius = 0.35f;
        public float life = 2.5f;

        public void Init(Team o, float dmg, Vector3 vel, Color c, float r)
        {
            owner = o;
            damage = dmg;
            velocity = vel;
            color = c;
            radius = r;
        }

        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector3 step = velocity * dt;
            float dist = step.magnitude;
            if (dist > 0.0001f)
            {
                int n = Physics.SphereCastNonAlloc(transform.position, radius, step / dist, hitBuffer, dist, ~0, QueryTriggerInteraction.Collide);
                Health best = null;
                float bestD = float.MaxValue;
                Vector3 bestP = transform.position;
                for (int i = 0; i < n; i++)
                {
                    var hit = hitBuffer[i];
                    if (hit.collider == null) continue;
                    var h = hit.collider.GetComponentInParent<Health>();
                    if (h == null || h.team == owner || h.IsDead) continue;
                    if (hit.distance < bestD)
                    {
                        bestD = hit.distance;
                        best = h;
                        bestP = hit.distance <= 0f ? transform.position : hit.point;
                    }
                }
                if (best != null)
                {
                    best.TakeDamage(damage);
                    FX.Spark(bestP, color, owner == Team.Player ? 0.8f : 1.1f);
                    Sfx.Play(SfxId.Hit, 0.25f, Random.Range(0.9f, 1.2f));
                    Destroy(gameObject);
                    return;
                }
            }

            transform.position += step;
            life -= dt;
            if (life <= 0f || transform.position.z > 220f || transform.position.z < -40f)
                Destroy(gameObject);
        }
    }
}
