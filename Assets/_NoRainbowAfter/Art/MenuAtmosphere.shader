Shader "NoRainbowAfter/UI/MenuAtmosphere"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _WeatherTime ("Unscaled time", Float) = 0
        _Aspect ("Viewport aspect", Float) = 1.7778
        _ScrimStrength ("Left shadow scrim", Range(0, 1)) = 0.92
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
            float _WeatherTime, _Aspect, _ScrimStrength;

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
            float fog(float2 p) { return noise(p) * 0.56 + noise(p * 2.05 + 7.3) * 0.29 + noise(p * 4.13 + 13.7) * 0.15; }

            float rain(float2 uv, float density, float speed, float offset, float slant)
            {
                float2 p = float2(uv.x * _Aspect + uv.y * slant, uv.y) * float2(density, 4.6);
                float cell = floor(p.x);
                float h = hash(float2(cell, offset));
                float x = abs(frac(p.x) - (0.14 + h * 0.72));
                float tip = frac(p.y + _WeatherTime * speed + h * 11.0);
                float streak = smoothstep(0.18, 0.0, tip) * smoothstep(0.014, 0.002, x);
                return streak * step(0.24, h);
            }

            // Realistic glassy raindrops & thin damp trails on dark glass
            float2 slidingDrops(float2 uv, float cols, float rows, float speed, float seedOffset, float t)
            {
                float2 st = float2(uv.x * _Aspect, uv.y) * float2(cols, rows);
                float2 id = floor(st);
                float2 gv = frac(st) - 0.5;
                float n = hash(id + seedOffset);
                float active = step(0.48, n);

                float slideT = t * speed * (0.62 + n * 0.68) + n * 19.0;
                float yPos = -frac(slideT * 0.16) + 0.5;
                float wobble = sin(uv.y * 20.0 + n * 31.0) * 0.065;
                float xPos = (hash(id + seedOffset + 17.3) - 0.5) * 0.52 + wobble;

                float2 d = gv - float2(xPos, yPos);
                // Small crisp teardrop head (not oversized chalky oval)
                float dropDist = length(d * float2(1.45, 0.92));
                float dropBody = smoothstep(0.085, 0.032, dropDist) * active;
                // Sharp glassy crescent highlight on lower rim (like real water refraction)
                float spec = smoothstep(0.038, 0.008, length((d - float2(0.012, -0.028)) * float2(1.5, 1.3))) * active;

                // Very thin damp vertical water streak above the droplet
                float aboveDrop = smoothstep(yPos - 0.01, yPos + 0.04, gv.y);
                float trailFade = smoothstep(0.50, yPos, gv.y);
                float trailWidth = smoothstep(0.016, 0.003, abs(gv.x - xPos));
                float trail = aboveDrop * trailFade * trailWidth * active * 0.36;

                float beadY = frac(gv.y * 6.0 + n * 7.0) - 0.5;
                float bead = smoothstep(0.024, 0.007, length(float2((gv.x - xPos) * 1.6, beadY * 0.18))) * aboveDrop * trailFade * active * 0.42;

                return float2(saturate(dropBody + trail + bead), saturate(spec * 0.95 + trail * 0.28 + bead * 0.35));
            }

            float staticBeads(float2 uv, float scale, float seed)
            {
                float2 st = float2(uv.x * _Aspect, uv.y) * scale;
                float2 id = floor(st);
                float2 gv = frac(st) - 0.5;
                float2 rnd = hash2(id + seed);
                float keep = step(0.68, rnd.x);
                float2 offset = (rnd - 0.5) * 0.62;
                float r = 0.045 + rnd.y * 0.045;
                float d = length(gv - offset);
                float body = smoothstep(r, r * 0.35, d);
                float glint = smoothstep(r * 0.42, r * 0.08, length(gv - offset - float2(0.014, -0.018)));
                return (body * 0.14 + glint * 0.36) * keep;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float t = _WeatherTime;

                // 1. Organic creeping black shadow mass along left & corners (domain-warped smoke, zero sharp edges)
                float2 warp = float2(
                    fog(float2(uv.x * 2.4 + t * 0.014, uv.y * 2.2 - t * 0.011)),
                    fog(float2(uv.x * 2.1 - t * 0.010 + 4.3, uv.y * 2.6 + t * 0.013))
                );
                float smoke = fog(uv * float2(_Aspect * 1.8, 2.3) + (warp - 0.5) * 0.75);
                float leftMask = smoothstep(0.58 + (smoke - 0.5) * 0.22, 0.02, uv.x);
                float leftShadow = pow(leftMask, 1.25) * 0.84 * _ScrimStrength;
                float topShadow = smoothstep(0.72 + (smoke - 0.5) * 0.14, 1.0, uv.y) * 0.52;
                float bottomShadow = smoothstep(0.24 - (smoke - 0.5) * 0.12, 0.0, uv.y) * 0.58;
                float rightEdge = smoothstep(0.84, 1.0, uv.x) * 0.34;
                float scrimAlpha = saturate(leftShadow + topShadow * 0.65 + bottomShadow * 0.72 + rightEdge);
                float3 scrimCol = float3(0.008, 0.014, 0.022);

                // 2. Drifting cold midnight mist
                float mist = fog(float2(uv.x * _Aspect * 2.2 - t * 0.017, uv.y * 2.8 + t * 0.008));
                float lowBand = smoothstep(0.84, 0.04, uv.y);
                float rightOpen = smoothstep(0.18, 0.74, uv.x);
                float mistAlpha = smoothstep(0.36, 0.86, mist) * lowBand * (0.032 + 0.058 * rightOpen);
                float3 mistCol = float3(0.08, 0.20, 0.27);

                // 3. Multi-layered slanting midnight rain
                float rFar = rain(uv, 118.0, 1.05, 1.0, 0.032) * 0.13;
                float rMid = rain(uv, 70.0, 1.48, 5.0, 0.042) * 0.15;
                float rNear = rain(uv, 38.0, 2.05, 11.0, 0.052) * 0.09;
                float rainAmt = (rFar + rMid + rNear) * (0.22 + rightOpen * 0.78);
                float3 rainCol = float3(0.56, 0.75, 0.83);

                // 4. Wet glass water drops & sliding trails (heavier around outer edges & damp foreground)
                float edgeWet = saturate(smoothstep(0.42, 0.0, uv.x) * 0.55 + smoothstep(0.62, 1.0, uv.x) * 0.70 + smoothstep(0.70, 1.0, uv.y) * 0.65 + smoothstep(0.28, 0.0, uv.y) * 0.55 + 0.18);
                float2 dropsA = slidingDrops(uv, 9.5, 2.2, 0.48, 3.1, t);
                float2 dropsB = slidingDrops(uv, 16.0, 3.4, 0.34, 19.7, t);
                float microDrops = staticBeads(uv, 34.0, 7.2) * edgeWet * 0.42;
                float wetBody = saturate(dropsA.x * 0.55 + dropsB.x * 0.42) * edgeWet;
                float wetSpec = saturate(dropsA.y * 0.68 + dropsB.y * 0.48 + microDrops) * edgeWet;
                // Drops catch cold neon cyan on top/right and subtle warm amber near bottom-right doorway
                float doorZone = smoothstep(0.45, 0.85, uv.x) * smoothstep(0.55, 0.12, uv.y);
                float3 dropTint = lerp(float3(0.54, 0.76, 0.86), float3(0.92, 0.74, 0.48), doorZone * 0.62);
                float wetDarkRim = wetBody * 0.32;
                float wetGlintAlpha = wetSpec * 0.24;

                // 5. Dying fluorescent neon micro-flicker above kos doorway + puddle reflection
                float flickerCycle = frac(t * 0.19);
                float burst = smoothstep(0.88, 0.90, flickerCycle) * smoothstep(0.99, 0.95, flickerCycle);
                float microDip = step(0.45, frac(sin(floor(t * 28.0) * 91.7) * 43758.5));
                float neonPulse = burst * microDip * 0.18;
                float2 lampDelta = (uv - float2(0.745, 0.885)) * float2(_Aspect * 0.42, 1.6);
                float lampHalo = exp(-dot(lampDelta, lampDelta) * 14.0);
                float2 puddleDelta = (uv - float2(0.735, 0.12)) * float2(_Aspect * 0.55, 1.1);
                float puddleHalo = exp(-dot(puddleDelta, puddleDelta) * 10.0);
                float neonDarken = (lampHalo * 0.34 + puddleHalo * 0.24) * neonPulse;

                // Warm interior doorway breath
                float2 doorDelta = (uv - float2(0.785, 0.30)) * float2(_Aspect * 0.85, 1.0);
                float doorWarmth = exp(-dot(doorDelta, doorDelta) * 12.0) * (0.045 + 0.015 * sin(t * 0.85));
                float3 amberCol = float3(0.88, 0.66, 0.38);

                // Faint distant storm lightning
                float stormPhase = frac(t * 0.068 + 0.37);
                float flash = smoothstep(0.955, 0.968, stormPhase) * smoothstep(0.996, 0.975, stormPhase);
                float cloudMask = smoothstep(0.45, 0.98, uv.y) * smoothstep(0.92, 0.25, uv.x);
                float stormGlow = flash * cloudMask * 0.11;

                // Ultra-subtle film grain
                float grain = (hash(uv * 520.0 + frac(t * 17.3)) - 0.5) * 0.008;

                float totalAlpha = saturate(scrimAlpha + mistAlpha + rainAmt + wetDarkRim + wetGlintAlpha + neonDarken + doorWarmth + stormGlow);
                float3 col = scrimCol * (scrimAlpha + neonDarken + wetDarkRim)
                           + mistCol * mistAlpha
                           + rainCol * rainAmt
                           + dropTint * wetGlintAlpha
                           + amberCol * doorWarmth
                           + float3(0.44, 0.64, 0.78) * stormGlow;
                col = totalAlpha > 0.0001 ? col / totalAlpha : scrimCol;
                col += grain;

                float4 result = float4(saturate(col), totalAlpha) * i.color;
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
