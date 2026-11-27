// ============================================================================
// Shader: NoRainbowAfter/SpotlightBeam (Kerucut Cahaya Volumetrik Chapter 2)
// ============================================================================
// [GRAFIKA KOMPUTER — KONSEP UTAMA]
// 1. Render Queue & Additive Blending ("Queue"="Transparent+5", Blend SrcAlpha One):
//    - Digambar setelah objek opaque dan footprint lantai agar berkas sinar senter
//      menjumlahkan kecerahan (additive) di atas latar jalan malam tanpa menimpa z-buffer
//      (ZWrite Off).
//    - Cull Off merender sisi depan & belakang kerucut agar tetap terlihat dari dalam/luar.
// 2. Transformasi Ruang Koordinat Vertex Shader:
//    - Object Space -> World Space (TransformObjectToWorld) -> Clip Space (TransformWorldToHClip).
//    - Normal ikut diubah ke World Space (TransformObjectToWorldNormal) untuk efek Fresnel tepi.
// 3. Soft-Edge View Alignment (abs(dot(N, V))):
//    - Semakin tegak lurus permukaan kerucut terhadap arah kamera (tengah senter),
//      semakin tinggi opacity; tepi samping kerucut yang sejajar kamera (dot -> 0) memudar halus.
// 4. Value Noise Prosedural & Animasi Waktu (_Time.y):
//    - Fungsi hash21 + noise2 (interpolasi bilinear Hermite f*f*(3-2*f)) mensimulasikan
//      partikel gerimis/kabut yang melayang turun menembus berkas cahaya senter.
// ============================================================================
Shader "NoRainbowAfter/SpotlightBeam"
{
    SubShader
    {
        Tags { "Queue"="Transparent+5" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            // Additive blending: Output = Src * SrcAlpha + Dst * 1 (menambah terang di udara berkabut).
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

            // Pseudo-random hash 2D -> [0, 1] untuk membangkitkan noise prosedural tanpa tekstur eksternal.
            float hash21(float2 p)
            {
                p = frac(p * float2(234.34, 435.345));
                p += dot(p, p + 34.23);
                return frac(p.x * p.y);
            }

            // 2D Value Noise dengan kurva Hermite halus (3f^2 - 2f^3).
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
                // 1. Efek tepi lembut berbasis sudut pandang kamera terhadap normal selimut kerucut.
                float3 viewDir = normalize(GetCameraPositionWS() - input.positionWS);
                float3 n = normalize(input.normalWS);
                float facing = abs(dot(n, viewDir));
                float softEdge = pow(saturate(facing), 1.55);

                // 2. Garis hujan & kabut halus yang bergerak turun melewati sorotan lampu (_Time.y).
                float2 mistUv = float2(input.positionWS.x * 0.7 + input.positionWS.z * 0.7, input.positionWS.y * 1.4 + _Time.y * 0.65);
                float mist = noise2(mistUv * 1.3) * 0.65 + noise2(mistUv * 3.1 + float2(_Time.y * 0.2, 0.0)) * 0.35;

                // 3. Redupkan pelan tepat di dekat lensa kamera agar tidak terpotong kasar oleh near clip plane.
                float camDist = length(GetCameraPositionWS() - input.positionWS);
                float nearFade = smoothstep(0.35, 1.25, camDist);

                // 4. Atenuasi sepanjang kerucut (uv.y = 0 di lensa -> 1 di ujung jangkauan).
                float lengthFade = pow(saturate(1.0 - input.uv.y), 1.15);
                float lensCore = smoothstep(0.0, 0.04, input.uv.y);

                half alpha = input.color.a * (half)(softEdge * nearFade * lengthFade * lensCore * lerp(0.76, 1.26, mist));
                return half4(input.color.rgb, saturate(alpha));
            }
            ENDHLSL
        }
    }
}
