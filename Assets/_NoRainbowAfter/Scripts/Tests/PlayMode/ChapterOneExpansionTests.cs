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
        private IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity");yield return null;
            player=Object.FindFirstObjectByType<FirstPersonPlayer>();motor=player.GetComponent<FirstPersonMotor>();
            director=player.GetComponent<ChapterDirector>();world=player.GetComponent<ChapterOneCorridorShift>();
            monster=Object.FindFirstObjectByType<ShadowEncounterController>();
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
            Assert.That(transitions,Is.EqualTo(1));
            Assert.That(monster.GetComponent<CharacterController>().enabled,Is.False);
            Assert.That(wardrobe.TryExit(),Is.True);Assert.That(player.IsHiding,Is.False);
        }
        [UnityTest] public IEnumerator FirstCatchGivesEscape_SecondCatchRestoresCheckpointWithClue()
        {
            yield return Load();yield return Expand();
            Assert.That(motor.TryRestorePose(new Vector3(80f,.10f,-1.65f),Quaternion.Euler(0,90,0),false),Is.True);
            yield return null;yield return null;
            var caught=typeof(ShadowEncounterController).GetMethod("EnterCaught",BindingFlags.Instance|BindingFlags.NonPublic);
            caught.Invoke(monster,null);
            Assert.That(monster.EscapeUsed,Is.True);Assert.That(monster.CaughtCount,Is.Zero);
            Assert.That(player.IsTransitionLocked,Is.False);
            caught.Invoke(monster,null);
            Assert.That(monster.CaughtCount,Is.EqualTo(1));
            yield return new WaitForSeconds(1f);
            Assert.That(motor.transform.position.x,Is.LessThan(4));
            Assert.That(director.State.ClueIds,Does.Contain("CLUE_02"));
            Assert.That(player.IsTransitionLocked,Is.False);
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
    }
}
