Shader "Custom/GrassGeometry"
{
    Properties
    {
        _RootColor ("Root Color", Color) = (0.1, 0.3, 0.05, 1)
        _TipColor ("Tip Color", Color) = (0.4, 0.8, 0.15, 1)
        _BladeHeight ("Blade Height", Float) = 0.6
        _BladeWidth ("Blade Width", Float) = 0.08
        _BladeRandom ("Blade Randomness", Range(0, 1)) = 0.3
        _BladeTilt ("Blade Tilt", Range(0, 0.5)) = 0.1
        _WindDir ("Wind Direction", Vector) = (1, 0, 0, 0)
        _WindSpeed ("Wind Speed", Float) = 1.0
        _WindStrength ("Wind Strength", Float) = 0.3
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma target 4.0

            #include "UnityCG.cginc"

            fixed4 _RootColor;
            fixed4 _TipColor;

            float _BladeHeight;
            float _BladeWidth;
            float _BladeRandom;
            float _BladeTilt;

            float4 _WindDir;
            float _WindSpeed;
            float _WindStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2g
            {
                float4 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };


            struct g2f
            {
                float4 pos : SV_POSITION;
                float  height : TEXCOORD0;
            };

            float rand(float2 seed)
            {
                return frac(sin(dot(seed, float2(127.1, 311.7))) * 43758.5453);
            }

            float3x3 AngleAxis3x3(float angle, float3 axis)
            {
                float c, s;
                sincos(angle, s, c);
                float t = 1.0 - c;
                float x = axis.x, y = axis.y, z = axis.z;
                return float3x3(
                    t*x*x + c,   t*x*y - s*z, t*x*z + s*y,
                    t*x*y + s*z, t*y*y + c,   t*y*z - s*x,
                    t*x*z - s*y, t*y*z + s*x, t*z*z + c
                );
            }

            float3 WindDisplacement(float3 worldPos, float heightMask)
            {
                float phase = worldPos.x * 0.8 + worldPos.z * 0.8;

                float wave = sin(_Time.y * _WindSpeed + phase) * 0.7
                           + sin(_Time.y * _WindSpeed * 2.3 + phase * 1.5) * 0.3;

                float2 windDir = normalize(_WindDir.xz);
                return float3(
                    windDir.x * wave * _WindStrength * heightMask,
                    0.0,
                    windDir.y * wave * _WindStrength * heightMask
                );
            }

            v2g vert(appdata v)
            {
                v2g o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            [maxvertexcount(7)]
            void geom(point v2g input[1], inout TriangleStream<g2f> stream)
            {
                float3 basePos = input[0].worldPos.xyz;
                float3 up = float3(0, 1, 0);

                float2 seed = basePos.xz;
                float rAngle = rand(seed) * UNITY_TWO_PI;
                float rHeight = 0.7 + rand(seed + 1) * 0.6;
                float rWidth = 0.7 + rand(seed + 2) * 0.6;
                float rTilt = rand(seed + 3) * _BladeTilt;

                float bladeH = _BladeHeight * rHeight;
                float bladeW = _BladeWidth * rWidth;

                float3x3 yRot = AngleAxis3x3(rAngle, up);
                float3 right = mul(yRot, float3(1, 0, 0));
                float3 forward = mul(yRot, float3(0, 0, 1));

                float3x3 tiltRot = AngleAxis3x3(rTilt, right);
                float3 bladeUp = mul(tiltRot, up);

                const int SEG = 3;
                const float INV = 1.0 / (float)SEG;

                g2f o;

                [unroll]
                for (int i = 0; i <= SEG; i++)
                {
                    float t = i * INV;
                    float width_t = (1.0 - t);
                    float3 centre = basePos + bladeUp * (bladeH * t);

                    float mask = t * t;
                    centre += WindDisplacement(basePos, mask);

                    if (i < SEG)
                    {
                        float3 leftPos = centre - right * (bladeW * 0.5 * width_t);
                        o.pos = UnityWorldToClipPos(float4(leftPos, 1));
                        o.height = t;
                        stream.Append(o);

                        float3 rightPos = centre + right * (bladeW * 0.5 * width_t);
                        o.pos = UnityWorldToClipPos(float4(rightPos, 1));
                        o.height = t;
                        stream.Append(o);
                    }
                    else
                    {
                        o.pos = UnityWorldToClipPos(float4(centre, 1));
                        o.height = 1.0;
                        stream.Append(o);
                    }
                }

                stream.RestartStrip();
            }

            fixed4 frag(g2f i) : SV_Target
            {
                return lerp(_RootColor, _TipColor, i.height);
            }

            ENDCG
        }
    }

    FallBack "Diffuse"
}
