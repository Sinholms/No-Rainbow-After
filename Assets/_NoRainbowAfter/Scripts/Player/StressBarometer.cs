using System;
using UnityEngine;
using UnityEngine.UI;

namespace NoRainbowAfter.Player
{
    /// <summary>
    /// Menyimpan nilai stres Raka, mengatur batas minimum per bab, memicu audio detak jantung
    /// saat stres tinggi (&gt;= 70%), dan menggambar bilah HUD merah melalui event StressChanged.
    ///
    /// [TEKNIS KODING]
    /// Dipecah menjadi 2 file partial class:
    /// 1. StressBarometer.cs (nilai stres, batas minimum, detak jantung prosedural)
    /// 2. StressBarometer.Ui.cs (konstruksi &amp; pembaruan HUD StressBarometer serta vignet pulsa merah)
    /// </summary>
    public sealed partial class StressBarometer : MonoBehaviour
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
            {
                Destroy(pulseVignette.transform.parent.gameObject);
            }
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

        // Nilai dibatasi oleh minimum yang berlaku pada bab saat ini dan nilai maksimum stres (100%).
        public void SetStress(float value)
        {
            float clamped = Mathf.Clamp(value, MinimumStress, MaxStress);
            if (clamped > currentStress + 0.01f)
            {
                flashTimer = 0.35f;
            }
            currentStress = clamped;
            StressChanged?.Invoke(currentStress);
            RefreshUi();
        }

        public void SetMinimumStress(float value)
        {
            MinimumStress = Mathf.Clamp(value, 0f, MaxStress);
            minimumWarningText = $"STRES TINGGI • Batas minimum stress saat ini adalah {Mathf.RoundToInt(MinimumStress)}%.";
            if (currentStress < MinimumStress)
            {
                SetStress(currentStress);
            }
        }

        // Sistem lain mengirim perubahan bertanda: napas negatif, penalti tertangkap sorot positif.
        public void AddStress(float amount)
        {
            if (Mathf.Approximately(amount, 0f))
            {
                return;
            }
            SetStress(currentStress + amount);
        }

        public void ResetToInitial()
        {
            flashTimer = 0f;
            SetStress(InitialStress);
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
            if (!heartbeatSource.isPlaying)
            {
                heartbeatSource.Play();
            }
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
    }
}
