using UnityEngine;

namespace NebulaStriker
{
    /// <summary>Basic saucer drone: flies straight at the player while weaving on a sine/cosine path.</summary>
    public class DroneEnemy : EnemyBase
    {
        float phase, amplitude, frequency;
        Vector3 basePos;
        bool started;

        protected override void Awake()
        {
            base.Awake();
            phase = Random.value * Mathf.PI * 2f;
            amplitude = Random.Range(1.5f, 4f);
            frequency = Random.Range(1f, 2f);
        }

        /// <summary>Used by formations so every drone in the V weaves together.</summary>
        public void SetPattern(float ph, float amp, float freq)
        {
            phase = ph; amplitude = amp; frequency = freq;
        }

        protected override void Tick(float dt)
        {
            if (!started) { basePos = transform.position; started = true; }
            basePos.z -= speed * dt;
            Vector3 p = basePos;
            p.x += Mathf.Sin(age * frequency + phase) * amplitude;
            p.y += Mathf.Cos(age * frequency * 0.7f + phase) * amplitude * 0.5f;
            transform.position = p;
        }
    }
}
