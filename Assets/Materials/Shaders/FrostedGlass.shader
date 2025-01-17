Shader "Custom/FrostedGlass"
{
    Properties
    {
        _Color ("Base Color", Color) = (1,1,1,0.5)
        _MainTex ("Texture", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _Distortion ("Distortion Intensity", Range(0, 1)) = 0.1
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard alpha:fade

        sampler2D _MainTex;
        sampler2D _NormalMap;
        float4 _Color;
        float _Distortion;
        float _Smoothness;
        float _Metallic;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NormalMap;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Base color and transparency
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;

            // Normal distortion for frosted effect
            float3 normal = UnpackNormal(tex2D(_NormalMap, IN.uv_NormalMap));
            float2 distortion = normal.xy * _Distortion;
            fixed4 blurred = tex2D(_MainTex, IN.uv_MainTex + distortion);
            o.Albedo = lerp(o.Albedo, blurred.rgb, _Distortion);

            // Smoothness and Metallic for reflection
            o.Smoothness = _Smoothness;
            o.Metallic = _Metallic;
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
}
