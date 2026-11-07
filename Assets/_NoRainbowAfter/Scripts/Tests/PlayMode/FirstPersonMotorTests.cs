using System.Collections.Generic;
using NoRainbowAfter.Player;
using NUnit.Framework;
using UnityEngine;

namespace NoRainbowAfter.Tests
{
    public sealed class FirstPersonMotorTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private readonly Vector3 origin = new Vector3(500, 0, 0);

        [SetUp]
        public void SetUp() => Box("TestFloor", origin + Vector3.down * 0.25f, new Vector3(200, 0.5f, 200));

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in objects) if (obj != null) Object.DestroyImmediate(obj);
            objects.Clear();
        }

        [Test]
        public void RestoreRejectsBlockedSpawnAndResetsPoseAtClearSpawn()
        {
            var motor = Actor(origin + Vector3.forward * 5);
            var block = Box("SpawnBlock", origin + Vector3.up, new Vector3(1, 2, 1));
            Physics.SyncTransforms();
            Vector3 before = motor.transform.position;
            Assert.That(motor.TryRestorePose(origin, Quaternion.identity), Is.False);
            Assert.That(motor.transform.position, Is.EqualTo(before));
            Object.DestroyImmediate(block);
            Physics.SyncTransforms();
            motor.Simulate(new FirstPersonFrame { CrouchPressed = true, Look = new Vector2(50, 80) }, .02f);
            Assert.That(motor.TryRestorePose(origin, Quaternion.Euler(0, 10, 0)), Is.True);
            Assert.That(motor.transform.position, Is.EqualTo(origin));
            Assert.That(motor.Pitch, Is.Zero);
            Assert.That(motor.IsCrouched, Is.False);
            Assert.That(motor.GetComponent<CharacterController>().height, Is.EqualTo(1.8f).Within(.01f));
        }

        [Test]
        public void CharacterControllerStopsAtWallAndStaysOnFloor()
        {
            var motor = Actor(origin);
            Box("TestWall", origin + new Vector3(0, 1, 3), new Vector3(10, 2, 0.2f));
            Physics.SyncTransforms();
            Tick(motor, new FirstPersonFrame { Move = Vector2.up }, 150);
            Assert.That(motor.transform.position.z, Is.InRange(2.5f, 2.7f));
            Assert.That(motor.transform.position.y, Is.InRange(-0.06f, 0.06f));
        }

        [Test]
        public void DiagonalMovementHasSameSpeedAsForward()
        {
            var forward = Actor(origin);
            var diagonal = Actor(origin + Vector3.right * 10);
            Tick(forward, new FirstPersonFrame { Move = Vector2.up }, 100);
            Tick(diagonal, new FirstPersonFrame { Move = Vector2.one }, 100);
            var a = forward.transform.position - origin; a.y = 0;
            var b = diagonal.transform.position - origin - Vector3.right * 10; b.y = 0;
            Assert.That(a.magnitude, Is.EqualTo(b.magnitude).Within(0.02f));
        }

        [Test]
        public void SprintLastsFiveSecondsThenWalksAndRecharges()
        {
            var motor = Actor(origin);
            Tick(motor, new FirstPersonFrame { Move = Vector2.up, SprintHeld = true }, 250);
            Assert.That(motor.StaminaSeconds, Is.Zero.Within(0.01f));
            Tick(motor, new FirstPersonFrame { Move = Vector2.up, SprintHeld = true }, 10);
            Assert.That(motor.IsSprinting, Is.False);
            Assert.That(motor.StaminaSeconds, Is.Zero.Within(0.01f));
            Tick(motor, default, 270);
            Assert.That(motor.StaminaSeconds, Is.EqualTo(5).Within(0.01f));
            Tick(motor, new FirstPersonFrame { Move = Vector2.up, SprintHeld = true }, 10);
            Assert.That(motor.IsSprinting, Is.True);
        }

        [Test]
        public void HoldingSprintWhileIdleDoesNotUseStamina()
        {
            var motor = Actor(origin);
            Tick(motor, new FirstPersonFrame { SprintHeld = true }, 350);
            Assert.That(motor.StaminaSeconds, Is.EqualTo(5).Within(0.001f));
            Assert.That(motor.IsSprinting, Is.False);
        }

        [Test]
        public void CrouchCannotStandInsideLowRoofAndRecoversAfterExit()
        {
            var motor = Actor(origin);
            var cc = motor.GetComponent<CharacterController>();
            motor.Simulate(new FirstPersonFrame { CrouchPressed = true }, 0.02f);
            Tick(motor, default, 30);
            Box("LowRoof", origin + new Vector3(0, 1.5f, 0), new Vector3(4, 0.5f, 4));
            Physics.SyncTransforms();
            motor.Simulate(new FirstPersonFrame { CrouchPressed = true }, 0.02f);
            Tick(motor, default, 30);
            Assert.That(cc.height, Is.EqualTo(1.1f).Within(0.01f));
            Assert.That(motor.IsCrouched, Is.True);
            Assert.That(motor.CanStand(), Is.False);
            cc.enabled = false;
            motor.transform.position = origin + Vector3.forward * 6;
            cc.enabled = true;
            Physics.SyncTransforms();
            Tick(motor, default, 30);
            Assert.That(cc.height, Is.EqualTo(1.8f).Within(0.01f));
            Assert.That(motor.IsCrouched, Is.False);
            Assert.That(motor.transform.position.y, Is.InRange(-0.06f, 0.06f));
        }

        [Test]
        public void StandingDoesNotAutomaticallyCrouchIntoLowPassage()
        {
            var motor = Actor(origin);
            Box("LowPassage", origin + new Vector3(0, 1.4f, 3), new Vector3(3, 0.3f, 4));
            Physics.SyncTransforms();
            Tick(motor, new FirstPersonFrame { Move = Vector2.up }, 100);
            Assert.That(motor.IsCrouched, Is.False);
            Assert.That(motor.transform.position.z, Is.LessThan(1f));
            motor.Simulate(new FirstPersonFrame { CrouchPressed = true }, 0.02f);
            Tick(motor, new FirstPersonFrame { Move = Vector2.up }, 250);
            Assert.That(motor.transform.position.z, Is.GreaterThan(5f));
        }

        [Test]
        public void MouseDeltaIsFrameRateIndependentAndPitchIsClamped()
        {
            var a = Actor(origin);
            var b = Actor(origin + Vector3.right * 10);
            var frame = new FirstPersonFrame { Look = new Vector2(100, 5000) };
            a.Simulate(frame, 0.01f);
            b.Simulate(frame, 0.04f);
            Assert.That(a.transform.eulerAngles.y, Is.EqualTo(8).Within(0.01f));
            Assert.That(b.transform.eulerAngles.y, Is.EqualTo(a.transform.eulerAngles.y).Within(0.01f));
            Assert.That(a.Pitch, Is.EqualTo(-85).Within(0.01f));
            Assert.That(a.ViewCamera.transform.localEulerAngles.z, Is.Zero.Within(0.01f));
        }

        [Test]
        public void EyeStaysFirstPersonWithoutHeadbobAndViewSettingsAreBounded()
        {
            var motor = Actor(origin);
            Tick(motor, new FirstPersonFrame { Move = Vector2.up }, 100);
            Assert.That(motor.ViewCamera.transform.parent, Is.EqualTo(motor.transform));
            Assert.That(motor.ViewCamera.transform.localPosition, Is.EqualTo(new Vector3(0, 1.65f, 0)));
            motor.SetViewSettings(-10, 500);
            Assert.That(motor.MouseSensitivity, Is.EqualTo(0.01f));
            Assert.That(motor.ViewCamera.fieldOfView, Is.EqualTo(95));
        }

        private FirstPersonMotor Actor(Vector3 position)
        {
            var root = new GameObject("TestPlayer"); objects.Add(root);
            root.SetActive(false); root.transform.position = position;
            var cc = root.AddComponent<CharacterController>();
            cc.radius = 0.28f; cc.skinWidth = 0.025f; cc.stepOffset = 0.25f; cc.minMoveDistance = 0;
            var camera = new GameObject("TestEye", typeof(Camera));
            camera.GetComponent<Camera>().enabled = false;
            camera.transform.SetParent(root.transform, false);
            var motor = root.AddComponent<FirstPersonMotor>();
            root.SetActive(true);
            Physics.SyncTransforms();
            return motor;
        }

        private GameObject Box(string name, Vector3 position, Vector3 size)
        {
            var obj = new GameObject(name); objects.Add(obj);
            obj.transform.position = position;
            obj.AddComponent<BoxCollider>().size = size;
            Physics.SyncTransforms();
            return obj;
        }

        private static void Tick(FirstPersonMotor motor, FirstPersonFrame input, int count)
        {
            for (int i = 0; i < count; i++) motor.Simulate(input, 0.02f);
        }
    }
}
