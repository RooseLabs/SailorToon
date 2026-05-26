Shader "Custom/Slice"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0

        _SliceNormal ("Normal", Vector) = (0,0,0,0)
        _SliceCenter ("Center", Vector) = (0,0,0,0)
        _SliceOffsetDst ("Offset", Float) = 0
    }
    SubShader
    {
        Tags {
            "Queue" = "Geometry"
            "IgnoreProjector" = "True"
            "RenderType" = "Geometry"
        }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.0

        #pragma multi_compile _ _ENABLE_ROLLING_LOG
        #pragma multi_compile _ _ROLLING_LOG_SPHERE

        #include "../Includes/RollingLog.cginc"

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        // World space normal of slice, anything along this direction from center will be invisible
        float3 _SliceNormal;
        // World space center of slice
        float3 _SliceCenter;
        // Increasing makes more of the mesh visible, decreasing makes less of the mesh visible
        float _SliceOffsetDst;

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
            float3 adjustedCenter = _SliceCenter + _SliceNormal * _SliceOffsetDst;
            float3 offsetTo_SliceCenter = adjustedCenter - IN.worldPos;
            clip(dot(offsetTo_SliceCenter, _SliceNormal));

            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;

            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "VertexLit"
}
