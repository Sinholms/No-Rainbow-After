using NoRainbowAfter.Core;
using NoRainbowAfter.Level;
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
        private bool circularViewEnabled;
        private float circularViewRadius;
        public bool IsPaused { get; private set; }

        private void Awake()
        {
            SceneFlow.EnsureInstance();
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
            UpdateLabels();
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

        private void TogglePause()
        {
            var inspection = player.GetComponent<NoRainbowAfter.Interaction.PlayerInteractor>();
            if (inspection != null && inspection.CloseInspection()) return;
            SetPaused(!IsPaused);
        }
        private void PauseForFocusLoss()
        {
            var inspection = player.GetComponent<NoRainbowAfter.Interaction.PlayerInteractor>();
            if (inspection != null) inspection.CloseInspection();
            SetPaused(true);
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0 : 1;
            player.SetPaused(paused);
            pausePanel.SetActive(paused);
            gameplayHud.SetActive(!paused);
            ApplyCircularView();
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
            if (viewRadiusLabel != null)
                viewRadiusLabel.text = "Lebar pandang  " + Mathf.RoundToInt(circularViewRadius * 100f) + "%";
        }

        private void OnCircularViewChanged(bool enabled)
        {
            circularViewEnabled = enabled;
            PlayerPrefs.SetInt("NRA.CircularViewEnabled", enabled ? 1 : 0);
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
                if (label != null) label.text = "Kembali ke titik awal";
            }
            else if (label != null) label.text = "Titik awal terhalang";
        }
    }
}
