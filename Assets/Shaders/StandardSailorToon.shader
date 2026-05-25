Shader "Custom/StandardSailorToon"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Standard PBR lighting + shadow casting/receiving on all light types.
        // addshadow generates a shadow caster pass that also runs vert:vert,
        // so cast shadows bend with the geometry.
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.0

        // Toggled globally by RollingLogManager. multi_compile (not shader_feature)
        // because the keyword is set via Shader.EnableKeyword at runtime.
        #pragma multi_compile _ _ENABLE_ROLLING_LOG
        #pragma multi_compile _ _ROLLING_LOG_SPHERE

        #include "Includes/RollingLog.cginc"

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        void vert(inout appdata_full v)
        {
            #ifdef _ENABLE_ROLLING_LOG
            float3 worldPos    = mul(unity_ObjectToWorld, v.vertex).xyz;
            float3 worldNormal = UnityObjectToWorldNormal(v.normal);

            worldNormal = ApplyRollingLogNormal(worldNormal, worldPos);
            worldPos    = ApplyRollingLog(worldPos);

            v.vertex = mul(unity_WorldToObject, float4(worldPos, 1.0));
            v.normal = normalize(mul((float3x3)unity_WorldToObject, worldNormal));
            #endif
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
