Shader "Weedtopia/Buildings/Palette Clean"
{
    Properties
    {
        [HDR] _Cream("Warm Plaster", Color) = (0.78, 0.72, 0.61, 1)
        [HDR] _Sage("Sage Enamel", Color) = (0.21, 0.34, 0.25, 1)
        [HDR] _DarkSage("Dark Sage", Color) = (0.11, 0.20, 0.15, 1)
        [HDR] _Copper("Oxidized Copper", Color) = (0.55, 0.28, 0.10, 1)
        _BuildingHeight("Building Height", Float) = 1
        _GroundOffset("Ground Offset", Float) = 0
        _CrownStartFraction("Crown Start", Range(0, 1)) = 0.76
        _LowerTrimFraction("Lower Trim", Range(0, 0.2)) = 0.035
        _RoofNormalThreshold("Roof Normal Threshold", Range(0, 1)) = 0.68
        _Ambient("Ambient Light", Range(0, 1)) = 0.62
        _Directional("Directional Light", Range(0, 1)) = 0.38
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Unlit"
            "Queue" = "Geometry"
        }
        LOD 100

        Pass
        {
            Name "PaletteClean"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Cream;
                half4 _Sage;
                half4 _DarkSage;
                half4 _Copper;
                float _BuildingHeight;
                float _GroundOffset;
                half _CrownStartFraction;
                half _LowerTrimFraction;
                half _RoofNormalThreshold;
                half _Ambient;
                half _Directional;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
                float relativeHeightWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.relativeHeightWS = positionWS.y - (TransformObjectToWorld(float3(0.0, 0.0, 0.0)).y + _GroundOffset);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 normalWS = normalize(input.normalWS);
                half height01 = saturate(input.relativeHeightWS / max(_BuildingHeight, 0.001));
                half3 albedo = _Cream.rgb;

                if (height01 >= _CrownStartFraction)
                    albedo = _Copper.rgb;
                else if (height01 <= _LowerTrimFraction)
                    albedo = _DarkSage.rgb;
                else if (normalWS.y >= _RoofNormalThreshold)
                    albedo = _Sage.rgb;

                const half3 keyDirection = half3(0.381h, 0.889h, 0.254h);
                half lightFactor = saturate(_Ambient + _Directional * saturate(dot(normalWS, keyDirection)));
                return half4(albedo * lightFactor, 1.0h);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
