using UnityEngine;

namespace NoRainbowAfter.Player
{
    /// <summary>
    /// Attaches Raka's MiSide-style headless FPP student body (ACT_Raka_StudentBody) beneath
    /// Player_Raka_FPP and animates limbs procedurally to match movement and crouch state.
    /// </summary>
    [RequireComponent(typeof(FirstPersonMotor))]
    public sealed class FirstPersonBodyVisual : MonoBehaviour
    {
        [SerializeField] private GameObject bodyInstance;
        [SerializeField] private Vector3 bodyLocalOffset = new Vector3(0f, 0f, -0.10f);

        private FirstPersonMotor motor;
        private Transform torsoPivot;
        private Transform leftArmPivot;
        private Transform rightArmPivot;
        private Transform leftLegPivot;
        private Transform rightLegPivot;
        private Transform leftKneePivot;
        private Transform rightKneePivot;

        private Vector3 lastPosition;
        private float walkCycle;

        public GameObject BodyInstance => bodyInstance;

        public void Configure(GameObject instance)
        {
            bodyInstance = instance;
            BindPivots();
        }

        private void Awake()
        {
            motor = GetComponent<FirstPersonMotor>();
            BindPivots();
            lastPosition = transform.position;
        }

        private void OnEnable()
        {
            lastPosition = transform.position;
            BindPivots();
        }

        private void LateUpdate()
        {
            if (bodyInstance == null || motor == null) return;

            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 displacement = transform.position - lastPosition;
            displacement.y = 0f;
            float speed = displacement.magnitude / dt;
            lastPosition = transform.position;

            if (speed > 0.12f)
                walkCycle += dt * (motor.IsSprinting ? 10.5f : 7.0f);
            else
                walkCycle = Mathf.MoveTowards(walkCycle, 0f, dt * 4f);

            float swing = speed > 0.12f ? Mathf.Clamp01(speed / 3.4f) * (motor.IsSprinting ? 26f : 16f) : 0f;
            float stride = Mathf.Sin(walkCycle) * swing;
            float crouchBlend = motor.IsCrouched ? 1f : 0f;

            // Keep body tucked slightly back from camera near-clip while staying grounded on floor
            bodyInstance.transform.localPosition = bodyLocalOffset + new Vector3(0f, -0.34f * crouchBlend, -0.05f * crouchBlend);

            if (torsoPivot != null)
                torsoPivot.localRotation = Quaternion.Euler(14f * crouchBlend, 0f, 0f);

            if (leftLegPivot != null)
                leftLegPivot.localRotation = Quaternion.Euler(stride - 36f * crouchBlend, 0f, 0f);
            if (rightLegPivot != null)
                rightLegPivot.localRotation = Quaternion.Euler(-stride - 36f * crouchBlend, 0f, 0f);

            if (leftKneePivot != null)
                leftKneePivot.localRotation = Quaternion.Euler(Mathf.Max(0f, -stride * 0.6f) + 64f * crouchBlend, 0f, 0f);
            if (rightKneePivot != null)
                rightKneePivot.localRotation = Quaternion.Euler(Mathf.Max(0f, stride * 0.6f) + 64f * crouchBlend, 0f, 0f);

            if (leftArmPivot != null)
                leftArmPivot.localRotation = Quaternion.Euler(-stride * 0.55f + 6f * crouchBlend, 0f, 0f);
            if (rightArmPivot != null)
                rightArmPivot.localRotation = Quaternion.Euler(stride * 0.55f + 6f * crouchBlend, 0f, 0f);
        }

        private void BindPivots()
        {
            if (bodyInstance == null)
            {
                Transform existing = transform.Find("ACT_Raka_StudentBody");
                if (existing != null) bodyInstance = existing.gameObject;
            }
            if (bodyInstance == null) return;

            // Never allow FPP body visuals to block CharacterController or raycast interactions.
            foreach (Collider col in bodyInstance.GetComponentsInChildren<Collider>(true))
                Destroy(col);

            torsoPivot = FindDeepChild(bodyInstance.transform, "TorsoPivot");
            leftArmPivot = FindDeepChild(bodyInstance.transform, "LeftArmPivot");
            rightArmPivot = FindDeepChild(bodyInstance.transform, "RightArmPivot");
            leftLegPivot = FindDeepChild(bodyInstance.transform, "LeftLegPivot");
            rightLegPivot = FindDeepChild(bodyInstance.transform, "RightLegPivot");
            leftKneePivot = FindDeepChild(bodyInstance.transform, "LeftKneePivot");
            rightKneePivot = FindDeepChild(bodyInstance.transform, "RightKneePivot");
        }

        private static Transform FindDeepChild(Transform root, string targetName)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == targetName) return t;
            return null;
        }
    }
}
