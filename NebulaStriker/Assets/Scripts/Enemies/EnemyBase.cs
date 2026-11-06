using System.Collections.Generic;
using UnityEngine;

namespace NebulaStriker
{
    /// <summary>
    /// Base class for every hostile object (template-method pattern: subclasses implement Tick()).
    /// Handles registration, hit flash, off-screen cleanup, scoring, explosion and loot drops.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public abstract class EnemyBase : MonoBehaviour
    {
        public static readonly List<EnemyBase> Active = new List<EnemyBase>();

        public int scoreValue = 100;
        public float contactDamage = 20f;
        public float speed = 14f;
        public Color fxColor = new Color(1f, 0.5f, 0.2f);
        public float explosionScale = 1f;
        public float dropChance = 0.1f;

        protected Health health;
        protected float age;
        Renderer[] renderers;
        MaterialPropertyBlock mpb;
        float flash;

        public bool IsDead => health == null || health.IsDead;
        public Health Health => health;
        protected Transform Player => GameManager.Instance != null ? GameManager.Instance.PlayerTransform : null;

        protected virtual void Awake()
        {
            health = GetComponent<Health>();
            renderers = GetComponentsInChildren<MeshRenderer>();
            mpb = new MaterialPropertyBlock();
            health.onDeath += Die;
            health.onDamaged += _ => { flash = 0.07f; SetFlash(1f); };
        }

        protected virtual void OnEnable() => Active.Add(this);
        protected virtual void OnDisable() => Active.Remove(this);

        protected virtual void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;
            Tick(dt);

            if (flash > 0f)
            {
                flash -= dt;
                if (flash <= 0f) SetFlash(0f);
            }

            Vector3 p = transform.position;
            if (p.z < -30f || p.z > 260f || Mathf.Abs(p.x) > 90f || Mathf.Abs(p.y) > 70f)
                Destroy(gameObject);
        }

        protected abstract void Tick(float dt);

        void SetFlash(float v)
        {
            mpb.SetFloat("_Flash", v);
            foreach (var r in renderers) if (r != null) r.SetPropertyBlock(mpb);
        }

        protected void FireAt(Vector3 from, Vector3 target, float shotSpeed, float damage)
        {
            Vector3 dir = target - from;
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.back;
            ShipFactory.CreateShot(from, dir.normalized, ShipFactory.EnemyShotColor, Team.Enemy, damage, shotSpeed, true);
            Sfx.Play(SfxId.EnemyShot, 0.28f, Random.Range(0.9f, 1.1f));
        }

        protected virtual void Die()
        {
            if (GameManager.Instance != null) GameManager.Instance.AddScore(scoreValue);
            FX.Explosion(transform.position, fxColor, explosionScale);
            Sfx.Play(SfxId.Explosion, Mathf.Clamp(0.45f * explosionScale, 0.3f, 0.9f), Random.Range(0.85f, 1.15f));
            ChaseCamera.Shake(0.12f * explosionScale);
            if (Random.value < dropChance)
                ShipFactory.CreatePowerUp(transform.position, PowerUp.RandomType());
            Destroy(gameObject);
        }

        /// <summary>Called when the player physically crashes into this enemy.</summary>
        public virtual void OnRammed() => health.TakeDamage(9999f);
    }
}
