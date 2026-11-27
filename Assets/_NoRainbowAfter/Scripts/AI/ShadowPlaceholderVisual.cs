using System.Collections.Generic;
using UnityEngine;

namespace NoRainbowAfter.AI
{
    /// <summary>
    /// Menggerakkan postur prosedural monster bayangan, membungkuk saat melewati kusen pintu,
    /// serta meneruskan animasi reaksi ke Animator atau ShadowMotionPlayback.
    ///
    /// [GRAFIKA KOMPUTER &amp; ANIMASI]
    /// - ComputeTargetStoop() menembakkan Physics.SphereCastNonAlloc ke atas untuk mendeteksi
    ///   langit-langit atau kusen rendah sehingga kepala monster membungkuk halus tanpa tembus tembok.
    /// - ApplyPosture() memodifikasi rotasi tulang punggung (upperPivot) dengan kuaternion komposit.
    /// </summary>
    public sealed class ShadowPlaceholderVisual : MonoBehaviour
    {
        [SerializeField] private Transform visualWrapper;
        [SerializeField] private Transform upperPivot;
        [SerializeField] private float standingVisualHeight = 2.28f;
        [SerializeField] private float stoopedVisualHeight = 1.88f;
        [SerializeField] private float doorwayStoopPitch = 46f;
        [SerializeField] private float doorwaySinkY = 0.08f;

        private static readonly RaycastHit[] CeilingHits = new RaycastHit[16];

        private Renderer[] renderers;
        private Quaternion upperRestOffset = Quaternion.identity;
        private Vector3 wrapperBaseLocalPos;
        private Quaternion wrapperBaseLocalRot = Quaternion.identity;
        private float phase;
        private float stoopBlend;
        private bool initialized;
        private ShadowMotionPlayback motion;
        private Animator heroAnimator;
        private string currentHeroState;
        private bool finalHit;
        private Transform[] grabArms;
        private Vector3[] armPositions;
        private Quaternion[] armRotations;

        public float CurrentVisualHeight => Mathf.Lerp(standingVisualHeight, stoopedVisualHeight, stoopBlend);

        public float HitDuration(bool final) => motion != null ? motion.HitDuration(final) : .52f;

        public void SetHitVariant(bool final) => finalHit = final;

        public void Configure(Transform wrapper, Transform upper)
        {
            visualWrapper = wrapper;
            upperPivot = upper;
            initialized = false;
            EnsureInitialized();
        }

        private void Awake() => EnsureInitialized();

        private void EnsureInitialized()
        {
            // Configure juga dipanggil di Editor oleh installer prefab. Rig runtime tidak boleh terbawa ke aset prefab.
            if (!Application.isPlaying || initialized)
            {
                return;
            }
            if (visualWrapper == null && transform.childCount > 0)
            {
                visualWrapper = transform.GetChild(0);
            }
            if (upperPivot == null && visualWrapper != null)
            {
                foreach (var t in visualWrapper.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "PIV_ShadowUpper")
                    {
                        upperPivot = t;
                        break;
                    }
                }
            }
            if (visualWrapper != null)
            {
                wrapperBaseLocalPos = visualWrapper.localPosition;
                wrapperBaseLocalRot = visualWrapper.localRotation;
                renderers = visualWrapper.GetComponentsInChildren<Renderer>(true);
            }
            if (upperPivot != null)
            {
                upperPivot.localRotation = Quaternion.identity;
                upperRestOffset = Quaternion.Inverse(transform.rotation) * upperPivot.rotation;
            }

            motion = visualWrapper != null ? ShadowMotionPlayback.Attach(visualWrapper) : null;
            if (visualWrapper != null)
            {
                foreach (var anim in visualWrapper.GetComponentsInChildren<Animator>(true))
                {
                    if (anim.runtimeAnimatorController != null)
                    {
                        heroAnimator = anim;
                        break;
                    }
                }
            }
            if (motion != null)
            {
                upperPivot = motion.Spine;
                upperRestOffset = Quaternion.Inverse(transform.rotation) * upperPivot.rotation;
                if (heroAnimator == null)
                {
                    renderers = motion.GetComponentsInChildren<Renderer>(true);
                }
            }

            initialized = true;
            if (visualWrapper != null)
            {
                var arms = new List<Transform>();
                foreach (var t in visualWrapper.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.StartsWith("SM_ACT_ShadowArm"))
                    {
                        arms.Add(t);
                    }
                }
                grabArms = arms.ToArray();
                armPositions = new Vector3[grabArms.Length];
                armRotations = new Quaternion[grabArms.Length];
                for (int i = 0; i < grabArms.Length; i++)
                {
                    armPositions[i] = grabArms[i].localPosition;
                    armRotations[i] = grabArms[i].localRotation;
                }
            }
        }

        public void SetVisible(bool visible)
        {
            EnsureInitialized();
            if (visualWrapper != null)
            {
                visualWrapper.gameObject.SetActive(visible);
            }
        }

        public void ResetPostureImmediate(Transform ignoredPlayerRoot = null)
        {
            EnsureInitialized();
            phase = 0f;
            motion?.ResetPose();
            RestoreArms();
            stoopBlend = ComputeTargetStoop(ignoredPlayerRoot);
            ApplyPosture(0f, 0f, 0f, Vector3.zero);
        }

        public void TickSecondaryMotion(float deltaTime)
        {
            EnsureInitialized();
            if (visualWrapper != null && visualWrapper.gameObject.activeInHierarchy)
            {
                motion?.SampleHair(deltaTime);
            }
        }

        public void TickVisual(
            ShadowEncounterState state,
            float deltaTime,
            float moveSpeedNormalized,
            float stateProgressNormalized,
            Transform ignoredPlayerRoot = null,
            bool searching = false)
        {
            EnsureInitialized();
            if (visualWrapper == null || !visualWrapper.gameObject.activeSelf)
            {
                return;
            }
            RestoreArms();

            if (heroAnimator != null && heroAnimator.isActiveAndEnabled && heroAnimator.runtimeAnimatorController != null)
            {
                string desiredState = state == ShadowEncounterState.Spawn ? "Idle_Breath" :
                    state == ShadowEncounterState.FightResolved ? "Fight_Reaction" :
                    state == ShadowEncounterState.HideResolved ? "Hide_Pass" :
                    moveSpeedNormalized > 0.45f ? "Pursue" : "Walk_Lurch";
                if (currentHeroState != desiredState)
                {
                    currentHeroState = desiredState;
                    heroAnimator.CrossFadeInFixedTime(desiredState, 0.12f, 0);
                }
            }

            if (motion != null)
            {
                if (state == ShadowEncounterState.FightResolved)
                {
                    motion.SampleHit(stateProgressNormalized, finalHit);
                    visualWrapper.localPosition = wrapperBaseLocalPos;
                    return;
                }
                if (motion.IsHero) motion.SampleLoop(state, deltaTime, moveSpeedNormalized, searching);
                else motion.ResetPose();
            }

            float dt = Mathf.Max(0f, deltaTime);
            phase += dt * (state == ShadowEncounterState.Pursue ? Mathf.Lerp(2.2f, 5.4f, moveSpeedNormalized) : 1.5f);

            float targetStoop = ComputeTargetStoop(ignoredPlayerRoot);
            stoopBlend = Mathf.MoveTowards(stoopBlend, targetStoop, dt * 6.5f);

            float extraPitch = 0f;
            float swayYaw = 0f;
            float swayRoll = 0f;
            Vector3 recoilOffset = Vector3.zero;

            switch (state)
            {
                case ShadowEncounterState.Spawn:
                    extraPitch = Mathf.Sin(phase * 1.4f) * 2.2f;
                    swayRoll = Mathf.Sin(phase * 0.9f) * 1.4f;
                    break;
                case ShadowEncounterState.Pursue:
                    if (motion != null && motion.IsHero) break;
                    extraPitch = 5.5f * moveSpeedNormalized + Mathf.Sin(phase * 2f) * 2.8f;
                    swayYaw = Mathf.Sin(phase) * 3.5f * Mathf.Max(0.35f, moveSpeedNormalized);
                    swayRoll = Mathf.Cos(phase) * 3.2f * Mathf.Max(0.35f, moveSpeedNormalized);
                    break;
                case ShadowEncounterState.FightResolved:
                    float flinch = Mathf.Sin(Mathf.Clamp01(stateProgressNormalized) * Mathf.PI);
                    extraPitch = -18f * flinch;
                    swayRoll = 9f * flinch;
                    recoilOffset = new Vector3(0f, -0.14f * stateProgressNormalized, -0.48f * Mathf.Clamp01(stateProgressNormalized * 1.3f));
                    break;
                case ShadowEncounterState.HideResolved:
                    extraPitch = 10f * stateProgressNormalized;
                    recoilOffset = new Vector3(0f, -0.18f * stateProgressNormalized, 0f);
                    break;
            }

            ApplyPosture(extraPitch, swayYaw, swayRoll, recoilOffset);
        }

        public float GetHighestRenderedY()
        {
            EnsureInitialized();
            if (renderers == null || renderers.Length == 0)
            {
                return transform.position.y + CurrentVisualHeight;
            }
            float maxY = transform.position.y;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r != null && r.enabled && r.gameObject.activeInHierarchy)
                {
                    maxY = Mathf.Max(maxY, r.bounds.max.y);
                }
            }
            return maxY;
        }

        private void RestoreArms()
        {
            if (grabArms == null)
            {
                return;
            }
            for (int i = 0; i < grabArms.Length; i++)
            {
                if (grabArms[i] != null)
                {
                    grabArms[i].SetLocalPositionAndRotation(armPositions[i], armRotations[i]);
                }
            }
        }

        public void AnimateGrab(float progress)
        {
            EnsureInitialized();
            RestoreArms();
            if (motion != null)
            {
                motion.SampleGrab(progress);
                return;
            }

            float reach = Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI);
            ApplyPosture(
                18 * reach,
                Mathf.Sin(progress * 28) * 2 * reach,
                Mathf.Sin(progress * 36) * 3 * reach,
                new Vector3(0, -.07f * reach, .18f * reach));

            if (grabArms == null)
            {
                return;
            }
            foreach (var arm in grabArms)
            {
                float side = arm.name.Contains("Left") ? -1 : 1;
                Vector3 shoulder = transform.position + Vector3.up * 1.8f + transform.right * (.27f * side);
                arm.RotateAround(shoulder, transform.right, -72 * reach);
            }
        }

        public void AnimateRecovery(float progress)
        {
            EnsureInitialized();
            RestoreArms();
            motion?.SampleRecovery(progress);
            float stagger = Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI);
            ApplyPosture(-12f * stagger, 0f, 4f * stagger, Vector3.zero);
        }

        private float ComputeTargetStoop(Transform ignoredPlayerRoot)
        {
            float z = transform.position.z;
            // Kusen pintu kos berada di z = 5.0 (min.y = 2.05m); mulai membungkuk sebelum masuk.
            float doorwayFactor = 0f;
            if (z >= 4.25f && z <= 6.25f)
            {
                float distFromDoor = Mathf.Abs(z - 5.15f);
                doorwayFactor = Mathf.Clamp01(1f - (distFromDoor - 0.35f) / 0.75f);
            }

            // Uji pula geometri langit-langit nyata di atas kepala agar mesh tidak tembus balok/kusen.
            Vector3 probeOrigin = transform.position + Vector3.up * 1.35f;
            int hitCount = Physics.SphereCastNonAlloc(
                probeOrigin,
                0.18f,
                Vector3.up,
                CeilingHits,
                1.08f,
                ~0,
                QueryTriggerInteraction.Ignore);

            float minCeilingY = float.PositiveInfinity;
            for (int i = 0; i < hitCount; i++)
            {
                Collider col = CeilingHits[i].collider;
                if (col == null) continue;
                Transform t = col.transform;
                if (t.IsChildOf(transform)) continue;
                if (ignoredPlayerRoot != null && t.IsChildOf(ignoredPlayerRoot)) continue;
                float hitY = CeilingHits[i].point.y > 0.01f ? CeilingHits[i].point.y : col.bounds.min.y;
                minCeilingY = Mathf.Min(minCeilingY, hitY);
            }

            float ceilingFactor = minCeilingY < 2.40f
                ? Mathf.Clamp01((2.40f - minCeilingY) / 0.32f)
                : 0f;

            return Mathf.Max(doorwayFactor, ceilingFactor);
        }

        private void ApplyPosture(float extraPitch, float swayYaw, float swayRoll, Vector3 localRecoil)
        {
            if (visualWrapper != null)
            {
                visualWrapper.localPosition = wrapperBaseLocalPos +
                    Vector3.down * (doorwaySinkY * stoopBlend) +
                    localRecoil;
                visualWrapper.localRotation = wrapperBaseLocalRot;
            }

            if (upperPivot != null)
            {
                float totalPitch = doorwayStoopPitch * stoopBlend + extraPitch;
                if (motion != null)
                {
                    upperPivot.rotation = transform.rotation * Quaternion.Euler(totalPitch, swayYaw, swayRoll) *
                        Quaternion.Inverse(transform.rotation) * upperPivot.rotation;
                }
                else
                {
                    upperPivot.rotation = transform.rotation * Quaternion.Euler(totalPitch, swayYaw, swayRoll) * upperRestOffset;
                }
            }
        }
    }
}
