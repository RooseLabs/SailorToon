Shader "Custom/ProceduralSkybox"
{
    Properties
    {
        [Header(Sky Mode)]
        [KeywordEnum(DayNight, Synthwave)] _Mode ("Sky Mode", Float) = 0
        [KeywordEnum(Standard, Synthwave, Raymarched, Textured)] _SunMode ("Sun Mode", Float) = 0

        [Header(Day Night Sky)]
        _TimeOfDay      ("Time Of Day (0=noon 0.5=midnight)", Range(0,1)) = 0.0
        _DayZenith      ("Day Zenith Color", Color) = (0.1, 0.4, 0.9, 1)
        _DayHorizon     ("Day Horizon Color", Color) = (0.6, 0.8, 1.0, 1)
        _DayGround      ("Day Ground Color", Color) = (0.3, 0.25, 0.2, 1)
        _SunsetZenith   ("Sunset Zenith Color", Color) = (0.15, 0.2, 0.6, 1)
        _SunsetHorizon  ("Sunset Horizon Color", Color) = (0.95, 0.4, 0.1, 1)
        _NightZenith    ("Night Zenith Color", Color) = (0.01, 0.01, 0.06, 1)
        _NightHorizon   ("Night Horizon Color", Color) = (0.03, 0.04, 0.12, 1)
        _NightGround    ("Night Ground Color", Color) = (0.01, 0.01, 0.02, 1)

        [Header(Standard Sun)]
        _SunDir           ("Sun Direction", Vector) = (0.5, 0.8, 0.3, 0)
        _SunColor         ("Sun Color", Color) = (1.0, 0.95, 0.8, 1)
        _SunSize          ("Sun Disc Size", Range(0.001, 0.5)) = 0.02
        _SunEdgeSoftness  ("Edge Softness", Range(0, 1)) = 0.1
        _SunGlowColor     ("Glow Color", Color) = (1.0, 0.6, 0.2, 1)
        _SunGlowSharpness ("Glow Sharpness", Range(1, 64)) = 32.0
        _SunGlowStrength  ("Glow Strength", Range(0, 5)) = 0.8

        [Header(Moon)]
        _MoonDir          ("Moon Direction", Vector) = (-0.5, 0.6, -0.3, 0)
        _MoonColor        ("Moon Color", Color) = (0.9, 0.95, 1.0, 1)
        _MoonSize         ("Moon Disc Size", Range(0.001, 0.1)) = 0.025
        _MoonEdgeSoftness ("Edge Softness", Range(0, 1)) = 0.1
        _MoonCrescent     ("Moon Crescent Offset", Range(0.0, 1.0)) = 0.4

        [Header(Clouds)]
        _CloudColor     ("Cloud Color", Color) = (1.0, 0.98, 0.95, 1)
        _CloudShadow    ("Cloud Shadow Color", Color) = (0.6, 0.65, 0.75, 1)
        _CloudCover     ("Cloud Cover", Range(0,1)) = 0.5
        _CloudSharpness ("Cloud Sharpness", Range(1, 8)) = 4.0
        _CloudSpeed     ("Cloud Speed", Range(0, 0.1)) = 0.01
        _CloudScale     ("Cloud Scale", Range(1, 10)) = 4.0

        [Header(Stars)]
        _StarDensity    ("Star Density", Range(50, 500)) = 200.0
        _StarBrightness ("Star Brightness", Range(0, 2)) = 1.0
        _StarTwinkle    ("Star Twinkle Speed", Range(0, 5)) = 1.0

        [Header(Synthwave Sky)]
        _SWHorizonA     ("Horizon Color A", Color) = (0.9, 0.1, 0.6, 1)
        _SWHorizonB     ("Horizon Color B", Color) = (0.4, 0.0, 0.9, 1)
        _SWZenith       ("Zenith Color", Color) = (0.02, 0.0, 0.12, 1)
        _SWGridColor    ("Grid Color", Color) = (0.8, 0.1, 0.9, 1)
        _SWGridSpeed    ("Grid Speed", Range(0,2)) = 0.5

        [Header(Synthwave Sun)]
        _SWSunDir       ("Sun Direction", Vector) = (0.5, 0.0, 0.3, 0)
        _SWSunColor     ("Sun Color A", Color) = (1.0, 0.6, 0.1, 1)
        _SWSunColor2    ("Sun Color B", Color) = (0.9, 0.1, 0.5, 1)
        _SWSunSize      ("Sun Size", Range(0.05, 0.6)) = 0.28
        _SWSunBands     ("Sun Band Count", Range(3, 20)) = 8

        [Header(Raymarched Sun)]
        _RMSunDir               ("Sun Direction", Vector) = (0.5, 0.8, 0.3, 0)
        _RMSunSize              ("Sun Disc Size", Range(0.05, 0.5)) = 0.15
        _RMGlowColor            ("Glow Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _RMGlowStrength         ("Glow Strength", Range(0, 10)) = 1.0
        _RMGlowSharpness        ("Glow Sharpness", Range(1, 64)) = 32.0
        _RMEmissionBoost        ("Emission Boost", Range(1, 10)) = 2.0
        _RMLight1Color          ("Light 1 Color", Color) = (0.3, 0.65, 1.0, 1.0)
        _RMLight1Strength       ("Light 1 Strength", Range(0, 2)) = 0.75
        _RMLight2Color          ("Light 2 Color", Color) = (0.6, 0.35, 1.0, 1.0)
        _RMLight2Strength       ("Light 2 Strength", Range(0, 2)) = 0.75
        _RMLight3Color          ("Light 3 Color", Color) = (0.4, 0.5, 1.0, 1.0)
        _RMLight3Strength       ("Light 3 Strength", Range(0, 2)) = 0.5
        [Toggle] _RMSpecular1Enabled    ("Enable Specular 1", Float) = 1
        _RMSpecular1Color       ("Specular 1 Color", Color) = (0.4, 0.625, 1.0, 1.0)
        _RMSpecular1Strength    ("Specular 1 Strength", Range(0, 2)) = 1.0
        _RMSpecular1Sharpness   ("Specular 1 Sharpness", Range(1, 128)) = 12.0
        _RMSpecular1Position    ("Specular 1 Position", Vector) = (600, 800, -500, 0)
        [Toggle] _RMSpecular2Enabled    ("Enable Specular 2", Float) = 1
        _RMSpecular2Color       ("Specular 2 Color", Color) = (0.6, 0.5625, 1.0, 1.0)
        _RMSpecular2Strength    ("Specular 2 Strength", Range(0, 2)) = 0.75
        _RMSpecular2Sharpness   ("Specular 2 Sharpness", Range(1, 128)) = 16.0
        _RMSpecular2Position    ("Specular 2 Position", Vector) = (-600, -800, 0, 0)

        [Header(Textured Sun)]
        _TexSunSize             ("Sun Disc Size", Range(0.01, 0.3)) = 0.08
        _TexSunTex              ("Sun Texture", 2D) = "white" {}
        _TexSunSpikeCount       ("Spike Count", Range(3, 24)) = 12
        _TexSunSpikeLength      ("Spike Length", Range(0, 1)) = 0.35
        _TexSunSpikeSharpness   ("Spike Sharpness", Range(1, 16)) = 4.0

        [Header(Spectrum Bars)]
        _BarCount       ("Bar Count", Range(8, 128)) = 64
        _BarWidth       ("Bar Width (0=thin 1=full)", Range(0.1, 0.95)) = 0.7
        _BarHeight      ("Bar Max Height", Range(0.02, 0.6)) = 0.25
        _BarYOffset     ("Bar Horizon Offset", Range(-0.1, 0.1)) = 0.0
        _BarColorBot    ("Bar Color Bottom", Color) = (0.9, 0.1, 0.6, 1)
        _BarColorTop    ("Bar Color Top", Color) = (0.2, 0.8, 1.0, 1)
        _BarGlow        ("Bar Glow Width", Range(0.0, 0.05)) = 0.015
        _BarGlowColor   ("Bar Glow Color", Color) = (1.0, 0.3, 0.8, 1)
        _SpectrumTex    ("Spectrum Texture (256x1)", 2D) = "black" {}
        _SpectrumBoost  ("Spectrum Boost", Range(0.1, 10)) = 3.0

        [Header(Horizon Bend)]
        _HorizonBendScale ("Bend Scale (matches Rolling Log)", Range(0, 100)) = 30.0
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile _MODE_DAYNIGHT _MODE_SYNTHWAVE
            #pragma multi_compile _SUNMODE_STANDARD _SUNMODE_SYNTHWAVE _SUNMODE_RAYMARCHED _SUNMODE_TEXTURED
            #pragma multi_compile _ _ENABLE_ROLLING_LOG
            #pragma multi_compile _ _ROLLING_LOG_SPHERE
            #pragma target 3.0

            #include "UnityCG.cginc"

            float  _TimeOfDay;

            float3 _SunDir;
            float4 _SunColor, _SunGlowColor;
            float  _SunSize, _SunEdgeSoftness, _SunGlowSharpness, _SunGlowStrength;

            float3 _MoonDir;
            float4 _MoonColor;
            float  _MoonSize, _MoonEdgeSoftness, _MoonCrescent;

            float4 _DayZenith, _DayHorizon, _DayGround;
            float4 _SunsetZenith, _SunsetHorizon;
            float4 _NightZenith, _NightHorizon, _NightGround;

            float4 _CloudColor, _CloudShadow;
            float  _CloudCover, _CloudSharpness, _CloudSpeed, _CloudScale;

            float  _StarDensity, _StarBrightness, _StarTwinkle;

            float4 _SWHorizonA, _SWHorizonB, _SWZenith;
            float4 _SWGridColor, _SWSunColor, _SWSunColor2;
            float3 _SWSunDir;
            float  _SWGridSpeed;
            float  _SWSunSize, _SWSunBands;

            float3 _RMSunDir;
            float  _RMSunSize;
            float4 _RMGlowColor;
            float  _RMGlowStrength, _RMGlowSharpness, _RMEmissionBoost;
            float4 _RMLight1Color, _RMLight2Color, _RMLight3Color;
            float  _RMLight1Strength, _RMLight2Strength, _RMLight3Strength;
            float  _RMSpecular1Enabled, _RMSpecular1Strength, _RMSpecular1Sharpness;
            float4 _RMSpecular1Color, _RMSpecular1Position;
            float  _RMSpecular2Enabled, _RMSpecular2Strength, _RMSpecular2Sharpness;
            float4 _RMSpecular2Color, _RMSpecular2Position;

            float  _TexSunSize;
            sampler2D _TexSunTex;
            float4 _TexSunTex_ST;
            float  _TexSunSpikeCount, _TexSunSpikeLength, _TexSunSpikeSharpness;

            float  _BarCount, _BarWidth, _BarHeight, _BarYOffset;
            float4 _BarColorBot, _BarColorTop, _BarGlowColor;
            float  _BarGlow;

            sampler2D _SpectrumTex;
            float _SpectrumBoost;

            float _RL_Amount;
            float _HorizonBendScale;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = mul((float3x3)unity_ObjectToWorld, v.vertex.xyz);
                return o;
            }

            float hash11(float n) { return frac(sin(n) * 43758.5453); }
            float hash21(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 19.19);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(hash21(i),               hash21(i + float2(1,0)), u.x),
                    lerp(hash21(i + float2(0,1)), hash21(i + float2(1,1)), u.x),
                    u.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0, a = 0.5;
                for (int i = 0; i < 3; i++)
                {
                    v += a * valueNoise(p);
                    p  = p * 2.1 + float2(1.7, 9.2);
                    a *= 0.5;
                }
                return v;
            }

            float stars(float3 dir, float time)
            {
                float2 uv = float2(atan2(dir.z, dir.x) / (2.0 * 3.14159265),
                                   asin(clamp(dir.y, -1, 1)) / 3.14159265) * _StarDensity;
                float2 cell  = floor(uv);
                float2 local = frac(uv) - 0.5;
                float2 off   = float2(hash21(cell) - 0.5, hash21(cell + 17.3) - 0.5) * 0.8;
                float  dist  = length(local - off);
                float  brightness = hash21(cell + 31.7);
                float  twinkle    = 0.7 + 0.3 * sin(time * _StarTwinkle * 3.0 + brightness * 10.0);
                return smoothstep(0.08, 0.0, dist) * brightness * twinkle;
            }

            float cloudDensity(float3 dir, float time)
            {
                if (dir.y < 0.0) return 0.0;
                float2 uv = (dir.xz / (dir.y + 0.01)) * 0.3 * _CloudScale;
                uv += float2(time * _CloudSpeed, 0.0);
                float n = fbm(uv);
                return saturate((n - (1.0 - _CloudCover)) * _CloudSharpness);
            }

            float moonDisc(float3 dir, float3 moonDir)
            {
                float dp   = dot(normalize(dir), normalize(moonDir));
                float moonInner = _MoonSize * (1.0 - max(_MoonEdgeSoftness, 0.0001));
                float disc = smoothstep(_MoonSize, moonInner, acos(clamp(dp, -1, 1)));
                float3 crescentDir = normalize(moonDir + float3(0.03, 0.03, 0.0) * _MoonCrescent * 40.0);
                float  dpC         = dot(normalize(dir), crescentDir);
                float  hole        = smoothstep(_MoonSize * 0.9, _MoonSize * 0.4, acos(clamp(dpC, -1, 1)));
                return saturate(disc - hole);
            }

            float sampleSpectrum(float t)
            {
                return tex2Dlod(_SpectrumTex, float4(t, 0.5, 0, 0)).r * _SpectrumBoost;
            }

            // Downward shift of the apparent horizon to match the Rolling-Log-curved ground.
            // Sphere mode: uniform in dir.xz. Log mode: depth (dir.z) only.
            float HorizonDip(float3 dir)
            {
            #ifdef _ENABLE_ROLLING_LOG
                #ifdef _ROLLING_LOG_SPHERE
                float horizDist2 = dir.x * dir.x + dir.z * dir.z;
                #else
                float horizDist2 = dir.z * dir.z;
                #endif
                return _RL_Amount * _HorizonBendScale * horizDist2;
            #else
                return 0.0;
            #endif
            }

            float4 spectrumBars(float3 dir)
            {
                float barX = atan2(dir.z, dir.x) / (2.0 * 3.14159265) + 0.5;
                float barY = dir.y + HorizonDip(dir) - _BarYOffset;

                float bandLimit = _BarHeight + _BarGlow + 0.01;
                if (abs(barY) > bandLimit) return float4(0,0,0,0);

                float  barF    = barX * _BarCount;
                float  barID   = floor(barF);
                float  barFrac = frac(barF);

                float  halfGap = (1.0 - _BarWidth) * 0.5;
                if (barFrac < halfGap || barFrac > 1.0 - halfGap)
                    return float4(0,0,0,0);

                float quarterBars = _BarCount * 0.25;
                float quadID      = fmod(barID, _BarCount * 0.5);
                float mirrorID    = quadID < quarterBars ? quadID : _BarCount * 0.5 - 1.0 - quadID;
                float specU       = (mirrorID + 0.5) / quarterBars * 0.5;
                float barAmp      = saturate(sampleSpectrum(specU));
                float barTop      = barAmp * _BarHeight;

                if (barY < 0.0 || barY > barTop + _BarGlow)
                    return float4(0,0,0,0);

                float4 result;
                if (barY <= barTop)
                {
                    float  t        = barY / max(barTop, 0.001);
                    float3 barColor = lerp(_BarColorBot.rgb, _BarColorTop.rgb, t);

                    float tipGlow = smoothstep(0.6, 1.0, t) * 0.5;
                    barColor += _BarColorTop.rgb * tipGlow;

                    result = float4(barColor, 1.0);
                }
                else
                {
                    float glowT = (barY - barTop) / _BarGlow;
                    float glowA = pow(1.0 - glowT, 3.0);
                    result = float4(_BarGlowColor.rgb * glowA, glowA * 0.8);
                }

                return result;
            }

            // ---- Raymarched Sun ----
            #define RM_MAX_STEPS    32
            #define RM_MAX_DIST     4.0
            #define RM_SURF_DIST    0.002

            struct RM_Hit { float dist; float closest_dist; float3 p; };

            /// <summary>
            /// Calculates Blinn-Phong specular reflection.
            /// Uses the halfway vector between light and view directions for efficient specular computation.
            /// </summary>
            /// <param name="light_dir">Direction from surface to light source</param>
            /// <param name="ray_dir">Direction from surface to viewer (camera)</param>
            /// <param name="normal">Surface normal at the hit point</param>
            /// <returns>Specular intensity factor (0 to 1)</returns>
            float RM_specularBlinnPhong(float3 light_dir, float3 ray_dir, float3 normal)
            {
                return max(0.0, dot(normal, normalize(light_dir + ray_dir)));
            }

            /// <summary>
            /// Modulo 289 operation for noise generation.
            /// Ensures values wrap around at 289 to maintain noise continuity.
            /// </summary>
            /// <param name="x">Input vector</param>
            /// <returns>Vector with each component modulo 289</returns>
            float4 RM_mod289(float4 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }

            /// <summary>
            /// Permutation function for procedural noise generation.
            /// Creates pseudo-random values based on input coordinates.
            /// </summary>
            /// <param name="x">Input vector to permute</param>
            /// <returns>Permuted vector for noise calculation</returns>
            float4 RM_perm(float4 x)   { return RM_mod289(((x * 34.0) + 1.0) * x); }

            /// <summary>
            /// 3D Perlin-style noise function.
            /// Generates smooth, continuous pseudo-random values for surface displacement.
            /// Uses trilinear interpolation between gradient values at lattice points.
            /// </summary>
            /// <param name="p">3D position to sample noise at</param>
            /// <returns>Noise value typically in range [0, 1]</returns>
            float RM_noise(float3 p)
            {
                float3 a = floor(p), d = p - a;
                d = d * d * (3.0 - 2.0 * d);
                float4 b  = a.xxyy + float4(0,1,0,1);
                float4 k1 = RM_perm(b.xyxy);
                float4 k2 = RM_perm(k1.xyxy + b.zzww);
                float4 c  = k2 + a.zzzz;
                float4 k3 = RM_perm(c), k4 = RM_perm(c + 1.0);
                float4 o1 = frac(k3 * (1.0/41.0)), o2 = frac(k4 * (1.0/41.0));
                float4 o3 = o2 * d.z + o1 * (1.0 - d.z);
                float2 o4 = o3.yw * d.x + o3.xz * (1.0 - d.x);
                return o4.y * d.y + o4.x * (1.0 - d.y);
            }

            /// <summary>
            /// Signed Distance Function (SDF) for an animated, noise-displaced sphere.
            /// Defines the implicit surface by returning the distance to the nearest surface point.
            /// Combines multiple octaves of noise for detailed surface variation and animates over time.
            /// </summary>
            /// <param name="pos">3D position to evaluate the distance field</param>
            /// <returns>Signed distance to surface (negative = inside, positive = outside, 0 = on surface)</returns>
            float RM_SDF(float3 pos)
            {
                float3 p = float3(pos.xy, _Time.y * 0.3 + pos.z);
                float  n = (RM_noise(p) + RM_noise(p * 2.0) * 0.5 + RM_noise(p * 4.0) * 0.25) * 0.57;
                return length(pos) - 0.32 - n * 0.3;
            }

            /// <summary>
            /// Calculates the surface normal using finite differences.
            /// Samples the SDF at nearby points to approximate the gradient (normal direction).
            /// </summary>
            /// <param name="pos">3D position on or near the surface</param>
            /// <returns>Normalized surface normal vector</returns>
            float3 RM_getNormal(float3 pos)
            {
                float2 e = float2(0.002, 0.0);
                float3 n = float3(RM_SDF(pos - e.xyy), RM_SDF(pos - e.yxy), RM_SDF(pos - e.yyx));
                return normalize(RM_SDF(pos) - n);
            }

            /// <summary>
            /// Performs sphere tracing / ray marching through the signed distance field.
            /// Steps along the ray using the SDF value as the safe step distance.
            /// Tracks both the total distance traveled and the closest approach to any surface.
            /// </summary>
            /// <param name="p">Ray origin position</param>
            /// <param name="d">Ray direction (should be normalized)</param>
            /// <returns>Hit structure containing distance traveled, closest approach, and hit position</returns>
            RM_Hit RM_raymarch(float3 p, float3 d)
            {
                RM_Hit hit;
                hit.dist = 0.0;
                hit.closest_dist = RM_MAX_DIST;
                for (int i = 0; i < RM_MAX_STEPS; ++i)
                {
                    float sdf = RM_SDF(p);
                    p += d * sdf;
                    hit.closest_dist = min(hit.closest_dist, sdf);
                    hit.dist += sdf;
                    if (hit.dist >= RM_MAX_DIST || abs(sdf) <= RM_SURF_DIST) break;
                }
                hit.p = p;
                return hit;
            }
            // ---- End Raymarched Sun ----

            float4 frag(v2f i) : SV_Target
            {
                float3 dir  = normalize(i.dir);
                float  time = _Time.y;

                float  up      = dir.y + HorizonDip(dir);
                float  horizon = 1.0 - abs(up);
                // 0=noon→nb=0 (day), 0.5=midnight→nb=1 (night), matches C# tooltip
                float  nb      = smoothstep(0.0, 1.0, 1.0 - abs(_TimeOfDay - 0.5) * 2.0);
                float  sunsetT = smoothstep(0.0, 0.5, nb) * (1.0 - smoothstep(0.5, 1.0, nb));
                sunsetT *= pow(horizon, 1.5);

                float3 sunDir3  = normalize(_SunDir);
                float  sunDp    = dot(dir, sunDir3);
                float  sunAngle = acos(clamp(sunDp, -1, 1));

            #ifdef _MODE_SYNTHWAVE
                float mids  = sampleSpectrum(0.25);
                float highs = sampleSpectrum(0.7);
            #endif

            #ifdef _MODE_DAYNIGHT

                float3 dayColor;
                if (up >= 0.0)
                    dayColor = lerp(_DayHorizon.rgb, _DayZenith.rgb, saturate(up * 2.5));
                else
                    dayColor = lerp(_DayHorizon.rgb, _DayGround.rgb, saturate(-up * 3.0));

                float3 nightColor;
                if (up >= 0.0)
                    nightColor = lerp(_NightHorizon.rgb, _NightZenith.rgb, saturate(up * 2.5));
                else
                    nightColor = lerp(_NightHorizon.rgb, _NightGround.rgb, saturate(-up * 3.0));

                float3 sunsetColor = lerp(dayColor,
                    lerp(_SunsetHorizon.rgb, _SunsetZenith.rgb, saturate(up * 2.0)),
                    sunsetT);

                float3 skyColor = lerp(sunsetColor, nightColor, nb);

                float moonShape = moonDisc(dir, _MoonDir);
                skyColor = lerp(skyColor, _MoonColor.rgb, moonShape * nb);
                float3 moonDir3 = normalize(_MoonDir);
                float  moonDp   = dot(dir, moonDir3);
                float  moonGlow = pow(saturate(moonDp), 48.0) * nb * 0.3;
                skyColor += _MoonColor.rgb * moonGlow;

                float starVal = stars(dir, time) * _StarBrightness * nb * saturate(up + 0.1);
                skyColor += starVal;

                if (up > 0.0)
                {
                    float cd = cloudDensity(dir, time);
                    if (cd > 0.001)
                    {
                        float3 baseCloud   = lerp(_CloudShadow.rgb, _CloudColor.rgb, smoothstep(0.0, 1.0, cd));
                        float3 nightCloud  = _CloudColor.rgb * 0.15;
                        float3 sunsetCloud = lerp(baseCloud, float3(1.0, 0.55, 0.2), sunsetT * 0.6);
                        float3 cloudFinal  = lerp(sunsetCloud, nightCloud, nb);
                        skyColor = lerp(skyColor, cloudFinal, cd * (1.0 - nb * 0.8));
                    }
                }

            #else // _MODE_SYNTHWAVE

                float3 horizonColor = lerp(_SWHorizonA.rgb, _SWHorizonB.rgb, saturate(up * 2.0 + mids * 0.5));
                float3 skyColor = lerp(horizonColor, _SWZenith.rgb, smoothstep(0.0, 0.6, up + highs * 0.1));

                if (up < 0.15)
                {
                    float3 groundDir = float3(dir.x, dir.y - 0.0001, dir.z);
                    float2 gUV = groundDir.xz / (-groundDir.y + 0.0001) * 2.0;
                    gUV.y -= time * _SWGridSpeed;
                    float2 gFrac = frac(gUV);
                    float  gridX = smoothstep(0.96, 1.0, gFrac.x) + smoothstep(0.04, 0.0, gFrac.x);
                    float  gridZ = smoothstep(0.96, 1.0, gFrac.y) + smoothstep(0.04, 0.0, gFrac.y);
                    float  grid  = saturate(gridX + gridZ);
                    float  fade  = saturate(1.0 - (up + 0.15) / 0.3);
                    fade *= fade;
                    float3 gridCol = _SWGridColor.rgb;
                    skyColor = lerp(skyColor, gridCol, grid * fade * 0.9);
                }

                float swStar = stars(dir, time) * _StarBrightness * saturate(up + 0.1);
                swStar *= (0.5 + highs * 1.5);
                float3 starTint = lerp(float3(0.8, 0.2, 1.0), float3(0.2, 0.8, 1.0), hash11(swStar * 13.7));
                skyColor += swStar * starTint;

                float bandMask = pow(horizon, 4.0) * saturate(up + 0.05);
                skyColor += _SWHorizonA.rgb * bandMask * mids * 0.5;

                float4 bars = spectrumBars(dir);
                if (bars.a > 0.001)
                {
                    skyColor = lerp(skyColor, bars.rgb, bars.a * 0.9);
                    skyColor += bars.rgb * bars.a * 0.4;
                }

            #endif

            #ifdef _SUNMODE_STANDARD
                float glow = pow(saturate(sunDp), _SunGlowSharpness) * (1.0 - nb) * _SunGlowStrength;
                skyColor += _SunGlowColor.rgb * glow;
                float sunInner = _SunSize * (1.0 - max(_SunEdgeSoftness, 0.0001));
                float sunDisc = smoothstep(_SunSize, sunInner, sunAngle) * (1.0 - nb);
                skyColor = lerp(skyColor, _SunColor.rgb, sunDisc);

            #elif defined(_SUNMODE_SYNTHWAVE)
                float3 swSunDir3 = normalize(_SWSunDir);
                float swDp = dot(dir, swSunDir3);
                float swAngle = acos(clamp(swDp, -1.0, 1.0));

                // Wide corona that fades below the horizon
                float swCoronaFade = saturate(up * 3.0 + 0.6);
                float swGlow = pow(saturate(swDp), 3.0) * swCoronaFade;
                skyColor += lerp(_SWSunColor.rgb, _SWSunColor2.rgb, 0.5) * swGlow * 1.5;

                // Upper semicircle — hard clip at bent horizon
                float swDisc = smoothstep(_SWSunSize, _SWSunSize * 0.96, swAngle);
                if (swDisc > 0.001 && up >= 0.0)
                {
                    // discV: 0 at horizon, 1 at top of disc — use dir.y not up so bands stay straight.
                    // No saturate: lets the band pattern continue into the rolling-log-exposed lower half.
                    float  discV     = dir.y / _SWSunSize;
                    float  bandT     = frac(discV * _SWSunBands);
                    float  band      = step(0.4, bandT); // 40% black gap, 60% colour
                    float3 discColor = lerp(_SWSunColor2.rgb, _SWSunColor.rgb, saturate(discV));
                    skyColor = lerp(skyColor, discColor * band, swDisc);
                }

            #elif defined(_SUNMODE_RAYMARCHED)
                float3 rmSunDir3 = normalize(_RMSunDir);
                float  rmDp      = dot(dir, rmSunDir3);
                float  rmAngle   = acos(clamp(rmDp, -1.0, 1.0));

                // Smooth fade to zero at the bounding radius so the glow leaves no seam
                float rmEdgeFade = 1.0 - smoothstep(_RMSunSize * 2.0, _RMSunSize * 3.0, rmAngle);
                if (rmEdgeFade > 0.001)
                {
                    // Build tangent frame; guard against dir parallel to world-up
                    float3 rmTangent   = normalize(cross(abs(rmSunDir3.y) < 0.99 ? float3(0,1,0) : float3(1,0,0), rmSunDir3));
                    float3 rmBitangent = cross(rmSunDir3, rmTangent);
                    float2 rmUV        = float2(dot(dir, rmTangent), dot(dir, rmBitangent)) / _RMSunSize;

                    float3 rmRayPos = float3(0, 0, -1);
                    float3 rmRayDir = normalize(float3(rmUV, 1));
                    RM_Hit rmHit    = RM_raymarch(rmRayPos, rmRayDir);

                    float  rmGlowVal   = pow(max(0.0, 1.0 - rmHit.closest_dist), _RMGlowSharpness) * _RMGlowStrength;
                    float3 rmGlowColor = rmGlowVal * _RMGlowColor.rgb * (
                        max(0.0, dot(rmUV, float2( 0.707,  0.707))) * _RMLight1Color.rgb +
                        max(0.0, dot(rmUV, float2(-0.707, -0.707))) * _RMLight2Color.rgb +
                        _RMLight3Color.rgb
                    );
                    skyColor += rmGlowColor * rmEdgeFade;

                    if (rmHit.closest_dist < RM_SURF_DIST)
                    {
                        float3 rmNormal  = RM_getNormal(rmHit.p);
                        float3 rmViewDir = normalize(rmRayPos - rmHit.p);
                        float3 rmLit     = float3(0, 0, 0);

                        float rmFacing = max(0.0, sqrt(dot(rmNormal, float3( 0.707,  0.707, 0))) * 1.5 - dot(rmNormal, -rmRayDir));
                        rmLit = lerp(float3(0,0,0), _RMLight1Color.rgb, _RMLight1Strength * rmFacing * rmFacing * rmFacing);

                        rmFacing = max(0.0, sqrt(dot(rmNormal, float3(-0.707, -0.707, 0))) * 1.5 - dot(rmNormal, -rmRayDir));
                        rmLit += lerp(float3(0,0,0), _RMLight2Color.rgb, _RMLight2Strength * rmFacing * rmFacing * rmFacing);

                        rmFacing = max(0.0, sqrt(dot(rmNormal, float3(0, 0, -1))) * 1.5 - dot(rmNormal, -rmRayDir));
                        rmLit += lerp(float3(0,0,0), _RMLight3Color.rgb, _RMLight3Strength * rmFacing * rmFacing * rmFacing);

                        if (_RMSpecular1Enabled > 0.5)
                        {
                            float rmSpec1 = pow(RM_specularBlinnPhong(normalize(_RMSpecular1Position.xyz - rmHit.p), rmViewDir, rmNormal), _RMSpecular1Sharpness);
                            rmLit += lerp(float3(0,0,0), _RMSpecular1Color.rgb, rmSpec1 * _RMSpecular1Strength);
                        }
                        if (_RMSpecular2Enabled > 0.5)
                        {
                            float rmSpec2 = pow(RM_specularBlinnPhong(normalize(_RMSpecular2Position.xyz - rmHit.p), rmViewDir, rmNormal), _RMSpecular2Sharpness);
                            rmLit += lerp(float3(0,0,0), _RMSpecular2Color.rgb, rmSpec2 * _RMSpecular2Strength);
                        }

                        rmLit    = pow(max(rmLit, 0.0), float3(1.25, 1.25, 1.25)) * _RMEmissionBoost;
                        skyColor = rmLit + rmGlowColor;
                    }
                }

            #elif defined(_SUNMODE_TEXTURED)
                // Glow: shared with Standard Sun — uses _SunGlowColor/_SunGlowSharpness/_SunGlowStrength
                float txGlow = pow(saturate(sunDp), _SunGlowSharpness) * _SunGlowStrength;
                skyColor += _SunGlowColor.rgb * txGlow;

                // Expanded bounding angle to include spike tips
                float txMaxAngle = _TexSunSize * (1.0 + _TexSunSpikeLength + 0.15);
                if (sunAngle < txMaxAngle)
                {
                    // Tangent frame around shared _SunDir
                    float3 txTangent   = normalize(cross(abs(sunDir3.y) < 0.99 ? float3(0,1,0) : float3(1,0,0), sunDir3));
                    float3 txBitangent = cross(sunDir3, txTangent);
                    // txLocal: 0,0 = disc centre; length 1 = disc edge
                    float2 txLocal     = float2(dot(dir, txTangent), dot(dir, txBitangent)) / _TexSunSize;
                    float  txDist      = length(txLocal);

                    // Spikes — cosine-modulated radius beyond the disc edge
                    float txAng      = atan2(txLocal.y, txLocal.x);
                    float spikePulse = pow(max(0.0, cos(txAng * _TexSunSpikeCount)), _TexSunSpikeSharpness);
                    float spikeR     = 1.0 + spikePulse * _TexSunSpikeLength;
                    // Fade out at spike tip, fade in from disc edge so spikes don't bleed inside
                    float spikeMask  = smoothstep(spikeR, spikeR * 0.88, txDist)
                                     * smoothstep(0.85, 1.02, txDist);
                    skyColor = lerp(skyColor, _SunColor.rgb * 1.3, spikeMask);

                    // Textured disc (soft edge at txDist == 1)
                    float txDisc = smoothstep(1.0, 0.92, txDist);
                    if (txDisc > 0.001)
                    {
                        float2 txUV = (txLocal * 0.5 + 0.5) * _TexSunTex_ST.xy + _TexSunTex_ST.zw;
                        float4 txSample = tex2D(_TexSunTex, txUV);
                        skyColor = lerp(skyColor, txSample.rgb, txDisc * txSample.a);
                    }
                }

            #endif

                return float4(skyColor, 1.0);
            }

            ENDHLSL
        }
    }
    Fallback Off
}
