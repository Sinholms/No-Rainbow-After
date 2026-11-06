using NoRainbowAfter.Core;
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
        private SceneFlow flow;

        private void Awake()
        {
            flow = SceneFlow.EnsureInstance();
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
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        private void OnEnable()
        {
            flow.StateChanged += RefreshState;
            RefreshState();
        }

        private void Start() => Focus(primaryButton);

        private void OnDisable()
        {
            if (flow != null) flow.StateChanged -= RefreshState;
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
