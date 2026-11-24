using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NoRainbowAfter.AI;
using NoRainbowAfter.Interaction;
using NoRainbowAfter.Level;
using NoRainbowAfter.Narrative;
using NoRainbowAfter.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace NoRainbowAfter.Tests
{
    public sealed class ShadowEncounterTests
    {
        private const string ScenePath = "Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity";

        private string previousOverridePath;
        private string tempFolder;
        private string tempClueFile;
        private Mouse addedMouse;
        private readonly InputTestFixture inputFixture = new InputTestFixture();
        private InputSettings.BackgroundBehavior previousBackgroundBehavior;

        [SetUp]
        public void SetUpIsolation()
        {
            inputFixture.Setup();
            previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            Time.timeScale = 1f;
            previousOverridePath = ChapterDirector.StoragePathOverride;
            tempFolder = Path.Combine(Path.GetTempPath(), "NRA-T06-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempFolder);
            tempClueFile = Path.Combine(tempFolder, "clues.json");
            ChapterDirector.StoragePathOverride = tempClueFile;

            if (Mouse.current == null)
                addedMouse = InputSystem.AddDevice<Mouse>();
        }

        [TearDown]
        public void TearDownIsolation()
        {
            Time.timeScale = 1f;
            Mouse activeMouse = addedMouse ?? Mouse.current;
            if (activeMouse != null)
            {
                InputSystem.QueueStateEvent(activeMouse, new MouseState().WithButton(MouseButton.Left, false));
                InputSystem.Update();
            }

            if (addedMouse != null)
            {
                InputSystem.RemoveDevice(addedMouse);
                addedMouse = null;
            }
            InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
            inputFixture.TearDown();

            ChapterDirector.StoragePathOverride = previousOverridePath;
            if (!string.IsNullOrEmpty(tempFolder) && Directory.Exists(tempFolder))
            {
                foreach (string file in Directory.GetFiles(tempFolder))
                    File.Delete(file);
                Directory.Delete(tempFolder);
            }
        }

        [UnityTest]
        public IEnumerator FreshSession_SpawnReadability_WallBlockedClick_FightFeedback_AndSpamClickSingleResolution()
        {
            var beats = new List<string>();
            yield return EnterHorror(beats);
            var player = Object.FindAnyObjectByType<FirstPersonPlayer>();
            var motor = player.GetComponent<FirstPersonMotor>();
            var director = player.GetComponent<ChapterDirector>();
            var encounter = Object.FindAnyObjectByType<ShadowEncounterController>();
            Assert.That(encounter.State, Is.EqualTo(ShadowEncounterState.Spawn));
            Assert.That(director.State.HasClue("CLUE_02"), Is.True);
            Assert.That(beats, Is.EqualTo(new[] { "C1-01", "C1-02", "C1-03", "C1-04" }));
            Assert.That(Vector3.Distance(motor.transform.position, encounter.transform.position), Is.GreaterThan(3f));
            Assert.That(ShadowVisibility.IsLineOfSightClear(motor.ViewCamera.transform.position,
                encounter.transform.position + Vector3.up * 1.4f, motor.transform, encounter.transform), Is.True);

            var target = encounter.transform.position;
            Assert.That(motor.TryRestorePose(target + Vector3.left * 2f, Quaternion.Euler(0, 90, 0), false), Is.True);
            AimCameraAt(motor, target + Vector3.up * 1.3f);
            Physics.SyncTransforms();
            encounter.TickEncounter(0f);
            Assert.That(encounter.CanFightRightNow(), Is.True);
            Assert.That(encounter.IsFightPromptVisible, Is.True);
            Assert.That(player.enabled, Is.True);
            int fightRequests = 0;
            player.FightRequested += () => fightRequests++;
            var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                blocker.transform.position = Vector3.Lerp(motor.ViewCamera.transform.position, target + Vector3.up * 1.3f, .5f);
                blocker.transform.localScale = new Vector3(1f, 2f, 1f);
                Physics.SyncTransforms();
                encounter.TickEncounter(0f);
                Assert.That(encounter.CanFightRightNow(), Is.False);
                Assert.That(encounter.IsFightPromptVisible, Is.False);
                ClickLeftMouse();
                Assert.That(fightRequests, Is.EqualTo(1), "The blocked click must reach the player input handler.");
                Assert.That(encounter.SuccessfulHits, Is.Zero);
            }
            finally { Object.DestroyImmediate(blocker); Physics.SyncTransforms(); }
            for (int hit = 1; hit <= 3; hit++)
            {
                AimCameraAt(motor, encounter.transform.position + Vector3.up * 1.3f);
                Assert.That(encounter.CanFightRightNow(), Is.True);
                ClickLeftMouse();
                ClickLeftMouse();
                Assert.That(fightRequests, Is.EqualTo(1 + 2 * hit), "Both clicks must reach the player input handler.");
                Assert.That(encounter.SuccessfulHits, Is.EqualTo(hit), "Recoil blocks repeated clicks.");
                Assert.That(encounter.BlackoutAlpha, Is.GreaterThan(0f));
                float duration = encounter.GetComponent<ShadowPlaceholderVisual>().HitDuration(hit == 3);
                for (int step = 0; step < Mathf.CeilToInt(duration / .1f) + 1; step++) encounter.TickEncounter(.1f);
            }
            Assert.That(director.State.Chapter1Response, Is.EqualTo(EncounterResponse.Fight));
            Assert.That(beats, Is.EqualTo(new[] { "C1-01", "C1-02", "C1-03", "C1-04", "C1-05", "C1-06" }));
        }

        [UnityTest]
        public IEnumerator FreshSession_HideInEastWardrobe_ResolvesOnceAfterMonsterPasses()
        {
            yield return VerifyHideInWardrobe("Wardrobe_East");
        }

        [UnityTest]
        public IEnumerator FreshSession_HideInCornerWardrobe_ResolvesOnceAfterMonsterPasses()
        {
            yield return VerifyHideInWardrobe("Wardrobe_CornerA");
        }

        private IEnumerator VerifyHideInWardrobe(string name)
        {
            var beats = new List<string>();
            yield return EnterHorror(beats);
            var player = Object.FindAnyObjectByType<FirstPersonPlayer>();
            var motor = player.GetComponent<FirstPersonMotor>();
            var director = player.GetComponent<ChapterDirector>();
            var encounter = Object.FindAnyObjectByType<ShadowEncounterController>();
            var wardrobe = Object.FindObjectsByType<HidingWardrobe>().Single(w => w.name == name);
            Assert.That(motor.TryRestorePose(wardrobe.transform.TransformPoint(new Vector3(0, .08f, -1.35f)), wardrobe.transform.rotation, false), Is.True);
            wardrobe.Interact(player.GetComponent<PlayerInteractor>());
            Assert.That(player.IsHiding, Is.True);
            Assert.That(wardrobe.Spotted, Is.False);
            Assert.That(encounter.CanFightRightNow(), Is.False);
            yield return StepEncounterUntil(encounter, () => director.BeatIndex == 6, 12f);
            Assert.That(director.State.Chapter1Response, Is.EqualTo(EncounterResponse.Hide));
            Assert.That(beats, Is.EqualTo(new[] { "C1-01", "C1-02", "C1-03", "C1-04", "C1-05", "C1-06" }));
            Assert.That(encounter.GetComponent<CharacterController>().enabled, Is.False);
            Assert.That(wardrobe.TryExit(), Is.True);
            Assert.That(player.IsHiding, Is.False);
        }

        [UnityTest]
        public IEnumerator StandingNearWardrobeDoesNotHide_AndLeavingCancelsUnfinishedHide()
        {
            yield return EnterHorror(new List<string>());
            var player = Object.FindAnyObjectByType<FirstPersonPlayer>();
            var motor = player.GetComponent<FirstPersonMotor>();
            var director = player.GetComponent<ChapterDirector>();
            var encounter = Object.FindAnyObjectByType<ShadowEncounterController>();
            var wardrobe = Object.FindObjectsByType<HidingWardrobe>().Single(w => w.name == "Wardrobe_CornerA");
            Assert.That(motor.TryRestorePose(wardrobe.transform.TransformPoint(new Vector3(0, .08f, -1.35f)), wardrobe.transform.rotation, false), Is.True);
            yield return StepEncounterUntil(encounter, () => encounter.State == ShadowEncounterState.Pursue, 2f);
            for (int i = 0; i < 10; i++) encounter.TickEncounter(.1f);
            Assert.That(player.IsHiding, Is.False, "Standing outside does not enter a wardrobe.");
            wardrobe.Interact(player.GetComponent<PlayerInteractor>());
            Assert.That(player.IsHiding, Is.True);
            for (int i = 0; i < 40; i++) encounter.TickEncounter(.1f);
            Assert.That(wardrobe.TryExit(), Is.True);
            Assert.That(player.IsHiding, Is.False);
            encounter.TickEncounter(.1f);
            wardrobe.Interact(player.GetComponent<PlayerInteractor>());
            Assert.That(player.IsHiding, Is.True);
            Assert.That(wardrobe.Spotted, Is.False);
            for (int i = 0; i < 30; i++) encounter.TickEncounter(.1f);
            Assert.That(director.State.Chapter1Response, Is.EqualTo(EncounterResponse.None));
            Assert.That(encounter.State, Is.EqualTo(ShadowEncounterState.Pursue));
            yield return StepEncounterUntil(encounter, () => director.State.Chapter1Response == EncounterResponse.Hide, 5f);
        }

        [UnityTest]
        public IEnumerator PauseAndInspectFreezeEncounterAndBlockFight_AndCheckpointButtonAbortsCleanly()
        {
            var beats = new List<string>();
            yield return EnterHorror(beats);
            var player = Object.FindAnyObjectByType<FirstPersonPlayer>();
            var motor = player.GetComponent<FirstPersonMotor>();
            var interactor = player.GetComponent<PlayerInteractor>();
            var director = player.GetComponent<ChapterDirector>();
            var encounter = Object.FindAnyObjectByType<ShadowEncounterController>();
            var sessionType = Type.GetType("NoRainbowAfter.UI.FirstPersonSession, Assembly-CSharp");
            var session = GameObject.Find("UI_FirstPerson").GetComponent(sessionType);
            var setPaused = sessionType.GetMethod("SetPaused", BindingFlags.Instance | BindingFlags.Public);
            var restore = sessionType.GetMethod("RestoreCheckpoint", BindingFlags.Instance | BindingFlags.NonPublic);
            yield return StepEncounterUntil(encounter, () => encounter.State == ShadowEncounterState.Pursue, 2f);
            var target = encounter.transform.position;
            Assert.That(motor.TryRestorePose(target + Vector3.left * 2f, Quaternion.Euler(0, 90, 0), false), Is.True);
            AimCameraAt(motor, target + Vector3.up * 1.3f);
            Physics.SyncTransforms();
            Assert.That(encounter.CanFightRightNow(), Is.True);
            var frozen = encounter.transform.position;
            interactor.ShowInspection("Uji Inspect", "Bayangan berhenti saat inspeksi.");
            encounter.TickEncounter(.5f);
            ClickLeftMouse();
            Assert.That(encounter.CanFightRightNow(), Is.False);
            Assert.That(encounter.SuccessfulHits, Is.Zero);
            Assert.That(Vector3.Distance(frozen, encounter.transform.position), Is.LessThan(.001f));
            interactor.CloseInspection();
            setPaused.Invoke(session, new object[] { true });
            encounter.TickEncounter(.5f);
            ClickLeftMouse();
            Assert.That(encounter.CanFightRightNow(), Is.False);
            Assert.That(encounter.SuccessfulHits, Is.Zero);
            Assert.That(Vector3.Distance(frozen, encounter.transform.position), Is.LessThan(.001f));
            restore.Invoke(session, null);
            Assert.That(encounter.State, Is.EqualTo(ShadowEncounterState.Dormant));
            Assert.That(Vector3.Distance(motor.transform.position, GameObject.Find("DormSpawn_T04").transform.position), Is.LessThan(.05f));
            Assert.That(director.BeatIndex, Is.EqualTo(4));
            Assert.That(director.State.HasClue("CLUE_02"), Is.True);
            setPaused.Invoke(session, new object[] { false });
            Assert.That(encounter.IsReady, Is.False, "Encounter stays dormant outside the horror corridor.");
            Assert.That(player.GetComponent<ChapterOneCorridorShift>().Expanded, Is.False);
            yield return CrossSharedDoor(player);
            Assert.That(motor.TryRestorePose(new Vector3(80f, .10f, -1.65f), Quaternion.Euler(0, 90, 0), false), Is.True);
            yield return null;
            Assert.That(encounter.State, Is.EqualTo(ShadowEncounterState.Spawn), "The checkpoint must allow a new encounter.");
            Assert.That(beats, Is.EqualTo(new[] { "C1-01", "C1-02", "C1-03", "C1-04" }));
        }

        [UnityTest]
        public IEnumerator FirstEscapeThenCaptureRestartsChapterAndPreservesClues()
        {
            new GameState(tempClueFile).AddClue("CLUE_01");
            var beats = new List<string>();
            yield return EnterHorror(beats);
            var player = Object.FindAnyObjectByType<FirstPersonPlayer>();
            var director = player.GetComponent<ChapterDirector>();
            var encounter = Object.FindAnyObjectByType<ShadowEncounterController>();
            yield return StepEncounterUntil(encounter, () => encounter.State == ShadowEncounterState.Grabbed, 20f);
            Assert.That(encounter.State, Is.EqualTo(ShadowEncounterState.Grabbed));
            Assert.That(encounter.EscapeUsed, Is.True);
            Assert.That(encounter.CaughtCount, Is.Zero);
            Assert.That(player.IsTransitionLocked, Is.True);
            yield return new WaitForSeconds(1.3f);
            Assert.That(encounter.State, Is.EqualTo(ShadowEncounterState.Pursue));
            Assert.That(player.IsTransitionLocked, Is.False);
            yield return StepEncounterUntil(encounter, () => encounter.State == ShadowEncounterState.Grabbed, 20f);
            Assert.That(encounter.State, Is.EqualTo(ShadowEncounterState.Grabbed));
            yield return new WaitForSeconds(1.3f);
            Assert.That(encounter.CaughtCount, Is.EqualTo(1));
            float deadline = Time.realtimeSinceStartup + 15f;
            ChapterDirector fresh = null;
            while (Time.realtimeSinceStartup < deadline)
            {
                fresh = Object.FindAnyObjectByType<ChapterDirector>();
                if (fresh != null && fresh != director && fresh.State != null) break;
                yield return null;
            }
            Assert.That(fresh, Is.Not.Null);
            Assert.That(fresh, Is.Not.SameAs(director));
            Assert.That(fresh.BeatIndex, Is.EqualTo(1));
            Assert.That(fresh.State.Chapter1Response, Is.EqualTo(EncounterResponse.None));
            Assert.That(fresh.GetComponent<ChapterOneCorridorShift>().Expanded, Is.False);
            CollectionAssert.AreEquivalent(new[] { "CLUE_01", "CLUE_02" }, fresh.State.ClueIds);
        }

        [UnityTest]
        public IEnumerator MonsterColliderCannotPassHorrorCorridorWallOrLockedExit()
        {
            yield return EnterHorror(new List<string>());
            var encounter = Object.FindAnyObjectByType<ShadowEncounterController>();
            var cc = encounter.GetComponent<CharacterController>();
            cc.enabled = false;
            encounter.transform.position = new Vector3(77.6f, .1f, -1.65f);
            cc.enabled = true;
            Physics.SyncTransforms();
            for (int i = 0; i < 40; i++) cc.Move(new Vector3(0, -.04f, -.08f));
            Assert.That(encounter.transform.position.z, Is.GreaterThan(-2.65f), "The horror corridor wall stops the monster.");
            cc.enabled = false;
            encounter.transform.position = new Vector3(78.6f, .1f, 2f);
            cc.enabled = true;
            Physics.SyncTransforms();
            for (int i = 0; i < 40; i++) cc.Move(new Vector3(0, -.04f, -.08f));
            Assert.That(encounter.transform.position.z, Is.GreaterThan(.8f), "The locked exit door stops the monster.");
        }

        private static IEnumerator EnterHorror(List<string> beats)
        {
            yield return LoadFreshSession(beats, new List<string>(), null);
            var player = Object.FindAnyObjectByType<FirstPersonPlayer>();
            var motor = player.GetComponent<FirstPersonMotor>();
            var director = player.GetComponent<ChapterDirector>();
            var interactor = player.GetComponent<PlayerInteractor>();
            var encounter = Object.FindAnyObjectByType<ShadowEncounterController>();
            var world = player.GetComponent<ChapterOneCorridorShift>();
            Assert.That(encounter.State, Is.EqualTo(ShadowEncounterState.Dormant));
            Assert.That(encounter.IsReady, Is.False);
            yield return CompleteBookLoopAndSlip(motor, interactor, director);
            Assert.That(director.BeatIndex, Is.EqualTo(3));
            yield return CrossSharedDoor(player);
            Assert.That(encounter.State, Is.EqualTo(ShadowEncounterState.Dormant));
            Assert.That(encounter.IsReady, Is.False);
            Assert.That(motor.TryRestorePose(new Vector3(80f, .10f, -1.65f), Quaternion.Euler(0, 90, 0), false), Is.True);
            yield return null;
            Assert.That(encounter.State, Is.EqualTo(ShadowEncounterState.Spawn));
        }

        private static IEnumerator CrossSharedDoor(FirstPersonPlayer player)
        {
            var motor = player.GetComponent<FirstPersonMotor>();
            var interactor = player.GetComponent<PlayerInteractor>();
            var world = player.GetComponent<ChapterOneCorridorShift>();
            var door = Object.FindAnyObjectByType<SharedCorridorDoor>();
            Assert.That(motor.TryRestorePose(door.transform.TransformPoint(new Vector3(0, .04f, -1.16f)), Quaternion.identity), Is.True);
            door.Interact(interactor);
            Assert.That(door.IsOpen, Is.True);
            yield return new WaitForSeconds(.45f);
            Assert.That(motor.TryRestorePose(door.transform.TransformPoint(new Vector3(0, .04f, .35f)), Quaternion.identity), Is.True);
            float deadline = Time.realtimeSinceStartup + 5f;
            while ((!world.Expanded || player.IsTransitionLocked) && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(world.Expanded, Is.True, "Crossing the shared door enters the horror corridor.");
            Assert.That(player.IsTransitionLocked, Is.False);
        }

        private static IEnumerator LoadFreshSession(List<string> beats, List<string> cues, Action onTransition)
        {
            UnityEngine.Events.UnityAction<Scene, LoadSceneMode> hook = (scene, mode) =>
            {
                ChapterDirector loadedDirector = Object.FindAnyObjectByType<ChapterDirector>();
                if (loadedDirector != null)
                {
                    loadedDirector.BeatCompleted += beats.Add;
                    loadedDirector.CueRequested += cue => cues.Add(cue.AudioCueId);
                    if (onTransition != null)
                        loadedDirector.ChapterTransitionRequested += onTransition;
                }
            };

            SceneManager.sceneLoaded += hook;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            SceneManager.sceneLoaded -= hook;
            yield return null;

            FirstPersonPlayer player = Object.FindAnyObjectByType<FirstPersonPlayer>();
            Type sessionType = Type.GetType("NoRainbowAfter.UI.FirstPersonSession, Assembly-CSharp");
            Component session = GameObject.Find("UI_FirstPerson").GetComponent(sessionType);
            MethodInfo focusLossMethod = sessionType.GetMethod("PauseForFocusLoss", BindingFlags.Instance | BindingFlags.NonPublic);
            if (focusLossMethod != null)
            {
                var handler = (Action)Delegate.CreateDelegate(typeof(Action), session, focusLossMethod);
                player.FocusLost -= handler;
            }

            sessionType.GetMethod("SetPaused", BindingFlags.Instance | BindingFlags.Public)?.Invoke(session, new object[] { false });
            Time.timeScale = 1f;
            player.SetPaused(false);
        }

        private static IEnumerator CompleteBookLoopAndSlip(
            FirstPersonMotor motor,
            PlayerInteractor interactor,
            ChapterDirector director)
        {
            SceneInteractable book = Object.FindObjectsByType<SceneInteractable>(FindObjectsInactive.Exclude)
                .Single(x => x.StableId == "INT_BOOK");
            book.Interact(interactor);
            interactor.CloseInspection();

            yield return WalkTo(motor, new Vector2(0.16f, 6.25f));
            director.SamplePosition(motor.transform.position.z);
            yield return WalkTo(motor, new Vector2(0.16f, 4.20f));
            director.SamplePosition(motor.transform.position.z);

            SceneInteractable slip = Object.FindObjectsByType<SceneInteractable>(FindObjectsInactive.Exclude)
                .Single(x => x.StableId == "INT_OVERTIME");
            slip.Interact(interactor);
            interactor.CloseInspection();
        }

        private static IEnumerator WalkTo(FirstPersonMotor motor, Vector2 targetXZ)
        {
            const float doorThreshold = 5.25f;
            Vector3 start = motor.transform.position;
            bool crossesDoorway = (start.z < doorThreshold && targetXZ.y > doorThreshold) ||
                                  (start.z > doorThreshold && targetXZ.y < doorThreshold);

            if (crossesDoorway)
            {
                float approachZ = start.z < doorThreshold ? 4.45f : 6.05f;
                float exitZ = start.z < doorThreshold ? 6.05f : 4.45f;
                yield return WalkSegment(motor, new Vector2(0.16f, approachZ));
                yield return WalkSegment(motor, new Vector2(0.16f, exitZ));
            }

            yield return WalkSegment(motor, targetXZ);
        }

        private static IEnumerator WalkSegment(FirstPersonMotor motor, Vector2 destination)
        {
            ChapterDirector director = motor.GetComponent<ChapterDirector>();
            motor.StopMotion();
            for (int i = 0; i < 600; i++)
            {
                Vector3 p = motor.transform.position;
                Vector2 delta = destination - new Vector2(p.x, p.z);
                if (delta.magnitude < 0.13f)
                {
                    motor.StopMotion();
                    if (director != null && Mathf.Abs(p.x) <= 1.3f)
                        director.SamplePosition(p.z);
                    yield break;
                }

                Vector3 local = motor.transform.InverseTransformDirection(new Vector3(delta.x, 0f, delta.y));
                var frame = new FirstPersonFrame { Move = new Vector2(local.x, local.z).normalized };
                for (int sub = 0; sub < 4; sub++)
                {
                    motor.Simulate(frame, 0.02f);
                    Vector3 cur = motor.transform.position;
                    if (director != null && Mathf.Abs(cur.x) <= 1.3f)
                        director.SamplePosition(cur.z);
                    if ((destination - new Vector2(cur.x, cur.z)).magnitude < 0.13f)
                        break;
                }

                if (i % 2 == 0)
                    yield return new WaitForFixedUpdate();
            }

            Assert.Fail("Unable to walk to " + destination + "; stopped at " + motor.transform.position);
        }

        private static void AimCameraAt(FirstPersonMotor motor, Vector3 worldTarget)
        {
            Vector3 flat = worldTarget - motor.transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.0001f)
                motor.transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);

            Vector3 camDelta = worldTarget - motor.ViewCamera.transform.position;
            if (camDelta.sqrMagnitude > 0.0001f)
                motor.ViewCamera.transform.rotation = Quaternion.LookRotation(camDelta.normalized, Vector3.up);
        }

        private static void ClickLeftMouse()
        {
            Mouse mouse = Mouse.current;
            Assert.That(mouse, Is.Not.Null, "Mouse device must be present for Fight input test.");
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left, true));
            InputSystem.Update();
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left, false));
            InputSystem.Update();
        }

        private static IEnumerator StepEncounterUntil(
            ShadowEncounterController encounter,
            Func<bool> predicate,
            float maxSimulatedSeconds)
        {
            float elapsed = 0f;
            const float step = 0.025f;
            int stepIndex = 0;
            while (elapsed < maxSimulatedSeconds)
            {
                if (predicate())
                    yield break;
                encounter.TickEncounter(step);
                elapsed += step;
                stepIndex++;
                if (stepIndex % 20 == 0)
                    yield return new WaitForFixedUpdate();
            }

            Assert.That(predicate(), Is.True, "Condition not reached within " + maxSimulatedSeconds.ToString("F2") + "s; state=" + encounter.State);
        }
    }
}
