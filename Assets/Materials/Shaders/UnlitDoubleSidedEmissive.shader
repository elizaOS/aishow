Shader "Custom/UnlitDoubleSidedEmissive"
{
    Properties
    {
        _MainTex ("Emission Texture (RGB)", 2D) = "white" {}
        _EmissionColor ("Emission Color", Color) = (1, 1, 1, 1)
        _EmissionStrength ("Emission Strength", Float) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Opaque" }
        LOD 100
        Cull Off // Render both sides

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            fixed4 _EmissionColor;
            float _EmissionStrength;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 emissionTex = tex2D(_MainTex, i.uv);
                return (_EmissionColor * emissionTex * _EmissionStrength);
            }
            ENDCG
        }
    }
    FallBack "Unlit/Texture"
}
