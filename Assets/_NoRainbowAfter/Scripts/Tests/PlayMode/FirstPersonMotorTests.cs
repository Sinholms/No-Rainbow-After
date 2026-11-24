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
            Assert.That(motor.GetComponent<CharacterController>().height, Is.EqualTo(1.7f).Within(.01f));
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
            Assert.That(motor.transform.position.z - origin.z, Is.InRange(16.5f, 17.05f), "Five seconds of the slower sprint should cover about 17 metres.");
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
            Assert.That(cc.height, Is.EqualTo(1.7f).Within(0.01f));
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
            Assert.That(motor.ViewCamera.transform.localPosition, Is.EqualTo(new Vector3(0, 1.58f, 0)));
            motor.SetViewSettings(-10, 500);
            Assert.That(motor.MouseSensitivity, Is.EqualTo(0.01f));
            Assert.That(motor.ViewCamera.fieldOfView, Is.EqualTo(95));
        }

        [Test]
        public void StressGaitIncreasesSwayAndHeadDroopAndSlowsWalking()
        {
            var noBarometer = Actor(origin);
            var zeroStress = ActorWithStress(origin + Vector3.right * 10f, 0f);
            var lowStress = ActorWithStress(origin + Vector3.right * 20f, 30f);
            var highStress85 = ActorWithStress(origin + Vector3.right * 30f, 85f);
            var highStress95 = ActorWithStress(origin + Vector3.right * 40f, 95f);

            var walkFrame = new FirstPersonFrame { Move = Vector2.up };
            var noBarometerMetrics = SimulateWalkAndMeasure(noBarometer, walkFrame, 120);
            var zeroStressMetrics = SimulateWalkAndMeasure(zeroStress, walkFrame, 120);
            var lowStressMetrics = SimulateWalkAndMeasure(lowStress, walkFrame, 120);
            var high85Metrics = SimulateWalkAndMeasure(highStress85, walkFrame, 120);
            var high95Metrics = SimulateWalkAndMeasure(highStress95, walkFrame, 120);

            Assert.That(noBarometerMetrics.MaxAbsRoll, Is.Zero);
            Assert.That(noBarometerMetrics.MaxHeadDroop, Is.Zero);
            Assert.That(noBarometerMetrics.MaxPitchOffset, Is.Zero);
            Assert.That(noBarometer.ViewCamera.transform.localPosition, Is.EqualTo(new Vector3(0f, 1.58f, 0f)));

            Assert.That(zeroStressMetrics.MaxAbsRoll, Is.Zero);
            Assert.That(zeroStressMetrics.MaxHeadDroop, Is.Zero);
            Assert.That(zeroStressMetrics.MaxPitchOffset, Is.Zero);
            Assert.That(zeroStressMetrics.Distance, Is.EqualTo(noBarometerMetrics.Distance).Within(0.001f));

            Assert.That(lowStressMetrics.MaxAbsRoll, Is.Zero);
            Assert.That(lowStressMetrics.MaxHeadDroop, Is.Zero);
            Assert.That(lowStressMetrics.MaxPitchOffset, Is.Zero);
            Assert.That(lowStressMetrics.Distance, Is.EqualTo(noBarometerMetrics.Distance).Within(0.001f));

            Assert.That(high85Metrics.MaxAbsRoll, Is.GreaterThan(1f));
            Assert.That(high95Metrics.MaxAbsRoll, Is.GreaterThan(high85Metrics.MaxAbsRoll));
            Assert.That(high85Metrics.MaxHeadDroop, Is.GreaterThan(0.01f));
            Assert.That(high95Metrics.MaxHeadDroop, Is.GreaterThan(high85Metrics.MaxHeadDroop));
            Assert.That(high85Metrics.BobRange, Is.GreaterThan(0.005f));
            Assert.That(high95Metrics.BobRange, Is.GreaterThan(0.005f));
            Assert.That(high85Metrics.MaxPitchOffset, Is.GreaterThan(0.5f));
            Assert.That(high95Metrics.MaxPitchOffset, Is.GreaterThan(high85Metrics.MaxPitchOffset));
            Assert.That(high85Metrics.Distance, Is.LessThan(lowStressMetrics.Distance - 0.5f));
            Assert.That(high95Metrics.Distance, Is.LessThan(high85Metrics.Distance - 0.1f));
        }

        [Test]
        public void HighStressNarrowsFieldOfViewAndStopOrRestoreClearsStressOffsets()
        {
            var motor = ActorWithStress(origin, 90f);
            float baseFov = motor.FieldOfView;

            Tick(motor, new FirstPersonFrame { Move = Vector2.up }, 60);
            Assert.That(motor.ViewCamera.fieldOfView, Is.LessThan(baseFov - 1.5f));
            Assert.That(motor.StressPitchOffset, Is.GreaterThan(0f));
            Assert.That(motor.StressEyeOffset.y, Is.LessThan(0f));

            motor.StopMotion();
            Assert.That(motor.StressRollOffset, Is.Zero);
            Assert.That(motor.StressPitchOffset, Is.Zero);
            Assert.That(motor.StressEyeOffset, Is.EqualTo(Vector3.zero));
            Assert.That(motor.ViewCamera.fieldOfView, Is.EqualTo(baseFov));
            Assert.That(motor.ViewCamera.transform.localPosition, Is.EqualTo(new Vector3(0f, 1.58f, 0f)));
            Assert.That(motor.ViewCamera.transform.localRotation, Is.EqualTo(Quaternion.identity));

            Tick(motor, new FirstPersonFrame { Move = Vector2.up, Look = new Vector2(10f, -20f) }, 60);
            Assert.That(motor.ViewCamera.fieldOfView, Is.LessThan(baseFov - 1.5f));
            Assert.That(motor.StressPitchOffset, Is.GreaterThan(0f));
            Assert.That(motor.StressEyeOffset.y, Is.LessThan(0f));

            Assert.That(motor.TryRestorePose(origin + Vector3.forward * 2f, Quaternion.identity), Is.True);
            Assert.That(motor.StressRollOffset, Is.Zero);
            Assert.That(motor.StressPitchOffset, Is.Zero);
            Assert.That(motor.StressEyeOffset, Is.EqualTo(Vector3.zero));
            Assert.That(motor.ViewCamera.fieldOfView, Is.EqualTo(baseFov));
            Assert.That(motor.ViewCamera.transform.localPosition, Is.EqualTo(new Vector3(0f, 1.58f, 0f)));
            Assert.That(motor.ViewCamera.transform.localRotation, Is.EqualTo(Quaternion.identity));
        }

        [Test]
        public void SprintStaminaDrainsFasterAtHighStress()
        {
            var lowStressMotor = ActorWithStress(origin, 30f);
            var highStressMotor = ActorWithStress(origin + Vector3.right * 10f, 90f);
            var sprintFrame = new FirstPersonFrame { Move = Vector2.up, SprintHeld = true };

            Tick(lowStressMotor, sprintFrame, 150);
            Tick(highStressMotor, sprintFrame, 150);
            Assert.That(lowStressMotor.IsSprinting, Is.True);
            Assert.That(highStressMotor.IsSprinting, Is.True);
            Assert.That(highStressMotor.StaminaSeconds, Is.LessThan(lowStressMotor.StaminaSeconds - 0.5f));

            Tick(lowStressMotor, sprintFrame, 60);
            Tick(highStressMotor, sprintFrame, 60);
            Assert.That(highStressMotor.StaminaSeconds, Is.Zero.Within(0.001f));
            Assert.That(highStressMotor.IsSprinting, Is.False);
            Assert.That(lowStressMotor.StaminaSeconds, Is.GreaterThan(0.5f));
            Assert.That(lowStressMotor.IsSprinting, Is.True);
        }

        private FirstPersonMotor ActorWithStress(Vector3 position, float stressLevel)
        {
            var motor = Actor(position);
            var barometer = motor.gameObject.AddComponent<StressBarometer>();
            barometer.SetStress(stressLevel);
            return motor;
        }

        private struct WalkMetrics
        {
            public float Distance;
            public float MaxAbsRoll;
            public float MaxHeadDroop;
            public float MaxPitchOffset;
            public float BobRange;
        }

        private static WalkMetrics SimulateWalkAndMeasure(FirstPersonMotor motor, FirstPersonFrame input, int ticks)
        {
            Vector3 start = motor.transform.position;
            float maxAbsRoll = 0f;
            float maxHeadDroop = 0f;
            float maxPitch = 0f;
            float minEyeY = float.MaxValue;
            float maxEyeY = float.MinValue;

            for (int i = 0; i < ticks; i++)
            {
                motor.Simulate(input, 0.02f);
                maxAbsRoll = Mathf.Max(maxAbsRoll, Mathf.Abs(motor.StressRollOffset));
                maxHeadDroop = Mathf.Max(maxHeadDroop, -motor.StressEyeOffset.y);
                maxPitch = Mathf.Max(maxPitch, motor.StressPitchOffset);
                if (i >= 40)
                {
                    minEyeY = Mathf.Min(minEyeY, motor.StressEyeOffset.y);
                    maxEyeY = Mathf.Max(maxEyeY, motor.StressEyeOffset.y);
                }
            }

            Vector3 delta = motor.transform.position - start;
            delta.y = 0f;
            return new WalkMetrics
            {
                Distance = delta.magnitude,
                MaxAbsRoll = maxAbsRoll,
                MaxHeadDroop = maxHeadDroop,
                MaxPitchOffset = maxPitch,
                BobRange = maxEyeY > minEyeY ? maxEyeY - minEyeY : 0f
            };
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
