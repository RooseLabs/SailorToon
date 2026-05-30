Shader "Custom/Slice"
{
    // The goal of this shader is to render a standard PBR-lit mesh that can be
    // clipped against an arbitrary world-space plane. _SliceCenter and _SliceNormal
    // define the plane, and _SliceOffsetDst shifts it along its normal; fragments
    // on the negative side of the plane are discarded via clip(), so the mesh
    // appears progressively cut away. Used together with the portal system so
    // objects passing through a portal can be hidden on the far side cleanly.
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        [Enum(Off,0,Front,1,Back,2)] _Cull ("Cull Mode", Float) = 2

        // Stencil. Set _StencilWriteOp to Replace (2) on materials that should mark
        // the stencil buffer (e.g. boat hull) so other shaders (e.g. water) can mask
        // against _StencilRef. Default Keep (0) leaves the buffer untouched.
        [IntRange] _StencilRef ("Stencil Ref", Range(0,255)) = 1
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilWriteOp ("Stencil Write Op", Float) = 0
    }
    SubShader
    {
        Tags {
            "Queue" = "Geometry"
            "IgnoreProjector" = "True"
            "RenderType" = "Geometry"
        }
        LOD 200

        Cull [_Cull]

        Stencil
        {
            Ref [_StencilRef]
            Comp Always
            Pass [_StencilWriteOp]
        }

        CGPROGRAM
        #pragma surface surf Toon fullforwardshadows noambient novertexlights noforwardadd vertex:vert addshadow
        #pragma target 3.0

        #pragma multi_compile _ _TL_HALFTONE_ON
        #pragma multi_compile _ _ENABLE_ROLLING_LOG
        #pragma multi_compile _ _ROLLING_LOG_SPHERE

        #include "../Includes/RollingLog.cginc"
        #include "../Includes/ToonLighting.cginc"

        sampler2D _MainTex;
        fixed4 _Color;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float4 screenPos;
        };

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

        void surf(Input IN, inout SurfaceOutputToon o)
        {
            float3 adjustedCenter = _SliceCenter + _SliceNormal * _SliceOffsetDst;
            float3 offsetTo_SliceCenter = adjustedCenter - IN.worldPos;
            clip(dot(offsetTo_SliceCenter, _SliceNormal));

            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
            o.screenPos = IN.screenPos;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
