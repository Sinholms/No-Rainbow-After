using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace NoRainbowAfter.Player
{
    public enum BreathingPhase
    {
        Inhale,
        Exhale
    }

    /// <summary>
    /// Minigame pengaturan napas organik yang diaktifkan dengan tombol [Tab] saat eksplorasi FPP.
    /// Memandu ritme TARIK -> TAHAN -> HEMBUSKAN untuk menurunkan StressBarometer.
    ///
    /// [GRAFIKA KOMPUTER]
    /// Menggunakan shader SDF cincin konsentris (NoRainbowAfter/UI/BreathingRing), modulasi FOV/pitch
    /// kamera mengikuti kurva AnimationCurve + Perlin Noise shake, serta URP Vignette &amp; ChromaticAberration.
    ///
    /// [TEKNIS KODING]
    /// Dipecah menjadi 3 file partial class:
    /// 1. BreathingMinigame.cs (state siklus napas, SmoothDamp inersia paru, penurunan stres)
    /// 2. BreathingMinigame.SensoryAndUi.cs (efek kamera, post-processing URP, update shader cincin)
    /// 3. BreathingMinigame.Audio.cs (audio napas, detak jantung tenang, low-pass ducking ambien)
    /// </summary>
    public sealed partial class BreathingMinigame : MonoBehaviour
    {
        // =========================================================================
        // Konstanta Ritme Napas, Inersia, & Pengaturan Sensori
        // =========================================================================

        public const float BreathCycleDuration = 8f;
        public const float GreenZoneHalfWidth = 0.16f;
        public const float InhaleRiseRate = 0.22f;
        public const float ExhaleFallRate = 0.19f;
        public const float StressReliefPerSecond = 10f;
        public const float MaxCameraNodDegrees = 6.0f;

        private const float LungSmoothTime = 1.05f;
        private const float LungMaxSpeed = 0.22f;
        private const float MaxBreathFovBoost = 2.1f;
        private const float MaxBreathEyeLift = 0.032f;
        private const float AmbientDuckFactor = 0.38f;
        private const float AmbientLowPassCutoff = 650f;
        private const float NormalLowPassCutoff = 22000f;

        private static readonly Color CalmSageTeal = new Color(0.392f, 0.710f, 0.651f, 0.94f);
        private static readonly Color CalmGuideTint = new Color(0.74f, 0.89f, 0.86f, 0.62f);
        private static readonly Color IdleGuideTint = new Color(0.76f, 0.84f, 0.83f, 0.42f);
        private static readonly Color PaleWarningOrange = new Color(0.84f, 0.50f, 0.34f, 0.88f);
        private static readonly Color DimMaroon = new Color(0.66f, 0.24f, 0.22f, 0.92f);

        // =========================================================================
        // Field Inspector & Referensi Runtime
        // =========================================================================

        [SerializeField] private AudioClip inhaleClip;
        [SerializeField] private AudioClip exhaleClip;
        [SerializeField] private AnimationCurve breathCameraCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private FirstPersonPlayer player;
        private FirstPersonMotor motor;
        private StressBarometer stressBarometer;
        private AudioSource audioSource;
        private AudioSource calmHeartbeatSource;

        private AudioClip runtimeInhaleFallback;
        private AudioClip runtimeExhaleFallback;
        private AudioClip runtimeHeartbeatClip;

        private Canvas overlayCanvas;
        private GameObject ringRoot;
        private RectTransform targetRingRect;
        private RawImage targetRingImage;
        private Material targetRingMaterial;
        private RectTransform playerRingRect;
        private RawImage playerRingImage;
        private Material playerRingMaterial;
        private Texture2D fallbackRingTexture;
        private Text phaseLabel;
        private Text closeHintLabel;

        private Volume postVolume;
        private VolumeProfile postProfile;
        private Vignette vignetteOverride;
        private ChromaticAberration chromaticOverride;

        private readonly List<DuckedAudioEntry> duckedAudioSources = new List<DuckedAudioEntry>();

        private float cycleTime;
        private float indicatorVelocity;
        private float currentGlowIntensity = 0.65f;
        private float currentVignetteIntensity = 0.22f;
        private float currentChromaticIntensity;
        private float currentMicroShake;
        private float currentFovOffset;
        private float currentEyeOffsetY;
        private float phaseFadeAlpha = 1f;
        private string activeCueText = "T A R I K";
        private string pendingCueText = "T A R I K";
        private int openedFrame = -1;
        private bool wasInhalingAudio;
        private Color currentPlayerRingColor = CalmSageTeal;

        private struct DuckedAudioEntry
        {
            public AudioSource Source;
            public float OriginalVolume;
            public AudioLowPassFilter Filter;
            public bool AddedFilter;
            public float OriginalCutoff;
        }

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

        // =========================================================================
        // Siklus Hidup & Mulai/Henti Latihan Napas
        // =========================================================================

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
            {
                player.BreatheRequested -= OnBreatheRequested;
            }
            if (IsActive)
            {
                StopBreathing();
            }
        }

        private void OnDestroy()
        {
            RestoreDuckedAudio();
            if (overlayCanvas != null) Destroy(overlayCanvas.gameObject);
            if (targetRingMaterial != null) Destroy(targetRingMaterial);
            if (playerRingMaterial != null) Destroy(playerRingMaterial);
            if (fallbackRingTexture != null) Destroy(fallbackRingTexture);
            if (postVolume != null) Destroy(postVolume.gameObject);
            if (postProfile != null) Destroy(postProfile);
            if (runtimeInhaleFallback != null) Destroy(runtimeInhaleFallback);
            if (runtimeExhaleFallback != null) Destroy(runtimeExhaleFallback);
            if (runtimeHeartbeatClip != null) Destroy(runtimeHeartbeatClip);
        }

        private void OnBreatheRequested()
        {
            if (player != null && (player.IsUsingLaptop || player.LaptopClosedFrame == Time.frameCount))
            {
                return;
            }
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
            if (IsActive)
            {
                return false;
            }
            if (player != null && (player.IsPaused || player.IsInspecting || player.IsUsingLaptop || player.IsTransitionLocked || player.LaptopClosedFrame == Time.frameCount))
            {
                return false;
            }

            BuildUi();
            EnsurePostProcessingVolume();

            IsActive = true;
            openedFrame = Time.frameCount;
            cycleTime = 0f;
            indicatorVelocity = 0f;
            TargetCenter = EvaluateTargetCenter(cycleTime);
            IndicatorValue = TargetCenter;
            CurrentPhase = BreathingPhase.Inhale;
            CurrentCameraPitchOffset = 0f;
            currentFovOffset = 0f;
            currentEyeOffsetY = 0f;
            currentMicroShake = 0f;
            currentChromaticIntensity = 0f;
            currentVignetteIntensity = 0.28f;
            currentGlowIntensity = 0.85f;
            phaseFadeAlpha = 1f;
            activeCueText = "T A R I K";
            pendingCueText = activeCueText;
            wasInhalingAudio = false;

            // Nonaktifkan kendali gerak selama latihan; status bersembunyi di lemari tetap dipertahankan.
            player?.SetBreathing(true);
            motor?.SetBreathingCameraEffects(0f, 0f, 0f, Vector2.zero);
            BeginAudioDucking();
            UpdateZoneCheck();
            RefreshUi(0f);
            BreathingStateChanged?.Invoke(true);
            return true;
        }

        public void StopBreathing()
        {
            if (!IsActive)
            {
                return;
            }
            IsActive = false;
            CurrentCameraPitchOffset = 0f;
            currentFovOffset = 0f;
            currentEyeOffsetY = 0f;
            currentMicroShake = 0f;
            indicatorVelocity = 0f;

            motor?.SetBreathingCameraEffects(0f, 0f, 0f, Vector2.zero);
            if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
            if (calmHeartbeatSource != null && calmHeartbeatSource.isPlaying) calmHeartbeatSource.Stop();
            RestoreDuckedAudio();

            if (postVolume != null) postVolume.weight = 0f;
            if (ringRoot != null) ringRoot.SetActive(false);
            if (overlayCanvas != null) overlayCanvas.enabled = false;
            player?.SetBreathing(false);
            BreathingStateChanged?.Invoke(false);
        }

        // =========================================================================
        // Simulasi Siklus & Inersia Paru Per Frame
        // =========================================================================

        private void Update()
        {
            if (!IsActive)
            {
                return;
            }
            if (player != null && player.IsPaused)
            {
                if (overlayCanvas != null) overlayCanvas.enabled = false;
                if (postVolume != null) postVolume.weight = 0f;
                return;
            }
            if (overlayCanvas != null && !overlayCanvas.enabled)
            {
                overlayCanvas.enabled = true;
            }

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

        /// <summary>
        /// Menahan Spasi/Klik memperbesar cincin napas Raka, melepasnya memperkecil cincin.
        /// Selama ukurannya mendekati cincin target, stres berkurang sesuai durasi.
        /// </summary>
        public void Tick(float deltaTime, bool holdInhale)
        {
            if (!IsActive)
            {
                return;
            }
            float dt = Mathf.Max(0f, deltaTime);

            cycleTime = (cycleTime + dt) % BreathCycleDuration;
            float halfCycle = BreathCycleDuration * 0.5f;
            CurrentPhase = cycleTime < halfCycle ? BreathingPhase.Inhale : BreathingPhase.Exhale;
            TargetCenter = EvaluateTargetCenter(cycleTime);

            // Inersia paru-paru dengan SmoothDamp agar perubahan radius terasa organik.
            float lungGoal = holdInhale ? 0.86f : 0.16f;
            IndicatorValue = Mathf.Clamp01(Mathf.SmoothDamp(
                IndicatorValue,
                lungGoal,
                ref indicatorVelocity,
                LungSmoothTime,
                LungMaxSpeed,
                Mathf.Max(0.0001f, dt)));

            UpdateZoneCheck();
            // Pemulihan hanya terjadi di zona hijau dan tetap dibatasi minimum stres bab.
            if (IsInGreenZone && stressBarometer != null)
            {
                stressBarometer.AddStress(-StressReliefPerSecond * dt);
            }

            UpdateSensoryCamera(dt, holdInhale);
            UpdatePostProcessing(dt);
            UpdateBreathAudio(holdInhale);
            UpdateCalmHeartbeat(dt);
            UpdateAudioDucking(dt);
            RefreshUi(dt);
        }

        private float EvaluateTargetCenter(float t)
        {
            float half = BreathCycleDuration * 0.5f;
            float norm = t < half ? (t / half) : 1f - ((t - half) / half);
            float shaped = Mathf.Clamp01(norm * 1.12f - 0.06f);
            float smooth = Mathf.SmoothStep(0f, 1f, shaped);
            return Mathf.Lerp(0.24f, 0.78f, smooth);
        }

        private string EvaluateCueKeyword()
        {
            float t = cycleTime;
            if (t < 3.2f) return "T A R I K";
            if (t < 4.3f) return "T A H A N";
            if (t < 7.4f) return "H E M B U S K A N";
            return "T A H A N";
        }

        private void UpdateZoneCheck()
        {
            IsInGreenZone = Mathf.Abs(IndicatorValue - TargetCenter) <= GreenZoneHalfWidth;
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

            if (calmHeartbeatSource == null)
            {
                var hbGo = new GameObject("BreathingCalmHeartbeat");
                hbGo.transform.SetParent(transform, false);
                calmHeartbeatSource = hbGo.AddComponent<AudioSource>();
                calmHeartbeatSource.playOnAwake = false;
                calmHeartbeatSource.loop = true;
                calmHeartbeatSource.spatialBlend = 0f;
                calmHeartbeatSource.clip = GetHeartbeatClip();
            }
        }
    }
}
