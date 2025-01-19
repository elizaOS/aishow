Shader "Custom/StandardDoubleSided"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        Cull Off  // Disable culling for double-sided rendering

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _OcclusionMap;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float facing : VFACE;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float _BumpScale;
        half _OcclusionStrength;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Sample the albedo texture
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            
            // Sample and apply normal map
            float3 normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
            normal.xy *= _BumpScale;
            
            // Flip normal if viewing backface
            if (IN.facing < 0) {
                normal.z *= -1.0;
            }
            o.Normal = normalize(normal);

            // Apply material properties
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            
            // Apply ambient occlusion
            o.Occlusion = lerp(1, tex2D(_OcclusionMap, IN.uv_MainTex).g, _OcclusionStrength);
            
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}