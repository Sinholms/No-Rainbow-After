using System.Collections;
using System.Linq;
using NoRainbowAfter.AI;
using NoRainbowAfter.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NoRainbowAfter.Tests
{
    public sealed class ChapterTwoRouteTests
    {
        [UnityTest]
        public IEnumerator TwoStationaryPostsHaveNoRearRouteAndStreetCanBeCrossed()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity");
            yield return null;
            var player = Object.FindAnyObjectByType<FirstPersonPlayer>();
            var motor = player.GetComponent<FirstPersonMotor>();
            player.enabled = false;
            player.SetPaused(false);
            Time.timeScale = 1f;
            motor.MovementScale = .75f;
            var prefab = Resources.Load<GameObject>("ChapterTwoStreet");
            Assert.That(prefab, Is.Not.Null);
            var street = Object.Instantiate(prefab).GetComponent<ChapterTwoStreet>();
            Assert.That(street.Enter(motor), Is.True);
            var encounter = street.Encounter;
            encounter.AutoTick = false;
            encounter.RenderFootprints = false;

            Assert.That(street.Lamps.Length, Is.EqualTo(2));
            Assert.That(street.Lamps.Any(lamp => lamp.IsChildOf(street.PatrolCar)), Is.False);
            Assert.That(street.PatrolCar.GetComponentsInChildren<Light>().Length, Is.EqualTo(2));
            foreach (var lamp in street.Lamps)
            {
                Vector3 fixedPost = lamp.parent.position;
                var head = street.GazePivots.Single(pivot => pivot.IsChildOf(lamp.parent));
                Quaternion initialGaze = head.rotation;
                Quaternion initialAim = lamp.localRotation;
                encounter.ManualTick(1f);
                Assert.That(lamp.parent.position, Is.EqualTo(fixedPost));
                Assert.That(Quaternion.Angle(initialAim, lamp.localRotation), Is.GreaterThan(.1f));
                Assert.That(Quaternion.Angle(initialGaze, head.rotation), Is.GreaterThan(.1f));
                Assert.That(lamp.parent.GetComponentsInChildren<MeshRenderer>().Length, Is.GreaterThan(15));

                Vector3 pos = street.transform.InverseTransformPoint(fixedPost);
                Vector3 beforeFence = new Vector3(Mathf.Sign(pos.x) * 6.28f, .16f, pos.z - 1.25f);
                Assert.That(motor.TryRestorePose(street.transform.TransformPoint(beforeFence), Quaternion.identity, false), Is.True);
                for (int i = 0; i < 65; i++)
                    motor.Simulate(new FirstPersonFrame { Move = Vector2.up, CrouchPressed = i == 0 }, .033f);
                Assert.That(street.transform.InverseTransformPoint(motor.transform.position).z,
                    Is.LessThan(pos.z - .22f), "Colliders must stop the player behind " + lamp.parent.name);
            }

            Assert.That(encounter.TryRestoreCheckpoint(), Is.True);
            encounter.ManualTick(.02f);
            var first = street.Lamps[0];
            Vector3 aim = Quaternion.AngleAxis(15f, Vector3.up) * Vector3.ProjectOnPlane(first.forward, Vector3.up).normalized;
            Vector3 exposed = first.position + aim * 1.95f;
            exposed.y = street.transform.position.y + .04f;
            string situation = DescribePose(exposed);
            Assert.That(motor.TryRestorePose(exposed, Quaternion.identity, false), Is.True, situation + " emitter " + first.position.ToString("F3"));
            encounter.ManualTick(.15f);
            Assert.That(encounter.IsIlluminated, Is.True);
            float exposureBeforePause = encounter.Exposure;
            encounter.AutoTick = true;
            player.SetPaused(true);
            yield return null;
            Assert.That(encounter.Exposure, Is.EqualTo(exposureBeforePause));
            player.SetPaused(false);
            encounter.AutoTick = false;
            for (int i = 0; i < 100 && encounter.CatchCount == 0; i++) encounter.ManualTick(.033f);
            Assert.That(encounter.CatchCount, Is.EqualTo(1));
            Assert.That(Vector3.Distance(motor.transform.position, street.Checkpoints[0].position), Is.LessThan(.05f));

            Vector2[] waypoints =
            {
                new(-3.55f, 10.9f), new(3.85f, 11.1f), new(3.85f, 21.9f),
                new(-3.82f, 21.9f), new(-3.82f, 42.9f), new(5.42f, 42.9f), new(5.42f, 38.6f)
            };
            foreach (var waypoint in waypoints)
            {
                Assert.That(MoveTo(waypoint, street, motor), Is.True,
                    $"Caught or blocked toward {waypoint} at {street.transform.InverseTransformPoint(motor.transform.position)}. Total catches: {encounter.CatchCount}");
                yield return null;
            }

            Assert.That(street.AtShelter, Is.True);
            // The ribbon runs wall-to-wall beyond the stop, including both pavements.
            Transform cordon = street.transform.Find("Jalan/Garis polisi jalan raya");
            Assert.That(cordon, Is.Not.Null);
            foreach (float x in new[] { -6.49f, 0f, 6.49f })
            {
                Vector3 start = cordon.TransformPoint(new Vector3(x, .19f, -1.4f));
                Assert.That(motor.TryRestorePose(start, cordon.rotation, false), Is.True);
                for (int i = 0; i < 80; i++)
                    motor.Simulate(new FirstPersonFrame { Move = Vector2.up, CrouchPressed = x < 0 && i == 0 }, .033f);
                Assert.That(cordon.InverseTransformPoint(motor.transform.position).z,
                    Is.InRange(-.65f, -.12f), "Police line at x=" + x);
            }
            Assert.That(street.TryRestoreCheckpoint(), Is.True);
            Assert.That(street.TryBoard(), Is.True);
            Assert.That(street.TryBoard(), Is.False);
            Assert.That(Vector3.Distance(motor.transform.position, street.BusSeat.position), Is.LessThan(.05f));
            Object.Destroy(street.gameObject);
        }

        private static string DescribePose(Vector3 feet)
        {
            const float radius = .24f;
            var blockers = Physics.OverlapCapsule(feet + Vector3.up * (radius + .05f), feet + Vector3.up * (1.7f - radius),
                radius, ~0, QueryTriggerInteraction.Ignore);
            return feet.ToString("F3") + " overlaps " + string.Join(", ", blockers.Select(c => c.name + " " + c.bounds));
        }

        private static bool MoveTo(Vector2 waypoint, ChapterTwoStreet street, FirstPersonMotor motor)
        {
            Vector3 target = street.transform.TransformPoint(new Vector3(waypoint.x, 0f, waypoint.y));
            int catches = street.Encounter.CatchCount;
            for (int step = 0; step < 420; step++)
            {
                Vector3 delta = Vector3.ProjectOnPlane(target - motor.transform.position, Vector3.up);
                if (delta.magnitude < .21f) return true;
                Vector3 input = motor.transform.InverseTransformDirection(delta.normalized);
                motor.Simulate(new FirstPersonFrame { Move = new Vector2(input.x, input.z) }, .033f);
                if (street.Encounter.enabled) street.Encounter.ManualTick(.033f);
                if (street.Encounter.CatchCount != catches) return false;
                street.CheckProgress();
            }
            return false;
        }
    }
}
