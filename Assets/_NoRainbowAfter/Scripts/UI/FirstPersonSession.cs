using NoRainbowAfter.Core;
using NoRainbowAfter.Level;
using NoRainbowAfter.Narrative;
using NoRainbowAfter.Player;
using NoRainbowAfter.Quality;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NoRainbowAfter.UI
{
    public sealed class FirstPersonSession : MonoBehaviour
    {
        private static readonly Color QualitySelectedBg = new Color(0.70f, 0.55f, 0.34f, 0.24f);
        private static readonly Color QualityIdleBg = new Color(0f, 0f, 0f, 0f);
        private static readonly Color QualitySelectedText = new Color(0.97f, 0.93f, 0.84f, 1f);
        private static readonly Color QualityIdleText = new Color(0.62f, 0.73f, 0.78f, 0.86f);

        [SerializeField] private FirstPersonPlayer player;
        [SerializeField] private FirstPersonMotor motor;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button checkpointButton;
        [SerializeField] private DormCheckpoint checkpoint;
        [SerializeField] private Slider sensitivity;
        [SerializeField] private Slider fov;
        [SerializeField] private Text sensitivityLabel;
        [SerializeField] private Text fovLabel;
        [SerializeField] private GameObject staminaRoot;
        [SerializeField] private RectTransform staminaFill;
        [SerializeField] private GameObject gameplayHud;
        [SerializeField] private CircularViewMask viewMask;
        [SerializeField] private Toggle circularViewToggle;
        [SerializeField] private Slider viewRadius;
        [SerializeField] private Text viewRadiusLabel;
        [SerializeField] private Text qualityPresetLabel;
        [SerializeField] private Button qualityLowButton;
        [SerializeField] private Button qualityMediumButton;
        [SerializeField] private Button qualityHighButton;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Text masterVolumeLabel;
        [SerializeField] private CanvasGroup pauseCanvasGroup;
        [SerializeField] private RawImage pauseScrim;
        [SerializeField] private Text chapterStatusLabel;
        [SerializeField] private Text stressStatusLabel;
        [SerializeField] private RectTransform stressFillBar;
        [SerializeField] private Image stressFillImage;

        private bool circularViewEnabled;
        private float circularViewRadius;
        private Material pauseScrimInstance;
        private RectTransform pauseScrimRect;
        private float pauseFadeAlpha = 1f;
        private StressBarometer stressBarometer;
        private ChapterDirector chapterDirector;

        public bool IsPaused { get; private set; }
        public QualityPreset CurrentQualityPreset => GraphicsQualityService.CurrentPreset;

        private void Awake()
        {
            SceneFlow.EnsureInstance();
            GraphicsQualityService.ApplySavedPreset();
            if (player != null)
                stressBarometer = player.GetComponent<StressBarometer>();
            chapterDirector = FindAnyObjectByType<ChapterDirector>();

            if (pauseScrim != null)
            {
                pauseScrimRect = pauseScrim.rectTransform;
                if (pauseScrim.material != null)
                {
                    pauseScrimInstance = new Material(pauseScrim.material);
                    pauseScrim.material = pauseScrimInstance;
                }
            }

            motor.SetViewSettings(PlayerPrefs.GetFloat("NRA.MouseSensitivity", motor.MouseSensitivity),
                PlayerPrefs.GetFloat("NRA.FieldOfView", motor.FieldOfView));
            sensitivity.SetValueWithoutNotify(motor.MouseSensitivity);
            fov.SetValueWithoutNotify(motor.FieldOfView);
            circularViewEnabled = PlayerPrefs.GetInt("NRA.CircularViewEnabled", 1) != 0;
            circularViewRadius = Mathf.Clamp(PlayerPrefs.GetFloat("NRA.CircularViewRadius", 0.50f), 0.45f, 0.80f);
            if (circularViewToggle != null)
            {
                circularViewToggle.SetIsOnWithoutNotify(circularViewEnabled);
                circularViewToggle.onValueChanged.AddListener(OnCircularViewChanged);
            }
            if (viewRadius != null)
            {
                viewRadius.SetValueWithoutNotify(circularViewRadius);
                viewRadius.onValueChanged.AddListener(OnViewRadiusChanged);
            }
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(AudioListener.volume);
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }
            if (qualityLowButton != null)
                qualityLowButton.onClick.AddListener(() => SelectQualityPreset(QualityPreset.Low));
            if (qualityMediumButton != null)
                qualityMediumButton.onClick.AddListener(() => SelectQualityPreset(QualityPreset.Medium));
            if (qualityHighButton != null)
                qualityHighButton.onClick.AddListener(() => SelectQualityPreset(QualityPreset.High));
            UpdateLabels();
            UpdateQualityButtons(GraphicsQualityService.CurrentPreset);
            sensitivity.onValueChanged.AddListener(_ => ApplySettings());
            fov.onValueChanged.AddListener(_ => ApplySettings());
            resumeButton.onClick.AddListener(() => SetPaused(false));
            menuButton.onClick.AddListener(ReturnToMenu);
            if (checkpointButton != null && checkpoint != null)
                checkpointButton.onClick.AddListener(RestoreCheckpoint);
            pausePanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (player != null)
            {
                player.PauseRequested += TogglePause;
                player.FocusLost += PauseForFocusLoss;
                if (stressBarometer == null)
                    stressBarometer = player.GetComponent<StressBarometer>();
            }
            if (stressBarometer != null)
                stressBarometer.StressChanged += OnStressChanged;
            GraphicsQualityService.PresetChanged += OnQualityPresetChanged;
            OnQualityPresetChanged(GraphicsQualityService.CurrentPreset);
        }

        private void Start() => SetPaused(false);

        private void OnDisable()
        {
            if (player != null)
            {
                player.PauseRequested -= TogglePause;
                player.FocusLost -= PauseForFocusLoss;
            }
            if (stressBarometer != null)
                stressBarometer.StressChanged -= OnStressChanged;
            GraphicsQualityService.PresetChanged -= OnQualityPresetChanged;
            Time.timeScale = 1;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            PlayerPrefs.Save();
        }

        private void OnDestroy()
        {
            if (pauseScrimInstance != null) Destroy(pauseScrimInstance);
        }

        private void Update()
        {
            staminaRoot.SetActive(!IsPaused && motor.StaminaNormalized < 0.999f);
            staminaFill.anchorMax = new Vector2(motor.StaminaNormalized, 1);

            if (IsPaused)
            {
                if (pauseCanvasGroup != null && pauseFadeAlpha < 1f)
                {
                    pauseFadeAlpha = Mathf.MoveTowards(pauseFadeAlpha, 1f, Time.unscaledDeltaTime * 7.5f);
                    pauseCanvasGroup.alpha = 1f - (1f - pauseFadeAlpha) * (1f - pauseFadeAlpha);
                }

                if (pauseScrimInstance != null)
                {
                    pauseScrimInstance.SetFloat("_UnscaledTime", Time.unscaledTime);
                    if (pauseScrimRect != null && pauseScrimRect.rect.height > 1f)
                        pauseScrimInstance.SetFloat("_Aspect", pauseScrimRect.rect.width / pauseScrimRect.rect.height);
                    float stressNorm = stressBarometer != null ? stressBarometer.NormalizedStress : 0f;
                    pauseScrimInstance.SetFloat("_StressLevel", stressNorm);
                }
            }
        }

        private void TogglePause()
        {
            SetPaused(!IsPaused);
        }

        private void PauseForFocusLoss()
        {
            SetPaused(true);
        }

        public void SetPaused(bool paused)
        {
            bool changed = IsPaused != paused;
            IsPaused = paused;
            // Hentikan waktu simulasi dan teruskan status jeda ke pengendali pemain.
            // Animasi panel di Update memakai unscaledDeltaTime supaya menu tetap bergerak.
            Time.timeScale = paused ? 0 : 1;
            if (player != null) player.SetPaused(paused);
            if (pausePanel != null) pausePanel.SetActive(paused);
            if (gameplayHud != null) gameplayHud.SetActive(!paused);
            ApplyCircularView();
            if (paused)
            {
                if (masterVolumeSlider != null)
                    masterVolumeSlider.SetValueWithoutNotify(AudioListener.volume);
                UpdateLabels();
                RefreshPauseContext();
                if (pauseCanvasGroup != null)
                {
                    pauseFadeAlpha = 0.16f;
                    pauseCanvasGroup.alpha = pauseFadeAlpha;
                }
                if (changed) MenuUiAudio.PlayPanelTransition();
                if (EventSystem.current != null && resumeButton != null)
                    EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
            }
            else
            {
                if (changed) MenuUiAudio.PlayPanelTransition();
                PlayerPrefs.Save();
            }
        }

        private void OnStressChanged(float _)
        {
            if (IsPaused) RefreshPauseContext();
        }

        private void RefreshPauseContext()
        {
            if (chapterStatusLabel != null)
                chapterStatusLabel.text = ResolveChapterLabel();

            if (stressBarometer == null && player != null)
                stressBarometer = player.GetComponent<StressBarometer>();

            if (stressStatusLabel != null)
            {
                if (stressBarometer != null)
                {
                    float stress = stressBarometer.CurrentStress;
                    float norm = stressBarometer.NormalizedStress;
                    string mood = DescribeStress(stress);
                    stressStatusLabel.text = "Kondisi Mental  ·  " + mood + " (" + Mathf.RoundToInt(stress) + "%)";
                    if (stressFillBar != null)
                        stressFillBar.anchorMax = new Vector2(Mathf.Clamp01(norm), 1f);
                    if (stressFillImage != null)
                    {
                        stressFillImage.color = stress >= 70f
                            ? new Color(0.76f, 0.24f, 0.22f, 0.95f)
                            : stress >= 45f
                                ? new Color(0.84f, 0.65f, 0.39f, 0.92f)
                                : new Color(0.42f, 0.66f, 0.72f, 0.90f);
                    }
                }
                else
                {
                    stressStatusLabel.text = "Mode Latihan Kontrol";
                    if (stressFillBar != null) stressFillBar.anchorMax = new Vector2(0.25f, 1f);
                }
            }
        }

        // Label di menu pause hanya menerjemahkan angka stres yang dihitung StressBarometer.
        private static string DescribeStress(float stress)
        {
            if (stress >= 80f) return "Tertekan Berat";
            if (stress >= 65f) return "Tegang";
            if (stress >= 45f) return "Gelisah";
            return "Tenang";
        }

        private string ResolveChapterLabel()
        {
            if (chapterDirector == null) chapterDirector = FindAnyObjectByType<ChapterDirector>();
            if (chapterDirector == null || chapterDirector.State == null)
                return "UJI KONTROL  ·  ORANG PERTAMA";

            switch (chapterDirector.State.ActiveChapter)
            {
                case "Chapter2":
                    return "BAB II  ·  PASAR MALAM";
                case "Chapter3":
                    return "BAB III  ·  PERJALANAN";
                default:
                    return "BAB I  ·  KOS RAKA";
            }
        }

        private void ApplySettings()
        {
            motor.SetViewSettings(sensitivity.value, fov.value);
            PlayerPrefs.SetFloat("NRA.MouseSensitivity", motor.MouseSensitivity);
            PlayerPrefs.SetFloat("NRA.FieldOfView", motor.FieldOfView);
            UpdateLabels();
        }

        private void OnMasterVolumeChanged(float value)
        {
            AudioListener.volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("NRA.MasterVolume", AudioListener.volume);
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            sensitivityLabel.text = "Sensitivitas mouse  " + motor.MouseSensitivity.ToString("F2");
            fovLabel.text = "Bidang pandang  " + Mathf.RoundToInt(motor.FieldOfView) + "°";
            if (viewRadiusLabel != null)
                viewRadiusLabel.text = "Lebar pandang  " + Mathf.RoundToInt(circularViewRadius * 100f) + "%";
            if (masterVolumeLabel != null)
                masterVolumeLabel.text = "Volume utama  " + Mathf.RoundToInt(AudioListener.volume * 100f) + "%";
            if (qualityPresetLabel != null)
                qualityPresetLabel.text = "Kualitas grafis  —  " + GraphicsQualityService.GetDisplayLabel(GraphicsQualityService.CurrentPreset);
        }

        public void SelectQualityPreset(QualityPreset preset)
        {
            GraphicsQualityService.ApplyPreset(preset, savePreference: true);
            MenuUiAudio.PlayConfirm();
        }

        private void OnQualityPresetChanged(QualityPreset preset)
        {
            UpdateLabels();
            UpdateQualityButtons(preset);
        }

        private void UpdateQualityButtons(QualityPreset activePreset)
        {
            StyleQualityButton(qualityLowButton, activePreset == QualityPreset.Low);
            StyleQualityButton(qualityMediumButton, activePreset == QualityPreset.Medium);
            StyleQualityButton(qualityHighButton, activePreset == QualityPreset.High);
        }

        private static void StyleQualityButton(Button button, bool selected)
        {
            if (button == null) return;
            var bg = button.GetComponent<Image>();
            if (bg != null)
                bg.color = selected ? QualitySelectedBg : QualityIdleBg;

            Transform activeBar = button.transform.Find("ActiveBar");
            if (activeBar != null) activeBar.gameObject.SetActive(selected);

            Transform outline = button.transform.Find("Border");
            if (outline != null) outline.gameObject.SetActive(false);

            var caption = button.GetComponentInChildren<Text>(true);
            if (caption != null)
            {
                caption.color = selected ? QualitySelectedText : QualityIdleText;
                caption.fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        private void OnCircularViewChanged(bool enabled)
        {
            circularViewEnabled = enabled;
            PlayerPrefs.SetInt("NRA.CircularViewEnabled", enabled ? 1 : 0);
            MenuUiAudio.PlayHover();
            ApplyCircularView();
        }

        private void OnViewRadiusChanged(float radius)
        {
            circularViewRadius = Mathf.Clamp(radius, 0.45f, 0.80f);
            PlayerPrefs.SetFloat("NRA.CircularViewRadius", circularViewRadius);
            UpdateLabels();
            ApplyCircularView();
        }

        private void ApplyCircularView()
        {
            if (viewMask != null)
                viewMask.ApplySettings(circularViewEnabled && !IsPaused, circularViewRadius);
        }

        private void ReturnToMenu()
        {
            Time.timeScale = 1;
            SceneFlow.EnsureInstance().Load(GameScene.MainMenu);
        }

        private void RestoreCheckpoint()
        {
            var label = checkpointButton.GetComponentInChildren<Text>();
            if (checkpoint.TryRestore())
            {
                if (label != null) label.text = "Kembali ke Titik Awal";
            }
            else if (label != null) label.text = "Titik awal terhalang";
        }
    }
}
