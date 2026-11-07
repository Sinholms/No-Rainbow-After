using NoRainbowAfter.Core;
using NoRainbowAfter.Quality;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NoRainbowAfter.UI
{
    public enum ShellKind { MainMenu, Gameplay, Credits }

    /// <summary>T01 scene shell. Gameplay, checkpoint saves and narrative arrive in later tasks.</summary>
    public sealed class ShellScreen : MonoBehaviour
    {
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
        private SceneFlow flow;

        private void Awake()
        {
            flow = SceneFlow.EnsureInstance();
            GraphicsQualityService.ApplySavedPreset();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (continueButton != null) continueButton.interactable = false;
            // No checkpoint is manufactured by this foundation task.
            if (primaryButton != null)
                primaryButton.onClick.AddListener(() => flow.Load(
                    kind == ShellKind.MainMenu ? GameScene.MasterGameplay : GameScene.MainMenu));
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

        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame || flow.IsLoading)
                return;
            if (settingsPanel != null && settingsPanel.activeSelf) ShowSettings(false);
            else if (kind != ShellKind.MainMenu) flow.Load(GameScene.MainMenu);
        }

        private void RefreshState()
        {
            bool settingsVisible = settingsPanel != null && settingsPanel.activeSelf;
            bool menuEnabled = !flow.IsLoading && !settingsVisible;
            controls.alpha = settingsVisible ? 0f : 1f;
            controls.interactable = menuEnabled;
            controls.blocksRaycasts = menuEnabled;
            SetMenuInteraction(menuEnabled);
            statusLabel.text = flow.Status;
        }

        private void ShowSettings(bool visible)
        {
            settingsPanel.SetActive(visible);
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
            {
                bg.color = selected
                    ? new Color(0.24f, 0.34f, 0.36f, 0.90f)
                    : new Color(0.08f, 0.13f, 0.16f, 0.72f);
            }
            var caption = button.GetComponentInChildren<Text>(true);
            if (caption != null)
            {
                caption.color = selected
                    ? new Color(0.95f, 0.92f, 0.82f, 1f)
                    : new Color(0.68f, 0.76f, 0.78f, 1f);
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
            statusLabel.text = "Tombol Keluar akan menutup aplikasi pada build Windows.";
#else
            Application.Quit();
#endif
        }
    }
}
