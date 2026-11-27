using UnityEngine;
using UnityEngine.UI;

namespace NoRainbowAfter.Player
{
    /// <summary>
    /// Konstruksi HUD bar stres, teks peringatan berkedip di tengah atas,
    /// dan kanvas vignet merah saat stres melewati ambang 70% pada StressBarometer.
    /// </summary>
    public sealed partial class StressBarometer
    {
        public void EnsureUiReady() => BuildOrAttachUi();

        private void BuildOrAttachUi()
        {
            RemoveLegacyOrDuplicateUi();
            if (rootPanel != null && overlayCanvas != null)
            {
                return;
            }

            // Geser TestObjective sedikit ke bawah agar tidak bertabrakan dengan bar stres di kiri atas.
            Transform hud = FindGameplayHud();
            if (hud != null)
            {
                Transform obj = hud.Find("TestObjective");
                if (obj != null)
                {
                    var objRect = obj.GetComponent<RectTransform>();
                    if (objRect != null && objRect.anchoredPosition.y > 410f)
                    {
                        objRect.anchoredPosition = new Vector2(objRect.anchoredPosition.x, 405f);
                    }
                }
            }

            var canvasGo = new GameObject("StressBarometerCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            overlayCanvas = canvasGo.GetComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 25;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            rootPanel = CreateRect("StressBarometer", canvasGo.transform,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(36f, -24f), new Vector2(310f, 58f));

            var labelObj = CreateRect("StressLabel", rootPanel,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, 28f));
            statusLabel = labelObj.gameObject.AddComponent<Text>();
            statusLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusLabel.fontSize = 23;
            statusLabel.fontStyle = FontStyle.Bold;
            statusLabel.alignment = TextAnchor.MiddleLeft;
            statusLabel.color = new Color(0.98f, 0.25f, 0.25f, 1f);
            statusLabel.raycastTarget = false;
            var outline = labelObj.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.92f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var trackRect = CreateRect("StressTrack", rootPanel,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 4f), new Vector2(0f, 16f));
            var trackBg = trackRect.gameObject.AddComponent<Image>();
            trackBg.color = new Color(0.14f, 0.03f, 0.03f, 0.92f);
            trackBg.raycastTarget = false;
            var trackOutline = trackRect.gameObject.AddComponent<Outline>();
            trackOutline.effectColor = new Color(0.62f, 0.12f, 0.12f, 0.95f);
            trackOutline.effectDistance = new Vector2(1.5f, -1.5f);

            fillRect = CreateRect("StressFill", trackRect,
                new Vector2(0f, 0f), new Vector2(NormalizedStress, 1f), new Vector2(0f, 0.5f),
                Vector2.zero, Vector2.zero);
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            fillImage = fillRect.gameObject.AddComponent<Image>();
            fillImage.color = new Color(0.86f, 0.14f, 0.14f, 0.98f);
            fillImage.raycastTarget = false;

            var warnObj = CreateRect("HighStressWarning", canvasGo.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -92f), new Vector2(1080f, 54f));
            warningLabel = warnObj.gameObject.AddComponent<Text>();
            warningLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            warningLabel.fontSize = 25;
            warningLabel.fontStyle = FontStyle.Bold;
            warningLabel.alignment = TextAnchor.MiddleCenter;
            warningLabel.color = new Color(1.0f, 0.28f, 0.24f, 1f);
            warningLabel.text = "PERINGATAN: STRESS TINGGI!  Cari tempat aman & tekan [TAB] untuk mengatur napas";
            warningLabel.raycastTarget = false;
            warningLabel.enabled = false;
            var warnOutline = warnObj.gameObject.AddComponent<Outline>();
            warnOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            warnOutline.effectDistance = new Vector2(1.8f, -1.8f);

            var vigRoot = new GameObject("StressVignetteCanvas", typeof(RectTransform), typeof(Canvas));
            vigRoot.transform.SetParent(transform, false);
            vignetteCanvas = vigRoot.GetComponent<Canvas>();
            vignetteCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            vignetteCanvas.sortingOrder = -2;

            var vigImgGo = CreateRect("StressTint", vigRoot.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            pulseVignette = vigImgGo.gameObject.AddComponent<Image>();
            pulseVignette.color = new Color(0.65f, 0.02f, 0.02f, 0f);
            pulseVignette.raycastTarget = false;
        }

        private void RemoveLegacyOrDuplicateUi()
        {
            Transform hud = FindGameplayHud();
            if (hud != null)
            {
                Transform staleHudBar = hud.Find("StressBarometer");
                if (staleHudBar != null)
                {
                    if (Application.isPlaying) Destroy(staleHudBar.gameObject);
                    else DestroyImmediate(staleHudBar.gameObject);
                }
            }

            if (overlayCanvas == null)
            {
                Transform staleCanvas = transform.Find("StressBarometerCanvas");
                if (staleCanvas != null)
                {
                    if (Application.isPlaying) Destroy(staleCanvas.gameObject);
                    else DestroyImmediate(staleCanvas.gameObject);
                }
                Transform staleVig = transform.Find("StressVignetteCanvas");
                if (staleVig != null)
                {
                    if (Application.isPlaying) Destroy(staleVig.gameObject);
                    else DestroyImmediate(staleVig.gameObject);
                }
            }
        }

        private Transform FindGameplayHud()
        {
            var uiRoot = GameObject.Find("UI_FirstPerson");
            return uiRoot != null ? uiRoot.transform.Find("GameplayHUD") : null;
        }

        // HUD memakai nilai yang sama: teks persentase, panjang bar, warna, dan peringatan.
        private void RefreshUi()
        {
            if (statusLabel != null)
            {
                int displayedStress = currentStress >= MaxStress
                    ? (int)MaxStress
                    : Mathf.Min((int)MaxStress - 1, Mathf.RoundToInt(currentStress));
                statusLabel.text = "STRESS  " + displayedStress + "%";
            }
            if (fillRect != null)
            {
                fillRect.anchorMax = new Vector2(NormalizedStress, 1f);
            }
            RefreshFillColor();
            RefreshWarningEffects();
        }

        // Pada ambang stres tinggi, vignette dan pesan membantu pemain mengenali kondisi.
        private void RefreshWarningEffects()
        {
            bool highStress = IsWarningActive;
            float slowWave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.5f);

            if (pulseVignette != null)
            {
                if (highStress)
                {
                    float severity = Mathf.InverseLerp(HighStressWarningThreshold, MaxStress, currentStress);
                    float alpha = 0.14f + 0.16f * severity + 0.06f * slowWave;
                    pulseVignette.color = new Color(0.65f, 0.02f, 0.02f, Mathf.Clamp01(alpha));
                }
                else
                {
                    pulseVignette.color = new Color(0.65f, 0.02f, 0.02f, 0f);
                }
            }

            if (warningLabel != null)
            {
                warningLabel.enabled = highStress;
                if (highStress)
                {
                    float blinkAlpha = 0.28f + 0.72f * slowWave;
                    bool recoveryLocked = ownerPlayer != null && ownerPlayer.IsBreathRecoveryRequired;
                    if (recoveryLocked)
                    {
                        warningLabel.text = ownerPlayer.IsUsingLaptop
                            ? "STRES 100%!  Tekan [TAB] untuk keluar dari laptop, lalu [TAB] lagi untuk mengatur napas."
                            : "STRES 100%!  Tekan [TAB] untuk mengatur napas sebelum melanjutkan!";
                    }
                    else if (currentStress <= MinimumStress)
                    {
                        warningLabel.text = minimumWarningText;
                    }
                    else
                    {
                        warningLabel.text = "PERINGATAN: STRES TINGGI!  Cari tempat aman & tekan [TAB] untuk mengatur napas";
                    }
                    warningLabel.color = new Color(1.0f, 0.28f, 0.24f, blinkAlpha);
                }
            }
        }

        private void RefreshFillColor()
        {
            if (fillImage == null)
            {
                return;
            }
            Color normalRed = new Color(0.86f, 0.14f, 0.14f, 0.98f);
            Color flashRed = new Color(1.0f, 0.34f, 0.28f, 1.0f);
            fillImage.color = flashTimer > 0f ? Color.Lerp(normalRed, flashRed, flashTimer / 0.35f) : normalRed;
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
    }
}
