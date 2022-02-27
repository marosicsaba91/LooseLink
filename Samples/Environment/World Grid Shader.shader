// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "World Grid"
{

    Properties
    {
        _GridSpacing ("Main Grid Spacing", Float) = 10.0
        _GridThickness ("Grid Thickness", Range(0,1)) = 0.01
        _SecondaryGridThickness ("Secondary Grid Thickness", Range(0,1)) = 0.5
        _SecondaryGridRation ("Secondary Grid Ration", Range(1,25)) = 5

        _BaseColour ("Base Colour", Color) = (0.0, 0.0, 0.0, 0.0)
        [HDR] _BaseEmissionColor("Base Emission Color", Color) = (0,0,0)
        _BaseGlossiness ("Base Smoothness", Range(0,1)) = 0.5
        _BaseMetallic ("Base Metallic", Range(0,1)) = 0.5

        _GridColour ("Grid Colour", Color) = (0.5, 0.5, 0.5, 0.5)
        [HDR] _GridEmissionColor("Grid Emission Color", Color) = (0,0,0)
        _GridGlossiness ("Grid Smoothness", Range(0,1)) = 0.5
        _GridMetallic ("Grid Metallic", Range(0,1)) = 0.5

    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types 
         #pragma surface surf Standard fullforwardshadows
        // #pragma surface surf Lambert vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        float4 _BaseColour;
        half _BaseGlossiness;
        half _BaseMetallic;
        fixed4 _BaseEmissionColor;

        float4 _GridColour;
        half _GridGlossiness;
        half _GridMetallic;
        fixed4 _GridEmissionColor;

        float _GridSpacing;
        float _GridThickness;
        float _SecondaryGridThickness;
        int _SecondaryGridRation;

        
        sampler2D_float _CameraDepthTexture;
        float4 _CameraDepthTexture_TexelSize;
 
        UNITY_INSTANCING_BUFFER_START(Props)
        // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)


        struct Input
        {
            int colorIndex;
            float2 uv_MainTex;
            float3 worldPos; 
            float4 screenPos;
        };
 

        float Calculate(float coordinate, float spacing, float thickness, float depthMulti)
        {
            float pos01 = abs(coordinate % spacing) / spacing;
            pos01 = 2 * abs(pos01 - 0.5) - 1 + thickness;
            pos01 = max(pos01, 0);
            pos01 *= depthMulti;
            return pos01;
        }

        void surf(Input input, inout SurfaceOutputStandard o)
        {
            //float4x4 wto = unity_WorldToObject;
            //float3 localPos =  mul(wto, input.worldPos);
            // localPos =  unity_ObjectToWorld._m03_m13_m23;
            //localPos = input.worldPos - mul(unity_ObjectToWorld, float4(0,0,0,1)).xyz;
            //localPos =  mul(input.worldPos, unity_WorldToObject);

            const float rawZ = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(input.screenPos));
            const float depth = LinearEyeDepth(rawZ);
            const float depthMulti = max(1, 500 * _GridSpacing / (depth));
            
            float grid = 0;
            if (_GridThickness > 0)
            {
                float x = Calculate(input.worldPos.x, _GridSpacing, _GridThickness, depthMulti);
                float y = Calculate(input.worldPos.y, _GridSpacing, _GridThickness, depthMulti);
                float z = Calculate(input.worldPos.z, _GridSpacing, _GridThickness, depthMulti);
                grid = min(x + z, 1);
            }
            if (_SecondaryGridRation > 0)
            {
                const float secondaryGridSpacing = _GridSpacing / _SecondaryGridRation;
                const float secondaryGridThickness = _GridThickness * _SecondaryGridRation * _SecondaryGridThickness;
                float x = Calculate(input.worldPos.x, secondaryGridSpacing, secondaryGridThickness, depthMulti);
                float y = Calculate(input.worldPos.y, secondaryGridSpacing, secondaryGridThickness, depthMulti);
                float z = Calculate(input.worldPos.z, secondaryGridSpacing, secondaryGridThickness, depthMulti);
                grid = min(grid + x + z, 1);
            }
            
            fixed4 c = lerp(_BaseColour, _GridColour, grid); 
            o.Albedo = c.rgb;
            o.Alpha = c.a; 
            o.Metallic = lerp(_BaseMetallic, _GridMetallic, grid );
            o.Smoothness = lerp(_BaseGlossiness, _GridGlossiness, grid );
            o.Emission = lerp( _BaseEmissionColor, _GridEmissionColor, grid );
        }
        ENDCG
    }
    FallBack "Diffuse"
}