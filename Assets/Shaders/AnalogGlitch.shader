Shader "Custom/AnalogGlitch"
{
    Properties
    {
        _MainTex          ("Texture", 2D) = "white" {}
        _ScanLineJitter   ("Scan Line Jitter", Float) = 0
        _VerticalJump     ("Vertical Jump (amount, time)", Vector) = (0, 0, 0, 0)
        _HorizontalShake  ("Horizontal Shake", Float) = 0
        _ColorDrift       ("Color Drift", Float) = 0
        _HorizontalRipple ("Horizontal Ripple", Float) = 0
        [Toggle(GRAYSCALE)] _Grayscale ("Grayscale", Float) = 0
        [Toggle(TINT_DRIFT)] _TintDrift ("Tint Color Drift", Float) = 0
        _DriftColor       ("Drift Color", Color) = (0, 0.4, 0, 1)
        _DriftIntensity   ("Drift Color Intensity", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert_img
            #pragma fragment frag
            #pragma target   3.0
            #pragma multi_compile _ GRAYSCALE
            #pragma multi_compile _ TINT_DRIFT

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_TexelSize;

            half  _ScanLineJitter;
            half2 _VerticalJump;
            half  _HorizontalShake;
            half  _ColorDrift;
            half  _HorizontalRipple;
            half4 _DriftColor;
            half  _DriftIntensity;

            // Bob Jenkins' one-at-a-time hash.
            uint JenkinsHash(uint x)
            {
                x += (x << 10u);
                x ^= (x >> 6u);
                x += (x << 3u);
                x ^= (x >> 11u);
                x += (x << 15u);
                return x;
            }

            uint JenkinsHash(uint2 v)
            {
                return JenkinsHash(v.x ^ JenkinsHash(v.y));
            }

            // Construct a float in [0, 1) from the upper 23 mantissa bits.
            float GenerateHashedRandomFloat(uint2 v)
            {
                return asfloat(0x3f800000u | (JenkinsHash(v) >> 9)) - 1.0;
            }

            // 1D gradient (value-derivative) noise in roughly [-1, 1]
            float GradientHash1D(float i, uint seed)
            {
                uint h = JenkinsHash(uint2(asuint(i), seed));
                return (asfloat(0x3f800000u | (h >> 9)) - 1.0) * 2.0 - 1.0; // [-1, 1]
            }

            half GradientNoise(float x, uint seed)
            {
                float i0 = floor(x);
                float f  = x - i0;

                // Quintic fade for C2 continuity.
                float u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);

                float g0 = GradientHash1D(i0,        seed);
                float g1 = GradientHash1D(i0 + 1.0,  seed);

                float v0 = g0 * f;
                float v1 = g1 * (f - 1.0);

                // Normalise the ~[-0.5, 0.5] gradient-noise range to ~[-1, 1].
                return lerp(v0, v1, u) * 2.0;
            }
            // --------------------------------------------------------------

            half MirrorRepeat(half x) { return 1 - abs(frac(x * 0.5h) * 2 - 1); }

            half4 frag(v2f_img input) : SV_Target
            {
                half u = input.uv.x;
                half v = input.uv.y;

                // Vertical pixel coordinate (_MainTex_TexelSize.w == texture height)
                uint p_y = (uint)floor(v * _MainTex_TexelSize.w);

                // Safe time parameters (_Time.y == time in seconds)
                float t = fmod(_Time.y, 600);
                uint fcount = (uint)floor(_Time.y * 60.0);

                // Scan line jitter
                uint2 jitterSeed1 = uint2(p_y, fcount);
                uint2 jitterSeed2 = uint2(p_y, fcount + 1000);
                half jitter1 = GenerateHashedRandomFloat(jitterSeed1) * 2 - 1;
                half jitter2 = GenerateHashedRandomFloat(jitterSeed2) * 2 - 1;
                jitter2 = jitter2 * jitter2 * jitter2 * jitter2 * jitter2;
                half jitter = (jitter1 + jitter2 * 2.5) * _ScanLineJitter;

                // Vertical jump
                half v_disp = frac(v + _VerticalJump.y);
                v_disp = max(1 - smoothstep(0, 0.05, v_disp), v_disp);
                half jump = lerp(v, v_disp, _VerticalJump.x);

                // Color drift
                half drift = _ColorDrift * 0.1;
                half noise1 = GradientNoise(jump * 1.5 - t * 10.11, 1);
                half noise2 = GradientNoise(jump * 1.5 - t * 13.04, 2);

                // Horizontal ripple
                half burst = abs(noise1);
                burst = burst / (burst + (1 - burst) * lerp(6, 1, _HorizontalRipple));
                half wiggle = abs(GradientNoise(jump * 20 + t * 16, 12));
                half ripple = 0.3 * _HorizontalRipple * burst * (wiggle + abs(jitter2));

                // Displaced samples
                half x = u + jitter + _HorizontalShake - ripple;
                half2 uv1 = half2(MirrorRepeat(x + noise1 * drift), jump);
                half2 uv2 = half2(MirrorRepeat(x + noise2 * drift), jump);
                half2 uv3 = half2(MirrorRepeat(x - noise2 * drift), jump);

                half3 lumaW = half3(0.299, 0.587, 0.114);

            #ifdef TINT_DRIFT
                // Force the chromatic drift into one chosen color. Measure how far
                // the displaced samples diverge from the undisplaced center, then
                // add that divergence as _DriftColor on top of the base image.
                half2 uvc = half2(MirrorRepeat(x), jump);
                half cen = dot(tex2D(_MainTex, uvc).rgb, lumaW);
                half fringe = abs(dot(tex2D(_MainTex, uv1).rgb, lumaW) - cen)
                            + abs(dot(tex2D(_MainTex, uv3).rgb, lumaW) - cen);

                #ifdef GRAYSCALE
                    half3 base = cen;
                #else
                    half3 base = tex2D(_MainTex, uvc).rgb;
                #endif

                half3 col = base + _DriftColor.rgb * _DriftIntensity * fringe;
            #else
                #ifdef GRAYSCALE
                    // Grayscale base, colored drift on top: sample Rec. 601 luma at
                    // each drifted UV. With no drift the three lumas match (pure
                    // gray); the channel displacement is what reintroduces color.
                    half r = dot(tex2D(_MainTex, uv1).rgb, lumaW);
                    half g = dot(tex2D(_MainTex, uv2).rgb, lumaW);
                    half b = dot(tex2D(_MainTex, uv3).rgb, lumaW);
                #else
                    half r = tex2D(_MainTex, uv1).r;
                    half g = tex2D(_MainTex, uv2).g;
                    half b = tex2D(_MainTex, uv3).b;
                #endif
                half3 col = half3(r, g, b);
            #endif

                return half4(col, 1);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
