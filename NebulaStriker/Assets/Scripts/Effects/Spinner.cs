using UnityEngine;

namespace NebulaStriker
{
    /// <summary>Constant rotation (used by drones, asteroids, pickups, kamikazes).</summary>
    public class Spinner : MonoBehaviour
    {
        public Vector3 degreesPerSecond;
        void Update() => transform.Rotate(degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
