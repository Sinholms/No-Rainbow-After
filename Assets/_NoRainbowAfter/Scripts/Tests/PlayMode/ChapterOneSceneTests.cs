using System;
using System.Collections;
using System.Linq;
using NoRainbowAfter.Interaction;
using NoRainbowAfter.Level;
using NoRainbowAfter.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace NoRainbowAfter.Tests
{
    public sealed class ChapterOneSceneTests
    {
        private const string ScenePath = "Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity";
        private float simulationSeconds;

        [TearDown] public void ResetTime() { Time.timeScale = 1f; }

        [UnityTest]
        public IEnumerator DormCorridorBothCoverZonesAndReturnAreTraversable()
        {
            simulationSeconds = 0f;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            // This route drives the motor manually. Editor focus loss must not freeze its
            // fixed-update trigger checks; product focus/pause behavior is tested separately.
            var sessionType = Type.GetType("NoRainbowAfter.UI.FirstPersonSession, Assembly-CSharp");
            var routeSession = (Behaviour)GameObject.Find("UI_FirstPerson").GetComponent(sessionType);
            routeSession.enabled = false;
            Time.timeScale = 1f;
            var player = Object.FindAnyObjectByType<FirstPersonPlayer>();
            var motor = player.GetComponent<FirstPersonMotor>();
            player.enabled = false;
            Assert.That(GameObject.Find("Chapter1Root"), Is.Not.Null);
            Assert.That(GameObject.Find("ENV_KosAtmosphere").transform.parent.name, Is.EqualTo("Chapter1Root"));
            var zones = Object.FindObjectsByType<CoverZone>(FindObjectsInactive.Exclude);
            CollectionAssert.AreEquivalent(new[] { "C1_COVER_LEFT", "C1_COVER_RIGHT" }, zones.Select(z => z.StableId));
            var left = zones.Single(z => z.StableId == "C1_COVER_LEFT");
            var right = zones.Single(z => z.StableId == "C1_COVER_RIGHT");
            yield return Walk(motor, new Vector2(0, 6));
            yield return Walk(motor, new Vector2(0, 17.8f));
            Assert.That(motor.transform.position.z, Is.GreaterThan(17.6f));
            yield return Walk(motor, new Vector2(0, 13));
            yield return Walk(motor, new Vector2(1.45f, 13));
            yield return new WaitForFixedUpdate();
            Assert.That(right.PlayerInside, Is.True, "The controller should enter the right volume by walking.");
            yield return CheckCoverSightlines(motor, 9, 17);
            yield return Walk(motor, new Vector2(0, 13));
            yield return new WaitForFixedUpdate();
            Assert.That(right.PlayerInside, Is.False);
            yield return Walk(motor, new Vector2(0, 9));
            yield return Walk(motor, new Vector2(-1.45f, 9));
            yield return new WaitForFixedUpdate();
            Assert.That(left.PlayerInside, Is.True, "The controller should enter the left volume by walking.");
            yield return CheckCoverSightlines(motor, 5, 13);
            yield return Walk(motor, new Vector2(0, 9));
            yield return new WaitForFixedUpdate();
            Assert.That(left.PlayerInside, Is.False);
            yield return Walk(motor, new Vector2(-1.45f, 9));
            yield return new WaitForFixedUpdate();
            Assert.That(left.PlayerInside, Is.True);
            var flags = player.GetComponent<PlayerInteractor>().Flags;
            flags.Set("objective.book");
            Assert.That(player.GetComponent<DormCheckpoint>().TryRestore(), Is.True);
            Assert.That(left.PlayerInside, Is.False, "Recovery must clear stale cover occupancy immediately.");
            Assert.That(flags.Contains("objective.book"), Is.True);
            yield return Walk(motor, new Vector2(0, 6));
            yield return Walk(motor, new Vector2(.15f, 1.1f));
            Assert.That(motor.transform.position.y, Is.InRange(-.1f, .2f), "The route must retain a floor.");
            while (simulationSeconds < 300f)
            {
                yield return Walk(motor, new Vector2(0, 6), 25);
                yield return Walk(motor, new Vector2(0, 17.8f), 25);
                yield return Walk(motor, new Vector2(0, 6), 25);
                yield return Walk(motor, new Vector2(.15f, 1.1f), 25);
            }
            Debug.Log("[T04 test] Simulated route traversal: " + simulationSeconds.ToString("F1") + " seconds.");
        }

        [UnityTest]
        public IEnumerator CheckpointRestoresPoseWithoutErasingBookOrChangingInspectionAndPause()
        {
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            var player = Object.FindAnyObjectByType<FirstPersonPlayer>();
            var motor = player.GetComponent<FirstPersonMotor>();
            var interactor = player.GetComponent<PlayerInteractor>();
            var checkpoint = player.GetComponent<DormCheckpoint>();
            var book = Object.FindObjectsByType<SceneInteractable>(FindObjectsInactive.Exclude).Single(x => x.StableId == "INT_BOOK");
            player.enabled = false;
            motor.Simulate(new FirstPersonFrame { CrouchPressed = true, Look = new Vector2(0, 100) }, .02f);
            for (int i = 0; i < 35; i++) motor.Simulate(new FirstPersonFrame { Move = Vector2.up }, .02f);
            Assert.That(motor.IsCrouched, Is.True);
            Assert.That(motor.Pitch, Is.Not.Zero);
            book.Interact(interactor);
            Assert.That(interactor.Flags.Contains("objective.book"), Is.True);
            Assert.That(book.Prompt, Is.Null);
            Assert.That(player.IsInspecting, Is.True);
            Assert.That(checkpoint.TryRestore(), Is.True);
            Assert.That(player.IsInspecting, Is.True);
            Assert.That(interactor.IsInspecting, Is.True);
            interactor.CloseInspection();
            var session = GameObject.Find("UI_FirstPerson").GetComponent(Type.GetType("NoRainbowAfter.UI.FirstPersonSession, Assembly-CSharp"));
            session.GetType().GetMethod("SetPaused").Invoke(session, new object[] { true });
            Assert.That(checkpoint.TryRestore(), Is.True);
            Assert.That(player.IsPaused, Is.True);
            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
            Assert.That(interactor.Flags.Contains("objective.book"), Is.True);
            Assert.That(book.Prompt, Is.Null);
            Assert.That(motor.transform.position, Is.EqualTo(GameObject.Find("DormSpawn_T04").transform.position));
            Assert.That(motor.Pitch, Is.Zero);
            Assert.That(motor.IsCrouched, Is.False);
        }

        private IEnumerator Walk(FirstPersonMotor motor, Vector2 destination, int yieldStride = 5)
        {
            motor.StopMotion();
            for (int i = 0; i < 900; i++)
            {
                Vector3 p = motor.transform.position;
                Vector2 delta = destination - new Vector2(p.x, p.z);
                Assert.That(p.y, Is.InRange(-.12f, .3f), "Player fell below the route floor.");
                if (delta.magnitude < .13f) { motor.StopMotion(); yield break; }
                Vector3 local = motor.transform.InverseTransformDirection(new Vector3(delta.x, 0, delta.y));
                motor.Simulate(new FirstPersonFrame { Move = new Vector2(local.x, local.z).normalized }, .02f);
                simulationSeconds += .02f;
                if (i % yieldStride == 0) yield return new WaitForFixedUpdate();
            }
            Assert.Fail("Unable to walk to " + destination + "; stopped at " + motor.transform.position);
        }

        private static IEnumerator CheckCoverSightlines(FirstPersonMotor motor, float nearZ, float farZ)
        {
            motor.Simulate(new FirstPersonFrame { CrouchPressed = true }, .02f);
            for (int i = 0; i < 40; i++) motor.Simulate(default, .02f);
            Physics.SyncTransforms();
            Vector3 eye = motor.ViewCamera.transform.position;
            foreach (float z in new[] { nearZ, farZ })
            {
                Vector3 origin = new Vector3(0, eye.y, z);
                var hits = Physics.RaycastAll(origin, eye - origin, Vector3.Distance(origin, eye), ~0, QueryTriggerInteraction.Ignore);
                Assert.That(hits.Any(h => !h.collider.transform.IsChildOf(motor.transform)), Is.True,
                    "Solid corridor geometry should obstruct the crouched cover sightline from z=" + z);
            }
            motor.Simulate(new FirstPersonFrame { CrouchPressed = true }, .02f);
            for (int i = 0; i < 40; i++) motor.Simulate(default, .02f);
            yield return new WaitForFixedUpdate();
        }
    }
}
