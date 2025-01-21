Shader "Custom/StandardDoubleSidedEmissive"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 1.0
        _OcclusionMap ("Occlusion", 2D) = "white" {}
        _OcclusionStrength ("Occlusion Strength", Range(0.0, 1.0)) = 1.0
        _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        _EmissionMap ("Emission (RGB)", 2D) = "black" {}
        _EmissionStrength ("Emission Strength", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        Cull Off  // Ensure both sides of the mesh are rendered

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _OcclusionMap;
        sampler2D _EmissionMap;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float facing : VFACE; // Face orientation (front or back)
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float _BumpScale;
        half _OcclusionStrength;
        fixed4 _EmissionColor;
        float _EmissionStrength;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Sample the albedo texture
            fixed4 albedo = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = albedo.rgb;

            // Sample and apply the normal map
            float3 normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
            normal.xy *= _BumpScale;

            // Flip the normal for backfaces
            if (IN.facing < 0)
            {
                normal.z *= -1.0;
            }
            o.Normal = normalize(normal);

            // Apply metallic and smoothness
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;

            // Apply ambient occlusion
            o.Occlusion = lerp(1.0, tex2D(_OcclusionMap, IN.uv_MainTex).g, _OcclusionStrength);

            // Emission logic: Always show emission regardless of face orientation
            fixed4 emissionTex = tex2D(_EmissionMap, IN.uv_MainTex);
            float3 emission = (_EmissionColor.rgb * emissionTex.rgb) * _EmissionStrength;
            o.Emission = emission;

            // Alpha channel
            o.Alpha = albedo.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
