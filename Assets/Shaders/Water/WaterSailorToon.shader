Shader "Custom/WaterSailorToon"
{
    Properties
    {
        [Header(Color)]
        _WaterColor ("Shallow Color", Color) = (0.10, 0.45, 0.82, 1)
        _DeepWaterColor ("Deep Color", Color) = (0.04, 0.22, 0.50, 1)
        _DepthFadeDistance ("Depth Fade Distance", Float) = 2.0

        [Header(Foam)]
        _FoamColor ("Foam Color", Color) = (0.95, 1.0, 1.0, 1)
        _FoamOutlineColor ("Foam Edge Color", Color) = (0.05, 0.28, 0.62, 1)
        _FoamDistance ("Foam Width", Float) = 0.9
        _ShoreGlowStrength ("Foam Edge Strength", Float) = 0.85

        [Header(Waves)]
        _WaveHeight ("Wave Height", Float) = 0.03
        _WaveSpeed ("Wave Speed", Float) = 0.5
        _WaveFrequency ("Wave Frequency", Float) = 0.8

        [Header(Surface Detail)]
        [KeywordEnum(Lines, Cells)] _WaterDetailMode ("Detail Mode", Float) = 0
        _DetailColor ("Cell Color", Color) = (0.30, 0.90, 0.80, 1)
        _DetailScale ("Detail Scale", Float) = 7.0
        _DetailStrength ("Detail Strength", Float) = 0.18

        [Header(Ripples)]
        _RippleColor ("Ripple Color", Color) = (0.85, 1.0, 1.0, 1)
        _RippleThickness ("Ripple Thickness", Float) = 0.18
        _RippleStrength ("Ripple Strength", Float) = 0.35

        [Header(Lighting)]
        _SpecularColor ("Specular Color", Color) = (1.0, 1.0, 1.0, 1)
        _SpecularPower ("Specular Power", Float) = 60.0
        _SpecularStrength ("Specular Strength", Float) = 0.6
        _FresnelPower ("Fresnel Power", Float) = 3.0
        _Transparency ("Transparency", Range(0,1)) = 0.75

        [Header(Stencil)]
        // Set _StencilComp to Always (8) to disable the stencil test per material.
        // Default rejects fragments whose stencil value equals _StencilRef (e.g. the boat hull).
        [IntRange] _StencilRef ("Stencil Ref", Range(0,255)) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Compare", Float) = 6
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            Stencil
            {
                Ref [_StencilRef]
                Comp [_StencilComp]
                Pass Keep
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _ENABLE_ROLLING_LOG
            #pragma multi_compile _ _ROLLING_LOG_SPHERE
            #pragma shader_feature _WATERDETAILMODE_LINES _WATERDETAILMODE_CELLS
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            #include "../Includes/RollingLog.cginc"

            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            // Colors
            float4 _WaterColor;
            float4 _DeepWaterColor;
            float  _DepthFadeDistance;
            float4 _FoamColor;
            float4 _FoamOutlineColor;
            float4 _RippleColor;

            // Wave
            float _WaveHeight;
            float _WaveSpeed;
            float _WaveFrequency;

            // Foam
            float _FoamDistance;
            float _ShoreGlowStrength;

            // Surface detail
            float4 _DetailColor;
            float  _DetailScale;
            float  _DetailStrength;

            // Ripples
            float _RippleThickness;
            float _RippleStrength;

            // Specular
            float4 _SpecularColor;
            float  _SpecularPower;
            float  _SpecularStrength;

            // Fresnel / transparency
            float _FresnelPower;
            float _Transparency;

            #define RIPPLE_COUNT 8
            float4 _RippleData[RIPPLE_COUNT];

            struct appdata { float4 vertex : POSITION; };

            struct v2f
            {
                float4 vertex      : SV_POSITION;
                float4 screenPos   : TEXCOORD0;
                float3 worldPos    : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
            };

            // Returns (waveHeight, dHeight/dx, dHeight/dz).
            // Three sine layers at different angles break up the axis-aligned look
            // and provide derivatives for an analytic surface normal.
            float3 ComputeWave(float3 worldPos)
            {
                float x = worldPos.x, z = worldPos.z, t = _Time.y;
                float s = _WaveFrequency;

                float c1 = cos((x + z) * s + t * _WaveSpeed);
                float c2 = cos(x * 1.7 - t * 0.7);
                float c3 = cos(z * 1.3 - t * 0.5);

                float height = (sin((x + z) * s + t * _WaveSpeed)
                              + sin(x * 1.7 - t * 0.7) * 0.5
                              + sin(z * 1.3 - t * 0.5) * 0.4) * _WaveHeight;

                float ddx = (c1 * s + c2 * 0.5 * 1.7) * _WaveHeight;
                float ddz = (c1 * s + c3 * 0.4 * 1.3) * _WaveHeight;

                return float3(height, ddx, ddz);
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                float3 wave = ComputeWave(worldPos);
                worldPos.y += wave.x;

                // Analytic normal from wave surface: N = normalize(-ddx, 1, -ddz)
                float3 worldNormal = normalize(float3(-wave.y, 1.0, -wave.z));

                #ifdef _ENABLE_ROLLING_LOG
                worldNormal = ApplyRollingLogNormal(worldNormal, worldPos);
                worldPos    = ApplyRollingLog(worldPos);
                #endif

                o.worldPos    = worldPos;
                o.worldNormal = worldNormal;
                o.vertex      = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                o.screenPos   = ComputeScreenPos(o.vertex);
                return o;
            }

            // Pseudo-random float2 per integer grid cell.
            float2 Hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            // Randomly-placed specks: each cell has a ~60 % chance of containing
            // a small dot at a random position. 3x3 neighbourhood search prevents
            // dots being clipped at cell boundaries.
            float WaterSpecks(float2 xz)
            {
                float2 uv = xz * _DetailScale * 0.25 + _Time.y * float2(0.04, 0.07);
                float2 id = floor(uv);
                float2 f  = frac(uv);

                float result = 0.0;
                [unroll] for (int dj = -1; dj <= 1; ++dj)
                [unroll] for (int di = -1; di <= 1; ++di)
                {
                    float2 nb  = float2(di, dj);
                    float2 h   = Hash2(id + nb);
                    float2 pos = nb + h * 0.8 + 0.1; // random pos, padded from cell edges
                    float  d   = length(f - pos);
                    result    += (1.0 - step(0.12, d)) * step(0.4, h.x);
                }
                return saturate(result);
            }

            float RippleRing(float2 xz, float4 ripple)
            {
                float2 center = ripple.xy;
                float  radius = ripple.z;
                float  fade   = ripple.w;

                float2 offset = xz - center;
                float  dist   = length(offset);

                dist += sin(xz.x * 3.0 + _Time.y) * 0.02;
                dist += cos(xz.y * 2.8 - _Time.y) * 0.02;

                float ring = 1.0 - smoothstep(0.0, _RippleThickness, abs(dist - radius));
                float breakup = smoothstep(-0.1, 0.55, sin(atan2(offset.y, offset.x) * 6.0 + dist * 10.0));
                return ring * breakup * fade * 0.7;
            }

            // A mathematically robust way to extract view space depth from raw depth.
            // Works for ALL projection matrices (standard, ortho, oblique).
            // Uses UNITY_MATRIX_P (the current projection matrix) dynamically.
            inline float GetTrueEyeDepth(float rawDepth, float2 screenUV)
            {
                float p20 = UNITY_MATRIX_P[2][0];
                float p21 = UNITY_MATRIX_P[2][1];

                // If this is a standard symmetrical projection matrix, rely on Unity's built-in optimized macro
                if (abs(p20) < 0.00001 && abs(p21) < 0.00001)
                {
                    return LinearEyeDepth(rawDepth);
                }

                // If we reach here, we are dealing with an Oblique projection matrix (like Portals use).
                // Skyboxes and cleared backgrounds will result in mathematically negative or invalid depths
                // when put back through the oblique matrix, so we catch the raw depth buffer clear values.
                #if defined(UNITY_REVERSED_Z)
                if (rawDepth < 0.00001) return 10000.0;
                #else
                if (rawDepth > 0.99999) return 10000.0;
                #endif

                // Reconstruct clip space Z
                float clipZ = rawDepth;
                #if !defined(UNITY_REVERSED_Z)
                clipZ = clipZ * 2.0 - 1.0;
                #endif

                // Get clip space XY.
                // We must restore the 'flipped' Y state that UNITY_MATRIX_P expects
                // during Render-To-Texture passes using _ProjectionParams.x
                float2 ndc = screenUV * 2.0 - 1.0;
                ndc.y *= _ProjectionParams.x;

                // UNITY_MATRIX_P maps View Space to Clip Space.
                // We want to solve for viewZ:
                float p00 = UNITY_MATRIX_P[0][0];
                float p02 = UNITY_MATRIX_P[0][2];
                float p11 = UNITY_MATRIX_P[1][1];
                float p12 = UNITY_MATRIX_P[1][2];
                float p22 = UNITY_MATRIX_P[2][2];
                float p23 = UNITY_MATRIX_P[2][3];

                // Substitute viewX and viewY into the clipZ equation
                float q = p20 * (ndc.x + p02) / p00 + p21 * (ndc.y + p12) / p11 - p22;

                float denom = clipZ - q;
                if (abs(denom) < 0.000001) return 10000.0; // Prevent divide by zero

                float viewZ = p23 / denom;

                // Empty background space can sometimes mathematically map to behind the camera on oblique frustums.
                if (viewZ <= 0.0) return 10000.0;

                return viewZ;
            }

            float4 frag(v2f i) : SV_Target
            {
                // Depth
                float2 screenUV   = i.screenPos.xy / i.screenPos.w;
                float  rawDepth   = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV);
                float  sceneDepth = GetTrueEyeDepth(rawDepth, screenUV);
                float  depthDiff  = sceneDepth - i.screenPos.w;

                // Base color: shallow-to-deep tint
                float3 outColor = lerp(_WaterColor.rgb, _DeepWaterColor.rgb, saturate(depthDiff / _DepthFadeDistance));

                // ---- Foam ----
                // Three sine layers warp the depth threshold, producing an uneven
                // boundary that reads as hand-drawn rather than a perfect ring.
                float warp = sin(i.worldPos.x * 5.1 + _Time.y * 0.80) * 0.13
                           + sin(i.worldPos.z * 4.3 - _Time.y * 0.60) * 0.13
                           + sin((i.worldPos.x - i.worldPos.z) * 7.7 + _Time.y * 1.15) * 0.05;

                float foamDepth = depthDiff / _FoamDistance + warp;

                // Hard step: the crisp binary edge is the defining comic/toon trait.
                float foam = 1.0 - step(1.0, foamDepth);

                // Ink accent band at the outer edge of the foam (where foam meets open water).
                // Covers the outermost ~28% of the foam zone.
                float inkLine = foam * step(0.72, foamDepth);

                // ---- Surface detail ----
                #if defined(_WATERDETAILMODE_CELLS)
                    float detail = WaterSpecks(i.worldPos.xz) * (1.0 - foam) * _DetailStrength;
                    outColor = lerp(outColor, _DetailColor.rgb, detail);
                #else
                    // Slow-drifting thin bands; a gentle sine bend keeps them from looking rigid.
                    float lineCoord = i.worldPos.z * _DetailScale * 0.28
                                    + i.worldPos.x * _DetailScale * 0.07
                                    + sin(i.worldPos.x * 1.8 + _Time.y * 0.30) * 0.06
                                    - _Time.y * 0.18;
                    float detail = step(0.91, frac(lineCoord)) * (1.0 - foam) * _DetailStrength;
                    outColor = lerp(outColor, outColor * 0.55, detail);
                #endif

                // Lay foam, then draw the ink band on top of it.
                outColor = lerp(outColor, _FoamColor.rgb, foam);
                outColor = lerp(outColor, _FoamOutlineColor.rgb, inkLine * _ShoreGlowStrength);

                // Ripple rings
                float rippleSum = 0.0;
                [unroll]
                for (int r = 0; r < RIPPLE_COUNT; ++r)
                    rippleSum += RippleRing(i.worldPos.xz, _RippleData[r]);
                outColor = lerp(outColor, _RippleColor.rgb, smoothstep(0.0, 0.8, rippleSum) * _RippleStrength);

                // Lighting
                float3 normal   = normalize(i.worldNormal);
                float3 viewDir  = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 halfDir  = normalize(lightDir + viewDir);
                float  spec     = smoothstep(0.3, 0.55, pow(saturate(dot(normal, halfDir)), _SpecularPower));
                outColor += _SpecularColor.rgb * _LightColor0.rgb * spec * _SpecularStrength;

                // Fresnel: more opaque at glancing angles, more transparent looking straight down
                float fresnel = pow(1.0 - saturate(dot(normal, viewDir)), _FresnelPower);
                return float4(outColor, lerp(_Transparency, 1.0, fresnel));
            }

            ENDCG
        }
    }
}
