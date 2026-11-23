Shader "NoRainbowAfter/OvercastStreet"
{
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Cull Front
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float3 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }
            float randomValue(float2 p)
            {
                return frac(sin(dot(p,float2(127.1,311.7))) * 43758.5453);
            }
            float cloud(float2 p)
            {
                float2 cell=floor(p), f=frac(p);
                f=f*f*(3-2*f);
                return lerp(lerp(randomValue(cell),randomValue(cell+float2(1,0)),f.x),
                    lerp(randomValue(cell+float2(0,1)),randomValue(cell+1),f.x),f.y);
            }
            half4 frag(Varyings input) : SV_Target
            {
                float3 dir=normalize(input.positionWS-GetCameraPositionWS());
                float height=max(.035,dir.y);
                float2 p=dir.xz / (height+.26) * float2(1.6,3.4);
                p += _Time.y * float2(.004, -.001);
                float cover=.52*cloud(p)+.29*cloud(p*2.2)+.14*cloud(p*4.3)+.05*cloud(p*9.1);
                float haze=exp2(-4.2*height);
                half3 sky=lerp(half3(.006,.012,.020),half3(.031,.045,.053),haze);
                return half4(sky*lerp(.58,1.19,cover),1);
            }
            ENDHLSL
        }
    }
}
