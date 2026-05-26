Shader "Custom/StandardSailorToon"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        [Enum(Off,0,Front,1,Back,2)] _Cull ("Cull Mode", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Cull [_Cull]

        CGPROGRAM
        #pragma surface surf Toon fullforwardshadows noambient novertexlights noforwardadd vertex:vert addshadow
        #pragma target 3.0

        #pragma multi_compile _ _HALFTONE_ON
        #pragma multi_compile _ _ENABLE_ROLLING_LOG
        #pragma multi_compile _ _ROLLING_LOG_SPHERE

        #include "Includes/RollingLog.cginc"
        #include "Includes/ToonLighting.cginc"

        sampler2D _MainTex;
        fixed4 _Color;

        struct Input
        {
            float2 uv_MainTex;
            float4 screenPos;
        };

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

        void surf(Input IN, inout SurfaceOutputToon o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
            o.screenPos = IN.screenPos;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
