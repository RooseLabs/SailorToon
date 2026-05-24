Shader "Custom/SynthwavePostProcess"
{
    Properties
    {
        _MainTex         ("Screen Texture", 2D) = "white" {}
        _ChromaStrength  ("Chromatic Aberration Strength", Range(0.0, 0.05)) = 0.02
        _ChromaPulse     ("Bass Pulse Multiplier", Range(0.0, 5.0)) = 5.0
        _Saturation      ("Saturation", Range(0.0, 4.0)) = 1.8
        _VignetteStr     ("Vignette Strength", Range(0.0, 1.0)) = 0.45
        _VignettePow     ("Vignette Hardness", Range(1.0, 8.0)) = 3.0
        _ScanlineStr     ("Scanline Strength", Range(0.0, 1.0)) = 0.08
        _ScanlineCount   ("Scanline Count", Range(100, 1200)) = 600.0
        _ShadowTint      ("Shadow Tint", Color) = (0.05, 0.0, 0.1, 1)
        _HighlightTint   ("Highlight Tint", Color) = (0.0, 0.05, 0.1, 1)
        _Brightness      ("Brightness", Range(0.5, 1.5)) = 1.05
        _Contrast        ("Contrast", Range(0.5, 2.0)) = 1.1
        _BassValue       ("Bass Value (0..1)", Range(0.0, 1.0)) = 0.0
        _ZoomStrength    ("Bass Zoom Strength", Range(0.0, 0.1)) = 0.03
        _ZoomCurve       ("Zoom Curve Power", Range(0.1, 1.0)) = 0.4
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   3.0

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_TexelSize;

            float  _ChromaStrength;
            float  _ChromaPulse;
            float  _Saturation;
            float  _VignetteStr;
            float  _VignettePow;
            float  _ScanlineStr;
            float  _ScanlineCount;
            float4 _ShadowTint;
            float4 _HighlightTint;
            float  _Brightness;
            float  _Contrast;
            float  _BassValue;
            float  _ZoomStrength;
            float  _ZoomCurve;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f     { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            float3 RGBtoHSV(float3 c)
            {
                float4 K = float4(0.0, -1.0/3.0, 2.0/3.0, -1.0);
                float4 p = c.g < c.b ? float4(c.bg, K.wz) : float4(c.gb, K.xy);
                float4 q = c.r < p.x ? float4(p.xyw, c.r) : float4(c.r, p.yzx);
                float  d = q.x - min(q.w, q.y);
                float  e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)),
                              d / (q.x + e),
                              q.x);
            }

            float3 HSVtoRGB(float3 c)
            {
                float4 K = float4(1.0, 2.0/3.0, 1.0/3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            float3 ChromaticAberration(float2 uv, float strength)
            {
                float2 dir    = uv - 0.5;
                float  dist   = length(dir);
                float2 offset = normalize(dir + 0.0001) * dist * strength;
                float r = tex2D(_MainTex, uv + offset).r;
                float g = tex2D(_MainTex, uv).g;
                float b = tex2D(_MainTex, uv - offset).b;
                return float3(r, g, b);
            }

            float Vignette(float2 uv, float strength, float power)
            {
                float2 d = uv - 0.5;
                d.x *= _MainTex_TexelSize.z / _MainTex_TexelSize.w;
                float dist = length(d);
                float vig  = 1.0 - pow(saturate(dist * 2.0), power);
                return lerp(1.0, vig, strength);
            }

            float Scanlines(float2 uv, float strength, float count)
            {
                float scanVal  = sin(uv.y * count * 3.14159265) * 0.5 + 0.5;
                float scanMult = lerp(1.0, scanVal, strength);
                return scanMult;
            }

            float3 ColorGrade(float3 col)
            {
                col = (col - 0.5) * _Contrast + 0.5;
                col *= _Brightness;
                float lum           = dot(col, float3(0.299, 0.587, 0.114));
                float shadowMask    = pow(1.0 - saturate(lum * 2.0), 2.0);
                float highlightMask = pow(saturate(lum * 2.0 - 1.0), 2.0);
                col += _ShadowTint.rgb    * shadowMask;
                col += _HighlightTint.rgb * highlightMask;
                return saturate(col);
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float bassCurved = pow(_BassValue, _ZoomCurve);
                float zoom       = 1.0 - bassCurved * _ZoomStrength;
                uv               = (uv - 0.5) * zoom + 0.5;

                float  chromaAmt = _ChromaStrength * (1.0 + bassCurved * _ChromaPulse);
                float3 col       = ChromaticAberration(uv, chromaAmt);

                float3 hsv = RGBtoHSV(col);
                hsv.y      = saturate(hsv.y * _Saturation);
                col        = HSVtoRGB(hsv);

                col  = ColorGrade(col);
                col *= Scanlines(uv, _ScanlineStr, _ScanlineCount);
                col *= Vignette(uv, _VignetteStr, _VignettePow);

                return float4(col, 1.0);
            }

            ENDHLSL
        }
    }

    Fallback Off
}
