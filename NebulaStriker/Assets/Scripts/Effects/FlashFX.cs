using UnityEngine;

namespace NebulaStriker
{
    /// <summary>Expanding, fading glow sphere (explosion flash, shockwaves).</summary>
    public class FlashFX : MonoBehaviour
    {
        float life = 0.3f, startScale = 1f, endScale = 4f, t;
        Color color = Color.white;
        Renderer[] renderers;
        MaterialPropertyBlock mpb;

        public void Setup(float lifetime, float fromScale, float toScale, Color c)
        {
            life = Mathf.Max(0.01f, lifetime);
            startScale = fromScale;
            endScale = toScale;
            color = c;
            transform.localScale = Vector3.one * fromScale;
        }

        void Start()
        {
            renderers = GetComponentsInChildren<Renderer>();
            mpb = new MaterialPropertyBlock();
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / life);
            float ease = 1f - (1f - k) * (1f - k);
            transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, ease);
            Color c = color;
            c.a = color.a * (1f - k);
            mpb.SetColor("_Color", c);
            foreach (var r in renderers) if (r != null) r.SetPropertyBlock(mpb);
            if (k >= 1f) Destroy(gameObject);
        }
    }
}
