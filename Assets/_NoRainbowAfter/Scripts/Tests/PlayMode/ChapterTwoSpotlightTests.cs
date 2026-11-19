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
            encounter.Configure(motor, new[] { e1, e2 }, null, new[] { checkpoint });
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
            encounter.Configure(motor, new[] { emitter }, null, new[] { checkpoint }); encounter.CatchSeconds = duration;
            Physics.SyncTransforms();
            encounter.ManualTick(duration - .01f); encounter.ManualTick(0f);
            Assert.That(encounter.CatchCount, Is.Zero);
            Assert.That(encounter.Exposure, Is.EqualTo(duration - .01f).Within(.001f));
            encounter.ManualTick(.011f);
            Assert.That(encounter.CatchCount, Is.EqualTo(1));
            Assert.That(encounter.Exposure, Is.Zero);
        }
    }
}
