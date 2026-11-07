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
        private int unpausedFrame = -1;
        public bool IsPaused { get; private set; }
        public bool IsInspecting { get; private set; }
        public bool IsTransitionLocked { get; private set; }
        public event Action InteractRequested;
        public event Action FightRequested;
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
            input.FightRequested += RequestFight;
        }

        private void OnEnable()
        {
            if (input == null) return;
            input.Enable();
            IsTransitionLocked = false;
            SetPaused(false);
        }

        private void OnDisable()
        {
            IsTransitionLocked = false;
            input?.Disable();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnDestroy() => input?.Dispose();
        private void RequestPause() => PauseRequested?.Invoke();
        private void RequestFight()
        {
            if (IsPaused || IsInspecting || IsTransitionLocked || Time.frameCount == unpausedFrame) return;
            FightRequested?.Invoke();
        }

        private void Update()
        {
            if (IsPaused || IsInspecting || IsTransitionLocked || input == null) return;
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
            ApplyInputAndCursorState();
        }

        public void SetTransitionLocked(bool locked)
        {
            IsTransitionLocked = locked;
            ApplyInputAndCursorState();
        }

        public void SetPaused(bool paused)
        {
            bool wasPaused = IsPaused;
            IsPaused = paused;
            if (wasPaused && !paused) unpausedFrame = Time.frameCount;
            ApplyInputAndCursorState();
        }

        private void ApplyInputAndCursorState()
        {
            input?.SetGameplayEnabled(!IsPaused && !IsInspecting && !IsTransitionLocked);
            motor.StopMotion();
            discardNextLook = true;
            bool unlockCursor = IsPaused || IsInspecting;
            Cursor.lockState = unlockCursor ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = unlockCursor;
        }
    }
}
