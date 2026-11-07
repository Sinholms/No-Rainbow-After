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

        [SetUp]
        public void SetUpIsolation()
        {
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
            var cues = new List<string>();
            int transitions = 0;

            yield return LoadFreshSession(beats, cues, () => transitions++);

            FirstPersonPlayer player = Object.FindAnyObjectByType<FirstPersonPlayer>();
            FirstPersonMotor motor = player.GetComponent<FirstPersonMotor>();
            PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
            ChapterDirector director = player.GetComponent<ChapterDirector>();
            ShadowEncounterController encounter = Object.FindAnyObjectByType<ShadowEncounterController>();

            Assert.That(encounter, Is.Not.Null, "MasterGameplay must contain the installed T06 ShadowEncounterController.");
            Assert.That(encounter.State, Is.EqualTo(ShadowEncounterState.Dormant));
            Assert.That(encounter.IsReady, Is.True);

            yield return CompleteBookLoopAndSlip(motor, interactor, director);
            CollectionAssert.AreEqual(new[] { "C1-01", "C1-02", "C1-03" }, beats);
            Assert.That(director.State.HasClue("CLUE_02"), Is.True);

            // Re-enter corridor to trigger C1-04 and spawn the shadow silhouette.
            yield return WalkTo(motor, new Vector2(0f, 6.25f));
            yield return null;

            Assert.That(beats, Is.EqualTo(new[] { "C1-01", "C1-02", "C1-03", "C1-04" }));
            Assert.That(encounter.State, Is.EqualTo(ShadowEncounterState.Spawn));

            float spawnDistance = Vector2.Distance(
                new Vector2(motor.transform.position.x, motor.transform.position.z),
                new Vector2(encounter.transform.position.x, encounter.transform.position.z));
            Assert.That(spawnDistance, Is.InRange(8.0f, 12.5f), "Spawn must stage at readable FPP distance, never glued to camera.");
            Assert.That(
                ShadowVisibility.IsLineOfSightClear(
                    motor.ViewCamera.transform.position,
                    encounter.transform.position + Vector3.up * 1.4f,
                    motor.transform,
                    encounter.transform),
                Is.True,
                "Spawned corridor silhouette should be readable from FPP.");

            // Advance from Spawn into Pursue and let the creature close in toward Fight range.
            yield return StepEncounterUntil(encounter, () => encounter.State == ShadowEncounterState.Pursue, 2.5f);
            Assert.That(encounter.State, Is.EqualTo(ShadowEncounterState.Pursue));

            yield return StepEncounterUntil(
                encounter,
                () => Vector3.Distance(motor.transform.position, encounter.transform.position) <= 2.25f,
                6.5f);
            Assert.That(Mathf.Abs(encounter.transform.position.x), Is.LessThan(0.68f), "Monster must stay inside corridor walls.");

            AimCameraAt(motor, encounter.transform.position + Vector3.up * 1.3f);
            Physics.SyncTransforms();
            encounter.TickEncounter(0f);

            // Solid obstacle between player and monster must block contextual Fight.
            GameObject blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                Vector3 mid = Vector3.Lerp(motor.ViewCamera.transform.position, encounter.transform.position + Vector3.up * 1.3f, 0.5f);
                blocker.transform.position = mid;
                blocker.transform.localScale = new Vector3(1.6f, 2.4f, 0.25f);
                Physics.SyncTransforms();
                encounter.TickEncounter(0f);

                Assert.That(encounter.CanFightRightNow(), Is.False, "Solid wall must block Fight.");
                Assert.That(encounter.IsFightPromptVisible, Is.False);
                ClickLeftMouse();
                Assert.That(encounter.State, Is.EqualTo(ShadowEncounterState.Pursue));
            }
            finally
            {
                Object.DestroyImmediate(blocker);
                Physics.SyncTransforms();
            }

            encounter.TickEncounter(0f);
            Assert.That(encounter.CanFightRightNow(), Is.True, "Unobstructed close aim must enable contextual Fight.");
            Assert.That(encounter.IsFightPromptVisible, Is.True);

            // Spam-click left mouse button 5 times across frames: must enter FightResolved once and resolve C1-05/C1-06 once.
            for (int i = 0; i < 5; i++)
            {
                ClickLeftMouse();
                encounter.TickEncounter(0.02f);
            }

            Assert.That(encounter.State, Is.EqualTo(ShadowEncounterState.FightResolved));
            Assert.That(encounter.BlackoutAlpha, Is.GreaterThan(0.01f), "Fight feedback pulse should be active.");

            yield return StepEncounterUntil(encounter, () => director.BeatIndex == 6, 1.5f);

            CollectionAssert.AreEqual(new[] { "C1-01", "C1-02", "C1-03", "C1-04", "C1-05", "C1-06" }, beats);
            Assert.That(director.State.Chapter1Response, Is.EqualTo(EncounterResponse.Fight));
            Assert.That(transitions, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator FreshSession_HideInLeftCover_ResolvesOnceAfterMonsterPasses()
        {
            yield return VerifyHideInCover("C1_COVER_LEFT", new Vector2(0f, 9.0f), new Vector2(-1.45f, 9.0f));
        }

        [UnityTest]
        public IEnumerator FreshSession_HideInRightCover_ResolvesOnceAfterMonsterPasses()
        {
            yield return VerifyHideInCover("C1_COVER_RIGHT", new Vector2(0f, 13.0f), new Vector2(1.45f, 13.0f));
        }

        private IEnumerator VerifyHideInCover(string coverId, Vector2 corridorEntry, Vector2 coverTarget)
        {
            var beats = new List<string>();
            var cues = new List<string>();
            int transitions = 0;

            yield return LoadFreshSession(beats, cues, () => transitions++);

            FirstPersonPlayer player = Object.FindAnyObjectByType<FirstPersonPlayer>();
            FirstPersonMotor motor = player.GetComponent<FirstPersonMotor>();
            PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
            ChapterDirector director = player.GetComponent<ChapterDirector>();
            ShadowEncounterController encounter = Object.FindAnyObjectByType<ShadowEncounterController>();
            CoverZone zone = Object.FindObjectsByType<CoverZone>(FindObjectsInactive.Exclude).Single(z => z.StableId == coverId);

            yield return CompleteBookLoopAndSlip(motor, interactor, director);
            yield return WalkTo(motor, new Vector2(0f, 6.25f));
            yield return null;
            Assert.That(encounter.State, Is.EqualTo(ShadowEncounterState.Spawn));

            // Walk into the chosen cover alcove and crouch down before the creature reaches the alcove.
            yield return WalkTo(motor, corridorEntry);
            yield return WalkTo(motor, coverTarget);
            yield return new WaitForFixedUpdate();
            Assert.That(zone.PlayerInside, Is.True, "Player must occupy " + coverId);

            SetCrouch(motor, true);
            Assert.That(motor.IsCrouched, Is.True);

            yield return StepEncounterUntil(encounter, () => director.BeatIndex == 6, 14f);

            CollectionAssert.AreEqual(new[] { "C1-01", "C1-02", "C1-03", "C1-04", "C1-05", "C1-06" }, beats);
            Assert.That(director.State.Chapter1Response, Is.EqualTo(EncounterResponse.Hide));
            Assert.That(transitions, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator StandingInCoverNeverAutoHides_And_LosingCoverCancelsUnfinishedHide()
        {
            var beats = new List<string>();
            var cues = new List<string>();
            int transitions = 0;

            yield return LoadFreshSession(beats, cues, () => transitions++);

            FirstPersonPlayer player = Object.FindAnyObjectByType<FirstPersonPlayer>();
            FirstPersonMotor motor = player.GetComponent<FirstPersonMotor>();
            PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
            ChapterDirector director = player.GetComponent<ChapterDirector>();
            ShadowEncounterController encounter = Object.FindAnyObjectByType<ShadowEncounterController>();
            CoverZone leftCover = Object.FindObjectsByType<CoverZone>(FindObjectsInactive.Exclude).Single(z => z.StableId == "C1_COVER_LEFT");

            yield return CompleteBookLoopAndSlip(motor, interactor, director);
            yield return WalkTo(motor, new Vector2(0f, 6.25f));
            yield return null;
            yield return StepEncounterUntil(encounter, () => encounter.State == ShadowEncounterState.Pursue, 2.0f);

            yield return WalkTo(motor, new Vector2(0f, 9.0f));
            yield return WalkTo(motor, new Vector2(-1.45f, 9.0f));
            yield return new WaitForFixedUpdate();
            Assert.That(leftCover.PlayerInside, Is.True);

            // 1) Crouch briefly to accumulate partial Hide progress, then stand up -> must cancel to 0 immediately.
            SetCrouch(motor, true);
            yield return StepEncounterUntil(encounter, () => encounter.HideElapsedSeconds >= 0.45f, 2.0f);
            Assert.That(encounter.HideElapsedSeconds, Is.GreaterThanOrEqualTo(0.45f));

            SetCrouch(motor, false);
            encounter.TickEncounter(0.02f);
            Assert.That(motor.IsCrouched, Is.False);
            Assert.That(encounter.HideElapsedSeconds, Is.EqualTo(0f), "Standing up must cancel unfinished Hide immediately.");
            Assert.That(director.BeatIndex, Is.EqualTo(4));

            // 2) Re-crouch briefly while monster is still away, then exit cover -> must cancel to 0 immediately.
            SetCrouch(motor, true);
            yield return StepEncounterUntil(encounter, () => encounter.HideElapsedSeconds >= 0.35f, 2.0f);
            SetCrouch(motor, false);
            yield return WalkTo(motor, new Vector2(0f, 9.0f));
            yield return new WaitForFixedUpdate();
            encounter.TickEncounter(0.02f);
            Assert.That(leftCover.PlayerInside, Is.False);
            Assert.That(encounter.HideElapsedSeconds, Is.EqualTo(0f), "Exiting cover must cancel unfinished Hide.");

            // 3) Re-enter cover standing upright -> never auto-hides; monster enters alcove and triggers Caught.
            yield return WalkTo(motor, new Vector2(-1.35f, 9.0f));
            yield return new WaitForFixedUpdate();
            Assert.That(leftCover.PlayerInside, Is.True);
            Assert.That(motor.IsCrouched, Is.False);

            yield return StepEncounterUntil(encounter, () => encounter.State == ShadowEncounterState.Caught, 8.0f);
            Assert.That(encounter.HideElapsedSeconds, Is.EqualTo(0f));
            Assert.That(encounter.State, Is.EqualTo(ShadowEncounterState.Caught));
            Assert.That(transitions, Is.Zero);
            Assert.That(director.BeatIndex, Is.EqualTo(4), "Caught must never advance C1-05/C1-06.");
        }

        [UnityTest]
        public IEnumerator PauseAndInspectFreezeEncounterAndBlockFight_AndCheckpointButtonAbortsCleanly()
        {
            var beats = new List<string>();
            var cues = new List<string>();
            int transitions = 0;

            yield return LoadFreshSession(beats, cues, () => transitions++);

            FirstPersonPlayer player = Object.FindAnyObjectByType<FirstPersonPlayer>();
            FirstPersonMotor motor = player.GetComponent<FirstPersonMotor>();
            PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
            ChapterDirector director = player.GetComponent<ChapterDirector>();
            ShadowEncounterController encounter = Object.FindAnyObjectByType<ShadowEncounterController>();

            Type sessionType = Type.GetType("NoRainbowAfter.UI.FirstPersonSession, Assembly-CSharp");
            Component session = GameObject.Find("UI_FirstPerson").GetComponent(sessionType);
            MethodInfo togglePause = sessionType.GetMethod("TogglePause", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo setPaused = sessionType.GetMethod("SetPaused", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo restoreCheckpoint = sessionType.GetMethod("RestoreCheckpoint", BindingFlags.Instance | BindingFlags.NonPublic);

            yield return CompleteBookLoopAndSlip(motor, interactor, director);
            yield return WalkTo(motor, new Vector2(0f, 6.25f));
            yield return null;
            yield return StepEncounterUntil(encounter, () => encounter.State == ShadowEncounterState.Pursue, 2.0f);
            yield return StepEncounterUntil(
                encounter,
                () => Vector3.Distance(motor.transform.position, encounter.transform.position) <= 2.25f,
                6.5f);

            AimCameraAt(motor, encounter.transform.position + Vector3.up * 1.3f);
            encounter.TickEncounter(0f);
            Assert.That(encounter.CanFightRightNow(), Is.True);

            // Inspect freezes AI, hides Fight prompt, and blocks Left-Click; Esc closes Inspect before Pause.
            Vector3 frozenMonsterPos = encounter.transform.position;
            interactor.ShowInspection("Uji Inspect", "Bayangan berhenti saat inspeksi.");
            encounter.TickEncounter(0.5f);
            ClickLeftMouse();
            Assert.That(player.IsInspecting, Is.True);
            Assert.That(encounter.IsFightPromptVisible, Is.False);
            Assert.That(encounter.State, Is.EqualTo(ShadowEncounterState.Pursue));
            Assert.That(Vector3.Distance(encounter.transform.position, frozenMonsterPos), Is.LessThan(0.0001f));

            togglePause.Invoke(session, null);
            Assert.That(player.IsInspecting, Is.False, "First Esc closes Inspect before opening Pause.");
            Assert.That(player.IsPaused, Is.False);

            // Pause freezes AI & blocks Left-Click; Pause 'Kembali ke titik awal' aborts encounter cleanly.
            setPaused.Invoke(session, new object[] { true });
            Assert.That(player.IsPaused, Is.True);
            encounter.TickEncounter(0.5f);
            ClickLeftMouse();
            Assert.That(encounter.State, Is.EqualTo(ShadowEncounterState.Pursue));
            Assert.That(Vector3.Distance(encounter.transform.position, frozenMonsterPos), Is.LessThan(0.0001f));

            restoreCheckpoint.Invoke(session, null);
            Assert.That(encounter.State, Is.EqualTo(ShadowEncounterState.Dormant), "Checkpoint recovery from Pause must abort active pursuit.");
            Assert.That(Vector3.Distance(motor.transform.position, GameObject.Find("DormSpawn_T04").transform.position), Is.LessThan(0.05f));
            Assert.That(director.BeatIndex, Is.EqualTo(4));
            Assert.That(director.State.HasClue("CLUE_02"), Is.True);
            Assert.That(transitions, Is.Zero);

            setPaused.Invoke(session, new object[] { false });
            Assert.That(player.IsPaused, Is.False);

            // Re-entering corridor starts a fresh encounter without repeating C1-04.
            yield return WalkTo(motor, new Vector2(0f, 6.25f));
            yield return null;
            Assert.That(encounter.State, Is.EqualTo(ShadowEncounterState.Spawn));
            CollectionAssert.AreEqual(new[] { "C1-01", "C1-02", "C1-03", "C1-04" }, beats);
        }

        [UnityTest]
        public IEnumerator RepeatedCaughtPreservesCluesAndFallbackSpawn_ThenRetryResolvesWithoutDuplicateBeats()
        {
            // Pre-seed CLUE_01 in isolated temp profile to prove Caught never wipes previously found clues either.
            new GameState(tempClueFile).AddClue("CLUE_01");

            var beats = new List<string>();
            var cues = new List<string>();
            int transitions = 0;

            yield return LoadFreshSession(beats, cues, () => transitions++);

            FirstPersonPlayer player = Object.FindAnyObjectByType<FirstPersonPlayer>();
            FirstPersonMotor motor = player.GetComponent<FirstPersonMotor>();
            PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
            ChapterDirector director = player.GetComponent<ChapterDirector>();
            ShadowEncounterController encounter = Object.FindAnyObjectByType<ShadowEncounterController>();

            yield return CompleteBookLoopAndSlip(motor, interactor, director);
            CollectionAssert.AreEquivalent(new[] { "CLUE_01", "CLUE_02" }, director.State.ClueIds);

            // Attempt #1 -> Caught -> normal DormSpawn_T04 checkpoint restore.
            yield return WalkTo(motor, new Vector2(0f, 6.25f));
            yield return null;
            yield return StepEncounterUntil(encounter, () => encounter.State == ShadowEncounterState.Caught, 8.0f);
            Assert.That(encounter.CaughtCount, Is.EqualTo(1));
            Assert.That(player.IsTransitionLocked, Is.True);

            yield return StepEncounterUntil(encounter, () => encounter.State == ShadowEncounterState.Dormant, 2.0f);
            Assert.That(player.IsTransitionLocked, Is.False, "Controls must unlock after Caught recovery.");
            Assert.That(Vector3.Distance(motor.transform.position, GameObject.Find("DormSpawn_T04").transform.position), Is.LessThan(0.05f));
            Assert.That(director.BeatIndex, Is.EqualTo(4));
            Assert.That(director.State.Chapter1Response, Is.EqualTo(EncounterResponse.None));
            Assert.That(director.LoopIndex, Is.EqualTo(1));
            Assert.That(interactor.Flags.Contains("objective.book"), Is.True);
            Assert.That(interactor.Flags.Contains("objective.overtime"), Is.True);
            CollectionAssert.AreEquivalent(new[] { "CLUE_01", "CLUE_02" }, new GameState(tempClueFile).ClueIds);
            Assert.That(transitions, Is.Zero);

            // Attempt #2 -> Block primary DormSpawn_T04 to verify safe fallback spawn without instant re-catch or control lock.
            yield return WalkTo(motor, new Vector2(0f, 6.25f));
            yield return null;
            Assert.That(encounter.State, Is.EqualTo(ShadowEncounterState.Spawn));
            CollectionAssert.AreEqual(new[] { "C1-01", "C1-02", "C1-03", "C1-04" }, beats, "C1-04 must not repeat on retry.");

            GameObject spawnBlocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                spawnBlocker.transform.position = GameObject.Find("DormSpawn_T04").transform.position + Vector3.up * 0.9f;
                spawnBlocker.transform.localScale = new Vector3(0.75f, 1.8f, 0.75f);
                Physics.SyncTransforms();

                yield return StepEncounterUntil(encounter, () => encounter.State == ShadowEncounterState.Caught, 8.0f);
                Assert.That(encounter.CaughtCount, Is.EqualTo(2));
                yield return StepEncounterUntil(encounter, () => encounter.State == ShadowEncounterState.Dormant, 2.0f);

                Assert.That(player.IsTransitionLocked, Is.False);
                Assert.That(motor.transform.position.z, Is.InRange(0.4f, 4.4f), "Fallback spawn must place Raka safely inside the dorm room.");
                Assert.That(Vector3.Distance(motor.transform.position, spawnBlocker.transform.position), Is.GreaterThan(0.7f));
            }
            finally
            {
                Object.DestroyImmediate(spawnBlocker);
                Physics.SyncTransforms();
            }

            // Attempt #3 -> Succeed via Fight -> exact 1 resolution, all clues preserved.
            yield return WalkTo(motor, new Vector2(0f, 6.25f));
            yield return null;
            yield return StepEncounterUntil(encounter, () => encounter.State == ShadowEncounterState.Pursue, 2.0f);
            yield return StepEncounterUntil(
                encounter,
                () => Vector3.Distance(motor.transform.position, encounter.transform.position) <= 2.25f,
                6.5f);

            AimCameraAt(motor, encounter.transform.position + Vector3.up * 1.3f);
            encounter.TickEncounter(0f);
            ClickLeftMouse();
            yield return StepEncounterUntil(encounter, () => director.BeatIndex == 6, 1.5f);

            CollectionAssert.AreEqual(new[] { "C1-01", "C1-02", "C1-03", "C1-04", "C1-05", "C1-06" }, beats);
            Assert.That(director.State.Chapter1Response, Is.EqualTo(EncounterResponse.Fight));
            Assert.That(transitions, Is.EqualTo(1));
            CollectionAssert.AreEquivalent(new[] { "CLUE_01", "CLUE_02" }, new GameState(tempClueFile).ClueIds);
        }

        [UnityTest]
        public IEnumerator CollisionAgainstCorridorAndLockedDoor_AndDoorwayLintelStoopClearance()
        {
            var beats = new List<string>();
            var cues = new List<string>();

            yield return LoadFreshSession(beats, cues, null);

            FirstPersonPlayer player = Object.FindAnyObjectByType<FirstPersonPlayer>();
            FirstPersonMotor motor = player.GetComponent<FirstPersonMotor>();
            PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
            ChapterDirector director = player.GetComponent<ChapterDirector>();
            ShadowEncounterController encounter = Object.FindAnyObjectByType<ShadowEncounterController>();
            ShadowPlaceholderVisual visual = encounter.GetComponent<ShadowPlaceholderVisual>();

            SceneInteractable lockedDoor = Object.FindObjectsByType<SceneInteractable>(FindObjectsInactive.Exclude)
                .Single(x => x.StableId == "INT_C1_DOOR");
            Assert.That(lockedDoor, Is.Not.Null, "INT_C1_DOOR must remain the locked T03 hall door.");

            yield return CompleteBookLoopAndSlip(motor, interactor, director);
            yield return WalkTo(motor, new Vector2(0f, 6.25f));
            yield return null;
            yield return StepEncounterUntil(encounter, () => encounter.State == ShadowEncounterState.Pursue, 2.0f);

            // Verify monster cannot penetrate the right corridor wall / INT_C1_DOOR at x = +0.89, z = 7.10.
            var monsterCc = encounter.GetComponent<CharacterController>();
            monsterCc.enabled = false;
            encounter.transform.position = new Vector3(0.25f, 0.02f, 7.10f);
            monsterCc.enabled = true;
            Physics.SyncTransforms();

            for (int i = 0; i < 40; i++)
                monsterCc.Move(new Vector3(0.08f, -0.04f, 0f));

            Assert.That(encounter.transform.position.x, Is.LessThan(0.70f), "Monster CharacterController must not penetrate INT_C1_DOOR or corridor wall.");

            // Retreat player into dorm room (z = 2.4) so monster navigates through the 0.66m doorway under the 2.05m lintel.
            yield return WalkTo(motor, new Vector2(0.15f, 2.40f));

            monsterCc.enabled = false;
            encounter.transform.position = new Vector3(0f, 0.02f, 6.60f);
            monsterCc.enabled = true;
            Physics.SyncTransforms();

            float maxRenderedYInDoorway = 0f;
            bool crossedDoorway = false;
            for (int i = 0; i < 240; i++)
            {
                encounter.TickEncounter(0.025f);
                float z = encounter.transform.position.z;
                if (z >= 4.85f && z <= 5.35f)
                {
                    crossedDoorway = true;
                    maxRenderedYInDoorway = Mathf.Max(maxRenderedYInDoorway, visual.GetHighestRenderedY());
                }
                if (z <= 4.35f) break;
            }

            Assert.That(crossedDoorway, Is.True, "Monster should navigate through the open dorm doorway.");
            Assert.That(encounter.transform.position.z, Is.LessThanOrEqualTo(4.40f), "Monster must fit cleanly into the dorm room.");
            Assert.That(maxRenderedYInDoorway, Is.LessThan(2.02f), "Monster upper silhouette must stoop below the 2.05m doorway lintel.");
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

        private static void SetCrouch(FirstPersonMotor motor, bool crouched)
        {
            if (motor.IsCrouched != crouched)
                motor.Simulate(new FirstPersonFrame { CrouchPressed = true }, 0.02f);
            for (int i = 0; i < 35; i++)
                motor.Simulate(default, 0.02f);
            Physics.SyncTransforms();
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
