using System.Collections.Generic;
using UnityEngine;

namespace NebulaStriker
{
    public enum PowerUpType { Health, RapidFire, Shield, Bomb }

    /// <summary>Floating pickup dropped by destroyed enemies. Gently magnetised towards the player.</summary>
    public class PowerUp : MonoBehaviour
    {
        public static readonly List<PowerUp> Active = new List<PowerUp>();

        public PowerUpType type;
        Vector3 velocity = new Vector3(0f, 0f, -9f);
        float age;
        bool collected;

        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);

        public static PowerUpType RandomType()
        {
            float r = Random.value;
            if (r < 0.32f) return PowerUpType.Health;
            if (r < 0.6f) return PowerUpType.RapidFire;
            if (r < 0.85f) return PowerUpType.Shield;
            return PowerUpType.Bomb;
        }

        public static Color ColorOf(PowerUpType t)
        {
            switch (t)
            {
                case PowerUpType.Health: return new Color(0.3f, 1f, 0.45f);
                case PowerUpType.RapidFire: return new Color(1f, 0.8f, 0.2f);
                case PowerUpType.Shield: return new Color(0.35f, 0.75f, 1f);
                default: return new Color(1f, 0.35f, 0.9f);
            }
        }

        public static string LabelOf(PowerUpType t)
        {
            switch (t)
            {
                case PowerUpType.Health: return "+ HULL REPAIR";
                case PowerUpType.RapidFire: return "+ RAPID FIRE";
                case PowerUpType.Shield: return "+ SHIELD";
                default: return "+ NOVA BOMB";
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;

            var gm = GameManager.Instance;
            Transform player = gm != null ? gm.PlayerTransform : null;
            Vector3 p = transform.position;
            if (player != null && Vector3.Distance(p, player.position) < 9f)
                p = Vector3.MoveTowards(p, player.position, 22f * dt);
            else
                p += velocity * dt;
            p.y += Mathf.Sin(age * 3f) * 0.6f * dt;
            transform.position = p;

            if (p.z < -30f) Destroy(gameObject);
        }

        public void Collect(PlayerController player)
        {
            if (collected) return;
            collected = true;
            switch (type)
            {
                case PowerUpType.Health: player.Heal(35f); break;
                case PowerUpType.RapidFire: player.Weapons.AddRapid(9f); break;
                case PowerUpType.Shield: player.AddShield(8f); break;
                case PowerUpType.Bomb: player.Weapons.AddBomb(); break;
            }
            Color c = ColorOf(type);
            FX.Spark(transform.position, c, 2f);
            Sfx.Play(SfxId.PowerUp, 0.7f);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UI.Toast(LabelOf(type), c);
                GameManager.Instance.AddScore(50);
            }
            Destroy(gameObject);
        }
    }
}
