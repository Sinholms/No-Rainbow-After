// ============================================================================
// Shader: NoRainbowAfter/SpotlightFootprint (Jejak Sorot di Aspal Basah Chapter 2)
// ============================================================================
// [GRAFIKA KOMPUTER — KONSEP UTAMA]
// 1. Additive Blending (Blend SrcAlpha One, ZWrite Off):
//    Menambahkan pancaran cahaya di atas tekstur aspal basah di jalan pasar malam.
// 2. World-Space Procedural Puddle Noise (input.positionWS.xz):
//    Memakai koordinat dunia XZ agar variasi pantulan genangan air tetap tertambat di aspal
//    walaupun mesh proyeksi sorot ikut berputar menyapu kiri dan kanan.
// 3. Atenuasi UV & Vertex Color:
//    Menggabungkan simetri melintang (uv.x -> beamCore) dan jatuh jarak memanjang (uv.y -> distFade)
//    dengan warna merah/biru-putih dari vertex color (C# ChapterTwoSpotlightEncounter).
// ============================================================================
Shader "NoRainbowAfter/SpotlightFootprint"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
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
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
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
                // Kilau genangan aspal basah (world-space XZ) + distribusi inti berkas cahaya melintang.
                float2 wp = input.positionWS.xz;
                float puddle = noise2(wp * 0.95) * 0.65 + noise2(wp * 2.8 + float2(0.0, _Time.y * 0.14)) * 0.35;

                // Ubah uv.x [0..1] menjadi jarak dari tepi ke tengah [0..1..0].
                float across = 1.0 - abs(input.uv.x * 2.0 - 1.0);
                float beamCore = smoothstep(0.0, 0.72, across);
                float distFade = pow(saturate(1.0 - input.uv.y), 0.75);

                half3 col = input.color.rgb * lerp(0.65h, 1.35h, (half)(puddle * 0.45 + beamCore * 0.55));
                half alpha = input.color.a * (half)(lerp(0.68, 1.15, puddle) * lerp(0.55, 1.0, beamCore) * lerp(0.45, 1.0, distFade));
                return half4(col, saturate(alpha));
            }
            ENDHLSL
        }
    }
}
