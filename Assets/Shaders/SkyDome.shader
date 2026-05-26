Shader "Custom/SkyDome"
{
    // The goal of this shader is to render a sky dome with a three-stop vertical
    // gradient driven by the mesh's V coordinate. _ColorA is at the bottom (horizon),
    // _ColorB is the midpoint, and _ColorC is at the top (zenith), with the two
    // segments blended linearly so the dome reads as a simple stylised sky.
    //
    // This is written as a vert+frag pass rather than a surface shader because the
    // dome is unlit: it just outputs a color from UV. Surface shaders are built
    // around Unity's lighting pipeline (albedo/normal/metallic feeding auto-generated
    // forward/deferred/shadow passes), which would add cost and variants for nothing.
    Properties
    {
        _ColorA ("Color A", Color) = (1,1,1,1)
        _ColorB ("Color B", Color) = (1,1,1,1)
        _ColorC ("Color C", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _ColorA;
            float4 _ColorB;
            float4 _ColorC;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float t = 1 - i.uv.y;
                if (t < 0.5) {
                    return lerp(_ColorA, _ColorB, t * 2);
                }
                return lerp(_ColorB, _ColorC, (t - 0.5) * 2);
            }
            ENDCG
        }
    }
}
