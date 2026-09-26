Shader "Weedtopia/AtlasGround"
{
    Properties
    {
        _MainTex("Atlas",2D)="white"{}
        _Region("Atlas region",Vector)=(0,.5,.5,.5)
        _Tint("Evening tint",Color)=(1,1,1,1)
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
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
            float4 _MainTex_ST;float4 _Region;float4 _Tint;
            CBUFFER_END
            V vert(A v){V o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.uv=v.uv*_MainTex_ST.xy+_MainTex_ST.zw;o.color=v.color;return o;}
            half4 frag(V i):SV_Target
            {
                float2 uv=_Region.xy+(.008+frac(i.uv)*.984)*_Region.zw;
                return SAMPLE_TEXTURE2D_GRAD(_MainTex,sampler_MainTex,uv,ddx(i.uv)*_Region.zw,ddy(i.uv)*_Region.zw)*_Tint*i.color;
            }
            ENDHLSL
        }
    }
}
