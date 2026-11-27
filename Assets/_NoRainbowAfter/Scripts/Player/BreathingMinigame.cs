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
    /// Organic breathing regulation minigame toggled via [Tab] during FPP exploration.
    /// Renders concentric breathing rings over the live 3D scene without opaque panels,
    /// drives lung-inertia SmoothDamp input, camera FOV/Y-axis breathing &amp; micro-shake,
    /// dynamic URP Vignette/ChromaticAberration, and ambient audio low-pass ducking.
    /// </summary>
    public sealed class BreathingMinigame : MonoBehaviour
    {
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

        private static readonly Color CalmSageTeal = new Color(0.392f, 0.710f, 0.651f, 0.94f);     // #64B5A6
        private static readonly Color CalmGuideTint = new Color(0.74f, 0.89f, 0.86f, 0.62f);
        private static readonly Color IdleGuideTint = new Color(0.76f, 0.84f, 0.83f, 0.42f);
        private static readonly Color PaleWarningOrange = new Color(0.84f, 0.50f, 0.34f, 0.88f);
        private static readonly Color DimMaroon = new Color(0.66f, 0.24f, 0.22f, 0.92f);

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

            // Nonaktifkan kendali gerak selama latihan; status bersembunyi tidak diubah.
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
            if (!IsActive) return;
            IsActive = false;
            CurrentCameraPitchOffset = 0f;
            currentFovOffset = 0f;
            currentEyeOffsetY = 0f;
            currentMicroShake = 0f;
            indicatorVelocity = 0f;

            motor?.SetBreathingCameraEffects(0f, 0f, 0f, Vector2.zero);
            if (audioSource != null && audioSource.isPlaying)
                audioSource.Stop();
            if (calmHeartbeatSource != null && calmHeartbeatSource.isPlaying)
                calmHeartbeatSource.Stop();
            RestoreDuckedAudio();

            if (postVolume != null) postVolume.weight = 0f;
            if (ringRoot != null) ringRoot.SetActive(false);
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
                if (postVolume != null) postVolume.weight = 0f;
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

        /// <summary>Menahan tombol memperbesar cincin napas Raka, melepasnya memperkecil cincin.
        /// Selama ukurannya mendekati cincin target, stres berkurang sesuai durasi.</summary>
        public void Tick(float deltaTime, bool holdInhale)
        {
            if (!IsActive) return;
            float dt = Mathf.Max(0f, deltaTime);

            // Waktu siklus menggerakkan target; input pemain menggerakkan indikator secara terpisah.
            cycleTime = (cycleTime + dt) % BreathCycleDuration;
            float halfCycle = BreathCycleDuration * 0.5f;
            CurrentPhase = cycleTime < halfCycle ? BreathingPhase.Inhale : BreathingPhase.Exhale;
            TargetCenter = EvaluateTargetCenter(cycleTime);

            // Lung-like inertia: quick initial draw/release, tapering smoothly near capacity
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
                stressBarometer.AddStress(-StressReliefPerSecond * dt);

            UpdateSensoryCamera(dt, holdInhale);
            UpdatePostProcessing(dt);
            UpdateBreathAudio(holdInhale);
            UpdateCalmHeartbeat(dt);
            UpdateAudioDucking(dt);
            RefreshUi(dt);
        }

        // Target bergerak naik dan turun selama satu siklus napas.
        private float EvaluateTargetCenter(float t)
        {
            float half = BreathCycleDuration * 0.5f;
            float norm = t < half ? (t / half) : 1f - ((t - half) / half);
            // Gentle plateau at peak/trough to match TARIK -> TAHAN -> HEMBUSKAN cadence
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

        private void UpdateSensoryCamera(float dt, bool holdInhale)
        {
            // Pitch nod: Inhale pitches UP (negative X Euler), Exhale pitches DOWN (positive X Euler)
            float levelNod = (0.5f - IndicatorValue) * 7.5f;
            float actionBias = holdInhale ? -2.25f : 2.25f;
            float desiredPitch = Mathf.Clamp(levelNod + actionBias, -MaxCameraNodDegrees, MaxCameraNodDegrees);
            CurrentCameraPitchOffset = Mathf.MoveTowards(CurrentCameraPitchOffset, desiredPitch, 18f * dt);

            // FOV expansion & subtle chest lift following AnimationCurve
            float lungNorm = Mathf.InverseLerp(0.18f, 0.84f, IndicatorValue);
            float curveEval = breathCameraCurve != null ? breathCameraCurve.Evaluate(lungNorm) : lungNorm;
            float targetFov = curveEval * MaxBreathFovBoost;
            float targetEyeY = curveEval * MaxBreathEyeLift;
            float smooth = dt > 0f ? 1f - Mathf.Exp(-7f * dt) : 1f;
            currentFovOffset = Mathf.Lerp(currentFovOffset, targetFov, smooth);
            currentEyeOffsetY = Mathf.Lerp(currentEyeOffsetY, targetEyeY, smooth);

            // High-frequency micro-shake when out of rhythm, damped to 0 in sweet spot
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
            if (postVolume == null || postProfile == null) return;
            postVolume.weight = 1f;

            float stressNorm = stressBarometer != null ? stressBarometer.NormalizedStress : 0.5f;
            float zonePenalty = IsInGreenZone ? 0f : Mathf.Clamp01((Mathf.Abs(IndicatorValue - TargetCenter) - GreenZoneHalfWidth) / 0.35f);
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
                vignetteOverride.intensity.value = currentVignetteIntensity;
            if (chromaticOverride != null)
                chromaticOverride.intensity.value = currentChromaticIntensity;
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

        private void UpdateCalmHeartbeat(float dt)
        {
            if (calmHeartbeatSource == null) return;
            if (player != null && player.IsPaused)
            {
                if (calmHeartbeatSource.isPlaying) calmHeartbeatSource.Pause();
                return;
            }

            float stressNorm = stressBarometer != null ? stressBarometer.NormalizedStress : 0.5f;
            float targetPitch = IsInGreenZone
                ? Mathf.Lerp(0.72f, 0.98f, stressNorm)
                : Mathf.Lerp(0.92f, 1.25f, stressNorm);
            float targetVol = Mathf.Lerp(0.14f, 0.36f, stressNorm);

            float smooth = dt > 0f ? 1f - Mathf.Exp(-2.5f * dt) : 1f;
            calmHeartbeatSource.pitch = Mathf.Lerp(calmHeartbeatSource.pitch, targetPitch, smooth);
            calmHeartbeatSource.volume = Mathf.Lerp(calmHeartbeatSource.volume, targetVol, smooth);
            if (!calmHeartbeatSource.isPlaying) calmHeartbeatSource.Play();
        }

        private void BeginAudioDucking()
        {
            RestoreDuckedAudio();
            var sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource src = sources[i];
                if (src == null || src == audioSource || src == calmHeartbeatSource) continue;
                if (src.gameObject == gameObject && src.clip != null && src.clip.name == "RakaStressHeartbeat") continue;

                var filter = src.GetComponent<AudioLowPassFilter>();
                bool added = false;
                float origCutoff = NormalLowPassCutoff;
                if (filter == null)
                {
                    filter = src.gameObject.AddComponent<AudioLowPassFilter>();
                    filter.cutoffFrequency = NormalLowPassCutoff;
                    added = true;
                }
                else
                {
                    origCutoff = filter.cutoffFrequency;
                }

                duckedAudioSources.Add(new DuckedAudioEntry
                {
                    Source = src,
                    OriginalVolume = src.volume,
                    Filter = filter,
                    AddedFilter = added,
                    OriginalCutoff = origCutoff
                });
            }
        }

        private void UpdateAudioDucking(float dt)
        {
            float smooth = dt > 0f ? 1f - Mathf.Exp(-5f * dt) : 1f;
            for (int i = 0; i < duckedAudioSources.Count; i++)
            {
                var entry = duckedAudioSources[i];
                if (entry.Source != null)
                    entry.Source.volume = Mathf.Lerp(entry.Source.volume, entry.OriginalVolume * AmbientDuckFactor, smooth);
                if (entry.Filter != null)
                    entry.Filter.cutoffFrequency = Mathf.Lerp(entry.Filter.cutoffFrequency, AmbientLowPassCutoff, smooth);
            }
        }

        private void RestoreDuckedAudio()
        {
            for (int i = 0; i < duckedAudioSources.Count; i++)
            {
                var entry = duckedAudioSources[i];
                if (entry.Source != null)
                    entry.Source.volume = entry.OriginalVolume;
                if (entry.Filter != null)
                {
                    if (entry.AddedFilter) Destroy(entry.Filter);
                    else entry.Filter.cutoffFrequency = entry.OriginalCutoff;
                }
            }
            duckedAudioSources.Clear();
        }

        private void PlayClip(AudioClip clip)
        {
            if (audioSource == null || clip == null) return;
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.volume = 0.58f;
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

        private AudioClip GetHeartbeatClip()
        {
            if (runtimeHeartbeatClip != null) return runtimeHeartbeatClip;
            const int rate = 22050;
            const float duration = 1.1f;
            int samples = Mathf.RoundToInt(rate * duration);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / rate;
                float lub = Mathf.Exp(-Mathf.Pow((t - 0.14f) / 0.045f, 2f)) * Mathf.Sin(2f * Mathf.PI * 48f * t);
                float dub = Mathf.Exp(-Mathf.Pow((t - 0.34f) / 0.055f, 2f)) * Mathf.Sin(2f * Mathf.PI * 42f * t) * 0.72f;
                data[i] = Mathf.Clamp((lub + dub) * 0.85f, -1f, 1f);
            }
            runtimeHeartbeatClip = AudioClip.Create("RakaCalmHeartbeat_Runtime", samples, 1, rate, false);
            runtimeHeartbeatClip.SetData(data, 0);
            return runtimeHeartbeatClip;
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

        private void EnsurePostProcessingVolume()
        {
            if (postVolume != null) return;
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

        private void RefreshUi(float dt)
        {
            if (ringRoot != null) ringRoot.SetActive(IsActive);
            if (overlayCanvas != null) overlayCanvas.enabled = IsActive && (player == null || !player.IsPaused);
            if (!IsActive) return;

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

            // Organic jitter on Raka's ring when outside sweet spot
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

            // Minimalist cue keyword with smooth fade transition
            string targetCue = EvaluateCueKeyword();
            if (targetCue != pendingCueText)
                pendingCueText = targetCue;

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

                Color textCol = IsInGreenZone
                    ? new Color(0.76f, 0.93f, 0.89f, phaseFadeAlpha * 0.95f)
                    : new Color(0.92f, 0.78f, 0.72f, phaseFadeAlpha * 0.90f);
                phaseLabel.color = textCol;
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

            // Frameless container centered slightly below eye-line so 3D scene stays fully visible
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
            if (fallbackRingTexture != null) return fallbackRingTexture;
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
