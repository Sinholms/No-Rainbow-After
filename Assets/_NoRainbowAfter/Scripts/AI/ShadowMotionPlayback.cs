using UnityEngine;

namespace NoRainbowAfter.AI
{
    // Generic-rig clips are sampled from the encounter clock, so pause and manual QA stay deterministic.
    public sealed class ShadowMotionPlayback : MonoBehaviour
    {
        public AnimationClip ChestHit, FinalHit, Grab, Rest;
        public Transform Spine;
        public bool IsHero { get; private set; }
        private AnimationClip walk, pursue, search, hide, hairSway;
        private float hairTime;
        private Transform facingRoot, upperLeft, foreLeft, handLeft, upperRight, foreRight, handRight;
        private float loopTime;
        private AnimationClip loopClip;
        private Vector3 rootPosition, rootScale;
        private Quaternion rootRotation;
        private Transform[] poseBones;
        private Quaternion[] blendRotations;
        private Vector3[] blendPositions;
        private float blendTime;
        public float HitDuration(bool final) => IsHero ? (final ? 1.2f : .65f) : (final ? FinalHit : ChestHit).length;

        public void SampleHit(float progress, bool final)
        {
            var clip = final ? FinalHit : ChestHit;
            if (IsHero)
            {
                Sample(clip, Mathf.Clamp01(progress) * clip.length);
                if (final && Spine != null)
                    Spine.rotation = Quaternion.AngleAxis(-35f * Mathf.SmoothStep(0f, 1f, progress), facingRoot.right) * Spine.rotation;
                loopClip = null;
                return;
            }
            clip.SampleAnimation(gameObject, Mathf.Clamp01(progress) * clip.length);
        }

        public void SampleGrab(float progress)
        {
            if (!IsHero) { Grab.SampleAnimation(gameObject, Mathf.Clamp01(progress) * Grab.length); return; }
            ResetPose();
            float reach = Mathf.SmoothStep(0f, 1f, progress / .27f) *
                (1f - Mathf.SmoothStep(0f, 1f, (progress - .78f) / .22f));
            ReachArm(upperLeft, foreLeft, handLeft, -1f, reach);
            ReachArm(upperRight, foreRight, handRight, 1f, reach);
        }

        private void ReachArm(Transform upper, Transform fore, Transform hand, float side, float reach)
        {
            if (upper == null || fore == null || hand == null || reach <= 0f) return;
            Vector3 shoulder = upper.position;
            Vector3 target = facingRoot.TransformPoint(new Vector3(side * .29f, 1.54f, .9f));
            Vector3 wrist = Vector3.Lerp(hand.position, target, reach);
            float a = Vector3.Distance(shoulder, fore.position), b = Vector3.Distance(fore.position, hand.position);
            Vector3 delta = wrist - shoulder;
            float distance = Mathf.Clamp(delta.magnitude, Mathf.Abs(a - b) + .001f, a + b - .001f);
            Vector3 axis = delta.normalized;
            wrist = shoulder + axis * distance;
            Vector3 pole = Vector3.ProjectOnPlane(facingRoot.right * side - facingRoot.up * .6f, axis).normalized;
            float along = (a * a - b * b + distance * distance) / (2f * distance);
            Vector3 elbow = shoulder + axis * along + pole * Mathf.Sqrt(Mathf.Max(0f, a * a - along * along));
            upper.rotation = Quaternion.FromToRotation(fore.position - shoulder, elbow - shoulder) * upper.rotation;
            fore.rotation = Quaternion.FromToRotation(hand.position - fore.position, wrist - fore.position) * fore.rotation;
        }

        public void ResetPose()
        {
            Sample(Rest, 0f);
            loopClip = null;
            loopTime = 0f;
        }

        public void SampleLoop(ShadowEncounterState state, float dt, float speed, bool searching = false)
        {
            AnimationClip clip = state == ShadowEncounterState.HideResolved ? hide :
                searching ? search : state != ShadowEncounterState.Pursue || speed < .05f ? Rest :
                speed > .45f ? pursue : walk;
            if (clip != loopClip)
            {
                loopClip = clip;
                loopTime = 0f;
                blendTime = 0f;
                for (int i = 0; i < poseBones.Length; i++)
                {
                    blendRotations[i] = poseBones[i].localRotation;
                    blendPositions[i] = poseBones[i].localPosition;
                }
            }
            loopTime += dt;
            Sample(clip, Mathf.Repeat(loopTime, clip.length));
            blendTime += dt;
            float blend = Mathf.SmoothStep(0f, 1f, blendTime / .20f);
            if (blend < 1f)
                for (int i = 0; i < poseBones.Length; i++)
                {
                    poseBones[i].localRotation = Quaternion.Slerp(blendRotations[i], poseBones[i].localRotation, blend);
                    poseBones[i].localPosition = Vector3.Lerp(blendPositions[i], poseBones[i].localPosition, blend);
                }
        }

        public void SampleHair(float deltaTime)
        {
            if (hairSway == null || deltaTime <= 0f) return;
            hairTime = Mathf.Repeat(hairTime + deltaTime, hairSway.length);
            hairSway.SampleAnimation(gameObject, hairTime);
        }

        public void ResetHair()
        {
            hairTime = 0f;
            if (hairSway != null) hairSway.SampleAnimation(gameObject, 0f);
        }

        public void SampleRecovery(float progress)
        {
            Sample(IsHero ? ChestHit : Rest, IsHero ? Mathf.Clamp01(progress) * ChestHit.length : 0f);
            loopClip = null;
        }

        private void Sample(AnimationClip clip, float time)
        {
            clip.SampleAnimation(gameObject, time);
            // Animation owns only the visible pose. AI remains the sole owner of world movement.
            if (IsHero) transform.SetLocalPositionAndRotation(rootPosition, rootRotation);
            if (IsHero) transform.localScale = rootScale;
        }

        public static ShadowMotionPlayback Attach(Transform wrapper)
        {
            // Prefer the visible Raka rig; never instantiate a hidden second rig to drive its reactions.
            foreach (var animator in wrapper.GetComponentsInChildren<Animator>(true))
            {
                if (animator.runtimeAnimatorController == null || !animator.name.StartsWith("ACT_Shadow_Raka")) continue;
                var hero = animator.GetComponent<ShadowMotionPlayback>();
                if (hero != null) return hero;
                hero = animator.gameObject.AddComponent<ShadowMotionPlayback>();
                hero.IsHero = true;
                hero.facingRoot = wrapper.parent;
                hero.rootPosition = animator.transform.localPosition;
                hero.rootRotation = animator.transform.localRotation;
                hero.rootScale = animator.transform.localScale;
                foreach (var clip in animator.runtimeAnimatorController.animationClips)
                {
                    switch (clip.name)
                    {
                        case "Idle_Breath": hero.Rest = clip; break;
                        case "Hair_Sway": hero.hairSway = clip; break;
                        case "Walk_Lurch": hero.walk = clip; break;
                        case "Pursue": hero.pursue = clip; break;
                        case "Search_Cover": hero.search = clip; break;
                        case "Hide_Pass": hero.hide = clip; break;
                        case "Fight_Reaction": hero.ChestHit = hero.FinalHit = clip; break;
                    }
                }
                foreach (var bone in hero.GetComponentsInChildren<Transform>(true))
                {
                    switch (bone.name)
                    {
                        case "Spine": hero.Spine = bone; break;
                        case "UpperArm.L": hero.upperLeft = bone; break;
                        case "Forearm.L": hero.foreLeft = bone; break;
                        case "Hand.L": hero.handLeft = bone; break;
                        case "UpperArm.R": hero.upperRight = bone; break;
                        case "Forearm.R": hero.foreRight = bone; break;
                        case "Hand.R": hero.handRight = bone; break;
                    }
                }
                if (hero.Rest == null || hero.walk == null || hero.pursue == null || hero.search == null ||
                    hero.hide == null || hero.ChestHit == null || hero.Spine == null || hero.handLeft == null || hero.handRight == null)
                    throw new System.InvalidOperationException("Raka encounter rig is missing required clips or arm bones.");
                hero.poseBones = hero.GetComponentsInChildren<Transform>(true);
                hero.blendRotations = new Quaternion[hero.poseBones.Length];
                hero.blendPositions = new Vector3[hero.poseBones.Length];
                // Sample from the encounter clock so pause, grab and checkpoint reset cannot compete with Animator.Update.
                animator.enabled = false;
                animator.applyRootMotion = false;
                hero.ResetPose();
                return hero;
            }
            var existing = wrapper.GetComponentInChildren<ShadowMotionPlayback>(true);
            if (existing != null) return existing;
            var asset = Resources.Load<GameObject>("C1ShadowMotion");
            if (asset == null) return null;
            var originals = wrapper.GetComponentsInChildren<Renderer>(true);
            var instance = Instantiate(asset, wrapper, false);
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var original in originals)
                    if (renderer.name == original.name) { renderer.sharedMaterials = original.sharedMaterials; break; }
            }
            foreach (var original in originals) original.enabled = false;
            var playback = instance.GetComponent<ShadowMotionPlayback>();
            playback.ResetPose();
            return playback;
        }
    }
}
