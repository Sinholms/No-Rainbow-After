using UnityEngine;

namespace NoRainbowAfter.AI
{
    /// <summary>
    /// Animator langkah kaki &amp; ayunan lengan prosedural dua kaki (bipedal) untuk monster bayangan.
    ///
    /// [GRAFIKA KOMPUTER — KINEMATIKA INVERSE (2-BONE LEG IK)]
    /// - Menghitung fase siklus langkah dari jarak tempuh nyata (bukan timer konstan) agar kaki tidak selip.
    /// - Menambatkan pergelangan kaki tumpu di lantai saat fase stance, lalu mengangkat telapak dengan
    ///   kurva sinus kuadratik saat fase ayun (swing).
    /// - Hukum Kosinus menentukan posisi lutut 3D sehingga paha dan betis menekuk natural.
    /// </summary>
    internal sealed class ShadowStride
    {
        private readonly Transform root;
        private readonly Transform pelvis;
        private readonly Transform spine;
        private readonly Transform neck;
        private readonly Leg left;
        private readonly Leg right;
        private readonly float strideLength;
        private readonly Vector3 pelvisRest;
        private Vector3 previousPosition;
        private float phase;
        private float weight;

        public ShadowStride(Transform root, Transform spine)
        {
            this.root = root;
            this.spine = spine;
            pelvis = spine.parent;
            pelvisRest = pelvis.localPosition;
            neck = spine.Find("Neck");
            left = new Leg(root, pelvis, spine, "L");
            right = new Leg(root, pelvis, spine, "R");
            strideLength = 1.48f * left.Length;
            Reset();
        }

        public void Reset()
        {
            previousPosition = root.position;
            weight = phase = 0f;
            left.Release();
            right.Release();
        }

        public void RestorePelvis() => pelvis.localPosition = pelvisRest;

        public void Sample(float dt, bool moving)
        {
            Vector3 delta = Vector3.ProjectOnPlane(root.position - previousPosition, root.up);
            previousPosition = root.position;
            float distance = moving ? delta.magnitude : 0f;
            bool stepping = distance > .00001f;

            weight = Mathf.MoveTowards(weight, stepping ? 1f : 0f, dt * 5f);
            if (weight <= 0f)
            {
                left.Release();
                right.Release();
                return;
            }

            phase = Mathf.Repeat(phase + distance / strideLength, 1f);
            float blend = Mathf.SmoothStep(0f, 1f, weight);
            float angle = phase * (2f * Mathf.PI);
            float shift = Mathf.Sin(angle);
            float twist = Mathf.Cos(angle);
            float dip = .111f + .015f * Mathf.Cos(2f * angle - .4f) + .009f * shift;

            pelvis.position += blend * left.Length * (root.right * (left.Side * .029f * shift) - root.up * dip);
            pelvis.rotation = Quaternion.AngleAxis(-left.Side * 5f * twist * blend, root.up) * pelvis.rotation;
            spine.rotation = Quaternion.AngleAxis((12f + 2f * shift) * blend, root.right)
                           * Quaternion.AngleAxis(left.Side * 9f * twist * blend, root.up)
                           * Quaternion.AngleAxis((3f - left.Side * 3f * shift) * blend, root.forward)
                           * spine.rotation;
            neck.rotation = Quaternion.AngleAxis(-8f * blend, root.right)
                          * Quaternion.AngleAxis(-left.Side * 4f * twist * blend, root.up)
                          * neck.rotation;

            left.Sample(root, phase, strideLength, blend, .115f, 13f);
            right.Sample(root, Mathf.Repeat(phase + .5f, 1f), strideLength, blend, .068f, 7f);
        }

        private sealed class Leg
        {
            private const float Stance = .58f;
            private readonly Transform thigh;
            private readonly Transform shin;
            private readonly Transform foot;
            private readonly Transform arm;
            private readonly Transform forearm;
            private readonly float upper;
            private readonly float lower;
            private readonly Vector3 ankleRest;
            private readonly Quaternion soleRest;
            private Vector3 plant;
            private bool planted;
            private bool initialized;

            public float Length => upper + lower;
            public float Side => Mathf.Sign(ankleRest.x);

            public Leg(Transform root, Transform pelvis, Transform spine, string side)
            {
                thigh = pelvis.Find("Thigh." + side);
                shin = thigh.Find("Shin." + side);
                foot = shin.Find("Foot." + side);
                arm = spine.Find("UpperArm." + side);
                forearm = arm.Find("Forearm." + side);
                upper = Vector3.Distance(thigh.position, shin.position);
                lower = Vector3.Distance(shin.position, foot.position);
                ankleRest = root.InverseTransformPoint(foot.position);
                soleRest = Quaternion.Inverse(root.rotation) * foot.rotation;
            }

            public void Release() => initialized = false;

            public void Sample(Transform root, float phase, float stride, float weight,
                float clearance, float armSwing)
            {
                Vector3 neutral = root.TransformPoint(ankleRest);
                bool stance = phase < Stance;
                if (!initialized || (stance && !planted))
                {
                    plant = neutral + root.forward * (stride * (Stance * .5f - phase));
                }
                initialized = true;
                planted = stance;
                plant.y = neutral.y;

                // Saat menapak (stance), pergelangan kaki tertambat pada satu titik di lantai.
                // Saat mengayun (swing), prediksi titik pijakan berikutnya searah hadap saat ini.
                float swing = Mathf.Clamp01((phase - Stance) / (1f - Stance));
                float lift = Mathf.Sin(Mathf.PI * swing);
                Vector3 next = neutral + root.forward * (stride * (1f - phase + Stance * .5f));
                Vector3 ankle = Vector3.Lerp(plant, next, Mathf.SmoothStep(0f, 1f, swing))
                              + root.up * (Length * clearance * lift * lift);
                Vector3 target = Vector3.Lerp(foot.position, ankle, weight);
                Quaternion sole = Quaternion.Slerp(
                    foot.rotation,
                    Quaternion.AngleAxis(12f * Mathf.Sin(2f * Mathf.PI * swing) * lift, root.right) * root.rotation * soleRest,
                    weight);

                Vector3 axis = (target - thigh.position).normalized;
                float reach = Mathf.Clamp(Vector3.Distance(target, thigh.position),
                    Mathf.Abs(upper - lower) + .001f, Length - .001f);
                float along = (upper * upper - lower * lower + reach * reach) / (2f * reach);
                Vector3 bend = Vector3.ProjectOnPlane(root.forward + root.right * (Side * .08f), axis).normalized;
                Vector3 knee = thigh.position + axis * along + bend * Mathf.Sqrt(Mathf.Max(0f, upper * upper - along * along));

                thigh.rotation = Quaternion.FromToRotation(shin.position - thigh.position, knee - thigh.position) * thigh.rotation;
                shin.rotation = Quaternion.FromToRotation(foot.position - shin.position, target - shin.position) * shin.rotation;
                foot.rotation = sole;

                float beat = phase * 2f * Mathf.PI;
                arm.rotation = Quaternion.AngleAxis((armSwing * Mathf.Cos(beat - .32f) - 6f) * weight, root.right)
                             * Quaternion.AngleAxis(-Side * 8f * weight, root.forward)
                             * arm.rotation;
                forearm.rotation = Quaternion.AngleAxis((-7f + 4f * Mathf.Cos(beat - .7f)) * weight, root.right)
                                 * forearm.rotation;
            }
        }
    }
}
