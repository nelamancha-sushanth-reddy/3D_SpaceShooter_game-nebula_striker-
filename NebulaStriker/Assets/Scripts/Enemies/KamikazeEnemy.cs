using UnityEngine;

namespace NebulaStriker
{
    /// <summary>Spiked mine that locks on and homes towards the player with limited turn rate.</summary>
    public class KamikazeEnemy : EnemyBase
    {
        public float turnRate = 1.9f; // radians per second
        Vector3 velocity;

        protected override void Awake()
        {
            base.Awake();
            velocity = Vector3.back * speed;
        }

        protected override void Tick(float dt)
        {
            Transform player = Player;
            if (player != null && age > 0.8f && transform.position.z > player.position.z + 4f)
            {
                Vector3 desired = (player.position - transform.position).normalized * speed;
                velocity = Vector3.RotateTowards(velocity, desired, turnRate * dt, speed * dt);
            }
            transform.position += velocity * dt;
        }
    }
}
