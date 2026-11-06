using UnityEngine;

namespace NebulaStriker
{
    /// <summary>
    /// Owns the procedural nebula skybox material and smoothly cross-fades between colour palettes
    /// (a new sector every 5 waves). Also tints the key light and global ambient to match the sky.
    /// </summary>
    public class NebulaController : MonoBehaviour
    {
        struct Palette
        {
            public Color a, b, c, ambient, light;
            public Palette(Color a, Color b, Color c, Color ambient, Color light)
            { this.a = a; this.b = b; this.c = c; this.ambient = ambient; this.light = light; }
        }

        static readonly Palette[] Palettes =
        {
            // Sector 1 – Violet Veil
            new Palette(new Color(0.55f, 0.15f, 0.75f), new Color(0.05f, 0.25f, 0.65f), new Color(1f, 0.35f, 0.55f),
                        new Color(0.12f, 0.08f, 0.2f), new Color(1f, 0.92f, 0.95f)),
            // Sector 2 – Emerald Drift
            new Palette(new Color(0.1f, 0.6f, 0.5f), new Color(0.02f, 0.15f, 0.35f), new Color(1f, 0.75f, 0.3f),
                        new Color(0.05f, 0.14f, 0.14f), new Color(0.9f, 1f, 0.92f)),
            // Sector 3 – Crimson Forge
            new Palette(new Color(0.8f, 0.2f, 0.1f), new Color(0.25f, 0.03f, 0.2f), new Color(1f, 0.6f, 0.2f),
                        new Color(0.18f, 0.06f, 0.06f), new Color(1f, 0.85f, 0.75f)),
            // Sector 4 – Glacier Rift
            new Palette(new Color(0.3f, 0.5f, 1f), new Color(0.1f, 0.05f, 0.3f), new Color(0.8f, 0.4f, 1f),
                        new Color(0.07f, 0.09f, 0.2f), new Color(0.85f, 0.92f, 1f)),
        };

        Material sky;
        Light sun;
        Palette current, target;

        public void Init(Light keyLight, Camera cam)
        {
            sun = keyLight;
            var shader = Shader.Find("NebulaStriker/NebulaSkybox");
            if (shader != null)
            {
                sky = new Material(shader) { name = "NebulaSky" };
                RenderSettings.skybox = sky;
            }
            else
            {
                Debug.LogWarning("[Nebula Striker] Nebula skybox shader missing – using solid background.");
                cam.clearFlags = CameraClearFlags.SolidColor;
            }
            current = target = Palettes[0];
            Apply();
        }

        public void SetPalette(int index)
        {
            target = Palettes[Mathf.Abs(index) % Palettes.Length];
        }

        void Update()
        {
            float k = 1f - Mathf.Exp(-0.8f * Time.unscaledDeltaTime);
            current.a = Color.Lerp(current.a, target.a, k);
            current.b = Color.Lerp(current.b, target.b, k);
            current.c = Color.Lerp(current.c, target.c, k);
            current.ambient = Color.Lerp(current.ambient, target.ambient, k);
            current.light = Color.Lerp(current.light, target.light, k);
            Apply();
        }

        void Apply()
        {
            if (sky != null)
            {
                sky.SetColor("_ColorA", current.a);
                sky.SetColor("_ColorB", current.b);
                sky.SetColor("_ColorC", current.c);
            }
            Shader.SetGlobalColor("_NS_Ambient", current.ambient);
            if (sun != null) sun.color = current.light;
        }
    }
}
