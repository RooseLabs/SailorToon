Shader "Custom/Portal"
{
    // The goal of this shader is to render a portal effect by mapping a texture
    // exactly to the screen space coordinates of the object.
    // This ensures that the texture (usually a Render Texture from another camera)
    // acts like a seamless window to another location. It prevents the image from
    // being stretched by the mesh's UVs or aspect ratio, as it is mapped 1:1 with the screen.
    Properties { }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _ENABLE_ROLLING_LOG
            #pragma multi_compile _ _ROLLING_LOG_SPHERE
            #include "UnityCG.cginc"
            #include "../Includes/RollingLog.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            sampler2D _MainTex;
            int _DisplayMask; // 1 = display texture, 0 = discard fragment

            v2f vert(appdata v)
            {
                v2f o;
                #ifdef _ENABLE_ROLLING_LOG
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                worldPos = ApplyRollingLog(worldPos);
                o.vertex = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                #else
                o.vertex = UnityObjectToClipPos(v.vertex);
                #endif
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                clip(_DisplayMask - 0.5);
                return tex2Dproj(_MainTex, i.screenPos);
            }
            ENDCG
        }
    }
    Fallback Off
}
