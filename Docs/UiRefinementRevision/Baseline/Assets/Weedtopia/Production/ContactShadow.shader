Shader "Weedtopia/ContactShadow"
{
    Properties{_Color("Shadow",Color)=(.035,.07,.06,.3)}
    SubShader
    {
        Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A{float4 positionOS:POSITION;float2 uv:TEXCOORD0;};struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            CBUFFER_END
            V vert(A a){V o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.uv=a.uv;return o;}
            half4 frag(V i):SV_Target{float d=length((i.uv-.5)*2);return half4(_Color.rgb,_Color.a*(1-smoothstep(.15,1,d)));}
            ENDHLSL
        }
    }
}
