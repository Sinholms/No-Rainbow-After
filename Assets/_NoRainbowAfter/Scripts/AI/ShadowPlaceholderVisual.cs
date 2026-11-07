using UnityEngine;

namespace NoRainbowAfter.AI
{
    /// <summary>
    /// Drives procedural posture, doorway stooping, and brief state feedback on the T06 placeholder mesh.
    /// Kept separate from encounter logic so T13 can replace visuals/Animator without altering T06/T11 behavior.
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

        public float CurrentVisualHeight => Mathf.Lerp(standingVisualHeight, stoopedVisualHeight, stoopBlend);

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
            if (initialized) return;
            if (visualWrapper == null && transform.childCount > 0)
                visualWrapper = transform.GetChild(0);
            if (upperPivot == null && visualWrapper != null)
            {
                foreach (var t in visualWrapper.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "PIV_ShadowUpper") { upperPivot = t; break; }
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
            initialized = true;
        }

        public void SetVisible(bool visible)
        {
            EnsureInitialized();
            if (visualWrapper != null)
                visualWrapper.gameObject.SetActive(visible);
        }

        public void ResetPostureImmediate(Transform ignoredPlayerRoot = null)
        {
            EnsureInitialized();
            phase = 0f;
            stoopBlend = ComputeTargetStoop(ignoredPlayerRoot);
            ApplyPosture(0f, 0f, 0f, Vector3.zero);
        }

        public void TickVisual(
            ShadowEncounterState state,
            float deltaTime,
            float moveSpeedNormalized,
            float stateProgressNormalized,
            Transform ignoredPlayerRoot = null)
        {
            EnsureInitialized();
            if (visualWrapper == null || !visualWrapper.gameObject.activeSelf) return;

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
            if (renderers == null || renderers.Length == 0) return transform.position.y + CurrentVisualHeight;
            float maxY = transform.position.y;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r != null && r.enabled && r.gameObject.activeInHierarchy)
                    maxY = Mathf.Max(maxY, r.bounds.max.y);
            }
            return maxY;
        }

        private float ComputeTargetStoop(Transform ignoredPlayerRoot)
        {
            float z = transform.position.z;
            // Dorm doorway lintel sits around z = 5.0 with min.y = 2.05m; begin stooping smoothly before entering.
            float doorwayFactor = 0f;
            if (z >= 4.25f && z <= 6.25f)
            {
                float distFromDoor = Mathf.Abs(z - 5.15f);
                doorwayFactor = Mathf.Clamp01(1f - (distFromDoor - 0.35f) / 0.75f);
            }

            // Also probe actual overhead solid geometry so any low header/lintel prevents mesh clipping.
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
                upperPivot.rotation = transform.rotation *
                    Quaternion.Euler(totalPitch, swayYaw, swayRoll) *
                    upperRestOffset;
            }
        }
    }
}
