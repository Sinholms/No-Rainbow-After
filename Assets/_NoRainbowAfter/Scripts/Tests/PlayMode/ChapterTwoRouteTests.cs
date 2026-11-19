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
        public IEnumerator BothPhysicalRoutesReachShelter()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity");
            yield return null;
            var player = Object.FindAnyObjectByType<FirstPersonPlayer>();
            var motor = player.GetComponent<FirstPersonMotor>();
            player.enabled = false; Time.timeScale = 1f;
            var prefab = Resources.Load<GameObject>("ChapterTwoStreet");
            Assert.That(prefab, Is.Not.Null);
            for (int route = 0; route < 2; route++)
            {
                var obj = Object.Instantiate(prefab);
                var street = obj.GetComponent<ChapterTwoStreet>();
                Assert.That(street.Enter(motor), Is.True, "Street spawn was obstructed");
                street.Encounter.AutoTick = false;
                if (route == 0)
                {
                    street.Encounter.ManualTick(.11f);
                    yield return null;
                    System.IO.Directory.CreateDirectory("Captures/ChapterTwo");
                    var rt = new RenderTexture(1280, 720, 24);
                    var oldTarget = motor.ViewCamera.targetTexture; var oldActive = RenderTexture.active;
                    motor.ViewCamera.targetTexture = rt; motor.ViewCamera.Render(); RenderTexture.active = rt;
                    var image = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                    image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); image.Apply();
                    System.IO.File.WriteAllBytes("Captures/ChapterTwo/blockout.png", image.EncodeToPNG());
                    motor.ViewCamera.targetTexture = oldTarget; RenderTexture.active = oldActive;
                    Object.Destroy(image); Object.Destroy(rt);
                }
                street.Encounter.CatchSeconds = 999f; // This checks physical reachability separately from stealth timing.
                Vector2[] stops = route == 0 ? new[] { new Vector2(-4.25f,4), new Vector2(-4.25f,18), new Vector2(-4.1f,23), new Vector2(-3.1f,25), new Vector2(-3.1f,32), new Vector2(0,33), new Vector2(0,37.8f), new Vector2(2.5f,38) }
                    : new[] { new Vector2(-4.25f,4), new Vector2(-4.25f,18), new Vector2(-4.1f,23), new Vector2(3.2f,23), new Vector2(3.2f,31), new Vector2(3.2f,36), new Vector2(2.5f,38) };
                float maxExposure = 0;
                foreach (var stop in stops)
                {
                    Vector3 target = street.transform.TransformPoint(new Vector3(stop.x,0,stop.y));
                    int steps = 0;
                    while (Vector2.Distance(new Vector2(motor.transform.position.x,motor.transform.position.z), new Vector2(target.x,target.z)) > .3f && steps++ < 500)
                    {
                        Vector3 delta = target - motor.transform.position; delta.y = 0;
                        Vector3 local = motor.transform.InverseTransformDirection(delta.normalized);
                        motor.Simulate(new FirstPersonFrame { Move = new Vector2(local.x,local.z) }, .04f);
                        street.Encounter.ManualTick(.04f); street.CheckProgress();
                        maxExposure = Mathf.Max(maxExposure, street.Encounter.Exposure);
                        if (steps % 80 == 0) yield return null;
                    }
                    Assert.That(steps, Is.LessThan(500), "Route " + route + " blocked toward " + stop + " at " + street.transform.InverseTransformPoint(motor.transform.position));
                }
                Assert.That(street.AtShelter, Is.True, "Route " + route + " did not reach shelter");
                Debug.Log("[Chapter2 route] " + route + " maximum continuous exposure: " + maxExposure);
                Object.Destroy(obj); yield return null;
            }
        }
    }
}
