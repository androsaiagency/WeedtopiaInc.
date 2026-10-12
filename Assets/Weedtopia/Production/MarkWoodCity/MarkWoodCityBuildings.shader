Shader "Weedtopia/Mark Wood City/Building Palette"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _Cedar("Reclaimed Cedar", Color) = (1.06,0.94,0.76,1)
        _Forest("Forest Green Enamel", Color) = (0.39,0.66,0.49,1)
        _Copper("Aged Copper", Color) = (0.94,0.66,0.38,1)
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
            float4 _Color, _Cedar, _Forest, _Copper;
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
                half forest=smoothstep(.012,.07,c.g-c.r)*smoothstep(.01,.07,c.g-c.b);
                half copper=smoothstep(.08,.22,c.r-c.b)*smoothstep(.02,.12,c.r-c.g);
                half neutral=saturate(1-(max(c.r,max(c.g,c.b))-min(c.r,min(c.g,c.b)))*4);
                half3 cedar=lerp(c.rgb*_Cedar.rgb,luma*_Cedar.rgb,neutral*.24);
                half3 palette=lerp(cedar,luma*_Copper.rgb*1.23,copper*.55);
                palette=lerp(palette,luma*_Forest.rgb*1.48,forest);
                c.rgb=lerp(c.rgb,palette,_Strength);
                return c*i.color;
            }
            ENDHLSL
        }
    }
    FallBack Off
}