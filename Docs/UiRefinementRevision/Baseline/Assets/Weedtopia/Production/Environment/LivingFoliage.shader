Shader "Weedtopia/LivingFoliage"
{
    Properties
    {
        [PerRendererData] _MainTex("Painted foliage",2D)="white"{}
        _Color("Tint",Color)=(1,1,1,1)
        _Sway("Canopy sway",Range(0,.06))=.012
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" "CanUseSpriteAtlas"="True"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A{float4 positionOS:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;float _Sway;
            CBUFFER_END
            V vert(A v)
            {
                V o;float3 origin=TransformObjectToWorld(float3(0,0,0));
                float wave=sin(_Time.y*.85+origin.x*.081+origin.z*.057)+.32*sin(_Time.y*1.73+origin.z*.2);
                v.positionOS.x+=wave*max(0,v.positionOS.y)*_Sway;
                o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.uv=v.uv;o.color=v.color*_Color;return o;
            }
            half4 frag(V i):SV_Target{return SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.color;}
            ENDHLSL
        }
    }
}
