Shader "Custom/WireframeBowingEffect"
{
    Properties
    {
        _LineColor ("Line Color", Color) = (0, 1, 1, 1) // Emissive outline color
        _BowColor ("Bow Color", Color) = (1, 0, 0, 1)  // Emissive bow color
        _BowStrength ("Bow Strength", Range(0, 1)) = 0.5
        _EmissionStrength ("Emission Strength", Range(0, 10)) = 1.0
    }

    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            float4 _LineColor;
            float4 _BowColor;
            float _BowStrength;
            float _EmissionStrength;

            // Vertex Shader
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                // Apply bowing effect
                float3 center = float3(0, 0, 0);
                float distanceFromCenter = length(o.worldPos - center);
                float3 direction = normalize(o.worldPos - center);
                o.worldPos = center + direction * (distanceFromCenter + _BowStrength);

                return o;
            }

            // Fragment Shader
            fixed4 frag (v2f i) : SV_Target
            {
                return _LineColor * _EmissionStrength;
            }
            ENDCG
        }
    }
}
