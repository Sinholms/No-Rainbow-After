using System;
using UnityEngine;
using UnityEngine.UI;

namespace NoRainbowAfter.Player
{
    /// <summary>
    /// Tracks Raka's stress level and renders a single, non-duplicated
    /// red Stress Barometer overlay in the top-left corner of the HUD.
    /// </summary>
    public sealed class StressBarometer : MonoBehaviour
    {
        public const float InitialStress = 50f;
        public const float ChapterOneMinimumStress = 75f;
        public const float ChapterTwoMinimumStress = 45f;
        public const float HighStressWarningThreshold = 70f;
        public const float MaxStress = 100f;

        [SerializeField, Range(0f, 100f)] private float currentStress = InitialStress;

        private FirstPersonPlayer ownerPlayer;
        private Canvas overlayCanvas;
        private Canvas vignetteCanvas;
        private RectTransform rootPanel;
        private RectTransform fillRect;
        private Image fillImage;
        private Image pulseVignette;
        private Text statusLabel;
        private Text warningLabel;
        private string minimumWarningText;
        private float flashTimer;
        private AudioSource heartbeatSource;
        private AudioClip heartbeatClip;

        public float CurrentStress => currentStress;
        public float MinimumStress { get; private set; }
        public float NormalizedStress => Mathf.Clamp01(currentStress / MaxStress);
        public bool IsWarningActive => currentStress >= HighStressWarningThreshold;
        public Text Label => statusLabel;
        public Text WarningLabel => warningLabel;
        public Image FillImage => fillImage;
        public RectTransform RootPanel => rootPanel;
        public Canvas OverlayCanvas => overlayCanvas;

        public event Action<float> StressChanged;

        private void Awake()
        {
            var duplicates = GetComponents<StressBarometer>();
            if (duplicates.Length > 1 && duplicates[0] != this)
            {
                Destroy(this);
                return;
            }
            ownerPlayer = GetComponent<FirstPersonPlayer>();
            currentStress = Mathf.Clamp(InitialStress, 0f, MaxStress);
            BuildOrAttachUi();
            RefreshUi();
        }

        private void OnEnable()
        {
            if (ownerPlayer == null) ownerPlayer = GetComponent<FirstPersonPlayer>();
            BuildOrAttachUi();
            RefreshUi();
            if (overlayCanvas != null) overlayCanvas.enabled = ownerPlayer == null || !ownerPlayer.IsPaused;
        }

        private void OnDisable()
        {
            if (overlayCanvas != null) overlayCanvas.enabled = false;
            if (vignetteCanvas != null) vignetteCanvas.enabled = false;
            if (heartbeatSource != null && heartbeatSource.isPlaying) heartbeatSource.Stop();
        }

        private void OnDestroy()
        {
            if (overlayCanvas != null) Destroy(overlayCanvas.gameObject);
            else if (rootPanel != null) Destroy(rootPanel.gameObject);
            if (pulseVignette != null && pulseVignette.transform.parent != null)
                Destroy(pulseVignette.transform.parent.gameObject);
            if (heartbeatClip != null) Destroy(heartbeatClip);
        }

        private void Update()
        {
            bool shouldShow = ownerPlayer == null || !ownerPlayer.IsPaused;
            if (overlayCanvas != null && overlayCanvas.enabled != shouldShow)
                overlayCanvas.enabled = shouldShow;
            if (vignetteCanvas != null && vignetteCanvas.enabled != shouldShow)
                vignetteCanvas.enabled = shouldShow;

            if (flashTimer > 0f)
            {
                flashTimer = Mathf.Max(0f, flashTimer - Time.unscaledDeltaTime);
                RefreshFillColor();
            }

            RefreshWarningEffects();
            UpdateHeartbeatAudio();
        }

        private void UpdateHeartbeatAudio()
        {
            bool mute = ownerPlayer != null && (ownerPlayer.IsPaused || ownerPlayer.IsTransitionLocked || ownerPlayer.IsBreathing);
            if (!IsWarningActive || mute)
            {
                if (heartbeatSource != null && heartbeatSource.isPlaying) heartbeatSource.Pause();
                return;
            }

            if (heartbeatSource == null)
            {
                heartbeatSource = gameObject.AddComponent<AudioSource>();
                heartbeatSource.playOnAwake = false;
                heartbeatSource.loop = true;
                heartbeatSource.spatialBlend = 0f;
                heartbeatClip = CreateHeartbeatClip();
                heartbeatSource.clip = heartbeatClip;
            }

            float severity = Mathf.InverseLerp(HighStressWarningThreshold, MaxStress, currentStress);
            heartbeatSource.volume = Mathf.Lerp(0.08f, 0.34f, severity);
            heartbeatSource.pitch = Mathf.Lerp(0.88f, 1.42f, severity);
            if (!heartbeatSource.isPlaying) heartbeatSource.Play();
        }

        private static AudioClip CreateHeartbeatClip()
        {
            const int rate = 22050;
            const float duration = 1.05f;
            int samples = Mathf.RoundToInt(rate * duration);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / rate;
                float lub = Mathf.Exp(-Mathf.Pow((t - 0.14f) / 0.042f, 2f)) * Mathf.Sin(2f * Mathf.PI * 52f * t);
                float dub = Mathf.Exp(-Mathf.Pow((t - 0.33f) / 0.052f, 2f)) * Mathf.Sin(2f * Mathf.PI * 44f * t) * 0.78f;
                data[i] = Mathf.Clamp((lub + dub) * 0.9f, -1f, 1f);
            }
            var clip = AudioClip.Create("RakaStressHeartbeat", samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public void SetStress(float value)
        {
            float clamped = Mathf.Clamp(value, MinimumStress, MaxStress);
            if (clamped > currentStress + 0.01f)
                flashTimer = 0.35f;
            currentStress = clamped;
            StressChanged?.Invoke(currentStress);
            RefreshUi();
        }

        public void SetMinimumStress(float value)
        {
            MinimumStress = Mathf.Clamp(value, 0f, MaxStress);
            minimumWarningText = $"STRES TINGGI • Batas pemulihan bab ini {Mathf.RoundToInt(MinimumStress)}%.";
            if (currentStress < MinimumStress) SetStress(currentStress);
        }

        public void AddStress(float amount)
        {
            if (Mathf.Approximately(amount, 0f)) return;
            SetStress(currentStress + amount);
        }

        public void ResetToInitial()
        {
            flashTimer = 0f;
            SetStress(InitialStress);
        }

        public void EnsureUiReady() => BuildOrAttachUi();

        private void BuildOrAttachUi()
        {
            RemoveLegacyOrDuplicateUi();
            if (rootPanel != null && overlayCanvas != null) return;

            // Keep TestObjective in UI_FirstPerson clear beneath the top-left Stress Barometer.
            Transform hud = FindGameplayHud();
            if (hud != null)
            {
                Transform obj = hud.Find("TestObjective");
                if (obj != null)
                {
                    var objRect = obj.GetComponent<RectTransform>();
                    if (objRect != null && objRect.anchoredPosition.y > 410f)
                        objRect.anchoredPosition = new Vector2(objRect.anchoredPosition.x, 405f);
                }
            }

            // Use a dedicated top-most HUD overlay canvas (sortingOrder 25) so StressBarometer renders
            // cleanly in one single layer above gameplay HUD, LaptopTypingUI (15), and BreathingUI (16).
            var canvasGo = new GameObject("StressBarometerCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            overlayCanvas = canvasGo.GetComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 25;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            rootPanel = CreateRect("StressBarometer", canvasGo.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(36f, -24f), new Vector2(310f, 58f));

            var labelObj = CreateRect("StressLabel", rootPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, 0f), new Vector2(0f, 28f));
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

            var trackRect = CreateRect("StressTrack", rootPanel, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 4f), new Vector2(0f, 16f));
            var trackBg = trackRect.gameObject.AddComponent<Image>();
            trackBg.color = new Color(0.14f, 0.03f, 0.03f, 0.92f);
            trackBg.raycastTarget = false;
            var trackOutline = trackRect.gameObject.AddComponent<Outline>();
            trackOutline.effectColor = new Color(0.62f, 0.12f, 0.12f, 0.95f);
            trackOutline.effectDistance = new Vector2(1.5f, -1.5f);

            fillRect = CreateRect("StressFill", trackRect, new Vector2(0f, 0f), new Vector2(NormalizedStress, 1f), new Vector2(0f, 0.5f),
                Vector2.zero, Vector2.zero);
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            fillImage = fillRect.gameObject.AddComponent<Image>();
            fillImage.color = new Color(0.86f, 0.14f, 0.14f, 0.98f);
            fillImage.raycastTarget = false;

            // Slow-blinking top-center warning text when stress reaches >= 70%
            var warnObj = CreateRect("HighStressWarning", canvasGo.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -92f), new Vector2(1080f, 54f));
            warningLabel = warnObj.gameObject.AddComponent<Text>();
            warningLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            warningLabel.fontSize = 25;
            warningLabel.fontStyle = FontStyle.Bold;
            warningLabel.alignment = TextAnchor.MiddleCenter;
            warningLabel.color = new Color(1.0f, 0.28f, 0.24f, 1f);
            warningLabel.text = "PERINGATAN: STRES TINGGI!  Cari tempat persembunyian & tekan [TAB] untuk mengatur napas";
            warningLabel.raycastTarget = false;
            warningLabel.enabled = false;
            var warnOutline = warnObj.gameObject.AddComponent<Outline>();
            warnOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            warnOutline.effectDistance = new Vector2(1.8f, -1.8f);

            // Red stress vignette canvas when stress reaches >= 70%
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
            if (uiRoot == null) return null;
            return uiRoot.transform.Find("GameplayHUD");
        }

        private void RefreshUi()
        {
            if (statusLabel != null)
            {
                int displayedStress = currentStress >= MaxStress ? (int)MaxStress : Mathf.Min((int)MaxStress - 1, Mathf.RoundToInt(currentStress));
                statusLabel.text = "STRES  " + displayedStress + "%";
            }
            if (fillRect != null)
                fillRect.anchorMax = new Vector2(NormalizedStress, 1f);
            RefreshFillColor();
            RefreshWarningEffects();
        }

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
                        warningLabel.text = ownerPlayer.IsUsingLaptop
                            ? "STRES 100%!  Tekan [TAB] untuk keluar dari laptop, lalu [TAB] lagi untuk atur napas."
                            : "STRES 100%!  Tekan [TAB] untuk mengatur napas di bar hijau sebelum melanjutkan!";
                    else if (currentStress <= MinimumStress)
                        warningLabel.text = minimumWarningText;
                    else
                        warningLabel.text = "PERINGATAN: STRES TINGGI!  Cari tempat persembunyian & tekan [TAB] untuk mengatur napas";
                    warningLabel.color = new Color(1.0f, 0.28f, 0.24f, blinkAlpha);
                }
            }
        }

        private void RefreshFillColor()
        {
            if (fillImage == null) return;
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
