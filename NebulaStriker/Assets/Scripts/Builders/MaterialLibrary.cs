using System.Collections.Generic;
using UnityEngine;

namespace NebulaStriker
{
    /// <summary>Creates and caches every runtime material so identical looks share one Material (fewer draw-state changes).</summary>
    public static class MaterialLibrary
    {
        static Shader shipShader, glowShader;
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static Material particleMat;
        static Texture2D softTex;

        public static void Init()
        {
            shipShader = Shader.Find("NebulaStriker/ShipRim");
            if (shipShader == null)
            {
                Debug.LogWarning("[Nebula Striker] ShipRim shader not found, falling back to Standard.");
                shipShader = Shader.Find("Standard");
            }
            glowShader = Shader.Find("NebulaStriker/Additive");
            if (glowShader == null)
            {
                Debug.LogWarning("[Nebula Striker] Additive shader not found, falling back to legacy particles.");
                glowShader = Shader.Find("Legacy Shaders/Particles/Additive");
            }
            cache.Clear();
            particleMat = null;
            if (softTex == null) softTex = BuildSoftTexture(64);
        }

        public static Material Ship(Color baseCol, Color rim, Color emission, float rimPower = 3f)
        {
            string key = "S" + baseCol + rim + emission + rimPower;
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(shipShader) { name = "Ship_" + cache.Count };
            if (m.HasProperty("_Color")) m.SetColor("_Color", baseCol);
            if (m.HasProperty("_RimColor")) m.SetColor("_RimColor", rim);
            if (m.HasProperty("_Emission")) m.SetColor("_Emission", emission);
            if (m.HasProperty("_RimPower")) m.SetFloat("_RimPower", rimPower);
            if (m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission + rim * 0.25f);
            }
            cache[key] = m;
            return m;
        }

        /// <param name="soft">1 = fade by view angle</param>
        /// <param name="rimMode">0 = bright centre, 1 = bright rim (shields / atmosphere)</param>
        public static Material Glow(Color c, float intensity = 2f, float soft = 0f, float rimMode = 0f, float softPower = 1.5f)
        {
            string key = "G" + c + intensity + soft + rimMode + softPower;
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(glowShader) { name = "Glow_" + cache.Count };
            m.SetColor("_Color", c);
            if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", c);
            if (m.HasProperty("_Intensity")) m.SetFloat("_Intensity", intensity);
            if (m.HasProperty("_Soft")) m.SetFloat("_Soft", soft);
            if (m.HasProperty("_RimMode")) m.SetFloat("_RimMode", rimMode);
            if (m.HasProperty("_SoftPower")) m.SetFloat("_SoftPower", softPower);
            if (m.HasProperty("_UseVertexColor")) m.SetFloat("_UseVertexColor", 0f);
            cache[key] = m;
            return m;
        }

        public static Material ParticleMaterial
        {
            get
            {
                if (particleMat != null) return particleMat;
                particleMat = new Material(glowShader) { name = "Particles" };
                particleMat.mainTexture = softTex;
                particleMat.SetColor("_Color", Color.white);
                if (particleMat.HasProperty("_TintColor")) particleMat.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
                if (particleMat.HasProperty("_Intensity")) particleMat.SetFloat("_Intensity", 1.7f);
                if (particleMat.HasProperty("_UseVertexColor")) particleMat.SetFloat("_UseVertexColor", 1f);
                return particleMat;
            }
        }

        static Texture2D BuildSoftTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "SoftParticle"
            };
            var px = new Color[size * size];
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half, dy = (y - half) / half;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - r);
                    a = a * a * (3f - 2f * a);
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }
    }
}
