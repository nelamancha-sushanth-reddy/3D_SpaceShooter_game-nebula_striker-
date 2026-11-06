using UnityEngine;

namespace NebulaStriker
{
    /// <summary>
    /// Player ship: smooth inertial movement on the X/Y combat plane, banking/pitching visuals,
    /// barrel-roll dodge (i-frames), shield & damage blink, ramming and pickup collection.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class PlayerController : MonoBehaviour
    {
        public float moveSpeed = 20f;
        public Vector2 bounds = new Vector2(15f, 8.5f);
        public const float RollDuration = 0.45f;
        public const float RollCooldown = 1.4f;

        public bool IsAlive => health != null && !health.IsDead;
        public Health Health => health;
        public PlayerWeapons Weapons => weapons;
        public float ShieldTimeLeft => shieldTime;
        public bool RollReady => rollCd <= 0f;

        Health health;
        PlayerWeapons weapons;
        Transform model;
        Transform reticles;
        GameObject shield;
        Renderer[] modelRenderers;
        Collider col;

        Vector3 velocity;
        Quaternion bank = Quaternion.identity;
        float invuln, shieldTime, rollT, rollCd, rollDir = 1f;

        void Awake()
        {
            health = GetComponent<Health>();
            weapons = GetComponent<PlayerWeapons>();
            col = GetComponent<Collider>();
            model = transform.Find("Model");
            reticles = transform.Find("Reticles");
            modelRenderers = model.GetComponentsInChildren<Renderer>(true);
            shield = ShipFactory.CreateShieldBubble(transform);
            shield.SetActive(false);

            health.onDamaged += OnDamaged;
            health.onDeath += OnDeath;
            health.shieldCheck = () => shieldTime > 0f;
            health.onShieldHit = () =>
            {
                Sfx.Play(SfxId.Shield, 0.6f);
                ChaseCamera.Shake(0.15f);
            };
        }

        void Update()
        {
            if (!IsAlive) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;
            float dt = Time.deltaTime;

            // ---- Movement ----
            Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (input.sqrMagnitude > 1f) input.Normalize();
            Vector3 target = new Vector3(input.x, input.y, 0f) * moveSpeed;
            velocity = Vector3.Lerp(velocity, target, 1f - Mathf.Exp(-10f * dt));

            // ---- Barrel roll dodge ----
            if (rollCd > 0f) rollCd -= dt;
            bool rollPressed = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift) || Input.GetKeyDown(KeyCode.K);
            if (rollPressed && rollT <= 0f && rollCd <= 0f)
            {
                rollT = RollDuration;
                rollCd = RollCooldown;
                rollDir = Mathf.Abs(input.x) > 0.1f ? Mathf.Sign(input.x) : 1f;
                Sfx.Play(SfxId.Roll, 0.6f);
            }
            float rollAngle = 0f;
            Vector3 dash = Vector3.zero;
            if (rollT > 0f)
            {
                rollT -= dt;
                float k = 1f - Mathf.Clamp01(rollT / RollDuration);
                rollAngle = -360f * rollDir * Mathf.SmoothStep(0f, 1f, k);
                dash = new Vector3(rollDir * 16f * (1f - k), 0f, 0f);
            }

            Vector3 p = transform.position + (velocity + dash) * dt;
            if (Mathf.Abs(p.x) > bounds.x) { p.x = Mathf.Sign(p.x) * bounds.x; velocity.x = 0f; }
            if (Mathf.Abs(p.y) > bounds.y) { p.y = Mathf.Sign(p.y) * bounds.y; velocity.y = 0f; }
            p.z = 0f;
            transform.position = p;

            // ---- Visual banking ----
            Quaternion targetBank = Quaternion.Euler(-velocity.y * 1.6f, velocity.x * 0.6f, -velocity.x * 2.8f);
            bank = Quaternion.Slerp(bank, targetBank, 1f - Mathf.Exp(-12f * dt));
            model.localRotation = bank * Quaternion.AngleAxis(rollAngle, Vector3.forward);

            // ---- Invulnerability blink ----
            if (invuln > 0f)
            {
                invuln -= dt;
                SetVisible(invuln <= 0f || Mathf.Repeat(invuln * 12f, 1f) > 0.35f);
            }
            health.invulnerable = invuln > 0f || rollT > 0f;

            // ---- Shield ----
            if (shieldTime > 0f)
            {
                shieldTime -= dt;
                bool on = shieldTime > 2f || Mathf.Repeat(shieldTime * 8f, 1f) > 0.3f;
                shield.SetActive(shieldTime > 0f && on);
            }
            else if (shield.activeSelf) shield.SetActive(false);

            weapons.Tick(dt);
        }

        void SetVisible(bool v)
        {
            foreach (var r in modelRenderers) if (r != null) r.enabled = v;
        }

        void OnDamaged(float amount)
        {
            invuln = 1.1f;
            ChaseCamera.Shake(0.45f);
            Sfx.Play(SfxId.Explosion, 0.5f, 1.6f);
            FX.Spark(transform.position, new Color(1f, 0.5f, 0.3f), 1.5f);
            GameManager.Instance.OnPlayerHit();
        }

        void OnDeath()
        {
            SetVisible(false);
            if (reticles != null) reticles.gameObject.SetActive(false);
            shield.SetActive(false);
            if (col != null) col.enabled = false;
            FX.Explosion(transform.position, new Color(0.4f, 0.8f, 1f), 3f);
            Sfx.Play(SfxId.BigExplosion, 1f);
            ChaseCamera.Shake(1.2f);
            GameManager.Instance.OnPlayerDied();
        }

        // ---- Power-ups ----
        public void Heal(float amount) => health.Heal(amount);
        public void AddShield(float seconds) => shieldTime = Mathf.Max(shieldTime, seconds);

        void OnTriggerEnter(Collider other)
        {
            if (!IsAlive) return;
            var pu = other.GetComponentInParent<PowerUp>();
            if (pu != null) { pu.Collect(this); return; }

            var enemy = other.GetComponentInParent<EnemyBase>();
            if (enemy != null && !enemy.IsDead)
            {
                health.TakeDamage(enemy.contactDamage);
                enemy.OnRammed();
            }
        }
    }
}
