using UnityEngine;

namespace NoRainbowAfter.Player
{
    /// <summary>
    /// Memasang tubuh mahasiswa Raka (ACT_Raka_StudentBody) di bawah Player_Raka_FPP dan
    /// menganimasikan rig secara prosedural dengan 2-Bone Leg IK serta postur jongkok kontinu.
    ///
    /// [GRAFIKA KOMPUTER &amp; KINEMATIKA INVERSE]
    /// - SolveLegIK() memecahkan kinematika dua segmen tulang (paha &amp; betis) memakai Hukum Kosinus
    ///   agar kedua kaki tetap menapak di lantai saat Raka berdiri maupun jongkok penuh.
    /// - Dipecah menjadi FirstPersonBodyVisual.cs &amp; FirstPersonBodyVisual.RigBinding.cs.
    /// </summary>
    [RequireComponent(typeof(FirstPersonMotor))]
    public sealed partial class FirstPersonBodyVisual : MonoBehaviour
    {
        private struct BoneRest
        {
            public Transform Bone;
            public Vector3 LocalPos;
            public Quaternion LocalRot;
        }

        [SerializeField] private GameObject bodyInstance;
        [SerializeField] private Vector3 bodyLocalOffset = new Vector3(0f, 0f, -0.10f);

        private FirstPersonMotor motor;
        private GameObject boundInstance;

        private Transform rootBone;
        private Transform pelvisBone;
        private Transform spineBone;
        private Transform chestBone;
        private Transform lanyardBone;
        private Transform upperArmL;
        private Transform forearmL;
        private Transform handL;
        private Transform upperArmR;
        private Transform forearmR;
        private Transform handR;
        private Transform thighL;
        private Transform shinL;
        private Transform footL;
        private Transform thighR;
        private Transform shinR;
        private Transform footR;

        private BoneRest[] restTransforms;
        private float upperLegLengthL;
        private float lowerLegLengthL;
        private float upperLegLengthR;
        private float lowerLegLengthR;
        private Vector3 leftAnkleRestLocal;
        private Vector3 rightAnkleRestLocal;
        private Quaternion leftFootRestRotRelBody = Quaternion.identity;
        private Quaternion rightFootRestRotRelBody = Quaternion.identity;

        private Vector3 lastPosition;
        private float walkCycle;

        public GameObject BodyInstance => bodyInstance;
        public bool HasArmatureRig =>
            pelvisBone != null && spineBone != null && chestBone != null && lanyardBone != null &&
            thighL != null && shinL != null && footL != null &&
            thighR != null && shinR != null && footR != null;
        public Transform PelvisBone => pelvisBone;
        public Transform SpineBone => spineBone;
        public Transform ChestBone => chestBone;
        public Transform LanyardBone => lanyardBone;
        public Transform LeftFootBone => footL;
        public Transform RightFootBone => footR;

        public void Configure(GameObject instance)
        {
            bodyInstance = instance;
            boundInstance = null;
            BindRig();
        }

        private void Awake()
        {
            motor = GetComponent<FirstPersonMotor>();
            BindRig();
            lastPosition = transform.position;
        }

        private void OnEnable()
        {
            lastPosition = transform.position;
            BindRig();
        }

        private void LateUpdate() => ApplyPoseNow(Time.deltaTime);

        public void ApplyPoseNow(float deltaTime)
        {
            if (motor == null) motor = GetComponent<FirstPersonMotor>();
            BindRig();
            if (bodyInstance == null || motor == null || !HasArmatureRig)
            {
                return;
            }

            // Kecepatan dihitung dari perpindahan nyata, sehingga langkah berhenti saat gerak terhalang.
            float speed = 0f;
            if (deltaTime > 0.00001f)
            {
                Vector3 displacement = transform.position - lastPosition;
                displacement.y = 0f;
                speed = displacement.magnitude / deltaTime;
                lastPosition = transform.position;

                if (speed > 0.12f)
                {
                    walkCycle += deltaTime * (motor.IsSprinting ? 10.5f : 7.0f) * Mathf.Lerp(1f, 0.84f, motor.StressGaitIntensity);
                }
                else
                {
                    walkCycle = Mathf.MoveTowards(walkCycle, 0f, deltaTime * 4f);
                }
            }

            float stress = motor.StressGaitIntensity;
            float speedFactor = speed > 0.12f ? Mathf.Clamp01(speed / 3.4f) : 0f;
            float swing = speedFactor * (motor.IsSprinting ? 26f : 16f) * Mathf.Lerp(1f, 0.68f, stress);
            float stride = Mathf.Sin(walkCycle) * swing;
            float crouch = Mathf.Clamp01(motor.CrouchBlend);
            float lookDown = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(35f, 82f, motor.Pitch + motor.BreathingPitchOffset));

            // Mulai dari pose dasar setiap frame agar rotasi prosedural tidak menumpuk.
            RestoreRestPose();

            // Jaga telapak kaki menapak di y = 0 sambil menarik dada agak ke belakang agar aman di kamera FPP.
            bodyInstance.transform.localPosition = new Vector3(
                bodyLocalOffset.x,
                0f,
                bodyLocalOffset.z - 0.14f * crouch - 0.06f * crouch * lookDown);

            float walkDip = speedFactor * (1f - crouch) * (0.015f + 0.018f * stress) * Mathf.Abs(Mathf.Sin(walkCycle));
            pelvisBone.position += transform.up * (-0.50f * crouch - walkDip) + transform.forward * (-0.04f * crouch);

            float stressSlouch = Mathf.InverseLerp(0.45f, 1f, stress) * (1f - crouch * 0.65f) *
                                 Mathf.Max(speedFactor, Mathf.InverseLerp(0.65f, 1f, stress));
            float torsoRoll = motor.StressRollOffset * 0.45f;

            spineBone.rotation = Quaternion.AngleAxis(7f * crouch + 5.5f * stressSlouch, transform.right)
                * Quaternion.AngleAxis(torsoRoll, transform.forward)
                * spineBone.rotation;
            chestBone.rotation = Quaternion.AngleAxis(7f * crouch + 4.5f * stressSlouch, transform.right)
                * Quaternion.AngleAxis(torsoRoll * 0.6f, transform.forward)
                * chestBone.rotation;
            lanyardBone.rotation = Quaternion.AngleAxis(6f * crouch + 3.5f * stressSlouch, transform.right)
                * lanyardBone.rotation;

            if (upperArmL != null)
            {
                upperArmL.rotation = Quaternion.AngleAxis(stride * 0.65f + 10f * crouch, transform.right)
                    * Quaternion.AngleAxis(-8f * crouch, transform.forward)
                    * upperArmL.rotation;
            }
            if (upperArmR != null)
            {
                upperArmR.rotation = Quaternion.AngleAxis(-stride * 0.65f + 10f * crouch, transform.right)
                    * Quaternion.AngleAxis(8f * crouch, transform.forward)
                    * upperArmR.rotation;
            }
            if (forearmL != null)
            {
                forearmL.rotation = Quaternion.AngleAxis(-28f * crouch - Mathf.Max(0f, -stride * 0.35f), transform.right)
                    * forearmL.rotation;
            }
            if (forearmR != null)
            {
                forearmR.rotation = Quaternion.AngleAxis(-28f * crouch - Mathf.Max(0f, stride * 0.35f), transform.right)
                    * forearmR.rotation;
            }

            // IK menghitung posisi kedua kaki berdasarkan langkah dan perubahan tinggi saat jongkok.
            float strideExtent = speedFactor * (motor.IsSprinting ? 0.24f : 0.15f);
            SolveLegIK(thighL, shinL, footL, upperLegLengthL, lowerLegLengthL,
                leftAnkleRestLocal, leftFootRestRotRelBody, -1f, crouch, walkCycle, strideExtent);
            SolveLegIK(thighR, shinR, footR, upperLegLengthR, lowerLegLengthR,
                rightAnkleRestLocal, rightFootRestRotRelBody, 1f, crouch, walkCycle + Mathf.PI, strideExtent);
        }

        private void RestoreRestPose()
        {
            if (restTransforms == null)
            {
                return;
            }
            for (int i = 0; i < restTransforms.Length; i++)
            {
                BoneRest r = restTransforms[i];
                if (r.Bone != null)
                {
                    r.Bone.SetLocalPositionAndRotation(r.LocalPos, r.LocalRot);
                }
            }
        }

        // Analytic 2-Bone IK berbasis Hukum Kosinus untuk menambatkan telapak kaki di lantai.
        private void SolveLegIK(
            Transform thigh, Transform shin, Transform foot,
            float upper, float lower,
            Vector3 ankleRestLocal, Quaternion footRestRotRelBody,
            float sideSign, float crouch, float legPhase, float strideExtent)
        {
            Transform bodyTf = bodyInstance.transform;
            float stagger = sideSign < 0f ? 0.04f : -0.04f;
            float forwardStep = Mathf.Sin(legPhase) * strideExtent * (1f - 0.45f * crouch);
            float liftStep = Mathf.Max(0f, Mathf.Cos(legPhase)) * strideExtent * 0.28f;

            Vector3 neutral = bodyTf.TransformPoint(ankleRestLocal);
            Vector3 target = neutral
                + transform.forward * ((0.12f + stagger) * crouch + forwardStep)
                + transform.up * liftStep;

            float minFloorAnkleY = transform.position.y + ankleRestLocal.y;
            if (target.y < minFloorAnkleY)
            {
                target.y = minFloorAnkleY;
            }

            Vector3 hip = thigh.position;
            Vector3 delta = target - hip;
            float reach = Mathf.Clamp(delta.magnitude, Mathf.Abs(upper - lower) + 0.002f, upper + lower - 0.001f);
            Vector3 axis = delta.sqrMagnitude > 1e-8f ? delta.normalized : -transform.up;
            target = hip + axis * reach;

            float along = (upper * upper - lower * lower + reach * reach) / (2f * reach);
            Vector3 pole = Vector3.ProjectOnPlane(transform.forward + transform.right * (sideSign * 0.10f), axis);
            if (pole.sqrMagnitude < 1e-6f)
            {
                pole = Vector3.ProjectOnPlane(transform.forward, axis);
            }
            pole.Normalize();

            Vector3 knee = hip + axis * along + pole * Mathf.Sqrt(Mathf.Max(0f, upper * upper - along * along));

            thigh.rotation = Quaternion.FromToRotation((shin.position - thigh.position).normalized, (knee - thigh.position).normalized) * thigh.rotation;
            shin.rotation = Quaternion.FromToRotation((foot.position - shin.position).normalized, (target - shin.position).normalized) * shin.rotation;
            foot.rotation = bodyTf.rotation * footRestRotRelBody;
        }
    }
}
