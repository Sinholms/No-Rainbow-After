using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace NoRainbowAfter.Player
{
    /// <summary>
    /// Efek kamera organik, post-processing URP (Vignette + ChromaticAberration),
    /// serta UI shader cincin napas pada BreathingMinigame.
    ///
    /// [GRAFIKA KOMPUTER]
    /// - Mengirim parameter _Radius, _Thickness, _Softness, _GlowIntensity, _FogFalloff ke
    ///   shader SDF NoRainbowAfter/UI/BreathingRing agar cincin memancarkan halo lembut.
    /// - PerlinNoise 2D digunakan untuk micro-shake kamera & getaran cincin saat di luar ritme.
    /// </summary>
    public sealed partial class BreathingMinigame
    {
        // =========================================================================
        // Efek Kamera Napas & Volume Post-Processing Dinamis
        // =========================================================================

        private void UpdateSensoryCamera(float dt, bool holdInhale)
        {
            float levelNod = (0.5f - IndicatorValue) * 7.5f;
            float actionBias = holdInhale ? -2.25f : 2.25f;
            float desiredPitch = Mathf.Clamp(levelNod + actionBias, -MaxCameraNodDegrees, MaxCameraNodDegrees);
            CurrentCameraPitchOffset = Mathf.MoveTowards(CurrentCameraPitchOffset, desiredPitch, 18f * dt);

            float lungNorm = Mathf.InverseLerp(0.18f, 0.84f, IndicatorValue);
            float curveEval = breathCameraCurve != null ? breathCameraCurve.Evaluate(lungNorm) : lungNorm;
            float targetFov = curveEval * MaxBreathFovBoost;
            float targetEyeY = curveEval * MaxBreathEyeLift;
            float smooth = dt > 0f ? 1f - Mathf.Exp(-7f * dt) : 1f;
            currentFovOffset = Mathf.Lerp(currentFovOffset, targetFov, smooth);
            currentEyeOffsetY = Mathf.Lerp(currentEyeOffsetY, targetEyeY, smooth);

            float stressNorm = stressBarometer != null ? stressBarometer.NormalizedStress : 0.5f;
            float targetShake = IsInGreenZone ? 0f : Mathf.Lerp(0.12f, 0.42f, stressNorm);
            currentMicroShake = Mathf.MoveTowards(currentMicroShake, targetShake, (IsInGreenZone ? 1.4f : 0.9f) * dt);

            Vector2 shakeOffset = Vector2.zero;
            if (currentMicroShake > 0.001f)
            {
                float timeSeed = Time.unscaledTime * 23f;
                float sx = (Mathf.PerlinNoise(timeSeed, 1.3f) - 0.5f) * 2f * currentMicroShake;
                float sy = (Mathf.PerlinNoise(4.7f, timeSeed) - 0.5f) * 2f * currentMicroShake;
                shakeOffset = new Vector2(sx, sy);
            }

            motor?.SetBreathingCameraEffects(CurrentCameraPitchOffset, currentFovOffset, currentEyeOffsetY, shakeOffset);
        }

        private void UpdatePostProcessing(float dt)
        {
            if (postVolume == null || postProfile == null)
            {
                return;
            }
            postVolume.weight = 1f;

            float stressNorm = stressBarometer != null ? stressBarometer.NormalizedStress : 0.5f;
            float zonePenalty = IsInGreenZone
                ? 0f
                : Mathf.Clamp01((Mathf.Abs(IndicatorValue - TargetCenter) - GreenZoneHalfWidth) / 0.35f);
            float targetVignette = IsInGreenZone
                ? Mathf.Lerp(0.16f, 0.24f, stressNorm)
                : Mathf.Lerp(0.32f, 0.50f, Mathf.Max(stressNorm, zonePenalty));
            float targetChromatic = IsInGreenZone
                ? 0f
                : Mathf.Lerp(0.12f, 0.36f, Mathf.Max(stressNorm, zonePenalty));

            float smooth = dt > 0f ? 1f - Mathf.Exp(-4.5f * dt) : 1f;
            currentVignetteIntensity = Mathf.Lerp(currentVignetteIntensity, targetVignette, smooth);
            currentChromaticIntensity = Mathf.Lerp(currentChromaticIntensity, targetChromatic, smooth);

            if (vignetteOverride != null)
            {
                vignetteOverride.intensity.value = currentVignetteIntensity;
            }
            if (chromaticOverride != null)
            {
                chromaticOverride.intensity.value = currentChromaticIntensity;
            }
        }

        private void EnsurePostProcessingVolume()
        {
            if (postVolume != null)
            {
                return;
            }
            var volGo = new GameObject("BreathingSensoryVolume");
            volGo.transform.SetParent(transform, false);
            postVolume = volGo.AddComponent<Volume>();
            postVolume.isGlobal = true;
            postVolume.priority = 50f;
            postVolume.weight = 0f;

            postProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            postProfile.name = "BreathingSensoryProfile_Runtime";

            vignetteOverride = postProfile.Add<Vignette>(true);
            vignetteOverride.color.overrideState = true;
            vignetteOverride.color.value = new Color(0.04f, 0.05f, 0.06f, 1f);
            vignetteOverride.intensity.overrideState = true;
            vignetteOverride.intensity.value = 0.25f;
            vignetteOverride.smoothness.overrideState = true;
            vignetteOverride.smoothness.value = 0.48f;

            chromaticOverride = postProfile.Add<ChromaticAberration>(true);
            chromaticOverride.intensity.overrideState = true;
            chromaticOverride.intensity.value = 0f;

            postVolume.sharedProfile = postProfile;
        }

        // =========================================================================
        // Pembaruan & Konstruksi Cincin SDF Konsentris (UI Overlay)
        // =========================================================================

        private void RefreshUi(float dt)
        {
            if (ringRoot != null) ringRoot.SetActive(IsActive);
            if (overlayCanvas != null) overlayCanvas.enabled = IsActive && (player == null || !player.IsPaused);
            if (!IsActive)
            {
                return;
            }

            float targetRadius = Mathf.Lerp(0.10f, 0.23f, Mathf.Clamp01(TargetCenter));
            float playerRadius = Mathf.Lerp(0.10f, 0.23f, Mathf.Clamp01(IndicatorValue));
            float stressNorm = stressBarometer != null ? stressBarometer.NormalizedStress : 0.5f;

            float smooth = dt > 0f ? 1f - Mathf.Exp(-7f * dt) : 1f;
            Color desiredPlayerColor = IsInGreenZone
                ? CalmSageTeal
                : Color.Lerp(PaleWarningOrange, DimMaroon, stressNorm);
            currentPlayerRingColor = Color.Lerp(currentPlayerRingColor, desiredPlayerColor, smooth);

            float desiredGlow = IsInGreenZone ? 1.42f : 0.48f;
            currentGlowIntensity = Mathf.Lerp(currentGlowIntensity, desiredGlow, smooth);

            if (targetRingMaterial != null)
            {
                targetRingMaterial.SetFloat("_Radius", targetRadius);
                targetRingMaterial.SetFloat("_Thickness", 0.0065f);
                targetRingMaterial.SetFloat("_Softness", 0.015f);
                targetRingMaterial.SetFloat("_GlowIntensity", IsInGreenZone ? 0.72f : 0.35f);
                targetRingMaterial.SetColor("_Color", IsInGreenZone ? CalmGuideTint : IdleGuideTint);
            }
            else if (targetRingRect != null)
            {
                float size = targetRadius * 2f * 720f;
                targetRingRect.sizeDelta = new Vector2(size, size);
                targetRingImage.color = IsInGreenZone ? CalmGuideTint : IdleGuideTint;
            }

            if (playerRingMaterial != null)
            {
                playerRingMaterial.SetFloat("_Radius", playerRadius);
                playerRingMaterial.SetFloat("_Thickness", 0.011f);
                playerRingMaterial.SetFloat("_Softness", 0.024f);
                playerRingMaterial.SetFloat("_GlowIntensity", currentGlowIntensity);
                playerRingMaterial.SetFloat("_FogFalloff", 0.65f);
                playerRingMaterial.SetColor("_Color", currentPlayerRingColor);
            }
            else if (playerRingRect != null)
            {
                float size = playerRadius * 2f * 720f;
                playerRingRect.sizeDelta = new Vector2(size, size);
                playerRingImage.color = currentPlayerRingColor;
            }

            // Getaran Perlin pada cincin Raka bila keluar dari zona nyaman.
            if (playerRingRect != null)
            {
                if (IsInGreenZone)
                {
                    playerRingRect.anchoredPosition = Vector2.Lerp(playerRingRect.anchoredPosition, Vector2.zero, smooth);
                }
                else
                {
                    float jitterAmp = Mathf.Lerp(2.5f, 7.5f, stressNorm);
                    float t = Time.unscaledTime * 28f;
                    float jx = (Mathf.PerlinNoise(t, 0.5f) - 0.5f) * 2f * jitterAmp;
                    float jy = (Mathf.PerlinNoise(2.1f, t) - 0.5f) * 2f * jitterAmp;
                    playerRingRect.anchoredPosition = new Vector2(jx, jy);
                }
            }

            string targetCue = EvaluateCueKeyword();
            if (targetCue != pendingCueText)
            {
                pendingCueText = targetCue;
            }

            if (phaseLabel != null)
            {
                if (activeCueText != pendingCueText)
                {
                    phaseFadeAlpha = Mathf.MoveTowards(phaseFadeAlpha, 0f, 5.5f * dt);
                    if (phaseFadeAlpha <= 0.01f)
                    {
                        activeCueText = pendingCueText;
                        phaseLabel.text = activeCueText;
                    }
                }
                else
                {
                    phaseFadeAlpha = Mathf.MoveTowards(phaseFadeAlpha, 1f, 4.5f * dt);
                }

                phaseLabel.color = IsInGreenZone
                    ? new Color(0.76f, 0.93f, 0.89f, phaseFadeAlpha * 0.95f)
                    : new Color(0.92f, 0.78f, 0.72f, phaseFadeAlpha * 0.90f);
            }
        }

        private void BuildUi()
        {
            if (overlayCanvas != null)
            {
                return;
            }

            var canvasGo = new GameObject("BreathingMinigameUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            overlayCanvas = canvasGo.GetComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 16;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            ringRoot = new GameObject("OrganicBreathingRings", typeof(RectTransform));
            ringRoot.transform.SetParent(canvasGo.transform, false);
            var rootRect = (RectTransform)ringRoot.transform;
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = new Vector2(0f, -85f);
            rootRect.sizeDelta = new Vector2(720f, 720f);

            Shader ringShader = Shader.Find("NoRainbowAfter/UI/BreathingRing");
            Texture2D ringTex = ringShader == null ? GetFallbackRingTexture() : Texture2D.whiteTexture;

            targetRingRect = CreateRect("TargetGuideRing", ringRoot.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(720f, 720f));
            targetRingImage = targetRingRect.gameObject.AddComponent<RawImage>();
            targetRingImage.texture = ringTex;
            targetRingImage.raycastTarget = false;
            if (ringShader != null)
            {
                targetRingMaterial = new Material(ringShader) { name = "BreathingTargetRing_Runtime" };
                targetRingImage.material = targetRingMaterial;
            }

            playerRingRect = CreateRect("RakaBreathRing", ringRoot.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(720f, 720f));
            playerRingImage = playerRingRect.gameObject.AddComponent<RawImage>();
            playerRingImage.texture = ringTex;
            playerRingImage.raycastTarget = false;
            if (ringShader != null)
            {
                playerRingMaterial = new Material(ringShader) { name = "BreathingPlayerRing_Runtime" };
                playerRingImage.material = playerRingMaterial;
            }

            phaseLabel = CreateText("PhaseKeyword", ringRoot.transform, "T A R I K", 24, FontStyle.Normal,
                new Color(0.76f, 0.93f, 0.89f, 0.95f), new Vector2(0f, 0f), new Vector2(420f, 44f));

            closeHintLabel = CreateText("ControlsHint", ringRoot.transform, "[SPASI] Tahan / Lepas   |   [TAB] Selesai", 15, FontStyle.Normal,
                new Color(0.76f, 0.82f, 0.82f, 0.72f), new Vector2(0f, -215f), new Vector2(520f, 28f));

            ringRoot.SetActive(false);
            overlayCanvas.enabled = false;
        }

        private Texture2D GetFallbackRingTexture()
        {
            if (fallbackRingTexture != null)
            {
                return fallbackRingTexture;
            }
            const int size = 256;
            fallbackRingTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            Color32[] pixels = new Color32[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float d = Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - 0.82f);
                    float a = Mathf.Clamp01(1f - d / 0.14f);
                    byte alpha = (byte)(a * a * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }
            fallbackRingTexture.SetPixels32(pixels);
            fallbackRingTexture.Apply(false, true);
            return fallbackRingTexture;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;
            return rect;
        }

        private static Text CreateText(string name, Transform parent, string value, int fontSize, FontStyle style,
            Color color, Vector2 pos, Vector2 size)
        {
            var rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
            var txt = rect.gameObject.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = color;
            txt.text = value;
            txt.raycastTarget = false;
            return txt;
        }
    }
}
