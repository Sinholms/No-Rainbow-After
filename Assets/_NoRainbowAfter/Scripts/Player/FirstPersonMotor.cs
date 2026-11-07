using UnityEngine;

namespace NoRainbowAfter.Player
{
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
        [SerializeField] private float standingEyeHeight = 1.65f;
        [SerializeField] private float crouchingEyeHeight = 0.95f;
        [Header("Movement in metres per second")]
        [SerializeField, Min(0.1f)] private float walkSpeed = 2.4f;
        [SerializeField, Min(0.1f)] private float sprintSpeed = 4.8f;
        [SerializeField, Min(0.1f)] private float crouchSpeed = 1.2f;
        [SerializeField, Min(0.1f)] private float acceleration = 16f;
        [SerializeField] private float gravity = -20f;
        [Header("Stance")]
        [SerializeField] private float standingHeight = 1.8f;
        [SerializeField] private float crouchingHeight = 1.1f;
        [SerializeField] private float stanceSpeed = 3.5f;
        [SerializeField] private LayerMask obstructionMask = ~0;
        [Header("Sprint, seconds")]
        [SerializeField, Min(0.1f)] private float sprintCapacity = 5f;
        [SerializeField, Min(0.1f)] private float rechargeDuration = 4f;
        [SerializeField, Min(0)] private float rechargeDelay = 1f;

        private readonly Collider[] overlaps = new Collider[24];
        private CharacterController controller;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private float pitch;
        private float sinceSprint;
        private bool crouchRequested;
        private bool exhausted;

        public event System.Action PoseRestored;
        public Camera ViewCamera => viewCamera;
        public float FieldOfView => fieldOfView;
        public float MouseSensitivity => mouseSensitivity;
        public float StaminaSeconds { get; private set; }
        public float StaminaNormalized => StaminaSeconds / sprintCapacity;
        public bool IsCrouched { get; private set; }
        public bool IsSprinting { get; private set; }
        public float Pitch => pitch;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
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
            transform.Rotate(0f, input.Look.x * mouseSensitivity, 0f, Space.Self);
            pitch = Mathf.Clamp(pitch - input.Look.y * mouseSensitivity, -85f, 85f);
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);

            if (input.CrouchPressed) crouchRequested = !crouchRequested;
            bool alreadyLow = controller.height < standingHeight - 0.001f;
            IsCrouched = crouchRequested || (alreadyLow && !CanStand());
            float targetHeight = IsCrouched ? crouchingHeight : standingHeight;
            controller.height = Mathf.MoveTowards(controller.height, targetHeight, stanceSpeed * dt);
            controller.center = Vector3.up * (controller.height * 0.5f);
            UpdateEyeHeight();

            if (exhausted && (StaminaSeconds >= sprintCapacity - 0.001f ||
                (!input.SprintHeld && StaminaNormalized >= 0.25f))) exhausted = false;
            Vector2 move = Vector2.ClampMagnitude(input.Move, 1f);
            bool sprint = input.SprintHeld && move.sqrMagnitude > 0.01f && !IsCrouched &&
                controller.height >= standingHeight - 0.02f && !exhausted && StaminaSeconds > 0f;
            float speed = IsCrouched ? crouchSpeed : sprint ? sprintSpeed : walkSpeed;
            Vector3 target = transform.TransformDirection(new Vector3(move.x, 0, move.y)) * speed;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, target, acceleration * dt);
            if (controller.isGrounded && verticalVelocity < 0) verticalVelocity = -2f;
            verticalVelocity += gravity * dt;
            Vector3 before = transform.position;
            CollisionFlags flags = controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * dt);
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0) verticalVelocity = 0;
            if ((flags & CollisionFlags.Below) != 0 && verticalVelocity < 0) verticalVelocity = -2f;

            Vector3 travelled = transform.position - before;
            travelled.y = 0;
            IsSprinting = sprint && travelled.sqrMagnitude > 0.000001f;
            if (IsSprinting)
            {
                StaminaSeconds = Mathf.Max(0, StaminaSeconds - dt);
                sinceSprint = 0;
                if (StaminaSeconds <= 0.001f) { StaminaSeconds = 0; exhausted = true; }
            }
            else
            {
                sinceSprint += dt;
                if (sinceSprint >= rechargeDelay)
                    StaminaSeconds = Mathf.Min(sprintCapacity, StaminaSeconds + sprintCapacity / rechargeDuration * dt);
            }
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

        public bool TryRestorePose(Vector3 position, Quaternion rotation)
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
            controller.height = standingHeight;
            controller.center = Vector3.up * (standingHeight * 0.5f);
            controller.enabled = true;
            horizontalVelocity = Vector3.zero; verticalVelocity = 0f; pitch = 0f;
            crouchRequested = false; IsCrouched = false; IsSprinting = false;
            viewCamera.transform.localRotation = Quaternion.identity;
            UpdateEyeHeight();
            PoseRestored?.Invoke();
            return true;
        }

        public void StopMotion()
        {
            horizontalVelocity = Vector3.zero;
            IsSprinting = false;
        }

        private void UpdateEyeHeight()
        {
            float blend = Mathf.InverseLerp(crouchingHeight, standingHeight, controller.height);
            // No headbob, roll, or third-person offset.
            viewCamera.transform.localPosition = Vector3.up * Mathf.Lerp(crouchingEyeHeight, standingEyeHeight, blend);
        }
    }
}
