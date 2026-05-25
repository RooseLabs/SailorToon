Shader "Custom/Swaying"
{
    Properties
    {
        _ColorA ("Color A", Color) = (0.2, 0.8, 0.2, 1)
        _ColorB ("Color B", Color) = (0.4, 0.7, 0.1, 1)
        _ColorC ("Color C", Color) = (0.8, 0.4, 0.1, 1)
        _ColorD ("Color D", Color) = (0.9, 0.8, 0.2, 1)
        _ColorBottomHalf ("Bottom Half Color", Color) = (0.4, 0.2, 0.1, 1)
        _ColorChangeSpeed ("Color Change Speed", Float) = 1.0
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _SwayingStrength ("Swaying Strength", Float) = 0.2
        _SwayingSpeed ("Swaying Speed", Float) = 2
        _TopInfluence ("Top Influence", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.0
        #pragma multi_compile _ _ENABLE_ROLLING_LOG
        #pragma multi_compile _ _ROLLING_LOG_SPHERE

        #include "Includes/RollingLog.cginc"

        float4 _CameraRight;

        fixed4 _ColorA, _ColorB, _ColorC, _ColorD, _ColorBottomHalf;
        half _ColorChangeSpeed, _Glossiness, _Metallic, _SwayingStrength, _SwayingSpeed, _TopInfluence;

        struct Input
        {
            float2 meshUV;
        };

        float random(float2 uv)
        {
            return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453123);
        }

        fixed3 getCurrentColor(float r)
        {
            fixed3 c = _ColorA.rgb;
            if (r > 0.25) c = _ColorB.rgb;
            if (r > 0.50) c = _ColorC.rgb;
            if (r > 0.75) c = _ColorD.rgb;
            return c;
        }

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);

            float heightMask = pow(saturate(v.vertex.y * _TopInfluence), 1.5);
            float lean = sin(_Time.y * _SwayingSpeed);

            float3 localSwayDir = normalize(mul((float3x3)unity_WorldToObject, _CameraRight.xyz));
            v.vertex.xyz += localSwayDir * lean * _SwayingStrength * heightMask;
            v.vertex.y -= abs(lean) * (_SwayingStrength * 0.5) * heightMask;

            // Rolling Log (world space, applied after swaying)
            #ifdef _ENABLE_ROLLING_LOG
            float3 worldPos    = mul(unity_ObjectToWorld, v.vertex).xyz;
            float3 worldNormal = UnityObjectToWorldNormal(v.normal);

            worldNormal = ApplyRollingLogNormal(worldNormal, worldPos);
            worldPos    = ApplyRollingLog(worldPos);

            v.vertex = mul(unity_WorldToObject, float4(worldPos, 1.0));
            v.normal = normalize(mul((float3x3)unity_WorldToObject, worldNormal));
            #endif

            o.meshUV = v.texcoord.xy;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            if (IN.meshUV.y > 0.5)
            {
                float3 objectPos = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                float timeVal = _Time.y * _ColorChangeSpeed;
                float rndCurrent = random(objectPos.xz + floor(timeVal));
                float rndNext = random(objectPos.xz + floor(timeVal) + 1.0);
                o.Albedo = lerp(getCurrentColor(rndCurrent), getCurrentColor(rndNext), smoothstep(0.0, 1.0, frac(timeVal)));
            }
            else
            {
                o.Albedo = _ColorBottomHalf.rgb;
            }

            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }
}
