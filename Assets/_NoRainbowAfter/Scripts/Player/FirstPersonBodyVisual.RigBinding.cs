using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NoRainbowAfter.Player
{
    /// <summary>
    /// Pengikatan tulang armature tubuh FPP Raka, penyembunyian mesh kepala dari kamera FPP
    /// melalui bitmask cullingMask (~(1 &lt;&lt; headLayer)), dan penyimpanan pose dasar (rest pose).
    /// </summary>
    public sealed partial class FirstPersonBodyVisual
    {
        private void BindRig()
        {
            if (bodyInstance == null)
            {
                Transform existing = transform.Find("ACT_Raka_StudentBody");
                if (existing != null)
                {
                    bodyInstance = existing.gameObject;
                }
            }
            if (bodyInstance == null || boundInstance == bodyInstance)
            {
                return;
            }

            // Visual tubuh FPP tidak boleh menghalangi CharacterController maupun raycast interaksi.
            foreach (Collider col in bodyInstance.GetComponentsInChildren<Collider>(true))
            {
                Destroy(col);
            }

            foreach (Animator anim in bodyInstance.GetComponentsInChildren<Animator>(true))
            {
                anim.enabled = false;
                anim.applyRootMotion = false;
            }

            // Sembunyikan mesh kepala dari ViewCamera menggunakan bitmask layer PlayerHead,
            // dan pastikan SkinnedMeshRenderer tidak ter-cull saat kamera menunduk tajam.
            int headLayer = LayerMask.NameToLayer("PlayerHead");
            foreach (SkinnedMeshRenderer smr in bodyInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                smr.updateWhenOffscreen = true;
                if (smr.name != "SM_Raka_Head" || headLayer < 0)
                {
                    continue;
                }
                smr.gameObject.layer = headLayer;
                smr.shadowCastingMode = ShadowCastingMode.Off;
                if (motor != null && motor.ViewCamera != null)
                {
                    motor.ViewCamera.cullingMask &= ~(1 << headLayer);
                }
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
            var captured = new List<BoneRest>(candidates.Length);
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
            {
                if (t.name == targetName)
                {
                    return t;
                }
            }
            return null;
        }
    }
}
