using System.Collections;
using NoRainbowAfter.AI;
using NoRainbowAfter.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NoRainbowAfter.Tests
{
    public sealed class ChapterTwoTimedRouteTests
    {
        static readonly Vector2[] Approach =
        {
            new Vector2(-4.25f, 4), new Vector2(-4.25f, 18), new Vector2(-4.1f, 23)
        };

        static readonly Vector2[] Center =
        {
            new Vector2(-3.1f, 25), new Vector2(-3.1f, 32), new Vector2(0, 33),
            new Vector2(0, 37.8f), new Vector2(2.5f, 38)
        };

        static readonly Vector2[] Sidewalk =
        {
            new Vector2(3.2f, 23), new Vector2(6.3f, 25.4f), new Vector2(6.4f, 33.55f),
            new Vector2(3.3f, 34.65f), new Vector2(3.2f, 36), new Vector2(2.5f, 38)
        };

        [UnityTest]
        public IEnumerator BothRoutesReachTheShelterWithCaptureEnabled()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity");
            yield return null;
            var player = Object.FindAnyObjectByType<FirstPersonPlayer>();
            var motor = player.GetComponent<FirstPersonMotor>();
            player.enabled = false;
            Time.timeScale = 1f;
            var prefab = Resources.Load<GameObject>("ChapterTwoStreet");

            int attempt = 0;
            foreach (float duration in new[] { 2f, 1.5f })
            foreach (var route in new[] { Center, Sidewalk })
            {
                attempt++;
                var street = Object.Instantiate(prefab).GetComponent<ChapterTwoStreet>();
                Assert.IsTrue(street.Enter(motor));
                var threat = street.Encounter;
                Assert.AreEqual(2f, threat.CatchSeconds, .001f, "Default exposure threshold must remain 2.0 s");
                threat.AutoTick = false;
                threat.RenderFootprints = false;
                threat.CatchSeconds = duration;
                var segPeak = new float[3];
                float totalTime = 0f;
                // Each timed attempt begins at the same sweep phase and with full stamina.
                for (int step = 0; step < 150; step++) motor.Simulate(default, .04f);
                Follow(Approach, street, motor, segPeak, ref totalTime);
                Follow(route, street, motor, segPeak, ref totalTime);
                Assert.IsTrue(street.AtShelter);
                Assert.AreEqual(0, threat.CatchCount);
                float maxPeak = Mathf.Max(segPeak[0], Mathf.Max(segPeak[1], segPeak[2]));
                Debug.Log($"[C2 route] Attempt #{attempt} ({(route == Center ? "center" : "sidewalk")}, {duration:F1} s): " +
                          $"time={totalTime:F2}s, peakA={segPeak[0]:F2}s, peakB={segPeak[1]:F2}s, peakC={segPeak[2]:F2}s, " +
                          $"maxPeak={maxPeak:F2}s, margin={duration - maxPeak:F2}s");
                Object.Destroy(street.gameObject);
                yield return null;
            }
        }

        static void Follow(Vector2[] waypoints, ChapterTwoStreet street, FirstPersonMotor motor, float[] segPeak, ref float totalTime)
        {
            foreach (Vector2 waypoint in waypoints)
            {
                var target = street.transform.TransformPoint(new Vector3(waypoint.x, 0, waypoint.y));
                float elapsed = 0f;
                while (true)
                {
                    Vector3 delta = Vector3.ProjectOnPlane(target - motor.transform.position, Vector3.up);
                    if (delta.magnitude <= .3f) break;
                    Assert.That(elapsed, Is.LessThan(20f), "Blocked on the way to " + waypoint);
                    Vector3 local = motor.transform.InverseTransformDirection(delta.normalized);
                    motor.Simulate(new FirstPersonFrame { Move = new Vector2(local.x, local.z), SprintHeld = true }, .04f);
                    if (!street.AtShelter) street.Encounter.ManualTick(.04f);
                    street.CheckProgress();
                    int seg = Mathf.Clamp(street.Segment, 0, segPeak.Length - 1);
                    segPeak[seg] = Mathf.Max(segPeak[seg], street.Encounter.Exposure);
                    Assert.AreEqual(0, street.Encounter.CatchCount, "Caught on the way to " + waypoint);
                    elapsed += .04f;
                    totalTime += .04f;
                }
            }
        }
    }
}
