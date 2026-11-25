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
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float cloud(float2 p)
            {
                float2 cell = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(randomValue(cell), randomValue(cell + float2(1.0, 0.0)), f.x),
                    lerp(randomValue(cell + float2(0.0, 1.0)), randomValue(cell + 1.0), f.x), f.y);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.positionWS - GetCameraPositionWS());
                float height = max(0.035, dir.y);
                float2 p = dir.xz / (height + 0.24) * float2(1.8, 3.6);
                p += _Time.y * float2(0.008, -0.003);
                float cover = 0.50 * cloud(p) + 0.28 * cloud(p * 2.2) + 0.15 * cloud(p * 4.5) + 0.07 * cloud(p * 9.3);
                float haze = exp2(-3.2 * height);

                // Stormy midnight sky with moody slate-blue horizon haze so ruko roofs and cables silhouette clearly.
                half3 sky = lerp(half3(0.008, 0.015, 0.025), half3(0.032, 0.048, 0.064), (half)haze);

                // Occasional faint distant sheet-lightning pulse behind thick storm clouds.
                float stormCycle = frac(_Time.y * 0.065);
                float flash1 = exp(-pow((stormCycle - 0.14) / 0.012, 2.0));
                float flash2 = exp(-pow((stormCycle - 0.18) / 0.016, 2.0)) * 0.65;
                float lightning = (flash1 + flash2) * smoothstep(0.42, 0.85, cover) * smoothstep(0.05, 0.45, height);
                sky += half3(0.06, 0.085, 0.12) * (half)lightning;

                return half4(sky * lerp(0.58h, 1.28h, (half)cover), 1.0h);
            }
            ENDHLSL
        }
    }
}
