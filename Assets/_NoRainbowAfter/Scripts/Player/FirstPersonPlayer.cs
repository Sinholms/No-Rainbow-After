using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NoRainbowAfter.Player
{
    /// <summary>
    /// Menghubungkan Input System dengan FirstPersonMotor serta mengatur kunci kursor dan mode
    /// UI (IsPaused, IsInspecting, IsUsingLaptop, IsBreathing, IsHiding, IsTransitionLocked).
    /// </summary>
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
        public bool IsUsingLaptop { get; private set; }
        public bool IsBreathing { get; private set; }
        public bool IsBreathRecoveryRequired { get; private set; }
        public bool IsTransitionLocked { get; private set; }
        public bool IsHiding { get; private set; }
        public int LaptopClosedFrame { get; private set; } = -1;

        public event Action InteractRequested;
        public event Action FightRequested;
        public event Action PauseRequested;
        public event Action CloseInspectRequested;
        public event Action BreatheRequested;
        public event Action FocusLost;

        public void SetHiding(bool hiding)
        {
            IsHiding = hiding;
            motor.StopMotion();
        }

        public void SetBreathRecoveryRequired(bool required)
        {
            IsBreathRecoveryRequired = required;
            if (motor != null)
            {
                motor.SetRecoveryCrouch(required);
            }
        }

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
            input.CloseInspectRequested += () => CloseInspectRequested?.Invoke();
            input.BreatheRequested += () => BreatheRequested?.Invoke();
            input.InteractRequested += () => InteractRequested?.Invoke();
            input.FightRequested += RequestFight;
        }

        private void OnEnable()
        {
            if (input == null)
            {
                return;
            }
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
            if (IsPaused || IsInspecting || IsUsingLaptop || IsBreathing ||
                IsTransitionLocked || IsHiding || IsBreathRecoveryRequired ||
                Time.frameCount == unpausedFrame)
            {
                return;
            }
            FightRequested?.Invoke();
        }

        private void Update()
        {
            if (IsPaused || IsInspecting || IsUsingLaptop || IsBreathing || IsTransitionLocked || input == null)
            {
                return;
            }
            FirstPersonFrame frame = input.ReadFrame();

            // Di dalam lemari, Raka tetap boleh menoleh; gerak & jongkok ditahan sampai ia keluar.
            if (IsHiding)
            {
                frame.Move = Vector2.zero;
                frame.SprintHeld = false;
                frame.CrouchPressed = false;
            }
            if (discardNextLook)
            {
                frame.Look = Vector2.zero;
                discardNextLook = false;
            }
            motor.Simulate(frame, Time.deltaTime);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                FocusLost?.Invoke();
            }
        }

        public void SetInspecting(bool inspecting)
        {
            IsInspecting = inspecting;
            ApplyInputAndCursorState();
        }

        public void SetUsingLaptop(bool usingLaptop)
        {
            if (IsUsingLaptop && !usingLaptop)
            {
                LaptopClosedFrame = Time.frameCount;
            }
            IsUsingLaptop = usingLaptop;
            ApplyInputAndCursorState();
        }

        public void SetBreathing(bool breathing)
        {
            IsBreathing = breathing;
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
            if (wasPaused && !paused)
            {
                unpausedFrame = Time.frameCount;
            }
            ApplyInputAndCursorState();
        }

        private void ApplyInputAndCursorState()
        {
            input?.SetGameplayEnabled(!IsPaused && !IsInspecting && !IsUsingLaptop && !IsBreathing && !IsTransitionLocked);
            motor.StopMotion();

            // Abaikan delta mouse pertama setelah berganti mode supaya kamera tidak meloncat.
            discardNextLook = true;
            bool unlockCursor = IsPaused || IsInspecting || IsUsingLaptop;
            Cursor.lockState = unlockCursor ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = unlockCursor;
        }
    }
}
