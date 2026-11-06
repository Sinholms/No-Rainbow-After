using NoRainbowAfter.Core;
using NoRainbowAfter.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NoRainbowAfter.UI
{
    public sealed class FirstPersonSession : MonoBehaviour
    {
        [SerializeField] private FirstPersonPlayer player;
        [SerializeField] private FirstPersonMotor motor;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Slider sensitivity;
        [SerializeField] private Slider fov;
        [SerializeField] private Text sensitivityLabel;
        [SerializeField] private Text fovLabel;
        [SerializeField] private GameObject staminaRoot;
        [SerializeField] private RectTransform staminaFill;
        [SerializeField] private GameObject gameplayHud;
        public bool IsPaused { get; private set; }

        private void Awake()
        {
            SceneFlow.EnsureInstance();
            motor.SetViewSettings(PlayerPrefs.GetFloat("NRA.MouseSensitivity", motor.MouseSensitivity),
                PlayerPrefs.GetFloat("NRA.FieldOfView", motor.FieldOfView));
            sensitivity.SetValueWithoutNotify(motor.MouseSensitivity);
            fov.SetValueWithoutNotify(motor.FieldOfView);
            UpdateLabels();
            sensitivity.onValueChanged.AddListener(_ => ApplySettings());
            fov.onValueChanged.AddListener(_ => ApplySettings());
            resumeButton.onClick.AddListener(() => SetPaused(false));
            menuButton.onClick.AddListener(ReturnToMenu);
            pausePanel.SetActive(false);
        }

        private void OnEnable()
        {
            player.PauseRequested += TogglePause;
            player.FocusLost += PauseForFocusLoss;
        }

        private void Start() => SetPaused(false);

        private void OnDisable()
        {
            player.PauseRequested -= TogglePause;
            player.FocusLost -= PauseForFocusLoss;
            Time.timeScale = 1;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            PlayerPrefs.Save();
        }

        private void Update()
        {
            staminaRoot.SetActive(!IsPaused && motor.StaminaNormalized < 0.999f);
            staminaFill.anchorMax = new Vector2(motor.StaminaNormalized, 1);
        }

        private void TogglePause() => SetPaused(!IsPaused);
        private void PauseForFocusLoss() => SetPaused(true);

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0 : 1;
            player.SetPaused(paused);
            pausePanel.SetActive(paused);
            gameplayHud.SetActive(!paused);
            if (paused && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
            else PlayerPrefs.Save();
        }

        private void ApplySettings()
        {
            motor.SetViewSettings(sensitivity.value, fov.value);
            PlayerPrefs.SetFloat("NRA.MouseSensitivity", motor.MouseSensitivity);
            PlayerPrefs.SetFloat("NRA.FieldOfView", motor.FieldOfView);
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            sensitivityLabel.text = "Sensitivitas mouse  " + motor.MouseSensitivity.ToString("F2");
            fovLabel.text = "Bidang pandang  " + Mathf.RoundToInt(motor.FieldOfView) + "°";
        }

        private void ReturnToMenu()
        {
            Time.timeScale = 1;
            SceneFlow.EnsureInstance().Load(GameScene.MainMenu);
        }
    }
}
