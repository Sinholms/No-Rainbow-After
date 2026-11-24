Shader "NoRainbowAfter/SpotlightBeam"
{
    SubShader
    {
        Tags { "Queue"="Transparent+5" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(234.34, 435.345));
                p += dot(p, p + 34.23);
                return frac(p.x * p.y);
            }

            float noise2(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 viewDir = normalize(GetCameraPositionWS() - input.positionWS);
                float3 n = normalize(input.normalWS);
                float facing = abs(dot(n, viewDir));
                float softEdge = pow(saturate(facing), 1.35);

                // Subtle rain/mist streaks drifting downward through the flashlight cone.
                float2 mistUv = float2(input.positionWS.x * 0.7 + input.positionWS.z * 0.7, input.positionWS.y * 1.4 + _Time.y * 0.65);
                float mist = noise2(mistUv * 1.3) * 0.65 + noise2(mistUv * 3.1 + float2(_Time.y * 0.2, 0.0)) * 0.35;

                // Fade gently right near the camera to avoid harsh near-clip planes.
                float camDist = length(GetCameraPositionWS() - input.positionWS);
                float nearFade = smoothstep(0.35, 1.25, camDist);

                // Smooth fade along length from bright lens core to misty tail.
                float lengthFade = pow(saturate(1.0 - input.uv.y), 1.45);
                float lensCore = smoothstep(0.0, 0.06, input.uv.y);

                half alpha = input.color.a * (half)(softEdge * nearFade * lengthFade * lensCore * lerp(0.72, 1.28, mist));
                return half4(input.color.rgb, saturate(alpha));
            }
            ENDHLSL
        }
    }
}
