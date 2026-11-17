using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NoRainbowAfter.AI;
using NoRainbowAfter.Interaction;
using NoRainbowAfter.Narrative;
using NoRainbowAfter.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
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

        [SetUp] public void IsolateClues()
        {
            previousStoragePath = ChapterDirector.StoragePathOverride;
            isolatedStorageFolder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "NRA-C1-" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(isolatedStorageFolder);
            ChapterDirector.StoragePathOverride = System.IO.Path.Combine(isolatedStorageFolder, "clues.json");
        }

        [TearDown] public void RestoreClueStorage()
        {
            Time.timeScale = 1f;
            ChapterDirector.StoragePathOverride = previousStoragePath;
            if (System.IO.Directory.Exists(isolatedStorageFolder)) System.IO.Directory.Delete(isolatedStorageFolder, true);
        }
        private IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity");yield return null;
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
        private IEnumerator Expand()
        {
            var interact=player.GetComponent<PlayerInteractor>();
            interact.Flags.Set("objective.book");director.SamplePosition(6.5f);director.SamplePosition(4.4f);
            yield return new WaitForSeconds(1.1f);
            interact.Flags.Set("objective.overtime");interact.NotifyInteraction("INT_OVERTIME");
            Assert.That(motor.TryRestorePose(new Vector3(.16f,.04f,6.1f),Quaternion.identity),Is.True);
            yield return new WaitForSeconds(.8f);
            Assert.That(world.Expanded,Is.True);Assert.That(world.OwnsPlayer,Is.True);
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
            Assert.That(hint.text,Is.EqualTo("Berjalan ke ujung lorong."));
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
            int transitions=0;director.ChapterTransitionRequested+=()=>transitions++;
            yield return new WaitForSeconds(8f);
            Assert.That(director.State.Chapter1Response,Is.EqualTo(EncounterResponse.Hide));
            Assert.That(transitions,Is.Zero);
            Assert.That(monster.GetComponent<CharacterController>().enabled,Is.False);
            Assert.That(wardrobe.TryExit(),Is.True);Assert.That(player.IsHiding,Is.False);
            var door = GameObject.Find("C1_ExitDoor");
            Assert.That(door, Is.Not.Null);
            Assert.That(motor.TryRestorePose(new Vector3(78.6f,.1f,2.5f), Quaternion.Euler(0,180,0), false), Is.True);
            Physics.SyncTransforms();
            var target = PlayerInteractor.FindTarget(new Ray(motor.ViewCamera.transform.position, motor.ViewCamera.transform.forward), motor.transform);
            Assert.That(target, Is.Not.Null);
            Assert.That(target.StableId, Is.EqualTo("INT_C1_EXIT"));
            target.Interact(player.GetComponent<PlayerInteractor>());
            yield return new WaitForSeconds(.7f);
            Assert.That(director.ChapterOneFinished, Is.True);
            Assert.That(transitions, Is.EqualTo(1));
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
            Assert.That(motor.transform.position.x,Is.LessThan(4));
            Assert.That(director.State.ClueIds,Does.Contain("CLUE_02"));
            Assert.That(player.IsTransitionLocked,Is.False);
            Assert.That(monster.DamageBorderAlpha,Is.Zero);
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
