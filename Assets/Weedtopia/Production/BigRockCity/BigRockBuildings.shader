Shader "Weedtopia/Big Rock/Building Palette"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _Stone("Sandstone", Color) = (1.10,0.96,0.82,1)
        _Copper("Copper", Color) = (1.0,0.62,0.34,1)
        _Strength("Palette Strength", Range(0,1)) = 0.72
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _Color, _Stone, _Copper;
            float _Strength;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            Varyings Vert(Attributes i)
            { Varyings o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.uv=i.uv; o.color=i.color*_Color; return o; }
            half4 Frag(Varyings i):SV_Target
            {
                half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
                half luma=dot(c.rgb,half3(.299,.587,.114));
                half enamel=smoothstep(.012,.065,c.g-c.r)*smoothstep(.01,.07,c.g-c.b);
                half3 warm=c.rgb*_Stone.rgb;
                half3 copper=lerp(c.rgb,luma*_Copper.rgb*1.35,enamel);
                c.rgb=lerp(c.rgb,lerp(warm,copper,enamel),_Strength);
                return c*i.color;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
