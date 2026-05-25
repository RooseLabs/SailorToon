Shader "Custom/ProceduralSkybox"
{
    Properties
    {
        [KeywordEnum(DayNight, Synthwave)] _Mode ("Sky Mode", Float) = 0

        _TimeOfDay      ("Time Of Day (0=noon 1=midnight)", Range(0,1)) = 0.0
        _NightBlend     ("Night Blend (auto-driven)", Range(0,1)) = 0.0
        _SunDir         ("Sun Direction", Vector) = (0.5, 0.8, 0.3, 0)
        _SunColor       ("Sun Color", Color) = (1.0, 0.95, 0.8, 1)
        _SunSize        ("Sun Disc Size", Range(0.001, 0.1)) = 0.02
        _SunGlowSize    ("Sun Glow Size", Range(0.01, 0.5)) = 0.15
        _SunGlowColor   ("Sun Glow Color", Color) = (1.0, 0.6, 0.2, 1)
        _MoonDir        ("Moon Direction", Vector) = (-0.5, 0.6, -0.3, 0)
        _MoonColor      ("Moon Color", Color) = (0.9, 0.95, 1.0, 1)
        _MoonSize       ("Moon Disc Size", Range(0.001, 0.1)) = 0.025
        _MoonCrescent   ("Moon Crescent Offset", Range(0.0, 1.0)) = 0.4
        _DayZenith      ("Day Zenith Color", Color) = (0.1, 0.4, 0.9, 1)
        _DayHorizon     ("Day Horizon Color", Color) = (0.6, 0.8, 1.0, 1)
        _DayGround      ("Day Ground Color", Color) = (0.3, 0.25, 0.2, 1)
        _SunsetZenith   ("Sunset Zenith Color", Color) = (0.15, 0.2, 0.6, 1)
        _SunsetHorizon  ("Sunset Horizon Color", Color) = (0.95, 0.4, 0.1, 1)
        _NightZenith    ("Night Zenith Color", Color) = (0.01, 0.01, 0.06, 1)
        _NightHorizon   ("Night Horizon Color", Color) = (0.03, 0.04, 0.12, 1)
        _NightGround    ("Night Ground Color", Color) = (0.01, 0.01, 0.02, 1)
        _CloudColor     ("Cloud Color", Color) = (1.0, 0.98, 0.95, 1)
        _CloudShadow    ("Cloud Shadow Color", Color) = (0.6, 0.65, 0.75, 1)
        _CloudCover     ("Cloud Cover", Range(0,1)) = 0.5
        _CloudSharpness ("Cloud Sharpness", Range(1, 8)) = 4.0
        _CloudSpeed     ("Cloud Speed", Range(0, 0.1)) = 0.01
        _CloudScale     ("Cloud Scale", Range(1, 10)) = 4.0
        _StarDensity    ("Star Density", Range(50, 500)) = 200.0
        _StarBrightness ("Star Brightness", Range(0, 2)) = 1.0
        _StarTwinkle    ("Star Twinkle Speed", Range(0, 5)) = 1.0
        _SWHorizonA     ("Synthwave Horizon Color A", Color) = (0.9, 0.1, 0.6, 1)
        _SWHorizonB     ("Synthwave Horizon Color B", Color) = (0.4, 0.0, 0.9, 1)
        _SWZenith       ("Synthwave Zenith Color", Color) = (0.02, 0.0, 0.12, 1)
        _SWGridColor    ("Synthwave Grid Color", Color) = (0.8, 0.1, 0.9, 1)
        _SWGridSpeed    ("Synthwave Grid Speed", Range(0,2)) = 0.5
        _SWSunColor     ("Synthwave Sun Color A", Color) = (1.0, 0.6, 0.1, 1)
        _SWSunColor2    ("Synthwave Sun Color B", Color) = (0.9, 0.1, 0.5, 1)
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
        _HorizonBendScale ("Horizon Bend Scale (matches Rolling Log)", Range(0, 100)) = 30.0
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
            #pragma multi_compile _ _ENABLE_ROLLING_LOG
            #pragma multi_compile _ _ROLLING_LOG_SPHERE
            #pragma target 3.0

            #include "UnityCG.cginc"

            float  _TimeOfDay, _NightBlend;

            float3 _SunDir;
            float4 _SunColor, _SunGlowColor;
            float  _SunSize, _SunGlowSize;

            float3 _MoonDir;
            float4 _MoonColor;
            float  _MoonSize, _MoonCrescent;

            float4 _DayZenith, _DayHorizon, _DayGround;
            float4 _SunsetZenith, _SunsetHorizon;
            float4 _NightZenith, _NightHorizon, _NightGround;

            float4 _CloudColor, _CloudShadow;
            float  _CloudCover, _CloudSharpness, _CloudSpeed, _CloudScale;

            float  _StarDensity, _StarBrightness, _StarTwinkle;

            float4 _SWHorizonA, _SWHorizonB, _SWZenith;
            float4 _SWGridColor, _SWSunColor, _SWSunColor2;
            float  _SWGridSpeed;

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
                float disc = smoothstep(_MoonSize, _MoonSize * 0.5, acos(clamp(dp, -1, 1)));
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

                float4 result = float4(0,0,0,0);

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

            float4 frag(v2f i) : SV_Target
            {
                float3 dir  = normalize(i.dir);
                float  time = _Time.y;

                float  up      = dir.y + HorizonDip(dir);
                float  horizon = 1.0 - abs(up);
                float  nb      = saturate(_NightBlend);
                float  sunsetT = smoothstep(0.0, 0.5, nb) * (1.0 - smoothstep(0.5, 1.0, nb));
                sunsetT *= pow(horizon, 1.5);

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

                float3 sunDir3 = normalize(_SunDir);
                float  sunDp   = dot(dir, sunDir3);
                float  glow    = pow(saturate(sunDp), 32.0) * (1.0 - nb) * 0.8;
                skyColor += _SunGlowColor.rgb * glow;
                float sunAngle = acos(clamp(sunDp, -1, 1));
                float sunDisc  = smoothstep(_SunSize, _SunSize * 0.5, sunAngle) * (1.0 - nb);
                skyColor = lerp(skyColor, _SunColor.rgb, sunDisc);

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

            #else

                float bass  = sampleSpectrum(0.02);
                float mids  = sampleSpectrum(0.25);
                float highs = sampleSpectrum(0.7);

                float3 horizonColor = lerp(_SWHorizonA.rgb, _SWHorizonB.rgb,
                                          saturate(up * 2.0 + mids * 0.5));
                float3 skyColor     = lerp(horizonColor, _SWZenith.rgb,
                                          smoothstep(0.0, 0.6, up + highs * 0.1));

                float3 swSunDir = normalize(_SunDir);
                float  swSunDp  = dot(dir, swSunDir);
                float  swAngle  = acos(clamp(swSunDp, -1, 1));
                float  swGlow   = pow(saturate(swSunDp), 16.0) * (1.0 + bass * 0.5);
                skyColor += lerp(_SWSunColor.rgb, _SWSunColor2.rgb, 0.5) * swGlow * 0.6;

                float swDisc = smoothstep(_SunSize * 1.5, _SunSize * 0.7, swAngle);
                if (swDisc > 0.01)
                {
                    float  localY    = dot(dir, float3(0,1,0));
                    float  bandT     = frac(localY * 20.0 + bass * 2.0);
                    float  band      = step(0.45, bandT);
                    float3 discColor = lerp(_SWSunColor.rgb, _SWSunColor2.rgb, 1.0 - localY);
                    skyColor = lerp(skyColor, discColor * band, swDisc);
                }

                if (up < 0.15)
                {
                    float3 groundDir = float3(dir.x, dir.y - 0.0001, dir.z);
                    float2 gUV = groundDir.xz / (-groundDir.y + 0.0001) * 2.0;
                    gUV.y -= time * _SWGridSpeed + bass * 0.3;
                    float2 gFrac = frac(gUV);
                    float  gridX = smoothstep(0.96, 1.0, gFrac.x) + smoothstep(0.04, 0.0, gFrac.x);
                    float  gridZ = smoothstep(0.96, 1.0, gFrac.y) + smoothstep(0.04, 0.0, gFrac.y);
                    float  grid  = saturate(gridX + gridZ);
                    float  fade  = saturate(1.0 - (up + 0.15) / 0.3);
                    fade *= fade;
                    float3 gridCol = _SWGridColor.rgb * (0.6 + bass * 1.5);
                    skyColor = lerp(skyColor, gridCol, grid * fade * 0.9);
                }

                float swStar = stars(dir, time) * _StarBrightness * saturate(up + 0.1);
                swStar *= (0.5 + highs * 1.5);
                float3 starTint = lerp(float3(0.8, 0.2, 1.0), float3(0.2, 0.8, 1.0),
                                       hash11(swStar * 13.7));
                skyColor += swStar * starTint;

                float bandMask  = pow(horizon, 4.0) * saturate(up + 0.05);
                skyColor += _SWHorizonA.rgb * bandMask * mids * 0.5;

                float4 bars = spectrumBars(dir);
                if (bars.a > 0.001)
                {
                    skyColor = lerp(skyColor, bars.rgb, bars.a * 0.9);
                    skyColor += bars.rgb * bars.a * 0.4;
                }

            #endif

                return float4(skyColor, 1.0);
            }

            ENDHLSL
        }
    }
    Fallback Off
}
