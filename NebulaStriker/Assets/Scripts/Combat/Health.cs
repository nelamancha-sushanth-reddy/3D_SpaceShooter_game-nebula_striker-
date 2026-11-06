using UnityEngine;

namespace NebulaStriker
{
    public enum Team { Player, Enemy }

    /// <summary>Reusable hit-point component shared by the player, enemies and asteroids.</summary>
    public class Health : MonoBehaviour
    {
        public Team team = Team.Enemy;
        public float max = 100f;
        public float current = 100f;

        /// <summary>When true, damage is ignored silently (i-frames, barrel roll).</summary>
        public bool invulnerable;

        public System.Action<float> onDamaged;
        public System.Action onDeath;
        /// <summary>Returns true if a shield absorbs the hit.</summary>
        public System.Func<bool> shieldCheck;
        public System.Action onShieldHit;

        bool dead;
        public bool IsDead => dead;
        public float Percent => max <= 0f ? 0f : Mathf.Clamp01(current / max);

        public void Init(float maxHp)
        {
            max = maxHp;
            current = maxHp;
            dead = false;
        }

        public void TakeDamage(float amount)
        {
            if (dead || amount <= 0f || invulnerable) return;
            if (shieldCheck != null && shieldCheck())
            {
                onShieldHit?.Invoke();
                return;
            }
            current -= amount;
            onDamaged?.Invoke(amount);
            if (current <= 0f)
            {
                current = 0f;
                dead = true;
                onDeath?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (dead) return;
            current = Mathf.Min(max, current + amount);
        }
    }
}
