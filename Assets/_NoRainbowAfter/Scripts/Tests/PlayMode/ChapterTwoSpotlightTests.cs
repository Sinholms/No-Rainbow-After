using System.Collections.Generic;
using NoRainbowAfter.AI;
using NoRainbowAfter.Player;
using NUnit.Framework;
using UnityEngine;

namespace NoRainbowAfter.Tests
{
    public sealed class ChapterTwoSpotlightTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private GameObject New(string name) { var o = new GameObject(name); objects.Add(o); return o; }
        private GameObject Box(Vector3 pos, Vector3 scale)
        {
            var o = GameObject.CreatePrimitive(PrimitiveType.Cube); objects.Add(o); o.transform.position = pos; o.transform.localScale = scale; return o;
        }
        [TearDown] public void TearDown() { foreach (var o in objects) if (o != null) Object.DestroyImmediate(o); objects.Clear(); }
        [Test]
        public void OverlapUsesOneTimerCoverBlocksAndCheckpointResets()
        {
            Vector3 origin = new Vector3(500, 0, 0);
            Box(origin + Vector3.down * .25f, new Vector3(30, .5f, 30));
            var actor = New("player"); actor.transform.position = origin + Vector3.forward * 4;
            var camera = New("camera"); camera.transform.SetParent(actor.transform); camera.AddComponent<Camera>();
            actor.AddComponent<CharacterController>(); var motor = actor.AddComponent<FirstPersonMotor>();
            var e1 = New("light1").transform; e1.position = origin + Vector3.up;
            var e2 = New("light2").transform; e2.position = e1.position;
            var checkpoint = New("checkpoint").transform; checkpoint.position = origin + Vector3.right * 5;
            var encounter = New("encounter").AddComponent<ChapterTwoSpotlightEncounter>(); encounter.AutoTick = false;
            encounter.Configure(motor, new[] { e1, e2 }, new[] { checkpoint });
            Physics.SyncTransforms(); encounter.ManualTick(.5f);
            Assert.That(encounter.Exposure, Is.EqualTo(.5f).Within(.001f));
            var cover = Box(origin + new Vector3(0, 1, 2), new Vector3(2, 2, .5f));
            Physics.SyncTransforms(); encounter.ManualTick(.1f);
            Assert.That(encounter.IsIlluminated, Is.False);
            Assert.That(encounter.Exposure, Is.LessThan(.5f));
            Object.DestroyImmediate(cover); Physics.SyncTransforms(); encounter.ManualTick(2f);
            Assert.That(encounter.CatchCount, Is.EqualTo(1));
            Assert.That(encounter.Exposure, Is.Zero);
            Assert.That(actor.transform.position, Is.EqualTo(checkpoint.position));
        }
        [TestCase(1.5f)]
        [TestCase(2f)]
        public void ExposureThresholdAndZeroTickAreDeterministic(float duration)
        {
            Vector3 origin = new Vector3(600, 0, 0);
            Box(origin + Vector3.down * .25f, new Vector3(30, .5f, 30));
            var actor = New("player"); actor.transform.position = origin + Vector3.forward * 4;
            var camera = New("camera"); camera.transform.SetParent(actor.transform); camera.AddComponent<Camera>();
            actor.AddComponent<CharacterController>(); var motor = actor.AddComponent<FirstPersonMotor>();
            var emitter = New("light").transform; emitter.position = origin + Vector3.up;
            var checkpoint = New("checkpoint").transform; checkpoint.position = origin + Vector3.right * 5;
            var encounter = New("encounter").AddComponent<ChapterTwoSpotlightEncounter>(); encounter.AutoTick = false;
            encounter.Configure(motor, new[] { emitter }, new[] { checkpoint }); encounter.CatchSeconds = duration;
            Physics.SyncTransforms();
            encounter.ManualTick(duration - .01f); encounter.ManualTick(0f);
            Assert.That(encounter.CatchCount, Is.Zero);
            Assert.That(encounter.Exposure, Is.EqualTo(duration - .01f).Within(.001f));
            encounter.ManualTick(.011f);
            Assert.That(encounter.CatchCount, Is.EqualTo(1));
            Assert.That(encounter.Exposure, Is.Zero);
        }
        [Test]
        public void SetSegmentPreservesActiveExposureMidBeam()
        {
            Vector3 origin = new Vector3(700, 0, 0);
            Box(origin + Vector3.down * .25f, new Vector3(30, .5f, 30));
            var actor = New("player"); actor.transform.position = origin + Vector3.forward * 4;
            var camera = New("camera"); camera.transform.SetParent(actor.transform); camera.AddComponent<Camera>();
            actor.AddComponent<CharacterController>(); var motor = actor.AddComponent<FirstPersonMotor>();
            var emitter = New("light").transform; emitter.position = origin + Vector3.up;
            var cp0 = New("checkpoint0").transform; cp0.position = origin + Vector3.right * 5;
            var cp1 = New("checkpoint1").transform; cp1.position = origin + new Vector3(5, 0, 3);
            var encounter = New("encounter").AddComponent<ChapterTwoSpotlightEncounter>();
            encounter.AutoTick = false; encounter.RenderFootprints = false;
            encounter.Configure(motor, new[] { emitter }, new[] { cp0, cp1 });
            Physics.SyncTransforms(); encounter.ManualTick(.6f);
            Assert.That(encounter.IsIlluminated, Is.True);
            Assert.That(encounter.Exposure, Is.EqualTo(.6f).Within(.001f));
            encounter.SetSegment(1);
            Assert.That(encounter.Segment, Is.EqualTo(1));
            Assert.That(encounter.IsIlluminated, Is.True);
            Assert.That(encounter.Exposure, Is.EqualTo(.6f).Within(.001f));
            encounter.ManualTick(.4f);
            Assert.That(encounter.Exposure, Is.EqualTo(1f).Within(.001f));
            encounter.ManualTick(1.1f);
            Assert.That(encounter.CatchCount, Is.EqualTo(1));
            Assert.That(actor.transform.position, Is.EqualTo(cp1.position));
        }
        [Test]
        public void CrouchingBehindBarrierHidesPlayerWhileStandingExposes()
        {
            Vector3 origin = new Vector3(800, 0, 0);
            Box(origin + Vector3.down * .25f, new Vector3(30, .5f, 30));
            Box(origin + new Vector3(0, .52f, 6f), new Vector3(2.65f, 1.04f, .64f));
            var actor = New("player"); actor.transform.position = origin + Vector3.forward * 6.85f;
            var camera = New("camera"); camera.transform.SetParent(actor.transform); camera.AddComponent<Camera>();
            actor.AddComponent<CharacterController>(); var motor = actor.AddComponent<FirstPersonMotor>();
            var emitter = New("light").transform; emitter.position = origin + Vector3.up * 2f;
            emitter.localRotation = Quaternion.Euler(11.5f, 0, 0);
            var checkpoint = New("checkpoint").transform; checkpoint.position = origin + Vector3.right * 5;
            var encounter = New("encounter").AddComponent<ChapterTwoSpotlightEncounter>();
            encounter.AutoTick = false; encounter.RenderFootprints = false;
            encounter.Configure(motor, new[] { emitter }, new[] { checkpoint });
            Physics.SyncTransforms(); encounter.ManualTick(.2f);
            Assert.That(encounter.IsIlluminated, Is.True);

            motor.Simulate(new FirstPersonFrame { CrouchPressed = true }, .02f);
            for (int i = 0; i < 20; i++) motor.Simulate(default, .02f);
            Assert.That(motor.IsCrouched, Is.True);
            Assert.That(motor.CrouchBlend, Is.EqualTo(1f).Within(.01f));
            Physics.SyncTransforms(); encounter.ManualTick(.05f);
            Assert.That(encounter.IsIlluminated, Is.False);

            motor.Simulate(new FirstPersonFrame { CrouchPressed = true }, .02f);
            for (int i = 0; i < 20; i++) motor.Simulate(default, .02f);
            Assert.That(motor.IsCrouched, Is.False);
            Assert.That(motor.CrouchBlend, Is.EqualTo(0f).Within(.01f));
            Physics.SyncTransforms(); encounter.ManualTick(.05f);
            Assert.That(encounter.IsIlluminated, Is.True);
        }
        [Test]
        public void SweepWithPitchedFlashlightKeepsConstantPitchAndZeroRoll()
        {
            Vector3 origin = new Vector3(900, 0, 0);
            Box(origin + Vector3.down * .25f, new Vector3(30, .5f, 30));
            var actor = New("player"); actor.transform.position = origin + Vector3.back * 5;
            var camera = New("camera"); camera.transform.SetParent(actor.transform); camera.AddComponent<Camera>();
            actor.AddComponent<CharacterController>(); var motor = actor.AddComponent<FirstPersonMotor>();
            var post = New("post").transform; post.position = origin + Vector3.up * .12f; post.rotation = Quaternion.Euler(0, -90, 0);
            var emitter = New("light").transform; emitter.SetParent(post, false);
            emitter.localRotation = Quaternion.Euler(11.5f, 0, 0);
            var checkpoint = New("checkpoint").transform; checkpoint.position = origin + Vector3.right * 5;
            var encounter = New("encounter").AddComponent<ChapterTwoSpotlightEncounter>();
            encounter.AutoTick = false; encounter.RenderFootprints = false;
            encounter.Configure(motor, new[] { emitter }, new[] { checkpoint });
            const float yawRange = 27f;
            const float period = 4f;
            encounter.ConfigureSweep(0, yawRange, period);

            float minYaw = float.MaxValue, maxYaw = float.MinValue;
            for (int step = 0; step <= 20; step++)
            {
                if (step > 0) encounter.ManualTick(period / 20f);
                Vector3 euler = emitter.localEulerAngles;
                float pitch = Mathf.DeltaAngle(0f, euler.x);
                float yaw = Mathf.DeltaAngle(0f, euler.y);
                float roll = Mathf.DeltaAngle(0f, euler.z);
                Assert.That(pitch, Is.EqualTo(11.5f).Within(.01f));
                Assert.That(roll, Is.EqualTo(0f).Within(.01f));
                Assert.That((emitter.localRotation * Vector3.right).y, Is.EqualTo(0f).Within(.001f));
                minYaw = Mathf.Min(minYaw, yaw);
                maxYaw = Mathf.Max(maxYaw, yaw);
            }
            Assert.That(minYaw, Is.EqualTo(-yawRange).Within(.05f));
            Assert.That(maxYaw, Is.EqualTo(yawRange).Within(.05f));
        }
        [Test]
        public void IsFloorPointVisibleHandlesElevatedSidewalkAndRejectsCrateTop()
        {
            Vector3 origin = new Vector3(1000, 0, 0);
            Box(origin + Vector3.down * .25f, new Vector3(30, .5f, 30));
            Box(origin + new Vector3(1.5f, -.06f, 5f), new Vector3(2f, .36f, 6f));
            Box(origin + new Vector3(-1.5f, .5f, 5f), new Vector3(1.5f, 1f, 1.5f));
            var actor = New("player"); actor.transform.position = origin + Vector3.back * 5;
            var camera = New("camera"); camera.transform.SetParent(actor.transform); camera.AddComponent<Camera>();
            actor.AddComponent<CharacterController>(); var motor = actor.AddComponent<FirstPersonMotor>();
            var emitter = New("light").transform; emitter.position = origin + Vector3.up * 2f;
            emitter.localRotation = Quaternion.Euler(11.5f, 0, 0);
            var checkpoint = New("checkpoint").transform; checkpoint.position = origin + Vector3.right * 5;
            var encounter = New("encounter").AddComponent<ChapterTwoSpotlightEncounter>();
            encounter.AutoTick = false; encounter.RenderFootprints = false;
            encounter.Configure(motor, new[] { emitter }, new[] { checkpoint });
            Physics.SyncTransforms();

            Assert.That(encounter.IsFloorPointVisible(0, origin + new Vector3(1.5f, .12f, 5f)), Is.True);
            Assert.That(encounter.IsFloorPointVisible(0, origin + new Vector3(-1.5f, 1f, 5f)), Is.False);
        }
    }
}
