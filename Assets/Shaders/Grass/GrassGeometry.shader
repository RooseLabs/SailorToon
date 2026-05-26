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
        _RootBend ("Root Bend (along normal)", Range(0, 1)) = 0.2
        _WindSpeed ("Wind Speed", Float) = 1.0
        _WindStrength ("Wind Strength", Float) = 0.3
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off

        CGINCLUDE
        #include "UnityCG.cginc"
        #include "../Includes/RollingLog.cginc"

        fixed4 _RootColor;
        fixed4 _TipColor;

        float _BladeHeight;
        float _BladeWidth;
        float _BladeRandom;
        float _BladeTilt;
        float _RootBend;

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
            float3 worldPos    = mul(unity_ObjectToWorld, v.vertex).xyz;
            float3 worldNormal = UnityObjectToWorldNormal(v.normal);

            #ifdef _ENABLE_ROLLING_LOG
            worldNormal = ApplyRollingLogNormal(worldNormal, worldPos);
            worldPos    = ApplyRollingLog(worldPos);
            #endif

            o.worldPos    = float4(worldPos, 1.0);
            o.worldNormal = worldNormal;
            return o;
        }

        // Builds the 7 world-space blade vertices and writes them into `outPos[0..6]`
        // plus their corresponding height (0..1) into `outHeight[0..6]`.
        void BuildBlade(float3 basePos, float3 normal, out float3 outPos[7], out float outHeight[7])
        {
            float3 surfaceUp = normalize(normal);
            float3 worldUp = float3(0, 1, 0);

            float2 seed = basePos.xz;
            float rAngle = rand(seed) * UNITY_TWO_PI;
            float rHeight = 0.7 + rand(seed + 1) * 0.6;
            float rWidth = 0.7 + rand(seed + 2) * 0.6;
            float rTilt = rand(seed + 3) * _BladeTilt;

            float bladeH = _BladeHeight * rHeight;
            float bladeW = _BladeWidth * rWidth;

            // Build a tangent basis perpendicular to the surface normal.
            float3 refAxis = abs(surfaceUp.y) < 0.99 ? float3(0, 1, 0) : float3(1, 0, 0);
            float3 tangent = normalize(cross(refAxis, surfaceUp));

            float3x3 yRot = AngleAxis3x3(rAngle, surfaceUp);
            float3 right = mul(yRot, tangent);

            // Tip points up in world space, with a small random tilt around `right`.
            float3x3 tiltRot = AngleAxis3x3(rTilt, right);
            float3 tipDir = mul(tiltRot, worldUp);

            // Quadratic bezier:
            //   p0 = root, p1 = control point along surface normal (short),
            //   p2 = tip displaced along world up. This makes the blade leave the
            //   surface along its normal and then curve sharply toward world up.
            //
            // Scale the bend by how much the surface tilts away from world up so
            // that on a perfectly flat surface the bezier collapses to a straight
            // line (and _RootBend has no visible effect).
            float slope = saturate(1.0 - dot(surfaceUp, worldUp));
            float effectiveBend = _RootBend * smoothstep(0.0, 0.25, slope);

            float3 p0 = basePos;
            float3 p1 = basePos + surfaceUp * (bladeH * effectiveBend);
            float3 p2 = basePos + tipDir * bladeH;

            const int SEG = 3;
            const float INV = 1.0 / (float)SEG;

            int idx = 0;
            [unroll]
            for (int i = 0; i <= SEG; i++)
            {
                float t = i * INV;
                float omt = 1.0 - t;
                float3 center = omt * omt * p0 + 2.0 * omt * t * p1 + t * t * p2;

                // Taper width by actual world-space height ratio so _RootBend
                // doesn't visually inflate the blade by bunching wide vertices
                // higher up the curve.
                float yT = saturate((center.y - basePos.y) / max(bladeH, 1e-4));
                float width_t = 1.0 - yT;

                float mask = t * t;
                center += WindDisplacement(basePos, mask);

                if (i < SEG)
                {
                    outPos[idx] = center - right * (bladeW * 0.5 * width_t);
                    outHeight[idx] = yT;
                    idx++;
                    outPos[idx] = center + right * (bladeW * 0.5 * width_t);
                    outHeight[idx] = yT;
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
            #pragma multi_compile _ _ENABLE_ROLLING_LOG
            #pragma multi_compile _ _ROLLING_LOG_SPHERE

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
            #pragma multi_compile _ _ENABLE_ROLLING_LOG
            #pragma multi_compile _ _ROLLING_LOG_SPHERE

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
