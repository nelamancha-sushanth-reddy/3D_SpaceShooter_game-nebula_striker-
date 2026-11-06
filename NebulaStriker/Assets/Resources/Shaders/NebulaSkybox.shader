// Procedural, animated nebula skybox (Built-in Render Pipeline).
// Domain-warped fractal noise for gas clouds + dark dust lanes + bright cores
// and three layers of twinkling stars. No textures required.
Shader "NebulaStriker/NebulaSkybox"
{
    Properties
    {
        _ColorA ("Nebula Primary", Color) = (0.55, 0.15, 0.75, 1)
        _ColorB ("Nebula Secondary", Color) = (0.05, 0.25, 0.65, 1)
        _ColorC ("Nebula Highlight", Color) = (1.0, 0.35, 0.55, 1)
        _SpaceColor ("Deep Space", Color) = (0.008, 0.008, 0.025, 1)
        _Density ("Cloud Density", Range(0, 3)) = 1.35
        _CoreIntensity ("Core Intensity", Range(0, 4)) = 1.6
        _Scale ("Noise Scale", Range(0.5, 8)) = 2.6
        _Drift ("Drift Speed", Range(0, 0.2)) = 0.012
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            fixed4 _ColorA, _ColorB, _ColorC, _SpaceColor;
            float _Density, _CoreIntensity, _Scale, _Drift;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            float hash13(float3 p3)
            {
                p3 = frac(p3 * 0.1031);
                p3 += dot(p3, p3.zyx + 31.32);
                return frac((p3.x + p3.y) * p3.z);
            }

            float vnoise(float3 x)
            {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                float n000 = hash13(i);
                float n100 = hash13(i + float3(1, 0, 0));
                float n010 = hash13(i + float3(0, 1, 0));
                float n110 = hash13(i + float3(1, 1, 0));
                float n001 = hash13(i + float3(0, 0, 1));
                float n101 = hash13(i + float3(1, 0, 1));
                float n011 = hash13(i + float3(0, 1, 1));
                float n111 = hash13(i + float3(1, 1, 1));
                float a = lerp(lerp(n000, n100, f.x), lerp(n010, n110, f.x), f.y);
                float b = lerp(lerp(n001, n101, f.x), lerp(n011, n111, f.x), f.y);
                return lerp(a, b, f.z);
            }

            float fbm(float3 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int k = 0; k < 5; k++)
                {
                    v += a * vnoise(p);
                    p = p * 2.03 + float3(1.7, 9.2, 3.1);
                    a *= 0.5;
                }
                return v;
            }

            float stars(float3 d, float scale, float threshold)
            {
                float3 sp = d * scale;
                float3 cell = floor(sp);
                float h = hash13(cell);
                float3 jitter = float3(hash13(cell + 1.3), hash13(cell + 7.1), hash13(cell + 3.7)) - 0.5;
                float3 fr = frac(sp) - 0.5 - jitter * 0.6;
                float dist = length(fr);
                float s = step(threshold, h) * smoothstep(0.2, 0.0, dist);
                float twinkle = 0.65 + 0.35 * sin(_Time.y * (2.0 + h * 5.0) + h * 60.0);
                float brightness = (h - threshold) / (1.0 - threshold) * 0.8 + 0.4;
                return s * twinkle * brightness;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float t = _Time.y * _Drift;
                float3 p = d * _Scale + float3(t, t * 0.3, -t * 0.6);

                // Domain-warped layers
                float n1 = fbm(p);
                float n2 = fbm(p * 1.9 + float3(n1 * 2.2, n1 * 1.3, n1 * 1.7));
                float n3 = fbm(p * 3.7 - n2 * 1.5);

                float cloud = saturate(n2 * 1.55 - 0.45);
                cloud = cloud * cloud * _Density;

                float3 col = lerp(_ColorB.rgb, _ColorA.rgb, saturate(n1 * 1.8 - 0.45));
                col = lerp(col, _ColorC.rgb, saturate(n3 * 2.2 - 1.05));
                float3 neb = col * cloud;

                // Dark dust lanes
                float dust = saturate(fbm(p * 5.0 + 7.3) * 1.8 - 0.75);
                neb *= 1.0 - dust * 0.65;

                // Glowing cores
                float core = pow(saturate(n1 * n2 * 2.4 - 0.45), 3.0);
                neb += lerp(_ColorA.rgb, float3(1, 1, 1), 0.35) * core * _CoreIntensity;

                // Large-scale galactic band so the sky is not uniform
                float band = saturate(1.0 - abs(d.y * 1.3 + (vnoise(d * 1.5) - 0.5) * 1.4));
                neb *= lerp(0.3, 1.3, band);

                float3 outCol = _SpaceColor.rgb * (0.6 + 0.4 * n1) + neb;

                // Star layers (dimmed slightly behind thick gas)
                float starMask = 1.0 - saturate(cloud * 0.5);
                outCol += stars(d, 90.0, 0.994) * 2.0 * lerp(float3(1, 0.85, 0.7), float3(0.7, 0.85, 1), n3);
                outCol += stars(d, 220.0, 0.985) * float3(1, 0.95, 0.9) * starMask;
                outCol += stars(d, 480.0, 0.97) * 0.55 * float3(0.8, 0.9, 1) * starMask;

                return fixed4(outCol, 1);
            }
            ENDCG
        }
    }
    FallBack Off
}
