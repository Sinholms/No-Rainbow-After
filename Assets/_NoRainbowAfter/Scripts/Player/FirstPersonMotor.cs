using UnityEngine;

namespace NoRainbowAfter.Player
{
    // Paket input yang dibaca sekali per frame oleh pengendali pemain.
    public struct FirstPersonFrame
    {
        public Vector2 Move;
        public Vector2 Look;
        public bool SprintHeld;
        public bool CrouchPressed;
    }

    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonMotor : MonoBehaviour
    {
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

        private readonly Collider[] overlaps = new Collider[24];
        private CharacterController controller;
        private StressBarometer stressBarometer;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private float pitch;
        private float breathingPitchOffset;
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
        public float CrouchBlend => controller != null
            ? 1f - Mathf.InverseLerp(crouchingHeight, standingHeight, controller.height)
            : (IsCrouched ? 1f : 0f);

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

        public void SetViewSettings(float sensitivity, float fov)
        {
            mouseSensitivity = Mathf.Clamp(sensitivity, 0.01f, 0.3f);
            fieldOfView = Mathf.Clamp(fov, 65f, 95f);
            if (viewCamera != null) viewCamera.fieldOfView = fieldOfView;
        }

        /// <summary>The input driver calls once per frame. Mouse delta is deliberately not multiplied by dt.</summary>
        public void Simulate(FirstPersonFrame input, float deltaTime)
        {
            if (!enabled || !controller.enabled || deltaTime <= 0f) return;
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
                UpdateStressCamera(stress, 0f, dt);
                ApplyViewRotation();
                UpdateEyeHeight();
                return;
            }

            // Jongkok adalah toggle; halangan di atas dapat mencegah pemain berdiri.
            if (input.CrouchPressed) crouchRequested = !crouchRequested;
            bool alreadyLow = controller.height < standingHeight - 0.001f;
            IsCrouched = crouchRequested || (alreadyLow && !CanStand());
            float targetHeight = IsCrouched ? crouchingHeight : standingHeight;
            SetHeight(Mathf.MoveTowards(controller.height, targetHeight, stanceSpeed * dt));

            if (exhausted && (StaminaSeconds >= sprintCapacity - 0.001f ||
                (!input.SprintHeld && StaminaNormalized >= 0.25f))) exhausted = false;
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

            // Stres mengurangi percepatan saat pemain mulai melangkah.
            float accelScale = move.sqrMagnitude > .01f ? MovementScale * Mathf.Lerp(1f, 0.70f, StressGaitIntensity) : 1f;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, target, acceleration * accelScale * dt);
            if (controller.isGrounded && verticalVelocity < 0) verticalVelocity = -2f;
            verticalVelocity += gravity * dt;
            Vector3 before = transform.position;
            CollisionFlags flags = controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * dt);
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0) verticalVelocity = 0;
            if ((flags & CollisionFlags.Below) != 0 && verticalVelocity < 0) verticalVelocity = -2f;

            // Stamina sprint berkurang ketika pemain benar-benar berpindah.
            Vector3 travelled = transform.position - before;
            travelled.y = 0;
            float actualSpeed = travelled.magnitude / dt;
            IsSprinting = sprint && travelled.sqrMagnitude > 0.000001f;
            if (IsSprinting)
            {
                float drainScale = Mathf.Lerp(1f, 1.35f, StressGaitIntensity);
                StaminaSeconds = Mathf.Max(0, StaminaSeconds - dt * drainScale);
                sinceSprint = 0;
                if (StaminaSeconds <= 0.001f) { StaminaSeconds = 0; exhausted = true; }
            }
            else
            {
                sinceSprint += dt;
                if (sinceSprint >= rechargeDelay)
                    StaminaSeconds = Mathf.Min(sprintCapacity, StaminaSeconds + sprintCapacity / rechargeDuration * dt);
            }

            UpdateStressCamera(stress, actualSpeed, dt);
            ApplyViewRotation();
            UpdateEyeHeight();
        }

        private float ReadCurrentStress()
        {
            if (stressBarometer == null) stressBarometer = GetComponent<StressBarometer>();
            return stressBarometer != null ? stressBarometer.CurrentStress : 0f;
        }

        private void UpdateStressCamera(float stress, float actualSpeed, float dt)
        {
            if (StressGaitIntensity <= 0.0001f)
            {
                gaitBlend = 0f;
                StressRollOffset = 0f;
                StressPitchOffset = 0f;
                StressEyeOffset = Vector3.zero;
                if (viewCamera != null && !Mathf.Approximately(viewCamera.fieldOfView, fieldOfView))
                    viewCamera.fieldOfView = fieldOfView;
                return;
            }

            float refSpeed = Mathf.Max(0.4f, walkSpeed * MovementScale * 0.85f);
            float moveActivity = Mathf.Clamp01(actualSpeed / refSpeed);
            gaitBlend = Mathf.MoveTowards(gaitBlend, moveActivity, (moveActivity > gaitBlend ? 4.5f : 3.2f) * dt);

            if (moveActivity > 0.05f)
            {
                float cadence = IsSprinting ? 8.8f : (IsCrouched ? 4.6f : 5.6f);
                gaitPhase += dt * cadence * Mathf.Lerp(1f, 0.84f, StressGaitIntensity);
            }
            idleSwayClock += dt;

            float idleIntensity = stress > StressLimpOnset
                ? Mathf.InverseLerp(StressLimpOnset, StressBarometer.MaxStress, stress)
                : 0f;
            float idleRoll = Mathf.Sin(idleSwayClock * 0.95f) * 0.85f * idleIntensity;
            float idlePitch = Mathf.Sin(idleSwayClock * 0.65f + 0.8f) * 0.55f * idleIntensity;

            float walkWeight = StressGaitIntensity * gaitBlend * (IsCrouched ? 0.58f : 1f);
            // Asymmetric heavy sway + downward head-droop so walking feels exhausted and unsteady.
            float asymWave = Mathf.Sin(gaitPhase) + 0.32f * Mathf.Sin(2f * gaitPhase + 0.4f);
            float stepDip = Mathf.Abs(Mathf.Sin(gaitPhase));

            float targetRoll = asymWave * 3.1f * walkWeight + idleRoll;
            float targetPitch = 1.25f * StressGaitIntensity * gaitBlend + stepDip * 1.85f * walkWeight + idlePitch;
            Vector3 targetEyeOffset = new Vector3(
                Mathf.Sin(gaitPhase) * 0.032f * walkWeight,
                -stepDip * 0.044f * walkWeight - 0.014f * StressGaitIntensity * gaitBlend,
                0f);

            float smooth = 1f - Mathf.Exp(-10f * dt);
            StressRollOffset = Mathf.Lerp(StressRollOffset, targetRoll, smooth);
            StressPitchOffset = Mathf.Lerp(StressPitchOffset, targetPitch, smooth);
            StressEyeOffset = Vector3.Lerp(StressEyeOffset, targetEyeOffset, smooth);

            if (viewCamera != null)
            {
                float tunnel = stress > StressTunnelOnset
                    ? Mathf.InverseLerp(StressTunnelOnset, StressBarometer.MaxStress, stress)
                    : 0f;
                float targetFov = tunnel > 0.001f
                    ? Mathf.Clamp(fieldOfView - 4.2f * tunnel + Mathf.Sin(idleSwayClock * 1.4f) * 0.65f * tunnel, 60f, 95f)
                    : fieldOfView;
                viewCamera.fieldOfView = Mathf.Lerp(viewCamera.fieldOfView, targetFov, smooth);
            }
        }

        private void ClearStressCameraOffsets()
        {
            gaitBlend = 0f;
            StressRollOffset = 0f;
            StressPitchOffset = 0f;
            StressEyeOffset = Vector3.zero;
            if (viewCamera != null) viewCamera.fieldOfView = fieldOfView;
            ApplyViewRotation();
            if (controller != null && viewCamera != null) UpdateEyeHeight();
        }

        public bool CanStand()
        {
            float radius = Mathf.Max(0.05f, controller.radius - controller.skinWidth - 0.01f);
            Vector3 feet = transform.position;
            // Only check the additional torso/head volume; floor and stair risers are already occupied safely.
            int count = Physics.OverlapCapsuleNonAlloc(feet + Vector3.up * Mathf.Max(radius + 0.025f, crouchingHeight - radius),
                feet + Vector3.up * (standingHeight - radius), radius, overlaps, obstructionMask,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (overlaps[i] != controller && !overlaps[i].transform.IsChildOf(transform)) return false;
            return count < overlaps.Length;
        }

        public bool TryRestorePose(Vector3 position, Quaternion rotation, bool notify = true)
        {
            if (controller == null || !controller.enabled || viewCamera == null) return false;
            float radius = Mathf.Max(0.05f, controller.radius - controller.skinWidth - 0.01f);
            int count = Physics.OverlapCapsuleNonAlloc(position + Vector3.up * (radius + 0.05f),
                position + Vector3.up * (standingHeight - radius), radius, overlaps, obstructionMask,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (overlaps[i] != null && overlaps[i] != controller && !overlaps[i].transform.IsChildOf(transform)) return false;
            if (count >= overlaps.Length) return false;
            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            horizontalVelocity = Vector3.zero; verticalVelocity = 0f; pitch = 0f; breathingPitchOffset = 0f;
            gaitPhase = 0f; idleSwayClock = 0f; gaitBlend = 0f;
            StressGaitIntensity = 0f; StressSpeedMultiplier = 1f;
            StressRollOffset = 0f; StressPitchOffset = 0f; StressEyeOffset = Vector3.zero;
            crouchRequested = false; IsCrouched = recoveryCrouch; IsSprinting = false;
            viewCamera.fieldOfView = fieldOfView;
            viewCamera.transform.localRotation = Quaternion.identity;
            SetHeight(recoveryCrouch ? crouchingHeight : standingHeight);
            controller.enabled = true;
            if (notify) PoseRestored?.Invoke();
            return true;
        }

        public void SetBreathingPitchOffset(float pitchOffsetDegrees)
        {
            breathingPitchOffset = Mathf.Clamp(pitchOffsetDegrees, -15f, 15f);
            ApplyViewRotation();
        }

        private void ApplyViewRotation()
        {
            if (viewCamera != null)
            {
                float combinedPitch = Mathf.Clamp(pitch + breathingPitchOffset + StressPitchOffset, -85f, 85f);
                viewCamera.transform.localRotation = Quaternion.Euler(combinedPitch, 0f, StressRollOffset);
            }
        }

        public void StopMotion()
        {
            horizontalVelocity = Vector3.zero;
            IsSprinting = false;
            ClearStressCameraOffsets();
        }

        public void SetRecoveryCrouch(bool required)
        {
            if (recoveryCrouch == required) return;
            recoveryCrouch = required;
            if (!required) return;

            StopMotion();
            verticalVelocity = 0f;
            IsCrouched = true;
            if (controller != null && viewCamera != null) SetHeight(crouchingHeight);
        }

        private void SetHeight(float height)
        {
            controller.height = height;
            controller.center = Vector3.up * (height * 0.5f);
            UpdateEyeHeight();
        }

        private void UpdateEyeHeight()
        {
            float blend = Mathf.InverseLerp(crouchingHeight, standingHeight, controller.height);
            viewCamera.transform.localPosition = Vector3.up * Mathf.Lerp(crouchingEyeHeight, standingEyeHeight, blend) + StressEyeOffset;
        }
    }
}
