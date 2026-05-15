Shader "Custom/WorldBendingEffect"
{
    Properties
    {
        _Amount("AmountBend", Range(0.0,1.0)) = 0.5
        
        _BendX("Bend X", Float) = 0
        _BendZ("Bend Z", Float) = 0
        
        _MainTex("Texture", 2D) = "white" {}
        
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Lambert vertex:vert

        #pragma shader_feature _ENABLE_BEND
        #pragma target 3.0

        

        struct Input
        {
            float2 uv_MainTex;
        };

        float _Amount;
        float _BendX;
        float _BendZ;
        
        void vert(inout appdata_full v)
        {
            #ifdef  _ENABLE_BEND
            
            // Object -> World
            float3 worldpos = mul(unity_ObjectToWorld, v.vertex).xyz;
            float3 cam = worldpos - _WorldSpaceCameraPos;

            float bendz = -_Amount * pow(cam.z,2.0);
            float bendy = -_Amount * pow(cam.x, 2.0);
            float bendAdd = bendz + bendy;

            

            worldpos += float3(_BendX, bendAdd, _BendZ);

            //World -> Object
            v.vertex = mul(unity_WorldToObject, float4(worldpos,1.0));

            #endif
        }

        sampler2D _MainTex;

        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed3 t = tex2D(_MainTex, IN.uv_MainTex);
            o.Albedo = t.rgb;
        }

        
        
        ENDCG
    }
    FallBack "Diffuse"
}
