#ifndef TOON_LIGHTING_INCLUDED
#define TOON_LIGHTING_INCLUDED

// Custom Surface Shader lighting model — ports the original Toon.shader frag
// (hard-banded diffuse + flat Blinn-Phong specular + rim + optional screen-space
// halftone) into a reusable lighting function so PBR-style surface shaders can
// opt into the toon look without rewriting their lighting math.
//
// Usage (in a surface shader CGPROGRAM block):
//   #pragma surface surf Toon fullforwardshadows noambient novertexlights noforwardadd
//   #pragma multi_compile _ _TL_HALFTONE_ON
//   #include "Includes/ToonLighting.cginc"
//
// Then put `float4 screenPos;` in the Input struct, declare `SurfaceOutputToon`
// instead of SurfaceOutputStandard, and have surf() copy `IN.screenPos` onto
// `o.screenPos` so the halftone path can sample its lattice.
//
// All style properties (ambient/specular/rim/halftone params + _TL_HALFTONE_ON
// keyword) are pushed as scene-wide globals by ToonStyleManager via
// Shader.SetGlobal* / Shader.EnableKeyword

#include "Lighting.cginc"

float4 _TL_AmbientColor;
float4 _TL_SpecularColor;
float  _TL_Glossiness;
float4 _TL_RimColor;
float  _TL_RimAmount;
float  _TL_RimThreshold;

float  _TL_HalftoneScale;
float  _TL_HalftoneDotSize;
float  _TL_HalftoneSoftness;
float  _TL_HalftoneFalloff;
float  _TL_HalftoneAngle;

struct SurfaceOutputToon
{
    fixed3 Albedo;
    fixed3 Normal;
    fixed3 Emission;
    fixed  Alpha;
    // ComputeScreenPos output — surf must copy IN.screenPos here for halftone.
    float4 screenPos;
};

// Screen-space halftone: distance from each pixel to its nearest cell center,
// compared against a tone-driven dot radius. Returns 1 inside the dot
// (shadowed), 0 outside (lit). Cell size is fixed in screen pixels, so distant
// objects get oversized dots relative to their silhouette.
float ToonHalftoneMask(float2 pixelPos, float tone)
{
    float cell = max(_TL_HalftoneScale, 1.0);

    float a  = radians(_TL_HalftoneAngle);
    float cs = cos(a);
    float sn = sin(a);
    float2 rotated = float2(pixelPos.x * cs - pixelPos.y * sn,
                            pixelPos.x * sn + pixelPos.y * cs);

    float2 gridUV     = rotated / cell;
    float2 cellCenter = floor(gridUV) + 0.5;
    float  dist       = length(gridUV - cellCenter);

    float darkness = pow(saturate(1.0 - tone), _TL_HalftoneFalloff);
    float radius   = sqrt(darkness) * _TL_HalftoneDotSize;
    float aa       = max(fwidth(dist), 1e-4) + _TL_HalftoneSoftness;

    return 1.0 - smoothstep(radius - aa, radius + aa, dist);
}

half4 LightingToon(SurfaceOutputToon s, half3 lightDir, half3 viewDir, half atten)
{
    float3 normal = normalize(s.Normal);
    float3 v      = normalize(viewDir);
    float3 l      = normalize(lightDir);

    float NdotL   = dot(l, normal);
    float litMask = smoothstep(0.0, 0.01, NdotL * atten);

#ifdef _TL_HALFTONE_ON
    // Dots grow with shadow depth: 0 at the terminator, → 1 on back-facing or
    // fully cast-shadowed surfaces. Inside the shadow band, gaps reveal the lit
    // colour and the dots themselves stay shadowed.
    float  darkness = max(saturate(-NdotL), 1.0 - atten);
    float2 pixelPos = s.screenPos.xy / max(s.screenPos.w, 1e-4) * _ScreenParams.xy;
    float  dotMask  = ToonHalftoneMask(pixelPos, 1.0 - darkness);
    float  lightIntensity = lerp(1.0 - dotMask, 1.0, litMask);
#else
    float  lightIntensity = litMask;
#endif

    float3 light = lightIntensity * _LightColor0.rgb;

    // Blinn-Phong specular smoothstepped to a flat highlight. Multiplying by
    // lightIntensity keeps the highlight hidden in shadow.
    float3 halfVector = normalize(l + v);
    float  NdotH      = dot(normal, halfVector);
    float  specularIntensity       = pow(NdotH * lightIntensity, _TL_Glossiness * _TL_Glossiness);
    float  specularIntensitySmooth = smoothstep(0.005, 0.01, specularIntensity);
    float3 specular = specularIntensitySmooth * _TL_SpecularColor.rgb;

    // Rim is gated by pow(NdotL, threshold) so it only appears on lit sides.
    float rimDot       = 1.0 - dot(v, normal);
    float rimIntensity = rimDot * pow(saturate(NdotL), _TL_RimThreshold);
    rimIntensity       = smoothstep(_TL_RimAmount - 0.01, _TL_RimAmount + 0.01, rimIntensity);
    float3 rim = rimIntensity * _TL_RimColor.rgb;

    half4 c;
    c.rgb = (light + _TL_AmbientColor.rgb + specular + rim) * s.Albedo;
    c.a   = s.Alpha;
    return c;
}

#endif
