// Stylised sci-fi hull shader: Lambert + Blinn specular + coloured rim light + emission + hit flash.
// Ambient colour is driven globally by the nebula palette (_NS_Ambient).
Shader "NebulaStriker/ShipRim"
{
    Properties
    {
        _Color ("Base Color", Color) = (0.7, 0.7, 0.75, 1)
        _RimColor ("Rim Color", Color) = (0.3, 0.6, 1, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _Emission ("Emission", Color) = (0, 0, 0, 1)
        _Spec ("Specular", Range(0, 2)) = 0.6
        _Gloss ("Gloss", Range(2, 128)) = 32
        _Flash ("Hit Flash", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"

            fixed4 _Color, _RimColor, _Emission;
            float _RimPower, _Spec, _Gloss, _Flash;
            float4 _NS_Ambient;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float3 n : TEXCOORD0; float3 v : TEXCOORD1; };

            v2f vert (appdata i)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(i.vertex);
                o.n = UnityObjectToWorldNormal(i.normal);
                o.v = WorldSpaceViewDir(i.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.n);
                float3 v = normalize(i.v);
                float3 l = normalize(_WorldSpaceLightPos0.xyz);
                float diff = saturate(dot(n, l));
                float3 h = normalize(l + v);
                float spec = pow(saturate(dot(n, h)), _Gloss) * _Spec;
                float rim = pow(1.0 - saturate(dot(n, v)), _RimPower);

                float3 col = _Color.rgb * (diff * _LightColor0.rgb + _NS_Ambient.rgb + 0.06)
                           + spec * _LightColor0.rgb
                           + _RimColor.rgb * rim
                           + _Emission.rgb;
                col = lerp(col, float3(1, 1, 1), _Flash);
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
    FallBack "VertexLit"
}
