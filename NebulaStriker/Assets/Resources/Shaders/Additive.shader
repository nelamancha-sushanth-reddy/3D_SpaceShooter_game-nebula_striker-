// Additive glow shader used for lasers, engine flames, particles, shields and planet atmosphere.
// _Soft = 1 fades the mesh by view angle (center glow, or rim glow when _RimMode = 1).
Shader "NebulaStriker/Additive"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" {}
        _Intensity ("Intensity", Float) = 2
        _UseVertexColor ("Use Vertex Color (particles)", Float) = 0
        _Soft ("Soft Edge Amount", Float) = 0
        _SoftPower ("Soft Edge Power", Float) = 1.5
        _RimMode ("Rim Mode (0 = center, 1 = rim)", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Intensity, _UseVertexColor, _Soft, _SoftPower, _RimMode;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                float3 n : TEXCOORD1;
                float3 v : TEXCOORD2;
            };

            v2f vert (appdata i)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(i.vertex);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                o.color = i.color;
                o.n = UnityObjectToWorldNormal(i.normal);
                o.v = WorldSpaceViewDir(i.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                fixed4 vc = lerp(fixed4(1, 1, 1, 1), i.color, _UseVertexColor);
                float facing = abs(dot(normalize(i.n), normalize(i.v)));
                float centerGlow = pow(facing, _SoftPower);
                float rimGlow = pow(1.0 - facing, _SoftPower);
                float shape = lerp(centerGlow, rimGlow, _RimMode);
                float soft = lerp(1.0, shape, _Soft);

                fixed4 c = tex * _Color * vc;
                c.rgb *= _Intensity;
                c.a *= soft;
                return c;
            }
            ENDCG
        }
    }
}
