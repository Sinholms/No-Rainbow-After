using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NoRainbowAfter.AI;
using NoRainbowAfter.Interaction;
using NoRainbowAfter.Level;
using NoRainbowAfter.Narrative;
using NoRainbowAfter.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace NoRainbowAfter.Tests
{
    public class ChapterOneExpansionTests
    {
        private FirstPersonPlayer player;
        private FirstPersonMotor motor;
        private ChapterDirector director;
        private ChapterOneCorridorShift world;
        private ShadowEncounterController monster;
        private string previousStoragePath, isolatedStorageFolder;
        private InputSettings.BackgroundBehavior previousBackgroundBehavior;
        private readonly InputTestFixture inputFixture = new InputTestFixture();

        [SetUp] public void IsolateClues()
        {
            inputFixture.Setup();
            previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            previousStoragePath = ChapterDirector.StoragePathOverride;
            isolatedStorageFolder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "NRA-C1-" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(isolatedStorageFolder);
            ChapterDirector.StoragePathOverride = System.IO.Path.Combine(isolatedStorageFolder, "clues.json");
        }

        [TearDown] public void RestoreClueStorage()
        {
            Time.timeScale = 1f;
            InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
            inputFixture.TearDown();
            ChapterDirector.StoragePathOverride = previousStoragePath;
            if (System.IO.Directory.Exists(isolatedStorageFolder)) System.IO.Directory.Delete(isolatedStorageFolder, true);
        }
        private IEnumerator Load()
        {
            EnsureInputDevices(out _, out _);
            yield return SceneManager.LoadSceneAsync("Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity");yield return null;
            var uiModule = Object.FindFirstObjectByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (uiModule != null) { uiModule.UnassignActions(); uiModule.AssignDefaultActions(); }
            player=Object.FindFirstObjectByType<FirstPersonPlayer>();motor=player.GetComponent<FirstPersonMotor>();
            director=player.GetComponent<ChapterDirector>();world=player.GetComponent<ChapterOneCorridorShift>();
            monster=Object.FindFirstObjectByType<ShadowEncounterController>();
            Assert.That(motor.GetComponent<CharacterController>().height, Is.EqualTo(1.7f).Within(.001f));
            Assert.That(motor.ViewCamera.transform.localPosition.y, Is.EqualTo(1.58f).Within(.001f));
            var type=Type.GetType("NoRainbowAfter.UI.FirstPersonSession, Assembly-CSharp");
            var session=GameObject.Find("UI_FirstPerson").GetComponent(type);
            var method=type.GetMethod("PauseForFocusLoss",BindingFlags.Instance|BindingFlags.NonPublic);
            player.FocusLost-=(Action)Delegate.CreateDelegate(typeof(Action),session,method);
            type.GetMethod("SetPaused").Invoke(session,new object[]{false});player.SetPaused(false);Time.timeScale=1;
        }
        private static string DescribePose(Vector3 feet)
        {
            const float radius = .24f;
            var blockers = Physics.OverlapCapsule(feet + Vector3.up * (radius + .05f), feet + Vector3.up * (1.7f - radius),
                radius, ~0, QueryTriggerInteraction.Ignore);
            return feet.ToString("F3") + " overlaps " + string.Join(", ", blockers.Select(c => c.name + " " + c.bounds));
        }

        private IEnumerator Expand()
        {
            var interact=player.GetComponent<PlayerInteractor>();
            interact.Flags.Set("objective.book");director.SamplePosition(6.5f);director.SamplePosition(4.4f);
            yield return new WaitForSeconds(1.1f);
            interact.Flags.Set("objective.overtime");interact.NotifyInteraction("INT_OVERTIME");
            Assert.That(motor.TryRestorePose(new Vector3(.16f,.04f,6.1f),Quaternion.identity),Is.True);
            yield return null;
            Assert.That(world.Expanded, Is.False, "The old position in the corridor no longer teleports the player.");

            var exit = Object.FindAnyObjectByType<SharedCorridorDoor>();
            Assert.That(exit, Is.Not.Null);
            var approach = new Vector3(exit.transform.position.x, .04f, exit.transform.position.z - 1.16f);
            string situation = DescribePose(approach);
            Assert.That(motor.TryRestorePose(approach, Quaternion.identity), Is.True, situation + " door " + exit.transform.position.ToString("F3"));
            EnsureInputDevices(out var keyboard, out _);
            yield return TapKey(keyboard, Key.E);
            Assert.That(exit.IsOpen, Is.True);
            yield return new WaitForSeconds(.48f);
            Assert.That(world.Expanded, Is.False, "Opening the door leaves the player in the normal corridor.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            float deadline = Time.unscaledTime + 3.5f;
            while (!world.Expanded && Time.unscaledTime < deadline) yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSeconds(.25f);
            Assert.That(world.Expanded, Is.True); Assert.That(world.OwnsPlayer, Is.True);
        }
        [UnityTest] public IEnumerator RoomChangesOnReturn_AndSlipOpensExtendedRoute()
        {
            yield return Load();
            Assert.That(player.GetComponent<ChapterOneRoomShift>().HasShifted,Is.False);
            Assert.That(world.Expanded,Is.False);
            yield return Expand();
            Assert.That(player.GetComponent<ChapterOneRoomShift>().HasShifted,Is.True);
            Assert.That(director.State.ClueIds,Does.Contain("CLUE_02"));
            Assert.That(monster.State,Is.EqualTo(ShadowEncounterState.Dormant),"No monster before the exploration threshold.");
            Assert.That(director.BeatIndex,Is.EqualTo(3));
            var tracker=player.GetComponent<ObjectiveTracker>();
            var hint=(UnityEngine.UI.Text)typeof(ObjectiveTracker).GetField("hint",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(tracker);
            Assert.That(hint.text,Does.Contain("Berjalan ke ujung lorong"));
            var stress=player.GetComponent<StressBarometer>();
            float beforeCorridor=stress.CurrentStress;
            world.TickCorridorStress(ChapterOneCorridorShift.CorridorStressIntervalSeconds * 2f);
            Assert.That(stress.CurrentStress,Is.GreaterThan(beforeCorridor),"Corridor exposure must periodically raise Raka's stress.");
        }
        [UnityTest] public IEnumerator WardrobeHideDespawnsMonster_AndRecordsHideOnce()
        {
            yield return Load();yield return Expand();
            Assert.That(motor.TryRestorePose(new Vector3(80f,.10f,-1.65f),Quaternion.Euler(0,90,0),false),Is.True);
            yield return null;yield return null;
            Assert.That(monster.State,Is.EqualTo(ShadowEncounterState.Spawn));
            var wardrobe=Object.FindObjectsByType<HidingWardrobe>(FindObjectsSortMode.None).Single(x=>x.name=="Wardrobe_East");
            Assert.That(motor.TryRestorePose(wardrobe.transform.TransformPoint(new Vector3(0,.08f,-1.35f)),Quaternion.identity,false),Is.True);
            wardrobe.Interact(player.GetComponent<PlayerInteractor>());
            Assert.That(player.IsHiding,Is.True);Assert.That(HidingWardrobe.Occupied,Is.SameAs(wardrobe));
            var stress=player.GetComponent<StressBarometer>();
            var breathing=player.GetComponent<BreathingMinigame>();
            stress.SetStress(76f);
            float stressBeforeHideTick=stress.CurrentStress;
            world.TickCorridorStress(ChapterOneCorridorShift.CorridorStressIntervalSeconds * 2f);
            Assert.That(stress.CurrentStress,Is.EqualTo(stressBeforeHideTick).Within(0.01f),"Hiding in wardrobe pauses corridor stress gain.");
            Assert.That(breathing.StartBreathing(),Is.True,"Must allow [Tab] breathing inside hiding wardrobe.");
            for(int i=0;i<15;i++) breathing.Tick(0.10f,true);
            Assert.That(stress.CurrentStress,Is.LessThan(70f));
            breathing.StopBreathing();
            Assert.That(player.IsHiding,Is.True,"Exiting breathing keeps Raka safely hidden until pressing E.");

            int transitions=0;director.ChapterTransitionRequested+=()=>transitions++;
            yield return new WaitForSeconds(8f);
            Assert.That(director.State.Chapter1Response,Is.EqualTo(EncounterResponse.Hide));
            Assert.That(transitions,Is.Zero);
            Assert.That(monster.GetComponent<CharacterController>().enabled,Is.False);

            // From the end of Chapter 1 inside the wardrobe, play the entire session with keyboard and mouse only.
            EnsureInputDevices(out var keyboard, out var mouse);
            yield return TapKey(keyboard, Key.E);
            Assert.That(player.IsHiding,Is.False);
            var door = GameObject.Find("C1_ExitDoor");
            Assert.That(door, Is.Not.Null);
            Vector3[] corridorExitStops =
            {
                new Vector3(90f, 0f, -1.65f),
                new Vector3(92.6f, 0f, 10f),
                new Vector3(82f, 0f, 12.35f),
                new Vector3(78.6f, 0f, 2.35f)
            };
            foreach (Vector3 stop in corridorExitStops)
            {
                var path = new NavMeshPath();
                Assert.That(NavMesh.CalculatePath(motor.transform.position, stop, NavMesh.AllAreas, path), Is.True);
                Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete));
                foreach (Vector3 corner in path.corners.Skip(1))
                    yield return WalkToWithKeyboardAndMouse(keyboard, mouse, corner, true, .28f);
            }
            yield return LookAtWithMouse(mouse, door.GetComponent<Collider>().bounds.center);
            var target = PlayerInteractor.FindTarget(new Ray(motor.ViewCamera.transform.position, motor.ViewCamera.transform.forward), motor.transform);
            Assert.That(target, Is.Not.Null);
            Assert.That(target.StableId, Is.EqualTo("INT_C1_EXIT"));
            yield return TapKey(keyboard, Key.E);
            float wakeDeadline = Time.unscaledTime + 4f;
            while ((player.IsTransitionLocked || director.State.Chapter2Beat != ChapterTwoBeat.WokeInDorm) && Time.unscaledTime < wakeDeadline)
                yield return null;
            yield return null;
            Assert.That(director.ChapterOneFinished, Is.True);
            Assert.That(transitions, Is.EqualTo(1));
            Assert.That(world.Expanded, Is.False);
            Assert.That(motor.transform.position.x, Is.LessThan(4f), "Finishing Chapter 1 must return Raka to his dorm room, not MainMenu.");
            Assert.That(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, Is.EqualTo("MasterGameplay"));
            var objLabel = GameObject.Find("TestObjective").GetComponent<UnityEngine.UI.Text>();
            Assert.That(objLabel.text, Is.EqualTo("Kerjakan esai di laptop"));
            Assert.That(director.State.ActiveChapter, Is.EqualTo("Chapter2"));
            Assert.That(director.State.Chapter2Beat, Is.EqualTo(ChapterTwoBeat.WokeInDorm));
            Assert.That(player.IsTransitionLocked, Is.False);
            yield return CompleteChapterTwo(keyboard, mouse, objLabel);
        }

        private IEnumerator CompleteChapterTwo(Keyboard keyboard, Mouse mouse, UnityEngine.UI.Text objective)
        {
            var interactions = player.GetComponent<PlayerInteractor>();
            var laptop = Object.FindAnyObjectByType<LaptopTypingMinigame>();
            var phone = Object.FindAnyObjectByType<ChapterTwoPhone>();
            var exit = Object.FindAnyObjectByType<SharedCorridorDoor>();
            Assert.That(motor.MovementScale, Is.EqualTo(.75f));
            Assert.That(phone.Prompt, Is.Null);
            Assert.That(exit.IsOpen, Is.False);

            laptop.Interact(interactions);
            yield return null;
            for (int i = 0; i < 3; i++) laptop.Tick(60f, false);
            Assert.That(director.State.Chapter2Beat, Is.EqualTo(ChapterTwoBeat.StayedInBed));
            Assert.That(exit.Prompt, Is.EqualTo("Pintu terkunci"));
            Assert.That(Object.FindAnyObjectByType<ChapterTwoStreet>(), Is.Null);
            yield return new WaitForSeconds(3.9f);
            var retry = GameObject.Find("Ulangi Chapter 2").GetComponent<UnityEngine.UI.Button>();
            yield return ClickUiButtonWithMouse(keyboard, mouse, retry);
            yield return new WaitForSeconds(1.2f);
            Assert.That(director.State.Chapter2Beat, Is.EqualTo(ChapterTwoBeat.WokeInDorm));
            Assert.That(laptop.JudgedEssaySections, Is.Zero);
            Assert.That(director.State.EllaMessageSent, Is.False);

            // From the standing side of the desk both props must be reachable through the normal FPP raycast.
            var standingByDesk = new Vector3(.24f, .04f, 2.76f);
            Assert.That(motor.TryRestorePose(standingByDesk, Quaternion.identity), Is.True, DescribePose(standingByDesk));
            var photo = GameObject.Find("Foto Raka dan Ella").GetComponent<SceneInteractable>();
            var photoCollider = photo.GetComponent<BoxCollider>();
            yield return LookAtWithMouse(mouse, photoCollider.bounds.center);
            Assert.That(interactions.Focused, Is.SameAs(photo), "The framed photo must be visible on the desk.");
            yield return TapKey(keyboard, Key.E);
            Assert.That(director.State.HasClue("CLUE_01"), Is.True);
            var inspection = interactions.transform.Find("Interaction UI/Inspect");
            var print = inspection.Find("Foto cetak").GetComponent<UnityEngine.UI.RawImage>();
            Assert.That(print.gameObject.activeInHierarchy, Is.True);
            Assert.That(print.texture.name, Is.EqualTo("T_RakaEllaPhoto"));
            Assert.That(inspection.Find("Body").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("Wisuda bareng"));
            yield return TapKey(keyboard, Key.F);

            yield return LookAtWithMouse(mouse, phone.GetComponent<BoxCollider>().bounds.center);
            Assert.That(interactions.Focused, Is.SameAs(phone), "The phone must be reachable by looking at it.");
            Assert.That(interactions.transform.Find("Interaction UI/Prompt").GetComponent<UnityEngine.UI.Text>().text, Is.Empty);
            Assert.That(objective.text, Is.EqualTo("Kerjakan esai di laptop"));
            yield return TapKey(keyboard, Key.E);
            Assert.That(player.IsInspecting, Is.True);
            Assert.That(director.State.HasClue("CLUE_03"), Is.True);
            Assert.That(director.State.EllaMessageSent, Is.False);
            var decline = GameObject.Find("DoNotSend").GetComponent<UnityEngine.UI.Button>();
            Assert.That(decline.GetComponentInChildren<UnityEngine.UI.Text>().text, Is.EqualTo("Jangan kirim"));
            yield return ClickUiButtonWithMouse(keyboard, mouse, decline);
            Assert.That(player.IsInspecting, Is.False);
            Assert.That(director.State.EllaMessageSent, Is.False);

            yield return LookAtWithMouse(mouse, phone.GetComponent<BoxCollider>().bounds.center);
            yield return TapKey(keyboard, Key.E);
            yield return ClickUiButtonWithMouse(keyboard, mouse, GameObject.Find("Send").GetComponent<UnityEngine.UI.Button>());
            Assert.That(director.State.EllaMessageSent, Is.True);
            Assert.That(phone.TrySendMessage(), Is.False);
            Assert.That(phone.GetComponentsInChildren<UnityEngine.UI.Text>().Count(t => t.text.Contains("✓")), Is.EqualTo(1));
            yield return TapKey(keyboard, Key.F);

            laptop.Interact(interactions);
            yield return null;
            InputSystem.QueueTextEvent(keyboard, laptop.TargetSentence[0]);
            yield return null;
            float heldTime = laptop.RemainingTime;
            yield return TapKey(keyboard, Key.Tab);
            Assert.That(laptop.IsActive, Is.False);
            laptop.Interact(interactions);
            yield return null;
            Assert.That(laptop.CurrentInput, Has.Length.EqualTo(1));
            Assert.That(laptop.RemainingTime, Is.LessThanOrEqualTo(heldTime).And.GreaterThan(heldTime - .6f));
            yield return TypeSentenceOnKeyboard(keyboard, laptop.TargetSentence.Substring(1));
            yield return TapKey(keyboard, Key.Enter);
            Assert.That(laptop.CompletedSentences, Is.EqualTo(1));
            laptop.Tick(60f, false);
            laptop.Tick(60f, false);
            Assert.That(director.State.Chapter2Beat, Is.EqualTo(ChapterTwoBeat.OvertimeRequested));
            Assert.That(Object.FindAnyObjectByType<ChapterTwoStreet>(), Is.Null, "The essay does not teleport the player.");
            Assert.That(interactions.IsInspecting, Is.True, "Overtime is announced at the laptop.");
            yield return TapKey(keyboard, Key.F);
            Assert.That(motor.TryRestorePose(exit.transform.position + new Vector3(0, .04f, -1.1f), Quaternion.identity), Is.True);
            yield return TapKey(keyboard, Key.E);
            yield return new WaitForSeconds(.45f);
            Assert.That(exit.IsOpen, Is.True);
            Assert.That(director.State.Chapter2Beat, Is.EqualTo(ChapterTwoBeat.OvertimeRequested));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            float deadline = Time.unscaledTime + 4f;
            while (director.State.Chapter2Beat != ChapterTwoBeat.StreetEntered && Time.unscaledTime < deadline)
                yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSeconds(.75f);

            var street = Object.FindAnyObjectByType<ChapterTwoStreet>();
            Assert.That(street, Is.Not.Null);
            Assert.That(director.State.Chapter2Beat, Is.EqualTo(ChapterTwoBeat.StreetEntered));
            Assert.That(street.TryRestoreCheckpoint(), Is.True);
            Assert.That(director.State.EllaMessageSent, Is.True);
            Assert.That(street.GetComponent<AudioSource>().isPlaying, Is.True);

            int handoffs = 0;
            director.ChapterThreeTransitionRequested += () => handoffs++;
            Vector3 stop = street.transform.TransformPoint(new Vector3(.55f, .19f, 37.82f));
            Assert.That(motor.TryRestorePose(stop, Quaternion.identity), Is.True);
            street.CheckProgress();
            Assert.That(director.State.Chapter2Beat, Is.EqualTo(ChapterTwoBeat.ShelterReached));
            Physics.SyncTransforms();
            yield return LookAtWithMouse(mouse, street.BusDoor.GetComponent<Collider>().bounds.center);
            Assert.That(interactions.Focused, Is.SameAs(street.BusDoor));
            yield return TapKey(keyboard, Key.E);
            yield return new WaitForSeconds(1.2f);
            Assert.That(street.Boarded, Is.True);
            Assert.That(handoffs, Is.EqualTo(1));
            Assert.That(director.State.ActiveChapter, Is.EqualTo("Chapter3"));
            Assert.That(objective.text, Is.EqualTo("Berangkat kerja"));
            Assert.That(player.IsTransitionLocked, Is.False);
            Assert.That(director.State.EllaMessageSent, Is.True);
            Assert.That(street.GetComponent<AudioSource>().isPlaying, Is.False);
            Assert.That(EndingResolver.Resolve(FinalChoice.Hide, director.State), Is.EqualTo(StoryEnding.Bad));
            director.State.AddClue("CLUE_04");
            Assert.That(EndingResolver.Resolve(FinalChoice.Hide, director.State), Is.EqualTo(StoryEnding.Hidden));
            var unsentState = new GameState(System.IO.Path.Combine(isolatedStorageFolder, "unsent.json"));
            foreach (string clue in director.State.ClueIds) unsentState.AddClue(clue);
            Assert.That(EndingResolver.Resolve(FinalChoice.Hide, unsentState), Is.EqualTo(StoryEnding.Bad));
            Assert.That(EndingResolver.Resolve(FinalChoice.Approach, director.State), Is.EqualTo(StoryEnding.Good));
        }

        private static void EnsureInputDevices(out Keyboard keyboard, out Mouse mouse)
        {
            keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
            mouse = Mouse.current ?? InputSystem.AddDevice<Mouse>();
            InputSystem.EnableDevice(keyboard);
            InputSystem.EnableDevice(mouse);
            keyboard.MakeCurrent();
            mouse.MakeCurrent();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.QueueStateEvent(mouse, new MouseState());
            InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.QueueStateEvent(mouse, new MouseState());
            InputSystem.Update();
        }

        private static IEnumerator TapKey(Keyboard keyboard, Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }

        private static IEnumerator TypeSentenceOnKeyboard(Keyboard keyboard, string sentence)
        {
            for (int i = 0; i < sentence.Length; i++)
            {
                InputSystem.QueueTextEvent(keyboard, sentence[i]);
                if ((i + 1) % 14 == 0) yield return null;
            }
            yield return null;
        }

        private IEnumerator LookAtWithMouse(Mouse mouse, Vector3 targetWorld)
        {
            for (int step = 0; step < 6; step++)
            {
                Physics.SyncTransforms();
                Vector3 eye = motor.ViewCamera.transform.position;
                Vector3 dir = (targetWorld - eye).normalized;
                float targetYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                float yawDiff = Mathf.DeltaAngle(motor.transform.eulerAngles.y, targetYaw);
                float targetPitch = -Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
                float pitchDiff = targetPitch - motor.Pitch;
                if (Mathf.Abs(yawDiff) < .15f && Mathf.Abs(pitchDiff) < .15f) break;
                InputSystem.QueueStateEvent(mouse, new MouseState
                {
                    delta = new Vector2(yawDiff / motor.MouseSensitivity, -pitchDiff / motor.MouseSensitivity)
                });
                yield return null;
            }
            InputSystem.QueueStateEvent(mouse, new MouseState { delta = Vector2.zero });
            yield return null;
        }

        private IEnumerator WalkToWithKeyboardAndMouse(Keyboard keyboard, Mouse mouse, Vector3 targetWorld, bool sprint, float tolerance)
        {
            float elapsed = 0f;
            while (true)
            {
                Vector3 planar = Vector3.ProjectOnPlane(targetWorld - motor.transform.position, Vector3.up);
                if (planar.magnitude <= tolerance) break;
                Assert.That(elapsed, Is.LessThan(25f), "Blocked walking toward " + targetWorld + " at " + motor.transform.position);
                Vector3 dir = planar.normalized;
                float targetYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                float yawDiff = Mathf.DeltaAngle(motor.transform.eulerAngles.y, targetYaw);
                float pitchDiff = 0f - motor.Pitch;
                InputSystem.QueueStateEvent(mouse, new MouseState
                {
                    delta = new Vector2(yawDiff / motor.MouseSensitivity, -pitchDiff / motor.MouseSensitivity)
                });
                InputSystem.QueueStateEvent(keyboard, sprint ? new KeyboardState(Key.W, Key.LeftShift) : new KeyboardState(Key.W));
                yield return null;
                elapsed += Time.deltaTime;
            }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.QueueStateEvent(mouse, new MouseState { delta = Vector2.zero });
            yield return null;
        }

        private static IEnumerator ClickUiButtonWithMouse(Keyboard keyboard, Mouse mouse, UnityEngine.UI.Button button)
        {
            Canvas.ForceUpdateCanvases();
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, button.transform.position);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screenPos });
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screenPos }.WithButton(MouseButton.Left, true));
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screenPos }.WithButton(MouseButton.Left, false));
            yield return null;
        }
        [UnityTest] public IEnumerator RiggedHitAndGrabStayGroundedAndReset()
        {
            yield return Load(); yield return Expand();
            Assert.That(motor.TryRestorePose(new Vector3(80f,.1f,-1.65f),Quaternion.Euler(0,90,0),false),Is.True);
            yield return null;
            var target=monster.transform.position;
            Assert.That(motor.TryRestorePose(target+Vector3.left*2f,Quaternion.Euler(0,90,0),false),Is.True);
            motor.ViewCamera.transform.rotation=Quaternion.LookRotation(target+Vector3.up*1.3f-motor.ViewCamera.transform.position);
            Physics.SyncTransforms();
            var rig=monster.GetComponentInChildren<ShadowMotionPlayback>(true);
            Assert.That(rig, Is.Not.Null, "C1 needs playback on its visible rig.");
            Assert.That(rig.IsHero, Is.True, "Reactions must animate the visible Raka model.");
            Assert.That(monster.GetComponentsInChildren<ShadowMotionPlayback>(true).Length, Is.EqualTo(1), "Do not instantiate a hidden duplicate rig.");
            Assert.That(rig.GetComponentsInChildren<SkinnedMeshRenderer>().Any(r=>r.enabled), Is.True);
            var fore=rig.GetComponentsInChildren<Transform>().Single(t=>t.name=="Hand.L");
            var head=rig.GetComponentsInChildren<Transform>().Single(t=>t.name=="Head");
            rig.ResetPose();var rest=fore.position;var fixedRoot=monster.transform.position;
            rig.SampleGrab(.5f);
            Assert.That(Vector3.Dot(fore.position-rest,monster.transform.forward),Is.GreaterThan(.2f),"Hands must reach forward.");
            Assert.That(fore.position.y,Is.LessThan(head.position.y),"Elbows must not rise above the head.");
            rig.ResetPose();Assert.That(Vector3.Distance(rest,fore.position),Is.LessThan(.005f));
            var spine = rig.Spine.localRotation;
            monster.GetComponent<ShadowPlaceholderVisual>().TickVisual(ShadowEncounterState.FightResolved, .1f, 0f, .5f, motor.transform);
            Assert.That(Quaternion.Angle(spine, rig.Spine.localRotation), Is.GreaterThan(2f), "Hit must affect the visible rig.");
            rig.ResetPose();
            Assert.That(monster.transform.position,Is.EqualTo(fixedRoot));
            string output=Environment.GetEnvironmentVariable("NRA_MOTION_CAPTURE");
            if(!string.IsNullOrEmpty(output))
            {
                Time.timeScale=0;
                typeof(ShadowEncounterController).GetMethod("OnFightRequested",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(monster,null);
                for(int i=0;i<42;i++) { if(i<28)monster.TickEncounter(.02f); CaptureMotionFrame(motor.ViewCamera,output,"Hit",i); yield return null; }
                Assert.That(motor.TryRestorePose(target+Vector3.left*1.05f,Quaternion.Euler(0,90,0),false),Is.True);
                motor.ViewCamera.transform.rotation=Quaternion.LookRotation(target+Vector3.up*1.65f-motor.ViewCamera.transform.position);
                typeof(ShadowEncounterController).GetMethod("EnterCaught",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(monster,null);
                for(int i=0;i<58;i++) { monster.TickEncounter(.02f); CaptureMotionFrame(motor.ViewCamera,output,"Grab",i); yield return null; }
                Time.timeScale=1;
            }
        }
        [UnityTest] public IEnumerator RakaHairIsContinuousIndependentAndPausedWithEncounter()
        {
            yield return Load();
            yield return Expand();
            Assert.That(motor.TryRestorePose(new Vector3(80f, .1f, -1.65f), Quaternion.Euler(0, 90, 0), false), Is.True);
            yield return null;
            Time.timeScale = 0f;
            var playback = monster.GetComponentInChildren<ShadowMotionPlayback>(true);
            Assert.That(playback, Is.Not.Null);
            var hair = playback.GetComponentsInChildren<Transform>(true)
                .Where(bone => bone.name.StartsWith("HairSway.")).ToArray();
            Assert.That(hair.Length, Is.EqualTo(9));
            var clip = playback.GetComponent<Animator>().runtimeAnimatorController.animationClips
                .Single(animation => animation.name == "Hair_Sway");
            var root = monster.transform.position;
            playback.ResetPose();
            playback.ResetHair();
            var rest = hair.Select(bone => bone.localRotation).ToArray();
            var previous = rest.ToArray();
            float maximumStep = 0f;
            int steps = Mathf.RoundToInt(clip.length * 60f);
            for (int step = 0; step < steps; step++)
            {
                playback.SampleHair(1f / 60f);
                for (int i = 0; i < hair.Length; i++)
                {
                    maximumStep = Mathf.Max(maximumStep, Quaternion.Angle(previous[i], hair[i].localRotation));
                    previous[i] = hair[i].localRotation;
                }
            }
            Assert.That(maximumStep, Is.InRange(.01f, 2f), "Hair should move continuously at 60 FPS.");
            for (int i = 0; i < hair.Length; i++)
                Assert.That(Quaternion.Angle(rest[i], hair[i].localRotation), Is.LessThan(.05f), "Loop seam must return smoothly.");
            playback.ResetHair();
            playback.SampleHair(1f);
            var oneSecond = hair.Select(bone => bone.localRotation).ToArray();
            playback.ResetHair();
            for (int step = 0; step < 60; step++) playback.SampleHair(1f / 60f);
            for (int i = 0; i < hair.Length; i++)
                Assert.That(Quaternion.Angle(oneSecond[i], hair[i].localRotation), Is.LessThan(.05f));
            playback.ResetHair();
            for (int step = 0; step < 30; step++) playback.SampleHair(1f / 30f);
            for (int i = 0; i < hair.Length; i++)
                Assert.That(Quaternion.Angle(oneSecond[i], hair[i].localRotation), Is.LessThan(.05f), "Sway must be frame-rate independent.");
            var visual = monster.GetComponent<ShadowPlaceholderVisual>();
            var beforeReset = hair.Select(bone => bone.localRotation).ToArray();
            visual.ResetPostureImmediate(motor.transform);
            visual.TickSecondaryMotion(1f / 60f);
            for (int i = 0; i < hair.Length; i++)
                Assert.That(Quaternion.Angle(beforeReset[i], hair[i].localRotation), Is.LessThan(2f), "Posture changes must preserve the hair clock.");
            var spine = playback.Spine.localRotation;
            playback.SampleHair(.1f);
            Assert.That(Quaternion.Angle(spine, playback.Spine.localRotation), Is.LessThan(.01f), "Hair clip must not overwrite body bones.");
            player.SetPaused(true);
            var frozen = hair.Select(bone => bone.localRotation).ToArray();
            monster.TickEncounter(.1f);
            for (int i = 0; i < hair.Length; i++)
                Assert.That(Quaternion.Angle(frozen[i], hair[i].localRotation), Is.LessThan(.01f));
            player.SetPaused(false);
            monster.TickEncounter(.1f);
            Assert.That(hair.Select((bone, i) => Quaternion.Angle(frozen[i], bone.localRotation)).Max(), Is.GreaterThan(.05f));
            Assert.That(monster.transform.position, Is.EqualTo(root));
            string output = Environment.GetEnvironmentVariable("NRA_MOTION_CAPTURE");
            if (!string.IsNullOrEmpty(output))
            {
                Vector3 target = monster.transform.position;
                Assert.That(motor.TryRestorePose(target + Vector3.left * 2.4f, Quaternion.Euler(0, 90, 0), false), Is.True);
                motor.ViewCamera.transform.rotation = Quaternion.LookRotation(target + Vector3.up * 1.95f - motor.ViewCamera.transform.position);
                for (int i = 0; i < 48; i++)
                {
                    monster.TickEncounter(1f / 12f);
                    CaptureMotionFrame(motor.ViewCamera, output, "Hair", i);
                }
            }
        }

        private static void CaptureMotionFrame(Camera camera,string folder,string clip,int frame)
        {
            string path=System.IO.Path.Combine(folder,clip);System.IO.Directory.CreateDirectory(path);
            var target=new RenderTexture(960,540,24);var pixels=new Texture2D(960,540,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;var previousTarget=camera.targetTexture;
            try
            {
                camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,960,540),0,0);pixels.Apply();
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(path,frame.ToString("D3")+".png"),pixels.EncodeToPNG());
            }
            finally { camera.targetTexture=previousTarget;RenderTexture.active=previous;Object.DestroyImmediate(pixels);target.Release();Object.DestroyImmediate(target); }
        }
        [UnityTest] public IEnumerator MonsterRequiresThreeSeparateHits()
        {
            yield return Load(); yield return Expand();
            Assert.That(motor.TryRestorePose(new Vector3(80f,.1f,-1.65f),Quaternion.Euler(0,90,0),false),Is.True);
            yield return null;
            Assert.That(monster.State,Is.EqualTo(ShadowEncounterState.Spawn));
            var target = monster.transform.position;
            Assert.That(motor.TryRestorePose(target + Vector3.left * 2f, Quaternion.Euler(0,90,0), false), Is.True);
            motor.ViewCamera.transform.rotation = Quaternion.LookRotation(target + Vector3.up * 1.3f - motor.ViewCamera.transform.position);
            Physics.SyncTransforms();
            var attack = typeof(ShadowEncounterController).GetMethod("OnFightRequested", BindingFlags.Instance | BindingFlags.NonPublic);
            for (int hit=1;hit<=3;hit++)
            {
                Assert.That(monster.CanFightRightNow(), Is.True);
                attack.Invoke(monster,null); attack.Invoke(monster,null);
                Assert.That(monster.SuccessfulHits,Is.EqualTo(hit));
                Assert.That(director.State.Chapter1Response,Is.EqualTo(EncounterResponse.None));
                float duration=monster.GetComponent<ShadowPlaceholderVisual>().HitDuration(hit>=3);
                for(int step=0;step<Mathf.CeilToInt(duration/.1f)+1;step++) monster.TickEncounter(.1f);
                if(hit<3) Assert.That(monster.State,Is.EqualTo(ShadowEncounterState.Pursue));
            }
            Assert.That(director.State.Chapter1Response,Is.EqualTo(EncounterResponse.Fight));
        }
        [UnityTest] public IEnumerator FirstCatchGivesEscape_SecondCatchRestoresCheckpointWithClue()
        {
            yield return Load();yield return Expand();
            Assert.That(motor.TryRestorePose(new Vector3(80f,.10f,-1.65f),Quaternion.Euler(0,90,0),false),Is.True);
            yield return null;yield return null;
            var caught=typeof(ShadowEncounterController).GetMethod("EnterCaught",BindingFlags.Instance|BindingFlags.NonPublic);
            caught.Invoke(monster,null);
            Assert.That(monster.EscapeUsed,Is.True);Assert.That(monster.CaughtCount,Is.Zero);
            Assert.That(monster.State,Is.EqualTo(ShadowEncounterState.Grabbed));
            Assert.That(player.IsTransitionLocked,Is.True);
            yield return new WaitForSeconds(.3f);
            Assert.That(monster.DamageBorderAlpha,Is.GreaterThan(.01f));
            var rig = monster.GetComponentInChildren<ShadowMotionPlayback>(true);
            var hand = rig.GetComponentsInChildren<Transform>().Single(t=>t.name=="Hand.L");
            player.SetPaused(true);
            var frozen= motor.ViewCamera.transform.localRotation;
            var frozenHand = hand.position;
            monster.TickEncounter(.1f);
            yield return null;
            Assert.That(Vector3.Distance(hand.position, frozenHand), Is.LessThan(.001f), "Visible rig must freeze during pause.");
            Assert.That(motor.ViewCamera.transform.localRotation,Is.EqualTo(frozen));
            player.SetPaused(false);
            yield return new WaitForSeconds(1.1f);
            Assert.That(monster.State,Is.EqualTo(ShadowEncounterState.Pursue));
            Assert.That(player.IsTransitionLocked,Is.False);
            Assert.That(monster.DamageBorderAlpha,Is.GreaterThan(.01f),"Red injury border persists after the first escape.");
            Time.timeScale = 0f;
            var stopped = monster.transform.position;
            var recoveryPose = rig.Spine.localRotation;
            for (int i=0;i<10;i++) monster.TickEncounter(.1f);
            Assert.That(Vector3.Distance(stopped,monster.transform.position),Is.LessThan(.001f), "Grace period gives time to escape.");
            Assert.That(Quaternion.Angle(recoveryPose,rig.Spine.localRotation),Is.GreaterThan(1f), "Recovery animates instead of freezing.");
            for (int i=0;i<25;i++) monster.TickEncounter(.1f);
            Assert.That(Vector3.Distance(stopped,monster.transform.position),Is.GreaterThan(.2f), "Monster must physically resume pursuit after recovery.");
            for (int i=0;i<100 && monster.State!=ShadowEncounterState.Grabbed;i++) monster.TickEncounter(.1f);
            Assert.That(monster.State,Is.EqualTo(ShadowEncounterState.Grabbed));
            Time.timeScale = 1f;
            yield return new WaitForSeconds(1.3f);
            Assert.That(monster.CaughtCount,Is.EqualTo(1));
            yield return new WaitForSeconds(1f);
            Assert.That(Vector3.Distance(motor.transform.position, world.Entry), Is.LessThan(0.6f), "Shadow catch must respawn Raka at corridor start, not back in the dorm.");
            Assert.That(director.State.ClueIds,Does.Contain("CLUE_02"));
            Assert.That(player.IsTransitionLocked,Is.False);
            Assert.That(monster.DamageBorderAlpha,Is.Zero);
            var stress=player.GetComponent<StressBarometer>();
            Assert.That(stress.CurrentStress,Is.EqualTo(100f).Within(0.01f),"Being caught by Shadow must spike stress to 100%.");
            Assert.That(stress.IsWarningActive,Is.True);
            Assert.That(world.RequiresBreathRecovery,Is.True);
            Assert.That(player.IsBreathRecoveryRequired,Is.True,"Player must regulate breath with [Tab] before continuing.");
            var breathing=player.GetComponent<BreathingMinigame>();
            Assert.That(breathing.StartBreathing(),Is.True);
            for(int step=0;step<40 && stress.CurrentStress >= StressBarometer.HighStressWarningThreshold;step++)
                breathing.Tick(0.10f, breathing.IndicatorValue <= breathing.TargetCenter);
            breathing.StopBreathing();
            Assert.That(stress.CurrentStress,Is.LessThan(StressBarometer.HighStressWarningThreshold));
            world.TickCorridorStress(0.02f);
            Assert.That(world.RequiresBreathRecovery,Is.False);
            Assert.That(player.IsBreathRecoveryRequired,Is.False);
        }
        [UnityTest] public IEnumerator PhysicalTraversalConnectsEntry_LongLoop_AndWardrobeExit()
        {
            yield return Load();yield return Expand();monster.enabled=false;player.enabled=false;
            Vector3[] stops={new Vector3(58.15f,0,-1.8f),new Vector3(90,0,-1.65f),new Vector3(92.6f,0,10),new Vector3(82,0,12.35f),new Vector3(78.6f,0,2)};
            float distance=0;
            foreach(var stop in stops)
            {
                var path=new NavMeshPath();Assert.That(NavMesh.CalculatePath(motor.transform.position,stop,NavMesh.AllAreas,path),Is.True);
                Assert.That(path.status,Is.EqualTo(NavMeshPathStatus.PathComplete));
                foreach(var corner in path.corners.Skip(1))
                {
                    int steps=0;
                    while(Vector2.Distance(new Vector2(motor.transform.position.x,motor.transform.position.z),new Vector2(corner.x,corner.z))>.23f && steps++<900)
                    {
                        var delta=corner-motor.transform.position;delta.y=0;
                        var local=motor.transform.InverseTransformDirection(delta.normalized);
                        var before=motor.transform.position;
                        motor.Simulate(new FirstPersonFrame{Move=new Vector2(local.x,local.z)},.04f);
                        distance+=Vector3.Distance(before,motor.transform.position);
                        if(steps%50==0) yield return null;
                    }
                    Assert.That(steps,Is.LessThan(900),"Blocked en route to "+corner+" at "+motor.transform.position);
                    Assert.That(motor.transform.position.y,Is.GreaterThan(-.3f),"Route has no supporting floor.");
                }
            }
            Assert.That(distance,Is.GreaterThan(65f));
            Debug.Log("[NRA] Physical extended-route traversal: "+distance.ToString("F1")+"m");
        }
        [UnityTest] public IEnumerator MonsterFollowsNavigationAroundCorners()
        {
            yield return Load();yield return Expand();
            Assert.That(motor.TryRestorePose(new Vector3(80,.10f,-1.65f),Quaternion.identity,false),Is.True);
            yield return null;yield return null;
            Assert.That(motor.TryRestorePose(new Vector3(85,.10f,12.35f),Quaternion.identity,false),Is.True);
            float distance=100;
            for(int n=0;n<1800 && distance>2.8f;n++)
            {
                monster.TickEncounter(.04f);
                var delta=monster.transform.position-motor.transform.position;delta.y=0;distance=delta.magnitude;
                Assert.That(monster.transform.position.y,Is.GreaterThan(-.3f));
                if(n%100==0)yield return null;
            }
            Assert.That(distance,Is.LessThanOrEqualTo(2.8f),"Monster got stuck at "+monster.transform.position);
        }
        [UnityTest] public IEnumerator LinearRouteSealsOldBranchesWithVisibleWalls_AndHasNoLoopShortcut()
        {
            yield return Load();yield return Expand();monster.enabled=false;
            // Both sides of the former branching hub are now continuous rendered walls.
            foreach(float x in new[]{62f,65.6f,67.3f,69.6f,73.6f,77.6f,78.6f})
            foreach(var direction in new[]{Vector3.forward,Vector3.back})
            {
                RaycastHit hit;
                Assert.That(Physics.Raycast(new Vector3(x,1.3f,-1.65f),direction,out hit,1.5f,~0,QueryTriggerInteraction.Ignore),Is.True,"Open side branch at "+x);
                Assert.That(hit.collider.GetComponent<MeshRenderer>(),Is.Not.Null,"Branch sealed only by invisible collision at "+x);
            }
            RaycastHit end;
            Assert.That(Physics.Raycast(new Vector3(78.6f,1.3f,2f),Vector3.back,out end,2f,~0,QueryTriggerInteraction.Ignore),Is.True);
            Assert.That(end.collider.name,Is.EqualTo("C1_ExitDoor"));
            Assert.That(end.collider.GetComponent<MeshRenderer>(),Is.Not.Null);
            var path=new NavMeshPath();
            Assert.That(NavMesh.CalculatePath(new Vector3(78.6f,0,2f),new Vector3(78.6f,0,-1.65f),NavMesh.AllAreas,path),Is.True);
            Assert.That(path.status,Is.EqualTo(NavMeshPathStatus.PathComplete));
            float length=0;for(int i=1;i<path.corners.Length;i++)length+=Vector3.Distance(path.corners[i-1],path.corners[i]);
            Assert.That(length,Is.GreaterThan(45f),"Old loop shortcut is still open.");
        }
        [UnityTest] public IEnumerator RepairedFloorAndPerimeterPreventFallingAtReturnJunction()
        {
            yield return Load();yield return Expand();monster.enabled=false;player.enabled=false;
            for(float x=77.55f;x<=79.35f;x+=.15f)
                Assert.That(Physics.Raycast(new Vector3(x,.35f,-2.7f),Vector3.down,.65f,~0,QueryTriggerInteraction.Ignore),Is.True,"Missing floor at "+x);
            Assert.That(motor.TryRestorePose(new Vector3(78.3f,.1f,-2.2f),Quaternion.Euler(0,180,0),false),Is.True);
            for(int n=0;n<100;n++)motor.Simulate(new FirstPersonFrame{Move=Vector2.up},.04f);
            Assert.That(motor.transform.position.z,Is.GreaterThan(-2.65f));
            Assert.That(motor.transform.position.y,Is.GreaterThan(-.2f));
            Assert.That(world.FallRecoveryCount,Is.Zero,"The physical boundary should stop the fall before recovery is needed.");
        }
        [UnityTest] public IEnumerator AllThreeCornerWardrobesAreReachableAndPermitEntryAndExit()
        {
            yield return Load();yield return Expand();monster.enabled=false;
            var all=Object.FindObjectsByType<HidingWardrobe>();Assert.That(all.Length,Is.EqualTo(5));
            foreach(var name in new[]{"Wardrobe_CornerA","Wardrobe_CornerB","Wardrobe_CornerC"})
            {
                var wardrobe=all.Single(x=>x.name==name);
                Assert.That(motor.TryRestorePose(wardrobe.transform.TransformPoint(new Vector3(0,.08f,-1.35f)),wardrobe.transform.rotation,false),Is.True,name+" exit must be clear");
                var camera=motor.ViewCamera;
                camera.transform.rotation=wardrobe.transform.rotation;
                Assert.That(PlayerInteractor.FindTarget(new Ray(camera.transform.position,camera.transform.forward),motor.transform),Is.SameAs(wardrobe),name+" must be reachable with E");
                wardrobe.Interact(player.GetComponent<PlayerInteractor>());Assert.That(player.IsHiding,Is.True,name);
                Assert.That(wardrobe.TryExit(),Is.True,name);Assert.That(player.IsHiding,Is.False);
            }
        }
        [UnityTest] public IEnumerator ArrivalMonologuesPlayInOrder_AndFallRecoveryPreservesClue()
        {
            yield return Load();
            var cues=new System.Collections.Generic.List<NarrativeCue>();director.CueRequested+=cues.Add;
            yield return Expand();monster.enabled=false;
            var arrival=cues.Where(c=>c.AudioCueId.StartsWith("AUD_C1_CORRIDOR_")).ToArray();
            Assert.That(arrival.Length,Is.EqualTo(2));
            Assert.That(arrival[0].Text,Is.EqualTo("Aku di mana? Kenapa aku bisa di sini?"));
            Assert.That(arrival[1].Text,Is.EqualTo("Aku harus menemukan jalan keluar dan pergi kerja."));
            var subtitles=player.GetComponent<SubtitlePlaceholder>();Assert.That(subtitles.CurrentText,Does.Contain(arrival[0].Text));
            yield return new WaitForSeconds(3.5f);Assert.That(subtitles.CurrentText,Does.Contain(arrival[1].Text));
            var cc=motor.GetComponent<CharacterController>();cc.enabled=false;motor.transform.position=new Vector3(65,-3,-1.65f);cc.enabled=true;
            yield return null;yield return null;
            Assert.That(world.FallRecoveryCount,Is.EqualTo(1));Assert.That(motor.transform.position.y,Is.GreaterThan(-.2f));
            Assert.That(director.State.ClueIds,Does.Contain("CLUE_02"));Assert.That(player.IsTransitionLocked,Is.False);
        }
        [UnityTest] public IEnumerator SideWardrobesReplaceCornerBays_AndEveryLightEdgeDischarges()
        {
            yield return Load();yield return Expand();monster.enabled=false;
            Assert.That(world.Corridor.GetComponentsInChildren<Transform>(true).Any(t=>t.name.StartsWith("HC3_Bay")),Is.False,"Old corner recesses must be removed.");
            var expected=new[]{new Vector3(11,0,.12f),new Vector3(34.37f,0,5.2f),new Vector3(25.5f,0,10.58f)};
            int index=0;
            foreach(var id in new[]{"CornerA","CornerB","CornerC"})
            {
                var wardrobe=world.Corridor.transform.Find("Wardrobe_"+id);
                Assert.That(Vector3.Distance(wardrobe.localPosition,expected[index++]),Is.LessThan(.02f));
                var exit=wardrobe.TransformPoint(new Vector3(0,.08f,-1.35f));
                var path=new NavMeshPath();Assert.That(NavMesh.CalculatePath(world.Entry,exit,NavMesh.AllAreas,path),Is.True,id);
                Assert.That(path.status,Is.EqualTo(NavMeshPathStatus.PathComplete),id);
            }
            var lamps=world.Corridor.GetComponentsInChildren<CorridorFaultyLight>();Assert.That(lamps.Length,Is.EqualTo(17));
            var fixture=lamps[0];
            Assert.That(fixture.Crackle.clip,Is.Not.Null);Assert.That(fixture.Crackle.spatialBlend,Is.EqualTo(1));
            int count=fixture.TransitionCount;
            fixture.SetPowered(false);
            Assert.That(fixture.Lamp.intensity,Is.Zero);Assert.That(fixture.Flash.enabled,Is.True);
            fixture.SetPowered(true);
            Assert.That(fixture.Lamp.intensity,Is.GreaterThan(0));
            Assert.That(fixture.TransitionCount-count,Is.EqualTo(2));
            Assert.That(fixture.SparkBurstCount,Is.EqualTo(fixture.TransitionCount),"Both off->on and on->off must discharge.");
            float gap=0,maxGap=0;bool sawOff=false;
            for(int i=0;i<1200;i++)
            {
                int before=fixture.TransitionCount;fixture.Tick(.02f);gap+=.02f;
                sawOff|=!fixture.Powered;
                if(before!=fixture.TransitionCount){maxGap=Mathf.Max(maxGap,gap);gap=0;}
            }
            Assert.That(sawOff,Is.True);Assert.That(maxGap,Is.InRange(1f,2.05f));
            Assert.That(fixture.SparkBurstCount,Is.EqualTo(fixture.TransitionCount));
            Assert.That(fixture.TransitionCount,Is.GreaterThan(10));
        }
    }
}
