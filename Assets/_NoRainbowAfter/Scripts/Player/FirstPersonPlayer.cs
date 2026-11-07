using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NoRainbowAfter.Player
{
    [RequireComponent(typeof(FirstPersonMotor))]
    public sealed class FirstPersonPlayer : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        private FirstPersonInputSource input;
        private FirstPersonMotor motor;
        private bool discardNextLook = true;
        public bool IsPaused { get; private set; }
        public bool IsInspecting { get; private set; }
        public event Action InteractRequested;
        public event Action PauseRequested;
        public event Action FocusLost;

        private void Awake()
        {
            motor = GetComponent<FirstPersonMotor>();
            if (actions == null)
            {
                Debug.LogError("[NRA] FirstPersonPlayer requires an Input System action asset.", this);
                enabled = false;
                return;
            }
            input = new FirstPersonInputSource(actions);
            input.PauseRequested += RequestPause;
            input.InteractRequested += () => InteractRequested?.Invoke();
        }

        private void OnEnable()
        {
            if (input == null) return;
            input.Enable();
            SetPaused(false);
        }

        private void OnDisable()
        {
            input?.Disable();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnDestroy() => input?.Dispose();
        private void RequestPause() => PauseRequested?.Invoke();

        private void Update()
        {
            if (IsPaused || IsInspecting || input == null) return;
            FirstPersonFrame frame = input.ReadFrame();
            if (discardNextLook) { frame.Look = Vector2.zero; discardNextLook = false; }
            motor.Simulate(frame, Time.deltaTime);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) FocusLost?.Invoke();
        }

        public void SetInspecting(bool inspecting)
        {
            IsInspecting = inspecting;
            SetPaused(IsPaused);
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            input?.SetGameplayEnabled(!paused && !IsInspecting);
            motor.StopMotion();
            discardNextLook = true;
            Cursor.lockState = paused || IsInspecting ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = paused || IsInspecting;
        }
    }
}
