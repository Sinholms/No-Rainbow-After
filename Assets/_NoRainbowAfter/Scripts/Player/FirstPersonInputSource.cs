using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NoRainbowAfter.Player
{
    // Membaca action Input System sebagai data per frame atau event.
    public sealed class FirstPersonInputSource : IDisposable
    {
        private readonly InputActionAsset ownedAsset;
        private readonly InputActionMap gameplay;
        private readonly InputActionMap global;
        private readonly InputAction move, look, sprint, crouch, pause, closeInspect, breathe, interact, fight;
        public event Action PauseRequested;
        public event Action CloseInspectRequested;
        public event Action BreatheRequested;
        public event Action InteractRequested;
        public event Action FightRequested;
        public bool GameplayEnabled => gameplay.enabled;

        public FirstPersonInputSource(InputActionAsset asset)
        {
            if (asset == null) throw new ArgumentNullException(nameof(asset));
            ownedAsset = UnityEngine.Object.Instantiate(asset);
            gameplay = ownedAsset.FindActionMap("Player", true);
            global = ownedAsset.FindActionMap("Global", true);
            move = gameplay.FindAction("Move", true);
            look = gameplay.FindAction("Look", true);
            sprint = gameplay.FindAction("Sprint", true);
            crouch = gameplay.FindAction("Crouch", true);
            interact = gameplay.FindAction("Interact") ?? gameplay.AddAction("Interact", InputActionType.Button, "<Keyboard>/e");
            interact.performed += OnInteract;
            fight = gameplay.FindAction("Fight") ?? gameplay.AddAction("Fight", InputActionType.Button, "<Mouse>/leftButton");
            fight.performed += OnFight;
            pause = global.FindAction("Pause", true);
            pause.performed += OnPause;
            closeInspect = global.FindAction("CloseInspect") ?? global.AddAction("CloseInspect", InputActionType.Button, "<Keyboard>/f");
            closeInspect.performed += OnCloseInspect;
            breathe = global.FindAction("Breathe") ?? global.AddAction("Breathe", InputActionType.Button, "<Keyboard>/tab");
            breathe.performed += OnBreathe;
        }

        public void Enable() { global.Enable(); gameplay.Enable(); }
        public void Disable() { gameplay.Disable(); global.Disable(); }
        public void SetGameplayEnabled(bool enabled)
        {
            if (enabled) gameplay.Enable(); else gameplay.Disable();
        }

        public FirstPersonFrame ReadFrame() => !gameplay.enabled ? default : new FirstPersonFrame
        {
            Move = move.ReadValue<Vector2>(),
            Look = look.ReadValue<Vector2>(),
            SprintHeld = sprint.IsPressed(),
            CrouchPressed = crouch.WasPressedThisFrame()
        };

        private void OnInteract(InputAction.CallbackContext context) => InteractRequested?.Invoke();
        private void OnFight(InputAction.CallbackContext context) => FightRequested?.Invoke();
        private void OnPause(InputAction.CallbackContext context) => PauseRequested?.Invoke();
        private void OnCloseInspect(InputAction.CallbackContext context) => CloseInspectRequested?.Invoke();
        private void OnBreathe(InputAction.CallbackContext context) => BreatheRequested?.Invoke();

        public void Dispose()
        {
            pause.performed -= OnPause;
            closeInspect.performed -= OnCloseInspect;
            breathe.performed -= OnBreathe;
            interact.performed -= OnInteract;
            fight.performed -= OnFight;
            ownedAsset.Disable();
            if (Application.isPlaying) UnityEngine.Object.Destroy(ownedAsset);
            else UnityEngine.Object.DestroyImmediate(ownedAsset);
        }

        public static InputActionAsset CreateDefaultAsset()
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "NRA_FirstPersonActions";
            var player = asset.AddActionMap("Player");
            player.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2")
                .AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            player.AddAction("Look", InputActionType.Value, "<Mouse>/delta", expectedControlLayout: "Vector2");
            player.AddAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
            player.AddAction("Crouch", InputActionType.Button, "<Keyboard>/leftCtrl");
            player.AddAction("Interact", InputActionType.Button, "<Keyboard>/e");
            player.AddAction("Fight", InputActionType.Button, "<Mouse>/leftButton");
            var globalMap = asset.AddActionMap("Global");
            globalMap.AddAction("Pause", InputActionType.Button, "<Keyboard>/escape");
            globalMap.AddAction("CloseInspect", InputActionType.Button, "<Keyboard>/f");
            globalMap.AddAction("Breathe", InputActionType.Button, "<Keyboard>/tab");
            return asset;
        }
    }
}
