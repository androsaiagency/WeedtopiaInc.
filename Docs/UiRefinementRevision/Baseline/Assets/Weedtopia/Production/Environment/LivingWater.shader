Shader "Weedtopia/LivingWater"
{
    Properties
    {
        _MainTex("Painted water atlas",2D)="white"{}
        _Tint("Clear river tint",Color)=(.75,1,1,1)
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-30"}
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
            struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;float4 color:COLOR;};
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _Tint;
            CBUFFER_END
            V vert(A v){V o;o.world=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.uv=v.uv;o.color=v.color;return o;}
            half4 frag(V i):SV_Target
            {
                float2 flow=i.world.xz*.038+float2(_Time.y*.003,-_Time.y*.006);
                // Mirrored repetition keeps the painted tile continuous at its borders.
                float2 tile=1-abs(frac(flow*.5)*2-1);
                float2 uv=float2(.5,0)+(.01+tile*.98)*.5;
                half4 c=SAMPLE_TEXTURE2D_GRAD(_MainTex,sampler_MainTex,uv,ddx(flow)*.5,ddy(flow)*.5)*_Tint;
                c.rgb=lerp(float3(.035,.24,.205),c.rgb,.32);
                float rim=1-smoothstep(.1,.48,i.color.a);
                float ripple=pow(saturate(sin(i.world.x*.7+i.world.z*.27+_Time.y*.65)),18);
                c.rgb=lerp(c.rgb,float3(.29,.48,.33),rim*.38)+ripple*.008;
                c.a=smoothstep(0,.15,i.color.a);return c;
            }
            ENDHLSL
        }
    }
}
