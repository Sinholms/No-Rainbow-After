using UnityEngine;
using UnityEngine.Rendering;

namespace NoRainbowAfter.Player
{
    /// <summary>
    /// Attaches Raka's student body (ACT_Raka_StudentBody) beneath
    /// Player_Raka_FPP and animates the Generic Armature rig with grounded 2-bone leg IK and
    /// camera-safe continuous crouch posture synchronized with FirstPersonMotor.CrouchBlend.
    /// </summary>
    [RequireComponent(typeof(FirstPersonMotor))]
    public sealed class FirstPersonBodyVisual : MonoBehaviour
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

        private void LateUpdate()
        {
            ApplyPoseNow(Time.deltaTime);
        }

        /// <summary>
        /// Evaluates the full FPP armature pose immediately for the current motor stance and camera pitch.
        /// Exposed so PlayMode tests can assert exact bone clearances synchronously after Simulate steps.
        /// </summary>
        public void ApplyPoseNow(float deltaTime)
        {
            if (motor == null) motor = GetComponent<FirstPersonMotor>();
            BindRig();
            if (bodyInstance == null || motor == null || !HasArmatureRig) return;

            float speed = 0f;
            if (deltaTime > 0.00001f)
            {
                Vector3 displacement = transform.position - lastPosition;
                displacement.y = 0f;
                speed = displacement.magnitude / deltaTime;
                lastPosition = transform.position;

                if (speed > 0.12f)
                    walkCycle += deltaTime * (motor.IsSprinting ? 10.5f : 7.0f);
                else
                    walkCycle = Mathf.MoveTowards(walkCycle, 0f, deltaTime * 4f);
            }

            float speedFactor = speed > 0.12f ? Mathf.Clamp01(speed / 3.4f) : 0f;
            float swing = speedFactor * (motor.IsSprinting ? 26f : 16f);
            float stride = Mathf.Sin(walkCycle) * swing;
            float crouch = Mathf.Clamp01(motor.CrouchBlend);
            float lookDown = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(35f, 82f, motor.Pitch + motor.BreathingPitchOffset));

            RestoreRestPose();

            // Keep feet grounded on y = 0 while pulling torso further back under the crouched camera & spotlight
            bodyInstance.transform.localPosition = new Vector3(
                bodyLocalOffset.x,
                0f,
                bodyLocalOffset.z - 0.14f * crouch - 0.06f * crouch * lookDown);

            float walkDip = speedFactor * (1f - crouch) * 0.015f * Mathf.Abs(Mathf.Sin(walkCycle));
            pelvisBone.position += transform.up * (-0.50f * crouch - walkDip) + transform.forward * (-0.04f * crouch);

            spineBone.rotation = Quaternion.AngleAxis(7f * crouch, transform.right) * spineBone.rotation;
            chestBone.rotation = Quaternion.AngleAxis(7f * crouch, transform.right) * chestBone.rotation;
            lanyardBone.rotation = Quaternion.AngleAxis(6f * crouch, transform.right) * lanyardBone.rotation;

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

            float strideExtent = speedFactor * (motor.IsSprinting ? 0.24f : 0.15f);
            SolveLegIK(
                thighL, shinL, footL,
                upperLegLengthL, lowerLegLengthL,
                leftAnkleRestLocal, leftFootRestRotRelBody,
                -1f, crouch, walkCycle, strideExtent);

            SolveLegIK(
                thighR, shinR, footR,
                upperLegLengthR, lowerLegLengthR,
                rightAnkleRestLocal, rightFootRestRotRelBody,
                1f, crouch, walkCycle + Mathf.PI, strideExtent);
        }

        private void RestoreRestPose()
        {
            if (restTransforms == null) return;
            for (int i = 0; i < restTransforms.Length; i++)
            {
                BoneRest r = restTransforms[i];
                if (r.Bone != null)
                    r.Bone.SetLocalPositionAndRotation(r.LocalPos, r.LocalRot);
            }
        }

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
                target.y = minFloorAnkleY;

            Vector3 hip = thigh.position;
            Vector3 delta = target - hip;
            float reach = Mathf.Clamp(delta.magnitude, Mathf.Abs(upper - lower) + 0.002f, upper + lower - 0.001f);
            Vector3 axis = delta.sqrMagnitude > 1e-8f ? delta.normalized : -transform.up;
            target = hip + axis * reach;

            float along = (upper * upper - lower * lower + reach * reach) / (2f * reach);
            Vector3 pole = Vector3.ProjectOnPlane(transform.forward + transform.right * (sideSign * 0.10f), axis);
            if (pole.sqrMagnitude < 1e-6f)
                pole = Vector3.ProjectOnPlane(transform.forward, axis);
            pole.Normalize();

            Vector3 knee = hip + axis * along + pole * Mathf.Sqrt(Mathf.Max(0f, upper * upper - along * along));

            thigh.rotation = Quaternion.FromToRotation((shin.position - thigh.position).normalized, (knee - thigh.position).normalized) * thigh.rotation;
            shin.rotation = Quaternion.FromToRotation((foot.position - shin.position).normalized, (target - shin.position).normalized) * shin.rotation;
            foot.rotation = bodyTf.rotation * footRestRotRelBody;
        }

        private void BindRig()
        {
            if (bodyInstance == null)
            {
                Transform existing = transform.Find("ACT_Raka_StudentBody");
                if (existing != null) bodyInstance = existing.gameObject;
            }
            if (bodyInstance == null || boundInstance == bodyInstance) return;

            // Never allow FPP body visuals to block CharacterController or raycast interactions.
            foreach (Collider col in bodyInstance.GetComponentsInChildren<Collider>(true))
                Destroy(col);

            // Prevent Animator from fighting procedural LateUpdate bone posing.
            foreach (Animator anim in bodyInstance.GetComponentsInChildren<Animator>(true))
            {
                anim.enabled = false;
                anim.applyRootMotion = false;
            }

            // Ensure SkinnedMeshRenderer bounds do not cull arms/legs when pitch changes steeply.
            int headLayer = LayerMask.NameToLayer("PlayerHead");
            foreach (SkinnedMeshRenderer smr in bodyInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                smr.updateWhenOffscreen = true;
                if (smr.name != "SM_Raka_Head" || headLayer < 0) continue;

                smr.gameObject.layer = headLayer;
                smr.shadowCastingMode = ShadowCastingMode.Off;
                if (motor != null && motor.ViewCamera != null)
                    motor.ViewCamera.cullingMask &= ~(1 << headLayer);
            }

            rootBone = FindDeepChild(bodyInstance.transform, "Root");
            pelvisBone = FindDeepChild(bodyInstance.transform, "Pelvis");
            spineBone = FindDeepChild(bodyInstance.transform, "Spine");
            chestBone = FindDeepChild(bodyInstance.transform, "Chest");
            lanyardBone = FindDeepChild(bodyInstance.transform, "Lanyard");

            upperArmL = FindDeepChild(bodyInstance.transform, "UpperArm.L");
            forearmL = FindDeepChild(bodyInstance.transform, "Forearm.L");
            handL = FindDeepChild(bodyInstance.transform, "Hand.L");
            upperArmR = FindDeepChild(bodyInstance.transform, "UpperArm.R");
            forearmR = FindDeepChild(bodyInstance.transform, "Forearm.R");
            handR = FindDeepChild(bodyInstance.transform, "Hand.R");

            thighL = FindDeepChild(bodyInstance.transform, "Thigh.L");
            shinL = FindDeepChild(bodyInstance.transform, "Shin.L");
            footL = FindDeepChild(bodyInstance.transform, "Foot.L");
            thighR = FindDeepChild(bodyInstance.transform, "Thigh.R");
            shinR = FindDeepChild(bodyInstance.transform, "Shin.R");
            footR = FindDeepChild(bodyInstance.transform, "Foot.R");

            Transform[] candidates =
            {
                rootBone, pelvisBone, spineBone, chestBone, lanyardBone,
                upperArmL, forearmL, handL, upperArmR, forearmR, handR,
                thighL, shinL, footL, thighR, shinR, footR
            };
            var captured = new System.Collections.Generic.List<BoneRest>(candidates.Length);
            for (int i = 0; i < candidates.Length; i++)
            {
                Transform b = candidates[i];
                if (b != null)
                {
                    captured.Add(new BoneRest
                    {
                        Bone = b,
                        LocalPos = b.localPosition,
                        LocalRot = b.localRotation
                    });
                }
            }
            restTransforms = captured.ToArray();

            Transform bodyTf = bodyInstance.transform;
            if (thighL != null && shinL != null && footL != null)
            {
                upperLegLengthL = Vector3.Distance(thighL.position, shinL.position);
                lowerLegLengthL = Vector3.Distance(shinL.position, footL.position);
                leftAnkleRestLocal = bodyTf.InverseTransformPoint(footL.position);
                leftFootRestRotRelBody = Quaternion.Inverse(bodyTf.rotation) * footL.rotation;
            }
            if (thighR != null && shinR != null && footR != null)
            {
                upperLegLengthR = Vector3.Distance(thighR.position, shinR.position);
                lowerLegLengthR = Vector3.Distance(shinR.position, footR.position);
                rightAnkleRestLocal = bodyTf.InverseTransformPoint(footR.position);
                rightFootRestRotRelBody = Quaternion.Inverse(bodyTf.rotation) * footR.rotation;
            }

            boundInstance = bodyInstance;
        }

        private static Transform FindDeepChild(Transform root, string targetName)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == targetName) return t;
            return null;
        }
    }
}
