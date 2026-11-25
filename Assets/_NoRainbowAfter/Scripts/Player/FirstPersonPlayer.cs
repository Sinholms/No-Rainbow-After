using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NoRainbowAfter.Player
{
    // Menghubungkan input dan motor; status UI menentukan kapan pemain boleh bergerak.
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
        // Lemari memanggil ini setelah Raka berhasil masuk atau keluar. Hentikan sisa gerak
        // supaya kecepatan lari sebelum masuk tidak terbawa ke posisi baru.
        public void SetHiding(bool hiding)
        {
            IsHiding = hiding;
            motor.StopMotion();
        }

        private bool CanReadMovement => !IsPaused && !IsInspecting && !IsUsingLaptop &&
            !IsBreathing && !IsTransitionLocked;
        public void SetBreathRecoveryRequired(bool required)
        {
            IsBreathRecoveryRequired = required;
            if (motor != null) motor.SetRecoveryCrouch(required);
        }
        public event Action InteractRequested;
        public event Action FightRequested;
        public event Action PauseRequested;
        public event Action CloseInspectRequested;
        public event Action BreatheRequested;
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
            input.CloseInspectRequested += () => CloseInspectRequested?.Invoke();
            input.BreatheRequested += () => BreatheRequested?.Invoke();
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
            // Klik untuk menutup menu jeda bisa jatuh pada frame yang sama dengan klik serangan.
            if (!CanReadMovement || IsHiding || IsBreathRecoveryRequired || Time.frameCount == unpausedFrame) return;
            FightRequested?.Invoke();
        }

        private void Update()
        {
            if (!CanReadMovement || input == null) return;
            FirstPersonFrame frame = input.ReadFrame();
            // Di dalam lemari, Raka masih bisa menoleh dan menekan E untuk keluar.
            // Kosongkan input langkah supaya WASD, sprint, dan jongkok tidak menggeser tubuhnya.
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
            if (!focused) FocusLost?.Invoke();
        }

        public void SetInspecting(bool inspecting)
        {
            IsInspecting = inspecting;
            ApplyInputAndCursorState();
        }

        public void SetUsingLaptop(bool usingLaptop)
        {
            if (IsUsingLaptop && !usingLaptop)
                LaptopClosedFrame = Time.frameCount;
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
            if (wasPaused && !paused) unpausedFrame = Time.frameCount;
            ApplyInputAndCursorState();
        }

        // Input global tetap tersedia saat peta gameplay dimatikan untuk mode lain.
        private void ApplyInputAndCursorState()
        {
            input?.SetGameplayEnabled(CanReadMovement);
            motor.StopMotion();
            // Abaikan delta mouse pertama setelah berganti mode supaya kamera tidak meloncat.
            discardNextLook = true;
            bool unlockCursor = IsPaused || IsInspecting || IsUsingLaptop;
            Cursor.lockState = unlockCursor ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = unlockCursor;
        }
    }
}
