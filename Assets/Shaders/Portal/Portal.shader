Shader "Custom/Portal"
{
    Properties
    {
        _InactiveColor ("Inactive Color", Color) = (0, 0, 0, 0)
    }
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
            float4 _InactiveColour;
            int _DisplayMask; // set to 1 to display texture, otherwise will draw inactive colour

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
                float2 uv = i.screenPos.xy / i.screenPos.w;
                fixed4 portalColor = tex2D(_MainTex, uv);
                return portalColor * _DisplayMask + _InactiveColour * (1 - _DisplayMask);
            }
            ENDCG
        }
    }
    Fallback "Standard"
}
