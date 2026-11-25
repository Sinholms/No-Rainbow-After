using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NoRainbowAfter.Player
{
    public enum BreathingPhase
    {
        Inhale,
        Exhale
    }

    /// <summary>
    /// Calm breathing regulation minigame toggled via [Tab] during FPP exploration.
    /// Player holds [Space] / [Left Mouse] to inhale and releases to exhale in order
    /// to keep the breath parameter inside the moving green bar zone ("bar hijau"),
    /// gradually reducing Raka's StressBarometer while playing synced breath audio
    /// and pitching the FPP camera up on inhale / down on exhale.
    /// </summary>
    public sealed class BreathingMinigame : MonoBehaviour
    {
        public const float BreathCycleDuration = 8f;
        public const float GreenZoneHalfWidth = 0.16f;
        public const float InhaleRiseRate = 0.22f;
        public const float ExhaleFallRate = 0.19f;
        public const float StressReliefPerSecond = 10f;
        public const float MaxCameraNodDegrees = 6.0f;

        [SerializeField] private AudioClip inhaleClip;
        [SerializeField] private AudioClip exhaleClip;

        private FirstPersonPlayer player;
        private FirstPersonMotor motor;
        private StressBarometer stressBarometer;
        private AudioSource audioSource;

        private AudioClip runtimeInhaleFallback;
        private AudioClip runtimeExhaleFallback;

        private Canvas overlayCanvas;
        private GameObject panelRoot;
        private RectTransform trackRect;
        private RectTransform greenZoneRect;
        private Image greenZoneImage;
        private RectTransform indicatorRect;
        private Image indicatorImage;
        private Text phaseLabel;
        private Text statusLabel;

        private float cycleTime;
        private int openedFrame = -1;
        private bool wasInhalingAudio;

        public bool IsActive { get; private set; }
        public float IndicatorValue { get; private set; } = 0.50f;
        public float TargetCenter { get; private set; } = 0.50f;
        public bool IsInGreenZone { get; private set; }
        public BreathingPhase CurrentPhase { get; private set; } = BreathingPhase.Inhale;
        public float CurrentCameraPitchOffset { get; private set; }

        public event Action<bool> BreathingStateChanged;

        public void ConfigureAudioClips(AudioClip inhale, AudioClip exhale)
        {
            inhaleClip = inhale;
            exhaleClip = exhale;
        }

        private void Awake()
        {
            var duplicates = GetComponents<BreathingMinigame>();
            if (duplicates.Length > 1 && duplicates[0] != this)
            {
                Destroy(this);
                return;
            }
            CacheReferences();
            BuildUi();
        }

        private void OnEnable()
        {
            CacheReferences();
            if (player != null)
            {
                player.BreatheRequested -= OnBreatheRequested;
                player.BreatheRequested += OnBreatheRequested;
            }
        }

        private void OnDisable()
        {
            if (player != null)
                player.BreatheRequested -= OnBreatheRequested;
            if (IsActive) StopBreathing();
        }

        private void OnDestroy()
        {
            if (overlayCanvas != null) Destroy(overlayCanvas.gameObject);
            if (runtimeInhaleFallback != null) Destroy(runtimeInhaleFallback);
            if (runtimeExhaleFallback != null) Destroy(runtimeExhaleFallback);
        }

        private void OnBreatheRequested()
        {
            if (player != null && (player.IsUsingLaptop || player.LaptopClosedFrame == Time.frameCount))
                return;
            ToggleBreathing();
        }

        public void ToggleBreathing()
        {
            if (IsActive) StopBreathing();
            else StartBreathing();
        }

        // Bersembunyi tidak melarang mode ini: pemain dapat mengatur napas di dalam lemari.
        public bool StartBreathing()
        {
            CacheReferences();
            if (IsActive) return false;
            if (player != null && (player.IsPaused || player.IsInspecting || player.IsUsingLaptop || player.IsTransitionLocked || player.LaptopClosedFrame == Time.frameCount))
                return false;

            BuildUi();
            IsActive = true;
            openedFrame = Time.frameCount;
            cycleTime = 0f;
            TargetCenter = EvaluateTargetCenter(cycleTime);
            IndicatorValue = TargetCenter;
            CurrentPhase = BreathingPhase.Inhale;
            CurrentCameraPitchOffset = 0f;
            wasInhalingAudio = false;

            // Nonaktifkan kendali gerak selama latihan; status bersembunyi tidak diubah.
            player?.SetBreathing(true);
            motor?.SetBreathingPitchOffset(0f);
            UpdateZoneCheck();
            RefreshUi();
            BreathingStateChanged?.Invoke(true);
            return true;
        }

        public void StopBreathing()
        {
            if (!IsActive) return;
            IsActive = false;
            CurrentCameraPitchOffset = 0f;
            motor?.SetBreathingPitchOffset(0f);
            if (audioSource != null && audioSource.isPlaying)
                audioSource.Stop();
            if (panelRoot != null) panelRoot.SetActive(false);
            if (overlayCanvas != null) overlayCanvas.enabled = false;
            player?.SetBreathing(false);
            BreathingStateChanged?.Invoke(false);
        }

        private void Update()
        {
            if (!IsActive) return;
            if (player != null && player.IsPaused)
            {
                if (overlayCanvas != null) overlayCanvas.enabled = false;
                return;
            }
            if (overlayCanvas != null && !overlayCanvas.enabled) overlayCanvas.enabled = true;

            bool holdInhale = false;
            if (Time.frameCount != openedFrame)
            {
                var kb = Keyboard.current;
                var mouse = Mouse.current;
                holdInhale = (kb != null && kb.spaceKey.isPressed) ||
                             (mouse != null && mouse.leftButton.isPressed);
            }

            Tick(Time.unscaledDeltaTime, holdInhale);
        }

        /// <summary>Menahan tombol menaikkan indikator napas. Melepasnya menurunkan indikator.
        /// Stres berkurang sesuai waktu yang dihabiskan indikator di zona hijau.</summary>
        public void Tick(float deltaTime, bool holdInhale)
        {
            if (!IsActive) return;
            float dt = Mathf.Max(0f, deltaTime);

            // Waktu siklus menggerakkan target; input pemain menggerakkan indikator secara terpisah.
            cycleTime = (cycleTime + dt) % BreathCycleDuration;
            float halfCycle = BreathCycleDuration * 0.5f;
            CurrentPhase = cycleTime < halfCycle ? BreathingPhase.Inhale : BreathingPhase.Exhale;
            TargetCenter = EvaluateTargetCenter(cycleTime);

            float deltaParam = holdInhale ? InhaleRiseRate * dt : -ExhaleFallRate * dt;
            IndicatorValue = Mathf.Clamp01(IndicatorValue + deltaParam);

            UpdateZoneCheck();
            // Pemulihan hanya terjadi di zona hijau dan tetap dibatasi minimum stres bab.
            if (IsInGreenZone && stressBarometer != null)
                stressBarometer.AddStress(-StressReliefPerSecond * dt);

            // Sync FPP camera nodding: Inhale pitches UP (negative X Euler), Exhale pitches DOWN (positive X Euler)
            float levelNod = (0.5f - IndicatorValue) * 7.5f;
            float actionBias = holdInhale ? -2.25f : 2.25f;
            float desiredOffset = Mathf.Clamp(levelNod + actionBias, -MaxCameraNodDegrees, MaxCameraNodDegrees);
            CurrentCameraPitchOffset = Mathf.MoveTowards(CurrentCameraPitchOffset, desiredOffset, 18f * dt);
            motor?.SetBreathingPitchOffset(CurrentCameraPitchOffset);

            UpdateBreathAudio(holdInhale);
            RefreshUi();
        }

        // Target bergerak naik dan turun selama satu siklus napas.
        private float EvaluateTargetCenter(float t)
        {
            float half = BreathCycleDuration * 0.5f;
            float norm = t < half ? (t / half) : 1f - ((t - half) / half);
            float smooth = Mathf.SmoothStep(0f, 1f, norm);
            return Mathf.Lerp(0.24f, 0.78f, smooth);
        }

        private void UpdateZoneCheck()
        {
            IsInGreenZone = Mathf.Abs(IndicatorValue - TargetCenter) <= GreenZoneHalfWidth;
        }

        private void UpdateBreathAudio(bool holdInhale)
        {
            if (audioSource == null) return;
            if (holdInhale == wasInhalingAudio && audioSource.isPlaying) return;

            if (holdInhale && !wasInhalingAudio)
            {
                PlayClip(inhaleClip ?? GetFallbackClip(isInhale: true));
                wasInhalingAudio = true;
            }
            else if (!holdInhale && wasInhalingAudio)
            {
                PlayClip(exhaleClip ?? GetFallbackClip(isInhale: false));
                wasInhalingAudio = false;
            }
        }

        private void PlayClip(AudioClip clip)
        {
            if (audioSource == null || clip == null) return;
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.volume = 0.55f;
            audioSource.Play();
        }

        private AudioClip GetFallbackClip(bool isInhale)
        {
            ref AudioClip cache = ref (isInhale ? ref runtimeInhaleFallback : ref runtimeExhaleFallback);
            if (cache != null) return cache;

            const int sampleRate = 22050;
            const int sampleCount = (int)(sampleRate * 2.2f);
            float[] samples = new float[sampleCount];
            var rng = new System.Random(isInhale ? 71 : 93);
            float lp = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float progress = (float)i / sampleCount;
                float env = Mathf.Sin(Mathf.PI * progress);
                float cutoff = isInhale ? (0.07f + 0.08f * progress) : (0.13f - 0.08f * progress);
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += cutoff * (white - lp);
                samples[i] = Mathf.Clamp(lp * env * 0.45f, -0.9f, 0.9f);
            }

            cache = AudioClip.Create(isInhale ? "RakaBreathInhale_Runtime" : "RakaBreathExhale_Runtime", sampleCount, 1, sampleRate, false);
            cache.SetData(samples, 0);
            return cache;
        }

        private void CacheReferences()
        {
            if (player == null) player = GetComponent<FirstPersonPlayer>();
            if (motor == null) motor = GetComponent<FirstPersonMotor>();
            if (stressBarometer == null) stressBarometer = GetComponent<StressBarometer>();
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 0f;
                }
            }
        }

        private void RefreshUi()
        {
            if (panelRoot != null) panelRoot.SetActive(IsActive);
            if (overlayCanvas != null) overlayCanvas.enabled = IsActive && (player == null || !player.IsPaused);
            if (!IsActive) return;

            if (greenZoneRect != null)
            {
                float minX = Mathf.Clamp01(TargetCenter - GreenZoneHalfWidth);
                float maxX = Mathf.Clamp01(TargetCenter + GreenZoneHalfWidth);
                greenZoneRect.anchorMin = new Vector2(minX, 0f);
                greenZoneRect.anchorMax = new Vector2(maxX, 1f);
            }
            if (greenZoneImage != null)
            {
                greenZoneImage.color = IsInGreenZone
                    ? new Color(0.22f, 0.78f, 0.36f, 0.92f)
                    : new Color(0.16f, 0.56f, 0.28f, 0.65f);
            }

            if (indicatorRect != null)
            {
                float clampedX = Mathf.Clamp01(IndicatorValue);
                indicatorRect.anchorMin = new Vector2(clampedX, 0.5f);
                indicatorRect.anchorMax = new Vector2(clampedX, 0.5f);
                indicatorRect.anchoredPosition = Vector2.zero;
            }
            if (indicatorImage != null)
            {
                indicatorImage.color = IsInGreenZone
                    ? new Color(0.75f, 1.0f, 0.80f, 1f)
                    : new Color(0.98f, 0.78f, 0.30f, 1f);
            }

            if (phaseLabel != null)
            {
                phaseLabel.text = CurrentPhase == BreathingPhase.Inhale
                    ? "TARIK NAPAS  ▲  (Tahan [SPASI] / Klik Kiri)"
                    : "KELUAR NAPAS  ▼  (Lepas [SPASI] / Klik)";
            }

            if (statusLabel != null)
            {
                if (IsInGreenZone)
                {
                    statusLabel.text = "<color=#7DF29A>Napas terjaga di bar hijau — Stres Raka menurun...</color>";
                }
                else
                {
                    statusLabel.text = IndicatorValue < TargetCenter
                        ? "<color=#F5B041>Tahan [SPASI] atau Klik Kiri agar indikator naik ke bar hijau</color>"
                        : "<color=#F5B041>Lepaskan [SPASI] agar indikator turun ke bar hijau</color>";
                }
            }
        }

        private void BuildUi()
        {
            if (overlayCanvas != null) return;

            var canvasGo = new GameObject("BreathingMinigameUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            overlayCanvas = canvasGo.GetComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 16;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            panelRoot = new GameObject("BreathingPanel", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(canvasGo.transform, false);
            var panelRect = (RectTransform)panelRoot.transform;
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 92f);
            panelRect.sizeDelta = new Vector2(760f, 168f);
            var panelBg = panelRoot.GetComponent<Image>();
            panelBg.color = new Color(0.03f, 0.045f, 0.055f, 0.86f);
            panelBg.raycastTarget = false;
            var outline = panelRoot.AddComponent<Outline>();
            outline.effectColor = new Color(0.24f, 0.54f, 0.36f, 0.72f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            phaseLabel = CreateText("PhaseLabel", panelRoot.transform, "TARIK NAPAS  ▲  (Tahan [SPASI] / Klik Kiri)", 22, FontStyle.Bold,
                new Color(0.86f, 0.94f, 0.90f, 1f), new Vector2(0f, 46f), new Vector2(700f, 34f));

            trackRect = CreateRect("BreathTrack", panelRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(640f, 34f));
            var trackBg = trackRect.gameObject.AddComponent<Image>();
            trackBg.color = new Color(0.10f, 0.13f, 0.15f, 0.96f);
            trackBg.raycastTarget = false;
            var trackOutline = trackRect.gameObject.AddComponent<Outline>();
            trackOutline.effectColor = new Color(0.30f, 0.38f, 0.42f, 0.85f);
            trackOutline.effectDistance = new Vector2(1f, -1f);

            greenZoneRect = CreateRect("GreenZone", trackRect, new Vector2(0.34f, 0f), new Vector2(0.66f, 1f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            greenZoneRect.offsetMin = new Vector2(0f, 3f);
            greenZoneRect.offsetMax = new Vector2(0f, -3f);
            greenZoneImage = greenZoneRect.gameObject.AddComponent<Image>();
            greenZoneImage.color = new Color(0.22f, 0.78f, 0.36f, 0.90f);
            greenZoneImage.raycastTarget = false;

            indicatorRect = CreateRect("Indicator", trackRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(12f, 44f));
            indicatorImage = indicatorRect.gameObject.AddComponent<Image>();
            indicatorImage.color = Color.white;
            indicatorImage.raycastTarget = false;
            var indOutline = indicatorRect.gameObject.AddComponent<Outline>();
            indOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            indOutline.effectDistance = new Vector2(1.5f, -1.5f);

            statusLabel = CreateText("StatusLabel", panelRoot.transform, "Tahan [SPASI] atau Klik Kiri agar indikator di bar hijau", 19, FontStyle.Normal,
                new Color(0.84f, 0.88f, 0.86f, 1f), new Vector2(0f, -42f), new Vector2(700f, 30f));
            statusLabel.supportRichText = true;

            CreateText("CloseHint", panelRoot.transform, "[TAB]  Selesai Atur Napas", 16, FontStyle.Normal,
                new Color(0.60f, 0.68f, 0.70f, 1f), new Vector2(0f, -68f), new Vector2(400f, 24f));

            panelRoot.SetActive(false);
            overlayCanvas.enabled = false;
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
