Shader "Custom/Plane"
{
    Properties
    {
        _Color ("Color", Color) = (0.3, 0.5, 0.2, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-1" }
        ZWrite On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;

            struct appdata { float4 vertex : POSITION; };
            struct v2f     { float4 pos : SV_POSITION; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target { return _Color; }
            ENDCG
        }
    }
}