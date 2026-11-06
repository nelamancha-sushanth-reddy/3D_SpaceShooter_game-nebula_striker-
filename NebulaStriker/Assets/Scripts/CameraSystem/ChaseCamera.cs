using UnityEngine;

namespace NebulaStriker
{
    /// <summary>
    /// Third-person chase camera with partial follow (keeps the play-field framed), look-ahead,
    /// a slow cinematic drift on the menu, and trauma-based screen shake.
    /// </summary>
    public class ChaseCamera : MonoBehaviour
    {
        public Vector3 offset = new Vector3(0f, 3.2f, -17f);
        [Range(0f, 1f)] public float follow = 0.55f;

        static float shake;
        Vector3 basePos;
        Vector3 lookPoint = new Vector3(0f, 0f, 40f);

        public static void Shake(float amount) => shake = Mathf.Clamp(Mathf.Max(shake, amount), 0f, 2f);

        void Start() => basePos = transform.position;

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            var gm = GameManager.Instance;
            Transform target = gm != null ? gm.PlayerTransform : null;

            Vector3 desiredPos, desiredLook;
            if (gm != null && gm.State == GameState.Menu)
            {
                float t = Time.unscaledTime * 0.12f;
                desiredPos = new Vector3(Mathf.Sin(t) * 7f, 4f + Mathf.Sin(t * 0.7f) * 2f, -20f);
                desiredLook = new Vector3(Mathf.Sin(t * 0.5f) * 10f, 2f, 80f);
            }
            else
            {
                Vector3 f = target != null ? target.position : Vector3.zero;
                desiredPos = new Vector3(f.x * follow, f.y * follow, 0f) + offset;
                desiredLook = new Vector3(f.x * 0.7f, f.y * 0.6f, 40f);
            }

            float k = 1f - Mathf.Exp(-5f * dt);
            basePos = Vector3.Lerp(basePos, desiredPos, k);
            lookPoint = Vector3.Lerp(lookPoint, desiredLook, k);

            Vector3 shakeOffset = Vector3.zero;
            if (shake > 0f)
            {
                float s = shake * shake; // trauma squared feels better
                shakeOffset = new Vector3(
                    (Mathf.PerlinNoise(Time.unscaledTime * 25f, 0f) - 0.5f),
                    (Mathf.PerlinNoise(0f, Time.unscaledTime * 25f) - 0.5f), 0f) * 2f * s;
                shake = Mathf.MoveTowards(shake, 0f, dt * 2.2f);
            }

            transform.position = basePos + shakeOffset;
            transform.rotation = Quaternion.LookRotation(lookPoint - basePos);
        }
    }
}
