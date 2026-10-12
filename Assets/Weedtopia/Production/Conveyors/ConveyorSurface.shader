Shader "Weedtopia/PaintedConveyor"
{
    Properties
    {
        _MainTex("Painted sprite",2D)="white"{}
        _Tint("Operating tint",Color)=(1,1,1,1)
        _Travel("Travel in metres",Float)=0
        _SpanUv("Length and world offset",Vector)=(1,0,0,0)
        _Junction("Transfer turntable",Float)=0
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" "RenderType"="TransparentCutout"}
        Cull Off ZWrite On
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _Tint, _SpanUv;
            float _Travel, _Junction;
            CBUFFER_END
            V vert(A i) { V o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.uv=i.uv; return o; }
            half4 frag(V i):SV_Target
            {
                float2 fixedUv=i.uv, movingUv=i.uv;
                float mask;
                if(_Junction>.5)
                {
                    float2 p=i.uv-.5;
                    mask=1-smoothstep(.31,.33,length(p));
                    float angle=_Travel*.25;
                    float s=sin(angle),c=cos(angle);
                    movingUv=.5+float2(c*p.x-s*p.y,s*p.x+c*p.y);
                }
                else
                {
                    // Only the rubber deck moves. Rails, bolts and bearing housings stay fixed.
                    mask=smoothstep(.295,.315,i.uv.y)*(1-smoothstep(.718,.738,i.uv.y));
                    fixedUv.x=i.uv.x*_SpanUv.x+_SpanUv.y;
                    movingUv.x=fixedUv.x-_Travel/18;
                }
                float2 fixedDx=ddx(fixedUv),fixedDy=ddy(fixedUv),movingDx=ddx(movingUv),movingDy=ddy(movingUv);
                if(_Junction<.5){fixedUv.x=frac(fixedUv.x);movingUv.x=frac(movingUv.x);}
                // Blend two independent UV samples so seams neither shear nor jump when the phase wraps.
                half4 frame=SAMPLE_TEXTURE2D_GRAD(_MainTex,sampler_MainTex,fixedUv,fixedDx,fixedDy);
                half4 deck=SAMPLE_TEXTURE2D_GRAD(_MainTex,sampler_MainTex,movingUv,movingDx,movingDy);
                half4 color=lerp(frame,deck,mask)*_Tint;
                clip(color.a-.15);
                return color;
            }
            ENDHLSL
        }
    }
}
