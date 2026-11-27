// ============================================================================
// Shader: NoRainbowAfter/OvercastStreet (Kubah Langit Mendung & Kilat Chapter 2)
// ============================================================================
// [GRAFIKA KOMPUTER — KONSEP UTAMA]
// 1. Sky Dome Rendering ("Queue"="Background", Cull Front, ZWrite Off):
//    - Digambar paling awal di belakang seluruh geometri (Background).
//    - Cull Front membalik culling sehingga kamera yang berada DI DALAM bola langit melihat
//      permukaan bagian dalam kubah tanpa menulis kedalaman (ZWrite Off).
// 2. Proyeksi Awan Planar Berbasis Arah Pandang (Ray-to-Cloud Plane):
//    Membagi arah pandang dir.xz dengan ketinggian (height + 0.24) menciptakan efek perspektif
//    awan mendung yang tampak memanjang ke arah cakrawala.
// 3. Fractal Brownian Motion (fBm) 4 Oktaf:
//    Menjumlahkan 4 lapisan cloud noise dengan frekuensi berlipat ganda (1x, 2.2x, 4.5x, 9.3x)
//    dan bobot menurun (0.50, 0.28, 0.15, 0.07) untuk pola awan badai prosedural tanpa tekstur!
// 4. Horizon Haze Eksponensial & Kilat Ganda Gaussian:
//    - exp2(-3.2 * height) memberi kabut cakrawala kebiruan agar siluet atap ruko & kabel jelas.
//    - Dua pulsa kurva Gaussian exp(-pow(...)) menerangi balik lapisan awan tebal secara berkala.
// ============================================================================
Shader "NoRainbowAfter/OvercastStreet"
{
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            // Render sisi dalam bola langit tanpa menutupi depth buffer scene.
            Cull Front
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

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
                float2 cell = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(randomValue(cell), randomValue(cell + float2(1.0, 0.0)), f.x),
                    lerp(randomValue(cell + float2(0.0, 1.0)), randomValue(cell + 1.0), f.x),
                    f.y);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.positionWS - GetCameraPositionWS());
                float height = max(0.035, dir.y);

                // Proyeksi bidang awan + pergeseran lambat oleh angin (_Time.y).
                float2 p = dir.xz / (height + 0.24) * float2(1.8, 3.6);
                p += _Time.y * float2(0.008, -0.003);

                // Fractal Brownian Motion (fBm) 4 lapis untuk ketebalan awan badai.
                float cover = 0.50 * cloud(p)
                            + 0.28 * cloud(p * 2.2)
                            + 0.15 * cloud(p * 4.5)
                            + 0.07 * cloud(p * 9.3);
                float haze = exp2(-3.2 * height);

                // Langit tengah malam dengan kabut biru-abu di cakrawala agar siluet ruko & kabel terbaca.
                half3 sky = lerp(half3(0.008, 0.015, 0.025), half3(0.032, 0.048, 0.064), (half)haze);

                // Denyut kilat jauh di balik awan badai tebal memakai dua kurva Gaussian beruntun.
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
