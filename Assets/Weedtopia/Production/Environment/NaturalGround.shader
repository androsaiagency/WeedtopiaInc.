Shader "Weedtopia/NaturalGround"
{
    Properties
    {
        _MainTex("Painted landscape atlas",2D)="white"{}
        _Region("Atlas region",Vector)=(0,.5,.5,.5)
        _Tint("Landscape tint",Color)=(1,1,1,1)
        _Variation("Broad meadow variation",Range(0,.3))=.1
        _Saturation("Material saturation",Range(0,1))=1
        _Contrast("Texture contrast",Range(0,1))=1
        _BaseColor("Quiet material midtone",Color)=(.17,.31,.09,1)
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-40"}
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
            float4 _MainTex_ST,_Region,_Tint,_BaseColor;float _Variation,_Saturation,_Contrast;
            CBUFFER_END
            V vert(A v){V o;o.world=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.uv=v.uv;o.color=v.color;return o;}
            half4 frag(V i):SV_Target
            {
                float2 uv=_Region.xy+(.008+frac(i.uv)*.984)*_Region.zw;
                half4 c=SAMPLE_TEXTURE2D_GRAD(_MainTex,sampler_MainTex,uv,ddx(i.uv)*_Region.zw,ddy(i.uv)*_Region.zw);
                float luminance=dot(c.rgb,float3(.2126,.7152,.0722));
                c.rgb=lerp(_BaseColor.rgb,lerp(luminance.xxx,c.rgb,_Saturation),_Contrast)*_Tint.rgb;
                float broad=sin(i.world.x*.021+sin(i.world.z*.027)*2)*cos(i.world.z*.018);
                c.rgb*=i.color.rgb*(1+broad*_Variation);
                float edge=sin(i.world.x*2.7+i.world.z)*sin(i.world.z*3.1)*.045;
                c.a*=smoothstep(.02,.95,i.color.a+edge*(1-i.color.a));return c;
            }
            ENDHLSL
        }
    }
}
