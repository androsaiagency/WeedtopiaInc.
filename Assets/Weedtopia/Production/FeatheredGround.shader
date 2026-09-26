Shader "Weedtopia/FeatheredGround"
{
    Properties
    {
        _MainTex("Preserved terrain atlas",2D)="white"{}
        _Region("Atlas region",Vector)=(.5,.5,.5,.5)
        _Tint("Color",Color)=(.79,.78,.75,1)
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-20"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A {float4 positionOS:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;float4 _Region;float4 _Tint;
            CBUFFER_END
            V vert(A v){V o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.uv=v.uv;o.color=v.color;return o;}
            half4 frag(V i):SV_Target
            {
                float2 uv=_Region.xy+(.008+frac(i.uv)*.984)*_Region.zw;
                half4 ground=SAMPLE_TEXTURE2D_GRAD(_MainTex,sampler_MainTex,uv,ddx(i.uv)*_Region.zw,ddy(i.uv)*_Region.zw)*_Tint;
                ground.rgb*=i.color.rgb;ground.a*=smoothstep(0,1,i.color.a);return ground;
            }
            ENDHLSL
        }
    }
}
