using System;
using System.Collections;
using System.IO;
using System.Linq;
using NoRainbowAfter.AI;
using NoRainbowAfter.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace NoRainbowAfter.Tests
{
    public sealed class ChapterTwoArtTests
    {
        [UnityTest]
        public IEnumerator LowCrateAndAllThreeRecoveryPosesWorkWithTheImportedKit()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity");
            yield return null;
            var player=Object.FindAnyObjectByType<FirstPersonPlayer>();
            var motor=player.GetComponent<FirstPersonMotor>();
            player.enabled=false;
            var street=Object.Instantiate(Resources.Load<GameObject>("ChapterTwoStreet")).GetComponent<ChapterTwoStreet>();
            Assert.IsTrue(street.Enter(motor));
            var encounter=street.Encounter;
            encounter.AutoTick=false;
            var stance=street.transform.TransformPoint(new Vector3(-2.98f,.035f,6.4f));
            Assert.IsTrue(motor.TryRestorePose(stance,street.transform.rotation,false));
            encounter.ManualTick(.05f);
            Assert.IsTrue(encounter.IsIlluminated,"The standing head is above the low crate");
            Vector3 crateFrontFloor=street.transform.TransformPoint(new Vector3(-.85f,.035f,6.9f));
            Assert.IsFalse(encounter.IsFloorPointVisible(0,crateFrontFloor,true),"Legacy 3D floor cone missed the asphalt between Patroli A and the low crate");
            Assert.IsTrue(encounter.IsFloorPointVisible(0,crateFrontFloor,false),"Aligned floor cone marks the lit asphalt in front of the low crate");
            float beforePause=encounter.Exposure;
            Quaternion lampRotation=street.Lamps[2].rotation;
            encounter.AutoTick=true;
            player.SetPaused(true);
            yield return null;
            yield return null;
            Assert.AreEqual(beforePause,encounter.Exposure);
            Assert.That(Quaternion.Angle(lampRotation,street.Lamps[2].rotation),Is.LessThan(.001f));
            player.SetPaused(false);
            encounter.AutoTick=false;
            motor.Simulate(new FirstPersonFrame{CrouchPressed=true},.08f);
            for(int i=0;i<6;i++) motor.Simulate(default,.08f);
            encounter.ManualTick(.05f);
            Assert.IsFalse(encounter.IsIlluminated,"The crate shades the crouched head and torso");
            for(int i=0;i<3;i++)
            {
                var checkpoint=street.Checkpoints[i];
                Assert.IsTrue(motor.TryRestorePose(checkpoint.position+street.transform.forward*.08f,checkpoint.rotation,false));
                street.CheckProgress();
                Assert.AreEqual(i,street.Segment);
                // Reuse a known static beam for the catch, preserving the segment selected by walking.
                Assert.IsTrue(motor.TryRestorePose(stance,checkpoint.rotation,false));
                encounter.ManualTick(encounter.CatchSeconds+.01f);
                Assert.AreEqual(i+1,encounter.CatchCount);
                Assert.That(Vector3.Distance(motor.transform.position,checkpoint.position),Is.LessThan(.05f));
                encounter.ManualTick(.04f);
                Assert.IsFalse(encounter.IsIlluminated,"Exposed respawn in segment "+i);
                Assert.AreEqual(0,encounter.Exposure);
            }

            // Lateral sweep (sapuan dari samping) in Segment B: floor footprint edge matches player detection.
            int catchesBeforeSweep=encounter.CatchCount;
            encounter.ResetEncounter();
            encounter.SetSegment(1);
            Assert.IsTrue(motor.TryRestorePose(street.Checkpoints[1].position,street.Checkpoints[1].rotation,false));
            encounter.ManualTick(2.0f);
            Vector3 guardForward=Vector3.ProjectOnPlane(street.Lamps[2].forward,Vector3.up).normalized;
            Vector3 lateralDir=Quaternion.AngleAxis(16.5f,Vector3.up)*guardForward;
            Vector3 sideSweepEdge=street.Lamps[2].position+lateralDir*4.5f;
            sideSweepEdge.y=street.transform.position.y+.04f;
            Assert.IsTrue(motor.TryRestorePose(sideSweepEdge,street.transform.rotation,false));
            encounter.ManualTick(.35f);
            Assert.IsTrue(encounter.IsIlluminated,"Segment B lateral sweep illuminates Raka at the cone edge");
            Vector3 currentGuardForward=Vector3.ProjectOnPlane(street.Lamps[2].forward,Vector3.up).normalized;
            Vector3 currentEdge=street.Lamps[2].position+(Quaternion.AngleAxis(16.5f,Vector3.up)*currentGuardForward)*4.5f;
            currentEdge.y=street.transform.position.y+.04f;
            Assert.IsFalse(encounter.IsFloorPointVisible(2,currentEdge,true),"Legacy 3D floor cone stopped short of Raka's feet on the lateral edge");
            Assert.IsTrue(encounter.IsFloorPointVisible(2,currentEdge,false),"Fixed floor cone reaches Raka's feet at the exact detection boundary");
            Assert.That(encounter.Exposure,Is.GreaterThan(.34f).And.LessThan(encounter.CatchSeconds));
            Assert.AreEqual(catchesBeforeSweep,encounter.CatchCount,"Warning leaves reaction time to retreat behind cover");
            Assert.IsTrue(motor.TryRestorePose(street.Checkpoints[1].position,street.Checkpoints[1].rotation,false));
            encounter.ManualTick(.45f);
            Assert.IsFalse(encounter.IsIlluminated,"Checkpoint B cover blocks the lateral sweep after retreating");
            Assert.AreEqual(0f,encounter.Exposure,.001f);

            // Crossing beams (dua lampu yang bersilangan) in Segment C share a single 1x exposure timer.
            encounter.SetSegment(2);
            Assert.IsTrue(encounter.TryRestoreCheckpoint());
            encounter.ManualTick(1.75f);
            Vector3 crossingSpot=street.transform.TransformPoint(new Vector3(1.25f,.04f,33.8f));
            Assert.IsTrue(motor.TryRestorePose(crossingSpot,street.transform.rotation,false));
            Assert.IsTrue(encounter.IsFloorPointVisible(3,crossingSpot),"Patrolling Shadow beam reaches the crossing zone");
            Assert.IsTrue(encounter.IsFloorPointVisible(4,crossingSpot) || encounter.IsFloorPointVisible(5,crossingSpot),"Police car headlamp beam crosses the same zone");
            encounter.ManualTick(.5f);
            Assert.IsTrue(encounter.IsIlluminated);
            Assert.AreEqual(.5f,encounter.Exposure,.001f,"Overlapping beams must fill one shared timer at 1x speed");
            Object.Destroy(street.gameObject);
        }

        [UnityTest]
        public IEnumerator LandmarkAndModulesMatchThePlayableStreet()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity");
            yield return null;
            var player=Object.FindAnyObjectByType<FirstPersonPlayer>();
            var motor=player.GetComponent<FirstPersonMotor>();
            player.enabled=false;
            var street=Object.Instantiate(Resources.Load<GameObject>("ChapterTwoStreet")).GetComponent<ChapterTwoStreet>();
            Assert.IsTrue(street.Enter(motor));
            street.Encounter.AutoTick=false;
            var names=street.GetComponentsInChildren<MeshFilter>().Where(m=>m.sharedMesh!=null)
                .Select(m=>m.sharedMesh.name).ToArray();
            foreach (var name in new[]{"WarungSri","SedanPatroli","PetiKayu","HaltePasar","BetonPolisi","Trotoar","TasRaka","RukoPasar"})
                Assert.That(names.Any(n=>n.StartsWith(name,StringComparison.Ordinal)),Is.True,name);
            Vector3 sign=street.transform.Find("Halte").TransformPoint(new Vector3(0,3.3f,-1.55f));
            var camera=motor.ViewCamera;
            Vector3 viewport=camera.WorldToViewportPoint(sign);
            Assert.That(viewport.x,Is.InRange(.05f,.95f));
            Assert.That(viewport.y,Is.InRange(.05f,.95f));
            Assert.That(viewport.z,Is.GreaterThan(35));
            Assert.IsFalse(Physics.Linecast(camera.transform.position,sign,out _,~0,QueryTriggerInteraction.Ignore),
                "Spawn must have a clear sightline to the lower face of the shelter sign.");
            if(Environment.GetEnvironmentVariable("NRA_STREET_CAPTURES")=="1" &&
                SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                for (int frame=0; frame<14; frame++) { street.Encounter.ManualTick(.04f); yield return null; }
                Capture(camera,"01-spawn");
                View(motor,street,new Vector3(-2.98f,.04f,6.4f),new Vector3(3.5f,.94f,8.4f),false);
                street.Encounter.ManualTick(.12f);
                Capture(camera,"02-standing");
                motor.Simulate(new FirstPersonFrame{CrouchPressed=true},.07f);
                for(int frame=0;frame<5;frame++) motor.Simulate(default,.07f);
                street.Encounter.ManualTick(.12f);
                Capture(camera,"03-crouching");
                View(motor,street,new Vector3(.6f,.04f,4.2f),new Vector3(-6.5f,1.90f,3.8f),false);
                Capture(camera,"04-warung");
                View(motor,street,new Vector3(-.1f,.04f,33.0f),new Vector3(2.55f,1.8f,38.7f),false);
                street.Encounter.ManualTick(.12f);
                Capture(camera,"05-halte");
                View(motor,street,new Vector3(3.0f,.17f,37.9f),new Vector3(3.05f,.92f,39.13f),true);
                Capture(camera,"06-tas");
                street.Encounter.ResetEncounter();
                Assert.IsTrue(motor.TryRestorePose(street.Checkpoints[1].position,street.Checkpoints[1].rotation,false));
                street.Encounter.ManualTick(2.0f);
                Vector3 guardForward=Vector3.ProjectOnPlane(street.Lamps[2].forward,Vector3.up).normalized;
                Vector3 edgeWorld=street.Lamps[2].position+(Quaternion.AngleAxis(16.5f,Vector3.up)*guardForward)*4.5f;
                Vector3 edgeLocal=street.transform.InverseTransformPoint(edgeWorld);
                edgeLocal.y=.04f;
                View(motor,street,edgeLocal,new Vector3(4.38f,.55f,19.3f),true);
                street.Encounter.ManualTick(.08f);
                street.Encounter.RebuildFootprints(true);
                Capture(camera,"07-mismatch",street.Encounter.Exposure/street.Encounter.CatchSeconds);
                street.Encounter.RebuildFootprints(false);
                Capture(camera,"08-sapuan-selaras",street.Encounter.Exposure/street.Encounter.CatchSeconds);
                street.Encounter.SetSegment(2);
                Assert.IsTrue(street.Encounter.TryRestoreCheckpoint());
                street.Encounter.ManualTick(1.75f);
                View(motor,street,new Vector3(1.25f,.04f,33.8f),new Vector3(-2.6f,.25f,32.2f),true);
                street.Encounter.ManualTick(.35f);
                Capture(camera,"09-persilangan",street.Encounter.Exposure/street.Encounter.CatchSeconds);
            }
            Object.Destroy(street.gameObject);
        }

        static void View(FirstPersonMotor motor,ChapterTwoStreet street,Vector3 from,Vector3 to,bool pitch)
        {
            Vector3 direction=to-from-Vector3.up*1.58f;
            var yaw=Quaternion.LookRotation(new Vector3(direction.x,0,direction.z));
            Assert.IsTrue(motor.TryRestorePose(street.transform.TransformPoint(from),yaw,false),"Camera viewpoint "+from);
            if(pitch) motor.ViewCamera.transform.rotation=Quaternion.LookRotation(direction);
        }

        static void Capture(Camera camera,string name,float exposureRatio=0f)
        {
            var dir=Path.GetFullPath("Captures/ChapterTwo");
            Directory.CreateDirectory(dir);
            var target=RenderTexture.GetTemporary(1440,810,24);
            var previousTarget=camera.targetTexture;
            var previousActive=RenderTexture.active;
            var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target;
                camera.Render();
                RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
                if(exposureRatio>0f)
                {
                    int barX=Mathf.RoundToInt(target.width*.4f),barW=Mathf.RoundToInt(target.width*.2f);
                    int fillW=Mathf.RoundToInt(barW*Mathf.Clamp01(exposureRatio));
                    int barY=Mathf.RoundToInt(target.height*(1f-.84f))-12;
                    var track=new Color(.12f,.12f,.12f);
                    var fill=new Color(1f,.2f,.2f);
                    for(int y=barY;y<barY+12;y++)
                        for(int x=barX;x<barX+barW;x++)
                            image.SetPixel(x,y,x-barX<fillW?fill:track);
                }
                image.Apply();
                File.WriteAllBytes(Path.Combine(dir,name+".png"),image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture=previousTarget;
                RenderTexture.active=previousActive;
                RenderTexture.ReleaseTemporary(target);
                Object.Destroy(image);
            }
        }
    }
}
