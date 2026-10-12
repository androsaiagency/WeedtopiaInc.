Shader "Weedtopia/Cassa Vadela/Building Palette"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _Plaster("Warm Sandstone", Color) = (1.12,1.04,0.82,1)
        _Teal("Terracotta Enamel", Color) = (0.84,0.42,0.26,1)
        _Strength("Palette Strength", Range(0,1)) = 0.78
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
            float4 _Color, _Plaster, _Teal;
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
                half neutral=saturate(1-(max(c.r,max(c.g,c.b))-min(c.r,min(c.g,c.b)))*4);
                half3 plaster=lerp(c.rgb*_Plaster.rgb,luma*_Plaster.rgb,neutral*.28);
                half3 teal=luma*_Teal.rgb*1.35;
                c.rgb=lerp(c.rgb,lerp(plaster,teal,enamel),_Strength);
                return c*i.color;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
