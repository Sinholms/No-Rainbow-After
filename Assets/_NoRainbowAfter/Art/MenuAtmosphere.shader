Shader "NoRainbowAfter/UI/MenuAtmosphere"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _WeatherTime ("Unscaled time", Float) = 0
        _Aspect ("Viewport aspect", Float) = 1.7778
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
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
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
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; float4 world : TEXCOORD1; };
            float4 _Color, _ClipRect;
            float _WeatherTime, _Aspect;

            v2f vert(appdata v)
            {
                v2f o;
                o.world = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i), hash(i + float2(1,0)), f.x), lerp(hash(i + float2(0,1)), hash(i + 1.0), f.x), f.y);
            }
            float fog(float2 p) { return noise(p) * 0.57 + noise(p * 2.03 + 7.0) * 0.28 + noise(p * 4.01 + 13.0) * 0.15; }
            float rain(float2 uv, float density, float speed, float offset)
            {
                float2 p = float2(uv.x * _Aspect + uv.y * 0.035, uv.y) * float2(density, 5.0);
                float cell = floor(p.x);
                float h = hash(float2(cell, offset));
                float x = abs(frac(p.x) - (0.15 + h * 0.7));
                float tip = frac(p.y + _WeatherTime * speed + h * 9.0);
                float streak = smoothstep(0.15, 0.0, tip) * smoothstep(0.012, 0.002, x);
                return streak * step(0.22, h);
            }
            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float t = _WeatherTime;
                float mist = fog(float2(uv.x * _Aspect * 2.5 - t * 0.013, uv.y * 3.0 + t * 0.008));
                float low = smoothstep(0.88, 0.1, uv.y);
                float right = smoothstep(0.18, 0.72, uv.x);
                float rainAmount = rain(uv, 105.0, 1.1, 1.0) * 0.14 + rain(uv, 62.0, 1.5, 5.0) * 0.12;
                float mistAlpha = smoothstep(0.35, 0.82, mist) * low * (0.018 + 0.06 * right);
                float alpha = mistAlpha + rainAmount * (0.18 + right * 0.82);
                float3 tint = lerp(float3(0.12, 0.30, 0.39), float3(0.52, 0.70, 0.78), saturate(rainAmount * 12.0));
                float4 result = float4(tint, alpha) * i.color;
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
