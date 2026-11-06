using UnityEngine;

namespace NebulaStriker
{
    /// <summary>Tumbling hull fragment that shrinks away.</summary>
    public class Debris : MonoBehaviour
    {
        public Vector3 velocity;
        public Vector3 spin;
        public float life = 1.2f;
        float t;
        Vector3 startScale;

        void Start() => startScale = transform.localScale;

        void Update()
        {
            float dt = Time.deltaTime;
            transform.position += velocity * dt;
            velocity *= 1f - dt * 0.8f;
            transform.Rotate(spin * dt);
            t += dt;
            transform.localScale = startScale * Mathf.Clamp01(1f - t / life);
            if (t >= life) Destroy(gameObject);
        }
    }
}
