using NoRainbowAfter.AI;
using NoRainbowAfter.Core;
using NoRainbowAfter.Quality;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NoRainbowAfter.UI
{
    public enum ShellKind { MainMenu, Gameplay, Credits }

    /// <summary>Main menu, gameplay shell, and credits UI controller with smooth unscaled transitions.</summary>
    public sealed class ShellScreen : MonoBehaviour
    {
        private static readonly Color QualitySelectedBg = new Color(0.70f, 0.55f, 0.34f, 0.24f);
        private static readonly Color QualityIdleBg = new Color(0f, 0f, 0f, 0f);
        private static readonly Color QualitySelectedText = new Color(0.97f, 0.93f, 0.84f, 1f);
        private static readonly Color QualityIdleText = new Color(0.62f, 0.73f, 0.78f, 0.86f);

        [SerializeField] private ShellKind kind;
        [SerializeField] private CanvasGroup controls;
        [SerializeField] private Button primaryButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button playTestButton;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button settingsBackButton;
        [SerializeField] private Slider masterVolume;
        [SerializeField] private Text volumeValue;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Text qualityPresetLabel;
        [SerializeField] private Button qualityLowButton;
        [SerializeField] private Button qualityMediumButton;
        [SerializeField] private Button qualityHighButton;
        [SerializeField] private CanvasGroup settingsCanvasGroup;
        [SerializeField] private RawImage settingsScrim;
        [SerializeField] private Graphic introFadeOverlay;

        private SceneFlow flow;
        private Material settingsScrimInstance;
        private RectTransform settingsScrimRect;
        private float introFadeAlpha = 1f;
        private float settingsFadeAlpha;

        private void Awake()
        {
            flow = SceneFlow.EnsureInstance();
            GraphicsQualityService.ApplySavedPreset();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (continueButton != null) continueButton.interactable = false;
            // No checkpoint is manufactured by this foundation task.
            if (primaryButton != null)
                primaryButton.onClick.AddListener(StartPrimary);
            if (creditsButton != null) creditsButton.onClick.AddListener(() => flow.Load(GameScene.Credits));
            if (menuButton != null) menuButton.onClick.AddListener(() => flow.Load(GameScene.MainMenu));
            if (playTestButton != null) playTestButton.onClick.AddListener(() => flow.Load(GameScene.FirstPersonTest));
            if (settingsButton != null) settingsButton.onClick.AddListener(() => ShowSettings(true));
            if (settingsBackButton != null) settingsBackButton.onClick.AddListener(() => ShowSettings(false));
            if (quitButton != null) quitButton.onClick.AddListener(Quit);
            if (masterVolume != null)
            {
                masterVolume.SetValueWithoutNotify(AudioListener.volume);
                UpdateVolumeLabel(AudioListener.volume);
                masterVolume.onValueChanged.AddListener(SetVolume);
            }
            if (qualityLowButton != null)
                qualityLowButton.onClick.AddListener(() => SelectQualityPreset(QualityPreset.Low));
            if (qualityMediumButton != null)
                qualityMediumButton.onClick.AddListener(() => SelectQualityPreset(QualityPreset.Medium));
            if (qualityHighButton != null)
                qualityHighButton.onClick.AddListener(() => SelectQualityPreset(QualityPreset.High));
            RefreshQualityControls(GraphicsQualityService.CurrentPreset);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (settingsScrim != null)
            {
                settingsScrimRect = settingsScrim.rectTransform;
                if (settingsScrim.material != null)
                {
                    settingsScrimInstance = new Material(settingsScrim.material);
                    settingsScrim.material = settingsScrimInstance;
                }
            }
            if (introFadeOverlay != null)
            {
                introFadeAlpha = 0.85f;
                introFadeOverlay.gameObject.SetActive(true);
                Color c = introFadeOverlay.color;
                c.a = introFadeAlpha;
                introFadeOverlay.color = c;
            }
        }

        private void StartPrimary()
        {
            if (kind != ShellKind.MainMenu)
            {
                flow.Load(GameScene.MainMenu);
                return;
            }

            if (flow.Load(GameScene.Chapter1)) ChapterOneCorridorShift.ShowCrashMemoryOnStart = true;
        }

        private void OnEnable()
        {
            flow.StateChanged += RefreshState;
            GraphicsQualityService.PresetChanged += RefreshQualityControls;
            RefreshQualityControls(GraphicsQualityService.CurrentPreset);
            RefreshState();
        }

        private void Start() => Focus(primaryButton);

        private void OnDisable()
        {
            if (flow != null) flow.StateChanged -= RefreshState;
            GraphicsQualityService.PresetChanged -= RefreshQualityControls;
        }

        private void OnDestroy()
        {
            if (settingsScrimInstance != null) Destroy(settingsScrimInstance);
        }

        private void Update()
        {
            UpdateTransitions();

            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame || flow.IsLoading)
                return;
            if (settingsPanel != null && settingsPanel.activeSelf) ShowSettings(false);
            else if (kind != ShellKind.MainMenu) flow.Load(GameScene.MainMenu);
        }

        private void UpdateTransitions()
        {
            if (introFadeOverlay != null && introFadeAlpha > 0f)
            {
                introFadeAlpha = Mathf.MoveTowards(introFadeAlpha, 0f, Time.unscaledDeltaTime * 1.65f);
                Color c = introFadeOverlay.color;
                c.a = introFadeAlpha * introFadeAlpha;
                introFadeOverlay.color = c;
                if (introFadeAlpha <= 0.001f)
                {
                    introFadeAlpha = 0f;
                    introFadeOverlay.gameObject.SetActive(false);
                }
            }

            if (settingsCanvasGroup != null && settingsPanel != null && settingsPanel.activeSelf && settingsFadeAlpha < 1f)
            {
                settingsFadeAlpha = Mathf.MoveTowards(settingsFadeAlpha, 1f, Time.unscaledDeltaTime * 6.5f);
                settingsCanvasGroup.alpha = 1f - (1f - settingsFadeAlpha) * (1f - settingsFadeAlpha);
            }

            if (settingsScrimInstance != null && settingsPanel != null && settingsPanel.activeSelf)
            {
                settingsScrimInstance.SetFloat("_UnscaledTime", Time.unscaledTime);
                if (settingsScrimRect != null && settingsScrimRect.rect.height > 1f)
                    settingsScrimInstance.SetFloat("_Aspect", settingsScrimRect.rect.width / settingsScrimRect.rect.height);
            }
        }

        private void RefreshState()
        {
            bool settingsVisible = settingsPanel != null && settingsPanel.activeSelf;
            bool menuEnabled = !flow.IsLoading && !settingsVisible;
            if (controls != null)
            {
                controls.alpha = settingsVisible ? 0f : 1f;
                controls.interactable = menuEnabled;
                controls.blocksRaycasts = menuEnabled;
            }
            SetMenuInteraction(menuEnabled);
            if (statusLabel != null) statusLabel.text = flow.Status;
        }

        private void ShowSettings(bool visible)
        {
            if (settingsPanel == null) return;
            settingsPanel.SetActive(visible);
            if (visible && masterVolume != null)
            {
                masterVolume.SetValueWithoutNotify(AudioListener.volume);
                UpdateVolumeLabel(AudioListener.volume);
            }
            if (settingsCanvasGroup != null)
            {
                settingsFadeAlpha = visible ? 0.12f : 0f;
                settingsCanvasGroup.alpha = visible ? settingsFadeAlpha : 0f;
            }
            MenuUiAudio.PlayPanelTransition();
            RefreshState();
            Focus(visible ? (Selectable)masterVolume : settingsButton);
            if (!visible) PlayerPrefs.Save();
        }

        private void SetMenuInteraction(bool enabled)
        {
            foreach (Button button in new[] { primaryButton, settingsButton, creditsButton, quitButton, menuButton, playTestButton })
                if (button != null) button.interactable = enabled;
        }

        private void SetVolume(float value)
        {
            AudioListener.volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("NRA.MasterVolume", AudioListener.volume);
            UpdateVolumeLabel(AudioListener.volume);
        }

        private void UpdateVolumeLabel(float value)
        {
            if (volumeValue != null) volumeValue.text = Mathf.RoundToInt(value * 100f) + "%";
        }

        public void SelectQualityPreset(QualityPreset preset)
        {
            GraphicsQualityService.ApplyPreset(preset, savePreference: true);
            MenuUiAudio.PlayConfirm();
        }

        private void RefreshQualityControls(QualityPreset activePreset)
        {
            if (qualityPresetLabel != null)
                qualityPresetLabel.text = "Kualitas Grafis  —  " + GraphicsQualityService.GetDisplayLabel(activePreset);
            StyleMenuQualityButton(qualityLowButton, activePreset == QualityPreset.Low);
            StyleMenuQualityButton(qualityMediumButton, activePreset == QualityPreset.Medium);
            StyleMenuQualityButton(qualityHighButton, activePreset == QualityPreset.High);
        }

        private static void StyleMenuQualityButton(Button button, bool selected)
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

        private static void Focus(Selectable target)
        {
            if (target != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(target.gameObject);
        }

        private void Quit()
        {
            PlayerPrefs.Save();
#if UNITY_EDITOR
            if (statusLabel != null)
                statusLabel.text = "Tombol Keluar akan menutup aplikasi pada build Windows.";
#else
            Application.Quit();
#endif
        }
    }
}
