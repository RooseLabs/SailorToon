Shader "Custom/GrassWind"
{
    Properties
    {
        _RootColor   ("Root Color",   Color) = (0.1, 0.3, 0.05, 1)
        _TipColor    ("Tip Color",    Color) = (0.4, 0.8, 0.15, 1)
        _WindDir     ("Wind Direction", Vector) = (1, 0, 0, 0)
        _WindSpeed   ("Wind Speed",   Float) = 1.0
        _WindStrength("Wind Strength",Float) = 0.3
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4  _RootColor;
            fixed4  _TipColor;
            float4  _WindDir;
            float   _WindSpeed;
            float   _WindStrength;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float  height   : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float  phase    = worldPos.x * 0.8 + worldPos.z * 0.8;

                float wave = sin(_Time.y * _WindSpeed + phase) * 0.7
                           + sin(_Time.y * _WindSpeed * 2.3 + phase * 1.5) * 0.3;

                float mask = saturate(v.vertex.y);

                float2 windDir = normalize(_WindDir.xz);
                float3 displacement = float3(
                    windDir.x * wave * _WindStrength * mask,
                    0.0,
                    windDir.y * wave * _WindStrength * mask
                );

                float4 displaced = v.vertex + float4(displacement, 0.0);
                o.pos    = UnityObjectToClipPos(displaced);

                o.height = mask;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return lerp(_RootColor, _TipColor, i.height);
            }

            ENDCG
        }
    }
}