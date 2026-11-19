using System.Collections;
using System.Linq;
using NoRainbowAfter.AI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NoRainbowAfter.Tests
{
    public class ShadowStrideTests
    {
        [UnityTest]
        public IEnumerator VisibleRigPlantsAlternatingFeetDuringPursuitAndSearch()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity");
            var actor = Object.FindAnyObjectByType<ShadowEncounterController>();
            actor.enabled = false;
            var visual = actor.GetComponent<ShadowPlaceholderVisual>();
            visual.SetVisible(true);
            var rig = actor.GetComponentInChildren<ShadowMotionPlayback>();
            var feet = new[] { "Foot.L", "Foot.R" }.Select(name =>
                rig.GetComponentsInChildren<Transform>().Single(t => t.name == name)).ToArray();

            foreach (bool searching in new[] { false, true })
            {
                actor.transform.SetPositionAndRotation(new Vector3(0f, 0f, -30f), Quaternion.identity);
                visual.ResetPostureImmediate();
                float dt = searching ? 1f / 30f : 1f / 60f;
                float speed = searching ? 1.1f : 2.15f;
                var rest = feet.Select(foot => foot.position.y).ToArray();
                var previous = feet.Select(foot => foot.position).ToArray();
                var plantedTime = new float[2];
                var longestPlant = new float[2];
                var maxLift = new float[2];
                for (float t = 0f; t < 3.5f; t += dt)
                {
                    actor.transform.position += Vector3.forward * (speed * dt);
                    visual.TickVisual(ShadowEncounterState.Pursue, dt, speed / 2.35f, 0f, null, searching);
                    for (int i = 0; i < feet.Length; i++)
                    {
                        Vector3 position = feet[i].position;
                        bool planted = t > .25f && Vector3.Distance(position, previous[i]) < .04f * dt;
                        plantedTime[i] = planted ? plantedTime[i] + dt : 0f;
                        longestPlant[i] = Mathf.Max(longestPlant[i], plantedTime[i]);
                        maxLift[i] = Mathf.Max(maxLift[i], position.y - rest[i]);
                        previous[i] = position;
                    }
                }
                for (int i = 0; i < feet.Length; i++)
                {
                    Assert.That(longestPlant[i], Is.GreaterThan(.25f), feet[i].name + " skated during support");
                    Assert.That(maxLift[i], Is.InRange(.05f, .2f), feet[i].name + " swing clearance");
                }
                Assert.That(maxLift[0], Is.GreaterThan(maxLift[1] + .025f), "The trailing foot should skim lower.");
                for (int i = 0; i < 20; i++)
                    visual.TickVisual(ShadowEncounterState.Pursue, dt, 0f, 0f, null, searching);
                for (int i = 0; i < feet.Length; i++)
                    Assert.That(feet[i].position.y, Is.EqualTo(rest[i]).Within(.005f), "Settle after stopping");
            }

            // A waypoint can change direction in one tick while the body is still turning.
            visual.ResetPostureImmediate();
            const float turnDt = 1f / 60f;
            var lastPosition = feet.Select(foot => foot.position).ToArray();
            for (int frame = 0; frame < 90; frame++)
            {
                Vector3 direction = frame < 39 ? Vector3.forward : Vector3.right;
                actor.transform.rotation = Quaternion.RotateTowards(actor.transform.rotation,
                    Quaternion.LookRotation(direction), 320f * turnDt);
                actor.transform.position += direction * (2.15f * turnDt);
                visual.TickVisual(ShadowEncounterState.Pursue, turnDt, 1f, 0f);
                for (int i = 0; i < feet.Length; i++)
                {
                    Assert.That(Vector3.Distance(lastPosition[i], feet[i].position), Is.LessThan(.18f),
                        feet[i].name + " jumped at frame " + frame);
                    lastPosition[i] = feet[i].position;
                }
            }
        }
    }
}
