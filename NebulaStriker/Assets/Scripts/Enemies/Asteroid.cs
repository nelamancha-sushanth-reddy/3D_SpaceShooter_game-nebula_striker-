using UnityEngine;

namespace NebulaStriker
{
    /// <summary>Drifting, tumbling space rock. Destructible obstacle that rewards a few points.</summary>
    public class Asteroid : EnemyBase
    {
        Vector3 velocity;

        public void Setup(Vector3 vel, float size)
        {
            velocity = vel;
            explosionScale = size * 0.55f;
            scoreValue = Mathf.RoundToInt(20f * size);
            contactDamage = 10f * size;
            fxColor = new Color(1f, 0.6f, 0.3f);
            dropChance = 0.06f;
        }

        protected override void Tick(float dt)
        {
            transform.position += velocity * dt;
        }
    }
}
