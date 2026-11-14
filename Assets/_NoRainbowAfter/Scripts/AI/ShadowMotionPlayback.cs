using UnityEngine;

namespace NoRainbowAfter.AI
{
    // Generic-rig clips are sampled from the encounter clock, so pause and manual QA stay deterministic.
    public sealed class ShadowMotionPlayback : MonoBehaviour
    {
        public AnimationClip ChestHit, FinalHit, Grab, Rest;
        public Transform Spine;
        public float HitDuration(bool final) => (final ? FinalHit : ChestHit).length;

        public void SampleHit(float progress, bool final)
        {
            var clip = final ? FinalHit : ChestHit;
            clip.SampleAnimation(gameObject, Mathf.Clamp01(progress) * clip.length);
        }

        public void SampleGrab(float progress) => Grab.SampleAnimation(gameObject, Mathf.Clamp01(progress) * Grab.length);
        public void ResetPose() => Rest.SampleAnimation(gameObject, 0f);

        public static ShadowMotionPlayback Attach(Transform wrapper)
        {
            var existing = wrapper.GetComponentInChildren<ShadowMotionPlayback>(true);
            if (existing != null) return existing;
            var asset = Resources.Load<GameObject>("C1ShadowMotion");
            if (asset == null) return null;
            var originals = wrapper.GetComponentsInChildren<Renderer>(true);
            var instance = Instantiate(asset, wrapper, false);
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                foreach (var original in originals)
                    if (renderer.name == original.name) { renderer.sharedMaterials = original.sharedMaterials; break; }
            foreach (var original in originals) original.enabled = false;
            var playback = instance.GetComponent<ShadowMotionPlayback>();
            playback.ResetPose();
            return playback;
        }
    }
}
