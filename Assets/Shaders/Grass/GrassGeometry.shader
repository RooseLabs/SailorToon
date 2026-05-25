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
        _WindSpeed ("Wind Speed", Float) = 1.0
        _WindStrength ("Wind Strength", Float) = 0.3
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off

        CGINCLUDE
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

        // Builds the 7 world-space blade vertices and writes them into `outPos[0..6]`
        // plus their corresponding height (0..1) into `outHeight[0..6]`.
        void BuildBlade(float3 basePos, float3 normal, out float3 outPos[7], out float outHeight[7])
        {
            float3 up = normalize(normal);

            float2 seed = basePos.xz;
            float rAngle = rand(seed) * UNITY_TWO_PI;
            float rHeight = 0.7 + rand(seed + 1) * 0.6;
            float rWidth = 0.7 + rand(seed + 2) * 0.6;
            float rTilt = rand(seed + 3) * _BladeTilt;

            float bladeH = _BladeHeight * rHeight;
            float bladeW = _BladeWidth * rWidth;

            // Build a tangent basis perpendicular to the surface normal.
            float3 refAxis = abs(up.y) < 0.99 ? float3(0, 1, 0) : float3(1, 0, 0);
            float3 tangent = normalize(cross(refAxis, up));

            float3x3 yRot = AngleAxis3x3(rAngle, up);
            float3 right = mul(yRot, tangent);

            float3x3 tiltRot = AngleAxis3x3(rTilt, right);
            float3 bladeUp = mul(tiltRot, up);

            const int SEG = 3;
            const float INV = 1.0 / (float)SEG;

            int idx = 0;
            [unroll]
            for (int i = 0; i <= SEG; i++)
            {
                float t = i * INV;
                float width_t = (1.0 - t);
                float3 center = basePos + bladeUp * (bladeH * t);

                float mask = t * t;
                center += WindDisplacement(basePos, mask);

                if (i < SEG)
                {
                    outPos[idx] = center - right * (bladeW * 0.5 * width_t);
                    outHeight[idx] = t;
                    idx++;
                    outPos[idx] = center + right * (bladeW * 0.5 * width_t);
                    outHeight[idx] = t;
                    idx++;
                }
                else
                {
                    outPos[idx] = center;
                    outHeight[idx] = 1.0;
                    idx++;
                }
            }
        }
        ENDCG

        // ----- Forward pass -----
        Pass
        {
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma target 4.0

            struct g2f
            {
                float4 pos : SV_POSITION;
                float  height : TEXCOORD0;
            };

            [maxvertexcount(7)]
            void geom(point v2g input[1], inout TriangleStream<g2f> stream)
            {
                float3 wpos[7];
                float  hgt[7];
                BuildBlade(input[0].worldPos.xyz, input[0].worldNormal, wpos, hgt);

                g2f o;
                [unroll]
                for (int i = 0; i < 7; i++)
                {
                    o.pos = UnityWorldToClipPos(float4(wpos[i], 1));
                    o.height = hgt[i];
                    stream.Append(o);
                }
                stream.RestartStrip();
            }

            fixed4 frag(g2f i) : SV_Target
            {
                return lerp(_RootColor, _TipColor, i.height);
            }
            ENDCG
        }

        // ----- Shadow caster pass -----
        Pass
        {
            Tags { "LightMode" = "ShadowCaster" }

            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geomShadow
            #pragma fragment fragShadow
            #pragma target 4.0
            #pragma multi_compile_shadowcaster

            struct g2fShadow
            {
                V2F_SHADOW_CASTER;
            };

            [maxvertexcount(7)]
            void geomShadow(point v2g input[1], inout TriangleStream<g2fShadow> stream)
            {
                float3 wpos[7];
                float  hgt[7];
                BuildBlade(input[0].worldPos.xyz, input[0].worldNormal, wpos, hgt);

                g2fShadow o;
                [unroll]
                for (int i = 0; i < 7; i++)
                {
                    // Replicate TRANSFER_SHADOW_CASTER_NORMALOFFSET in world space:
                    // project to clip space, then apply Unity's linear shadow bias.
                    float4 clipPos = UnityWorldToClipPos(float4(wpos[i], 1));
                    o.pos = UnityApplyLinearShadowBias(clipPos);
                    stream.Append(o);
                }
                stream.RestartStrip();
            }

            float4 fragShadow(g2fShadow i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
