// ============================================================================
// Shader: NoRainbowAfter/UI/BreathingRing (Cincin Napas SDF Prosedural)
// ============================================================================
// [GRAFIKA KOMPUTER — KONSEP UTAMA]
// - Signed Distance Field (SDF) 2D Lingkaran: abs(length(uv - 0.5) - _Radius) menggambar
//   cincin tajam pada resolusi apa pun tanpa pecah piksel.
// - Gaussian Glow Halo: exp(-pow(ringDist / haloWidth, 2.0)) menghasilkan pendar cahaya
//   halus di sekitar cincin saat pemain tepat berada di zona ritme hijau.
// ============================================================================
Shader "NoRainbowAfter/UI/BreathingRing"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0.392, 0.710, 0.651, 1)
        _Radius ("Radius", Range(0.02, 0.48)) = 0.28
        _Thickness ("Thickness", Range(0.002, 0.15)) = 0.016
        _Softness ("Softness", Range(0.002, 0.15)) = 0.025
        _GlowIntensity ("Glow Intensity", Range(0.0, 3.0)) = 0.75
        _FogFalloff ("Fog Falloff", Range(0.0, 1.0)) = 0.0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 world : TEXCOORD1;
            };

            float4 _Color;
            float4 _ClipRect;
            float _Radius;
            float _Thickness;
            float _Softness;
            float _GlowIntensity;
            float _FogFalloff;

            v2f vert(appdata v)
            {
                v2f o;
                o.world = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 centered = i.uv - 0.5;
                float dist = length(centered);
                float ringDist = abs(dist - _Radius);

                float halfThick = max(0.001, _Thickness * 0.5);
                float soft = max(0.002, _Softness);

                // Inti cincin SDF dengan tepi lembut.
                float core = 1.0 - smoothstep(halfThick, halfThick + soft, ringDist);

                // Halo cahaya Gaussian halus di sekeliling cincin.
                float haloWidth = halfThick + soft * (2.5 + _FogFalloff * 2.5);
                float halo = exp(-pow(ringDist / max(0.004, haloWidth), 2.0));

                // Kabut tipis di bagian dalam cincin saat _FogFalloff > 0.
                float innerMist = _FogFalloff * 0.14 * (1.0 - smoothstep(0.0, max(0.01, _Radius), dist));

                // Pastikan alfa habis mulus sebelum menyentuh tepi quad UV (0.5).
                float edgeFade = smoothstep(0.495, 0.34, dist);

                float intensity = saturate(core + halo * 0.45 * _GlowIntensity + innerMist) * edgeFade;
                float3 rgb = i.color.rgb * (1.0 + halo * 0.35 * _GlowIntensity);
                float4 result = float4(rgb, intensity * i.color.a);

                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(i.world.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001);
                #endif
                return result;
            }
            ENDCG
        }
    }
}
