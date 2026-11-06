using UnityEngine;

namespace NebulaStriker
{
    /// <summary>
    /// Twin wing-tip lasers that converge on the aiming reticle, a Rapid-Fire spread upgrade,
    /// and the screen-clearing Nova Bomb.
    /// </summary>
    public class PlayerWeapons : MonoBehaviour
    {
        public float fireInterval = 0.12f;
        public float damage = 10f;
        public float laserSpeed = 115f;
        public float convergeDistance = 45f;
        public const int MaxBombs = 5;

        public int Bombs { get; private set; } = 3;
        public float RapidTime { get; private set; }

        Transform[] muzzles;
        float cooldown, bombCooldown;
        int side;

        public void Setup(Transform[] muzzlePoints) => muzzles = muzzlePoints;
        public void AddRapid(float seconds) => RapidTime = Mathf.Max(RapidTime, seconds);
        public void AddBomb() => Bombs = Mathf.Min(Bombs + 1, MaxBombs);

        public void Tick(float dt)
        {
            cooldown -= dt;
            bombCooldown -= dt;
            if (RapidTime > 0f) RapidTime -= dt;

            bool fire = Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0) || Input.GetKey(KeyCode.J);
            if (fire && cooldown <= 0f)
            {
                Fire();
                cooldown = RapidTime > 0f ? fireInterval * 0.55f : fireInterval;
            }

            bool bomb = Input.GetKeyDown(KeyCode.B) || Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.L);
            if (bomb && Bombs > 0 && bombCooldown <= 0f)
            {
                Bombs--;
                bombCooldown = 1f;
                FX.NovaBomb(transform.position);
            }
        }

        void Fire()
        {
            if (muzzles == null || muzzles.Length == 0) return;
            Vector3 aim = transform.position + Vector3.forward * convergeDistance;

            Transform m = muzzles[side % muzzles.Length];
            side++;
            ShipFactory.CreateShot(m.position, (aim - m.position).normalized, ShipFactory.PlayerLaserColor, Team.Player, damage, laserSpeed, false);

            if (RapidTime > 0f)
            {
                // Extra spread pair from the nose
                Vector3 nose = transform.position + new Vector3(0f, 0.1f, 1.8f);
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector3 dir = Quaternion.Euler(0f, 5f * s, 0f) * Vector3.forward;
                    ShipFactory.CreateShot(nose, dir, new Color(0.5f, 1f, 0.6f), Team.Player, damage * 0.7f, laserSpeed, false);
                }
            }
            Sfx.Play(SfxId.Laser, 0.32f, Random.Range(0.94f, 1.06f));
        }
    }
}
