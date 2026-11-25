Shader "NoRainbowAfter/UI/PauseScrim"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _UnscaledTime ("Unscaled Time", Float) = 0
        _Aspect ("Viewport Aspect", Float) = 1.7778
        _StressLevel ("Normalized Stress", Range(0, 1)) = 0.5
        _ShowShadowFigure ("Show Shadow Silhouette", Range(0, 1)) = 1
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
            float _UnscaledTime, _Aspect, _StressLevel, _ShowShadowFigure;

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
            float2 hash2(float2 p) { return frac(sin(float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)))) * 43758.5453); }

            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i), hash(i + float2(1,0)), f.x), lerp(hash(i + float2(0,1)), hash(i + 1.0), f.x), f.y);
            }
            float fbm(float2 p)
            {
                return noise(p) * 0.54
                     + noise(p * 2.07 + float2(4.7, 1.3)) * 0.28
                     + noise(p * 4.19 + float2(11.3, 6.1)) * 0.13
                     + noise(p * 8.11 + float2(2.9, 9.7)) * 0.05;
            }

            // Realistic glassy raindrops & thin trickling water runs on dark glass
            float2 slidingDrops(float2 uv, float cols, float rows, float speed, float seedOffset, float t)
            {
                float2 st = float2(uv.x * _Aspect, uv.y) * float2(cols, rows);
                float2 id = floor(st);
                float2 gv = frac(st) - 0.5;
                float n = hash(id + seedOffset);
                float active = step(0.46, n);

                float slideT = t * speed * (0.60 + n * 0.68) + n * 23.0;
                float yPos = -frac(slideT * 0.15) + 0.5;
                float wobble = sin(uv.y * 19.0 + n * 27.0) * 0.068;
                float xPos = (hash(id + seedOffset + 13.7) - 0.5) * 0.52 + wobble;

                float2 d = gv - float2(xPos, yPos);
                float dropDist = length(d * float2(1.42, 0.90));
                float dropBody = smoothstep(0.088, 0.032, dropDist) * active;
                // Crisp lower-rim wet refraction crescent
                float spec = smoothstep(0.038, 0.008, length((d - float2(0.012, -0.028)) * float2(1.5, 1.3))) * active;

                // Thin damp water trail running above the drop
                float aboveDrop = smoothstep(yPos - 0.01, yPos + 0.04, gv.y);
                float trailFade = smoothstep(0.50, yPos, gv.y);
                float trailWidth = smoothstep(0.017, 0.003, abs(gv.x - xPos));
                float trail = aboveDrop * trailFade * trailWidth * active * 0.40;

                float beadY = frac(gv.y * 5.8 + n * 9.0) - 0.5;
                float bead = smoothstep(0.025, 0.007, length(float2((gv.x - xPos) * 1.6, beadY * 0.18))) * aboveDrop * trailFade * active * 0.45;

                return float2(saturate(dropBody + trail + bead), saturate(spec * 0.95 + trail * 0.30 + bead * 0.38));
            }

            float staticBeads(float2 uv, float scale, float seed)
            {
                float2 st = float2(uv.x * _Aspect, uv.y) * scale;
                float2 id = floor(st);
                float2 gv = frac(st) - 0.5;
                float2 rnd = hash2(id + seed);
                float keep = step(0.66, rnd.x);
                float2 offset = (rnd - 0.5) * 0.62;
                float r = 0.046 + rnd.y * 0.048;
                float d = length(gv - offset);
                float body = smoothstep(r, r * 0.35, d);
                float glint = smoothstep(r * 0.42, r * 0.08, length(gv - offset - float2(0.014, -0.018)));
                return (body * 0.14 + glint * 0.38) * keep;
            }

            // Tall eerie smoky black shadow silhouette (Sosok Bayangan Hitam) rising from the lower mist
            float shadowFigureMask(float2 uv, float t, float smoke)
            {
                float sway = sin(t * 0.48) * 0.007;
                float breathe = sin(t * 0.85 + 0.5) * 0.006;
                // Anchored cleanly from the bottom mist at uv.x ~ 0.375 (never floating in mid-air!)
                float2 p = uv - float2(0.375 + sway, 0.0);
                p.x *= _Aspect;
                p += (smoke - 0.5) * float2(0.055, 0.042);

                // Silhouette head
                float head = smoothstep(0.072, 0.028, length((p - float2(0.0, 0.56 + breathe)) * float2(1.22, 0.92)));
                // Narrow neck connecting head to shoulders
                float neck = smoothstep(0.036, 0.012, abs(p.x)) * smoothstep(0.52, 0.49, p.y) * smoothstep(0.44, 0.47, p.y);
                // Hunched broad shoulders & upper coat
                float shoulders = smoothstep(0.20, 0.055, length((p - float2(0.0, 0.39 + breathe * 0.6)) * float2(0.95, 1.42)));
                // Tall dark draped torso dissolving down into floor shadow
                float bodyWidth = lerp(0.23, 0.14, saturate(p.y / 0.42));
                float torso = smoothstep(bodyWidth, 0.04, abs(p.x)) * smoothstep(0.44, 0.32, p.y);
                return saturate(max(max(head, neck), max(shoulders, torso)));
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float t = _UnscaledTime;

                // 1. Domain-warped creeping black ink shadows (bayangan hitam) encroaching organically from all edges
                float2 warp = float2(
                    fbm(float2(uv.x * _Aspect * 1.4 + t * 0.016, uv.y * 1.6 - t * 0.012)),
                    fbm(float2(uv.x * _Aspect * 1.3 - t * 0.013 + 3.7, uv.y * 1.9 + t * 0.015))
                );
                float inkSmoke = fbm(uv * float2(_Aspect * 1.9, 2.2) + (warp - 0.5) * 1.15);
                float fineTendrils = fbm(uv * float2(_Aspect * 4.2, 4.6) + (warp - 0.5) * 1.55 - float2(0.0, t * 0.022));

                // Creeping organic shadow bands behind left navigation & right settings (keeps text crisp even over bright lamps, with ZERO boxes!)
                float leftDark = smoothstep(0.46 + (inkSmoke - 0.5) * 0.20, 0.06, uv.x);
                float rightDark = smoothstep(0.46 - (inkSmoke - 0.5) * 0.18, 0.72, uv.x);
                float topBottomDark = smoothstep(0.72, 1.0, uv.y) * 0.62 + smoothstep(0.24, 0.0, uv.y) * 0.76;
                float2 centered = (uv - float2(0.43, 0.52)) * float2(1.08, 1.15);
                float radial = saturate(dot(centered, centered) * 1.45);

                // Organic creeping black tendrils reaching inward from corners and edges
                float tendrilReach = saturate(radial * 0.92 + (inkSmoke - 0.42) * 0.68 + (fineTendrils - 0.45) * 0.52);
                float creepingBlack = smoothstep(0.24, 0.86, tendrilReach);

                // Translucent center opening (0.26) so the frozen 3D room/corridor clearly shows through,
                // deepening smoothly to ~0.78 behind text & dark shadow tendrils
                float textBacking = max(leftDark * 0.68, rightDark * 0.70);
                float baseAlpha = lerp(0.25, 0.78, saturate(textBacking + topBottomDark * 0.36 + creepingBlack * 0.50));

                // 2. Lurking smoky black shadow figure rising from the bottom-left corridor mist
                float figure = shadowFigureMask(uv, t, inkSmoke) * _ShowShadowFigure;
                float figureIntensity = lerp(0.64, 0.92, _StressLevel);
                float figureMask = figure * figureIntensity;
                // Subtle cold ghostly backlight right behind the figure's shoulders so its silhouette is hauntingly legible
                float2 auraDelta = (uv - float2(0.375, 0.46)) * float2(_Aspect * 0.85, 0.95);
                float eerieAura = exp(-dot(auraDelta, auraDelta) * 7.5) * (0.09 + 0.025 * sin(t * 0.8)) * _ShowShadowFigure;

                // 3. Realistic glossy wet raindrops & thin vertical damp trails (air basah)
                float dampMask = saturate(0.36 + radial * 0.72 + (inkSmoke - 0.4) * 0.30);
                float2 dropsLarge = slidingDrops(uv, 9.0, 2.1, 0.44, 2.3, t);
                float2 dropsMed = slidingDrops(uv, 16.0, 3.3, 0.30, 17.9, t);
                float microBeads = staticBeads(uv, 34.0, 9.4) * dampMask * 0.40;
                float wetBody = saturate(dropsLarge.x * 0.70 + dropsMed.x * 0.52) * dampMask;
                float wetGlint = saturate(dropsLarge.y * 0.80 + dropsMed.y * 0.55 + microBeads) * dampMask;

                float3 wetHighlightCol = lerp(float3(0.54, 0.76, 0.86), float3(0.90, 0.72, 0.46), smoothstep(0.65, 0.20, uv.x) * 0.38);
                float wetSheen = wetGlint * 0.28;
                float wetDarkening = wetBody * 0.34;

                // 4. High-stress psychological dread pulse (dark crimson veins creeping in wet edges)
                float highStress = smoothstep(0.60, 1.0, _StressLevel);
                float heartbeatPulse = 0.68 + 0.32 * sin(t * (2.6 + highStress * 2.5));
                float dreadVeins = pow(radial, 1.35) * smoothstep(0.35, 0.85, fineTendrils) * highStress * heartbeatPulse * 0.42;

                float vapor = smoothstep(0.36, 0.88, inkSmoke) * 0.055;
                float grain = (hash(uv * 540.0 + frac(t * 14.3)) - 0.5) * 0.008;

                float3 voidBlack = float3(0.004, 0.007, 0.011);
                float3 stormSlate = float3(0.015, 0.030, 0.044);
                float3 col = lerp(stormSlate, voidBlack, saturate(creepingBlack * 0.88 + figureMask));
                col += float3(0.11, 0.22, 0.28) * (vapor + eerieAura * (1.0 - figureMask * 0.85));
                col += wetHighlightCol * wetSheen;
                col += float3(0.46, 0.06, 0.07) * dreadVeins;
                col = lerp(col, voidBlack, saturate(figureMask * 0.92 + wetDarkening * 0.48));
                col += grain;

                float alpha = saturate(baseAlpha + figureMask * 0.42 + wetSheen * 0.26 + wetDarkening * 0.18 + dreadVeins * 0.36);
                float4 result = float4(saturate(col), alpha) * i.color;
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
