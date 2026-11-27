using UnityEngine;

namespace NoRainbowAfter.Player
{
    /// <summary>Paket input yang dibaca sekali per frame oleh pengendali pemain.</summary>
    public struct FirstPersonFrame
    {
        public Vector2 Move;
        public Vector2 Look;
        public bool SprintHeld;
        public bool CrouchPressed;
    }

    /// <summary>
    /// Motor pergerakan orang pertama (CharacterController) dengan dukungan jalan, sprint ber-stamina,
    /// jongkok mulus beserta cek ruang kepala (CanStand), serta modulasi stres/napas pada kamera.
    ///
    /// [TEKNIS KODING]
    /// Dipecah menjadi 2 file partial class:
    /// 1. FirstPersonMotor.cs (fisika gerak, stamina, jongkok, &amp; TryRestorePose anti-nyangkut)
    /// 2. FirstPersonMotor.CameraEffects.cs (FOV, ayunan kamera stres, tinggi mata, efek napas)
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed partial class FirstPersonMotor : MonoBehaviour
    {
        // =========================================================================
        // Pengaturan Inspector & Konstanta Ambang Stres
        // =========================================================================

        [Header("First person view")]
        [SerializeField] private Camera viewCamera;
        [SerializeField, Range(65, 95)] private float fieldOfView = 75f;
        [SerializeField, Range(0.01f, 0.3f)] private float mouseSensitivity = 0.08f;
        [SerializeField] private float standingEyeHeight = 1.58f;
        [SerializeField] private float crouchingEyeHeight = 0.95f;

        [Header("Movement in metres per second")]
        [SerializeField, Min(0.1f)] private float walkSpeed = 2.4f;
        [SerializeField, Min(0.1f)] private float sprintSpeed = 3.4f;
        [SerializeField, Min(0.1f)] private float crouchSpeed = 1.2f;
        [SerializeField, Min(0.1f)] private float acceleration = 16f;
        [SerializeField] private float gravity = -20f;

        [Header("Stance")]
        [SerializeField] private float standingHeight = 1.7f;
        [SerializeField] private float crouchingHeight = 1.1f;
        [SerializeField] private float stanceSpeed = 3.5f;
        [SerializeField] private LayerMask obstructionMask = ~0;

        [Header("Sprint, seconds")]
        [SerializeField, Min(0.1f)] private float sprintCapacity = 5f;
        [SerializeField, Min(0.1f)] private float rechargeDuration = 4f;
        [SerializeField, Min(0)] private float rechargeDelay = 1f;

        private const float StressSwayOnset = 35f;
        private const float StressSlowOnset = 55f;
        private const float StressTunnelOnset = 65f;
        private const float StressLimpOnset = 70f;

        // =========================================================================
        // State Gerak, Kamera, & Properti Publik
        // =========================================================================

        private readonly Collider[] overlaps = new Collider[24];
        private CharacterController controller;
        private StressBarometer stressBarometer;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private float pitch;
        private float breathingPitchOffset;
        private float breathingFovOffset;
        private float breathingEyeOffsetY;
        private Vector2 breathingShakeOffset;
        private float sinceSprint;
        private float gaitPhase;
        private float idleSwayClock;
        private float gaitBlend;
        private bool crouchRequested;
        private bool recoveryCrouch;
        private bool exhausted;

        public event System.Action PoseRestored;

        public Camera ViewCamera => viewCamera;
        public float MovementScale { get; set; } = 1f;
        public float FieldOfView => fieldOfView;
        public float MouseSensitivity => mouseSensitivity;
        public float StaminaSeconds { get; private set; }
        public float StaminaNormalized => StaminaSeconds / sprintCapacity;
        public bool IsCrouched { get; private set; }
        public bool IsSprinting { get; private set; }
        public float Pitch => pitch;
        public float BreathingPitchOffset => breathingPitchOffset;
        public float StressGaitIntensity { get; private set; }
        public float StressSpeedMultiplier { get; private set; } = 1f;
        public float StressRollOffset { get; private set; }
        public float StressPitchOffset { get; private set; }
        public Vector3 StressEyeOffset { get; private set; }

        // Sorot polisi Chapter 2 memakai nilai ini untuk menurunkan titik uji kepala & dada
        // secara bertahap saat kapsul pemain berubah dari berdiri (0) ke jongkok (1).
        public float CrouchBlend => controller != null
            ? 1f - Mathf.InverseLerp(crouchingHeight, standingHeight, controller.height)
            : (IsCrouched ? 1f : 0f);

        // =========================================================================
        // Inisialisasi, Simulasi Gerak Per Frame, & Pemulihan Pose Aman
        // =========================================================================

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            stressBarometer = GetComponent<StressBarometer>();
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
            if (viewCamera == null)
            {
                Debug.LogError("[NRA] FirstPersonMotor requires a child first-person Camera.", this);
                enabled = false;
                return;
            }

            controller.height = standingHeight;
            controller.center = Vector3.up * (standingHeight * 0.5f);
            StaminaSeconds = sprintCapacity;
            sinceSprint = rechargeDelay;
            SetViewSettings(mouseSensitivity, fieldOfView);
            UpdateEyeHeight();
        }

        public void Simulate(FirstPersonFrame input, float deltaTime)
        {
            if (!enabled || !controller.enabled || deltaTime <= 0f)
            {
                return;
            }
            float dt = Mathf.Min(deltaTime, 0.1f);
            float stress = ReadCurrentStress();

            StressGaitIntensity = stress > StressSwayOnset
                ? Mathf.Pow(Mathf.InverseLerp(StressSwayOnset, StressBarometer.MaxStress, stress), 1.15f)
                : 0f;
            StressSpeedMultiplier = stress > StressSlowOnset
                ? Mathf.Lerp(1f, 0.88f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(StressSlowOnset, StressBarometer.MaxStress, stress)))
                : 1f;

            transform.Rotate(0f, input.Look.x * mouseSensitivity, 0f, Space.Self);
            pitch = Mathf.Clamp(pitch - input.Look.y * mouseSensitivity, -85f, 85f);

            if (recoveryCrouch)
            {
                // Ketika pemulihan napas diwajibkan, Raka tetap bisa menoleh sambil diam di tempat.
                UpdateStressCamera(stress, 0f, dt);
                ApplyViewRotation();
                UpdateEyeHeight();
                return;
            }

            // Jongkok adalah toggle; halangan di atas kepala dapat mencegah pemain berdiri.
            if (input.CrouchPressed) crouchRequested = !crouchRequested;
            bool alreadyLow = controller.height < standingHeight - 0.001f;
            IsCrouched = crouchRequested || (alreadyLow && !CanStand());
            float targetHeight = IsCrouched ? crouchingHeight : standingHeight;
            SetHeight(Mathf.MoveTowards(controller.height, targetHeight, stanceSpeed * dt));

            if (exhausted && (StaminaSeconds >= sprintCapacity - 0.001f ||
                (!input.SprintHeld && StaminaNormalized >= 0.25f)))
            {
                exhausted = false;
            }

            Vector2 move = Vector2.ClampMagnitude(input.Move, 1f);
            bool sprint = input.SprintHeld && move.sqrMagnitude > 0.01f && !IsCrouched &&
                          controller.height >= standingHeight - 0.02f && !exhausted && StaminaSeconds > 0f;
            float speed = IsCrouched ? crouchSpeed : sprint ? sprintSpeed : walkSpeed;

            // Saat stres tinggi, irama langkah memperlambat gerak dan membuat Raka sedikit oleng.
            float limpFactor = stress > StressLimpOnset
                ? Mathf.InverseLerp(StressLimpOnset, StressBarometer.MaxStress, stress)
                : 0f;
            float cadenceDrag = 1f - limpFactor * 0.06f * Mathf.Max(0f, Mathf.Sin(gaitPhase));
            Vector3 moveDir = transform.TransformDirection(new Vector3(move.x, 0, move.y));
            Vector3 target = moveDir * (speed * MovementScale * StressSpeedMultiplier * cadenceDrag);
            if (limpFactor > 0.001f && moveDir.sqrMagnitude > 0.0001f && !IsCrouched)
            {
                Vector3 perp = Vector3.Cross(Vector3.up, moveDir.normalized);
                float weave = Mathf.Sin(gaitPhase * 0.5f) * limpFactor * 0.16f * MovementScale;
                target += perp * weave;
            }

            float accelScale = move.sqrMagnitude > .01f ? MovementScale * Mathf.Lerp(1f, 0.70f, StressGaitIntensity) : 1f;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, target, acceleration * accelScale * dt);

            if (controller.isGrounded && verticalVelocity < 0) verticalVelocity = -2f;
            verticalVelocity += gravity * dt;

            Vector3 before = transform.position;
            CollisionFlags flags = controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * dt);
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0) verticalVelocity = 0;
            if ((flags & CollisionFlags.Below) != 0 && verticalVelocity < 0) verticalVelocity = -2f;

            // Stamina sprint berkurang hanya ketika pemain benar-benar berpindah posisi.
            Vector3 travelled = transform.position - before;
            travelled.y = 0;
            float actualSpeed = travelled.magnitude / dt;
            IsSprinting = sprint && travelled.sqrMagnitude > 0.000001f;
            if (IsSprinting)
            {
                float drainScale = Mathf.Lerp(1f, 1.35f, StressGaitIntensity);
                StaminaSeconds = Mathf.Max(0, StaminaSeconds - dt * drainScale);
                sinceSprint = 0;
                if (StaminaSeconds <= 0.001f)
                {
                    StaminaSeconds = 0;
                    exhausted = true;
                }
            }
            else
            {
                sinceSprint += dt;
                if (sinceSprint >= rechargeDelay)
                {
                    StaminaSeconds = Mathf.Min(sprintCapacity, StaminaSeconds + sprintCapacity / rechargeDuration * dt);
                }
            }

            UpdateStressCamera(stress, actualSpeed, dt);
            ApplyViewRotation();
            UpdateEyeHeight();
        }

        public bool CanStand()
        {
            float radius = Mathf.Max(0.05f, controller.radius - controller.skinWidth - 0.01f);
            Vector3 feet = transform.position;
            int count = Physics.OverlapCapsuleNonAlloc(
                feet + Vector3.up * Mathf.Max(radius + 0.025f, crouchingHeight - radius),
                feet + Vector3.up * (standingHeight - radius),
                radius,
                overlaps,
                obstructionMask,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (overlaps[i] != controller && !overlaps[i].transform.IsChildOf(transform))
                {
                    return false;
                }
            }
            return count < overlaps.Length;
        }

        public bool TryRestorePose(Vector3 position, Quaternion rotation, bool notify = true)
        {
            // Pastikan kapsul muat di tujuan sebelum teleport agar tidak tersangkut collider.
            if (controller == null || !controller.enabled || viewCamera == null)
            {
                return false;
            }
            float radius = Mathf.Max(0.05f, controller.radius - controller.skinWidth - 0.01f);
            int count = Physics.OverlapCapsuleNonAlloc(
                position + Vector3.up * (radius + 0.05f),
                position + Vector3.up * (standingHeight - radius),
                radius,
                overlaps,
                obstructionMask,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (overlaps[i] != null && overlaps[i] != controller && !overlaps[i].transform.IsChildOf(transform))
                {
                    return false;
                }
            }
            if (count >= overlaps.Length)
            {
                return false;
            }

            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            pitch = 0f;
            breathingPitchOffset = 0f;
            breathingFovOffset = 0f;
            breathingEyeOffsetY = 0f;
            breathingShakeOffset = Vector2.zero;
            gaitPhase = 0f;
            idleSwayClock = 0f;
            gaitBlend = 0f;
            StressGaitIntensity = 0f;
            StressSpeedMultiplier = 1f;
            StressRollOffset = 0f;
            StressPitchOffset = 0f;
            StressEyeOffset = Vector3.zero;
            crouchRequested = false;
            IsCrouched = recoveryCrouch;
            IsSprinting = false;

            viewCamera.fieldOfView = fieldOfView;
            viewCamera.transform.localRotation = Quaternion.identity;
            SetHeight(recoveryCrouch ? crouchingHeight : standingHeight);
            controller.enabled = true;

            if (notify)
            {
                PoseRestored?.Invoke();
            }
            return true;
        }

        public void StopMotion()
        {
            horizontalVelocity = Vector3.zero;
            IsSprinting = false;
            ClearStressCameraOffsets();
        }

        public void SetRecoveryCrouch(bool required)
        {
            if (recoveryCrouch == required)
            {
                return;
            }
            recoveryCrouch = required;
            if (!required)
            {
                return;
            }
            StopMotion();
            verticalVelocity = 0f;
            IsCrouched = true;
            if (controller != null && viewCamera != null)
            {
                SetHeight(crouchingHeight);
            }
        }

        private void SetHeight(float height)
        {
            controller.height = height;
            controller.center = Vector3.up * (height * 0.5f);
            UpdateEyeHeight();
        }
    }
}
