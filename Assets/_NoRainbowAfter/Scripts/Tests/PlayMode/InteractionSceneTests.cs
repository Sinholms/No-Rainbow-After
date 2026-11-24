using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NoRainbowAfter.Interaction;
using NoRainbowAfter.Player;
using NoRainbowAfter.Narrative;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class InteractionSceneTests
{
    [UnityTest] public IEnumerator SceneHasInteractablesAndFKeyClosesInspectionWhileEscPauses()
    {
        yield return SceneManager.LoadSceneAsync("Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity");
        yield return null;
        var targets = Object.FindObjectsByType<SceneInteractable>(FindObjectsInactive.Exclude);
        CollectionAssert.AreEquivalent(new[] { "INT_BOOK", "INT_OVERTIME", "INT_C1_DOOR" }, targets.Select(x => x.StableId).ToArray());
        var laptop = Object.FindAnyObjectByType<LaptopTypingMinigame>();
        Assert.That(laptop, Is.Not.Null);
        Assert.That(laptop.StableId, Is.EqualTo("INT_LAPTOP"));

        var player = Object.FindAnyObjectByType<FirstPersonPlayer>();
        var interactor = player.GetComponent<PlayerInteractor>();
        Assert.That(interactor, Is.Not.Null);
        var session = GameObject.Find("UI_FirstPerson").GetComponent(Type.GetType("NoRainbowAfter.UI.FirstPersonSession, Assembly-CSharp"));
        var escape = session.GetType().GetMethod("TogglePause", BindingFlags.Instance | BindingFlags.NonPublic);
        var closeInspect = typeof(PlayerInteractor).GetMethod("OnCloseInspectRequested", BindingFlags.Instance | BindingFlags.NonPublic);

        var start = player.transform.position;
        interactor.ShowInspection("Uji", "Teks");
        Assert.That(player.IsInspecting, Is.True);
        Assert.That(player.IsPaused, Is.False);
        yield return null;
        Assert.That(player.transform.position, Is.EqualTo(start));

        // Esc pauses without closing document inspection; F closes document inspection.
        escape.Invoke(session, null);
        Assert.That(player.IsPaused, Is.True);
        Assert.That(interactor.IsInspecting, Is.True);
        escape.Invoke(session, null);
        Assert.That(player.IsPaused, Is.False);
        Assert.That(interactor.IsInspecting, Is.True);

        closeInspect.Invoke(interactor, null);
        Assert.That(interactor.IsInspecting, Is.False);
        Assert.That(player.IsInspecting, Is.False);
        Assert.That(player.IsPaused, Is.False);
    }

    [UnityTest] public IEnumerator LaptopMinigameStressBarometerAndRakaBodyWork()
    {
        yield return SceneManager.LoadSceneAsync("Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity");
        yield return null;

        var player = Object.FindAnyObjectByType<FirstPersonPlayer>();
        var interactor = player.GetComponent<PlayerInteractor>();
        var stress = player.GetComponent<StressBarometer>();
        var body = player.GetComponent<FirstPersonBodyVisual>();
        var laptop = Object.FindAnyObjectByType<LaptopTypingMinigame>();

        Assert.That(stress, Is.Not.Null, "StressBarometer must exist on Player_Raka_FPP.");
        Assert.That(Object.FindObjectsByType<StressBarometer>(FindObjectsInactive.Include).Length, Is.EqualTo(1), "Must have only one StressBarometer instance.");
        Assert.That(stress.OverlayCanvas, Is.Not.Null, "StressBarometer must render on its single dedicated overlay Canvas.");
        Assert.That(stress.CurrentStress, Is.EqualTo(75f).Within(0.01f), "Chapter 1 must start at its 75% floor.");
        Assert.That(stress.FillImage, Is.Not.Null);
        Assert.That(stress.FillImage.color.r, Is.GreaterThan(stress.FillImage.color.g * 2f), "Stress bar must be red.");

        string sampleRich = LaptopTypingMinigame.BuildMonkeytypeRichText("laptop", "lap");
        Assert.That(sampleRich, Does.Contain("#FFD54F"), "Matched typed characters must be yellow.");
        Assert.That(sampleRich, Does.Not.Contain("<u>").And.Not.Contain("</u>").And.Not.Contain("<br"), "Must not emit unsupported markup tags.");

        Assert.That(body, Is.Not.Null, "FirstPersonBodyVisual must exist on Player_Raka_FPP.");
        Assert.That(body.BodyInstance, Is.Not.Null);
        Assert.That(body.BodyInstance.GetComponentsInChildren<Collider>(true).Length, Is.Zero, "FPP body must not have colliders.");

        var objLabel = GameObject.Find("TestObjective").GetComponent<UnityEngine.UI.Text>();
        Assert.That(objLabel.text, Is.EqualTo("Mengerjakan tugas"));

        // Interact [E] to open Monkeytype laptop minigame (60s initial timer, no duplicate StressMirror)
        laptop.Interact(interactor);
        yield return null;
        Assert.That(laptop.IsActive, Is.True);
        Assert.That(player.IsUsingLaptop, Is.True);
        Assert.That(laptop.CurrentSentenceTimeLimit, Is.EqualTo(60f).Within(0.01f), "First sentence timer must be 60s.");
        Assert.That(GameObject.Find("StressMirror"), Is.Null, "Laptop must not spawn a duplicate StressMirror HUD layer.");

        // 1x [TAB] press exits the laptop immediately
        laptop.Tick(0.05f, enterPressed: false, tabPressed: true);
        Assert.That(laptop.IsActive, Is.False, "1x [Tab] must immediately exit laptop.");
        Assert.That(player.IsUsingLaptop, Is.False);

        // Re-open laptop to verify typo penalty, timeout auto-submit, 70% stress warning, 5 sentences (60->30s) & auto-exit
        laptop.Interact(interactor);
        yield return null;
        string firstTarget = laptop.TargetSentence;
        char wrongChar = firstTarget[0] == 'z' ? 'x' : 'z';
        laptop.ProcessTypedCharacter(wrongChar);
        Assert.That(stress.CurrentStress, Is.EqualTo(77f).Within(0.01f));

        // Expiration auto-submits incomplete text and raises stress (+15% -> 92%).
        laptop.Tick(60.5f, false);
        Assert.That(laptop.SubmittedAttempts, Is.EqualTo(1));
        Assert.That(stress.CurrentStress, Is.EqualTo(92f).Within(0.01f));

        // The high-stress warning remains visible after two more typos.
        laptop.ProcessTypedCharacter(laptop.TargetSentence[0] == 'z' ? 'x' : 'z');
        laptop.ProcessTypedCharacter(laptop.TargetSentence[0] == 'z' ? 'x' : 'z');
        Assert.That(stress.CurrentStress, Is.EqualTo(96f).Within(0.01f));
        Assert.That(stress.IsWarningActive, Is.True);
        Assert.That(stress.WarningLabel.enabled, Is.True, "Stress >= 70% must show blinking warning.");
        laptop.ProcessTypedCharacter('\b');
        laptop.ProcessTypedCharacter('\b');

        // Complete all 5 sentences cleanly (60 -> 52.5 -> 45 -> 37.5 -> 30s) -> auto-exits after 5th completed sentence
        float[] expectedLimits = { 60f, 52.5f, 45f, 37.5f, 30f };
        Assert.That(LaptopTypingMinigame.RequiredSentencesForObjective, Is.EqualTo(5));
        for (int i = 0; i < LaptopTypingMinigame.RequiredSentencesForObjective; i++)
        {
            Assert.That(laptop.IsActive, Is.True);
            Assert.That(laptop.CurrentSentenceTimeLimit, Is.EqualTo(expectedLimits[i]).Within(0.01f));
            foreach (char c in laptop.TargetSentence)
                laptop.ProcessTypedCharacter(c);
            laptop.Tick(0.05f, enterPressed: true);
        }

        Assert.That(laptop.CompletedSentences, Is.EqualTo(5));
        Assert.That(interactor.Flags.Contains("objective.laptop"), Is.True);
        Assert.That(interactor.Flags.Contains("objective.book"), Is.True);
        Assert.That(laptop.IsActive, Is.False, "Laptop must auto-exit automatically once 5 sentences are completed.");
        Assert.That(player.IsUsingLaptop, Is.False, "Player must return to FPP mode after auto-exit.");
    }

    [UnityTest] public IEnumerator BreathingMinigameReducesStressWithGreenBarAndCameraPitchNod()
    {
        yield return SceneManager.LoadSceneAsync("Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity");
        yield return null;

        var player = Object.FindAnyObjectByType<FirstPersonPlayer>();
        var motor = player.GetComponent<FirstPersonMotor>();
        var stress = player.GetComponent<StressBarometer>();
        var breathing = player.GetComponent<BreathingMinigame>();

        Assert.That(breathing, Is.Not.Null, "BreathingMinigame must be attached on Player_Raka_FPP.");
        stress.SetStress(85f);
        Assert.That(stress.IsWarningActive, Is.True, "Warning must activate at >= 70% stress.");
        Assert.That(stress.WarningLabel.enabled, Is.True);

        Assert.That(breathing.StartBreathing(), Is.True);
        Assert.That(breathing.IsActive, Is.True);
        Assert.That(player.IsBreathing, Is.True);

        // Matching the green zone reaches the chapter's floor while the camera nods up.
        for (int i = 0; i < 15; i++)
            breathing.Tick(0.10f, true);

        Assert.That(breathing.CurrentCameraPitchOffset, Is.LessThan(-0.5f), "Camera must pitch UP when inhaling.");
        Assert.That(breathing.IsInGreenZone, Is.True, "Holding breath cleanly tracks the rising green zone.");
        Assert.That(stress.CurrentStress, Is.EqualTo(75f));
        Assert.That(stress.IsWarningActive, Is.True);
        Assert.That(stress.WarningLabel.text, Does.Contain("Batas pemulihan"));

        // Exhale: camera nods DOWN (positive X Euler offset)
        for (int i = 0; i < 35; i++)
            breathing.Tick(0.10f, false);

        Assert.That(breathing.CurrentCameraPitchOffset, Is.GreaterThan(0.5f), "Camera must pitch DOWN when exhaling.");

        breathing.StopBreathing();
        Assert.That(breathing.IsActive, Is.False);
        Assert.That(player.IsBreathing, Is.False);
        Assert.That(motor.BreathingPitchOffset, Is.EqualTo(0f).Within(0.001f));
    }
    [UnityTest] public IEnumerator InteractionRecastsWhenNewWallAppears()
    {
        yield return SceneManager.LoadSceneAsync("Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity");
        yield return null;
        var player = Object.FindAnyObjectByType<FirstPersonPlayer>();
        var sessionType = Type.GetType("NoRainbowAfter.UI.FirstPersonSession, Assembly-CSharp");
        if (sessionType != null)
        {
            var session = GameObject.Find("UI_FirstPerson")?.GetComponent(sessionType);
            var pauseFocus = sessionType.GetMethod("PauseForFocusLoss", BindingFlags.Instance | BindingFlags.NonPublic);
            if (session != null && pauseFocus != null)
                player.FocusLost -= (Action)Delegate.CreateDelegate(typeof(Action), session, pauseFocus);
        }
        player.SetPaused(false);
        Time.timeScale = 1f;
        var interactor = player.GetComponent<PlayerInteractor>();
        var camera = player.GetComponentInChildren<Camera>();
        var controller = player.GetComponent<CharacterController>();
        controller.enabled = false;
        player.transform.position = new Vector3(100, 100, 100);
        camera.transform.rotation = Quaternion.identity;
        var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            target.transform.position = camera.transform.position + Vector3.forward * 2;
            target.AddComponent<SceneInteractable>().Configure("TEST", "Uji", "Uji", "test.flag", "Uji", "Uji", false);
            wall.transform.position = camera.transform.position + Vector3.forward;
            wall.SetActive(false);
            Physics.SyncTransforms();
            typeof(PlayerInteractor).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(interactor, null);
            Assert.That(interactor.Focused, Is.Not.Null);
            wall.SetActive(true); Physics.SyncTransforms();
            interactor.Interact();
            Assert.That(interactor.Flags.Contains("test.flag"), Is.False);
            Assert.That(interactor.IsInspecting, Is.False);
        }
        finally { Object.Destroy(target); Object.Destroy(wall); }
    }

    [UnityTest] public IEnumerator SlipCanBeTargetedStandingAndCrouchedWithoutSelfOcclusion()
    {
        yield return SceneManager.LoadSceneAsync("Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity");
        yield return null;
        var player = Object.FindAnyObjectByType<FirstPersonPlayer>();
        var motor = player.GetComponent<FirstPersonMotor>();
        var controller = player.GetComponent<CharacterController>();
        player.enabled = false;
        controller.enabled = false;
        player.transform.position = new Vector3(.7f, .04f, 2.6f);
        controller.enabled = true;
        var slip = Object.FindObjectsByType<SceneInteractable>(FindObjectsInactive.Exclude).Single(x => x.StableId == "INT_OVERTIME");
        var director = player.GetComponent<ChapterDirector>();
        player.SetPaused(false);
        var camera = player.GetComponentInChildren<Camera>();
        var interactor = player.GetComponent<PlayerInteractor>();
        var laptop = Object.FindAnyObjectByType<LaptopTypingMinigame>();
        Physics.SyncTransforms();
        var laptopCenter = laptop.GetComponent<Collider>().bounds.center;
        camera.transform.rotation = Quaternion.LookRotation(laptopCenter - camera.transform.position);
        interactor.Interact();
        Assert.That(laptop.IsActive, Is.True, "The laptop must be reachable from the study desk.");
        yield return null;
        for (int i = 0; i < LaptopTypingMinigame.RequiredSentencesForObjective; i++)
        {
            foreach (char character in laptop.TargetSentence) laptop.ProcessTypedCharacter(character);
            laptop.Tick(.05f, enterPressed: true);
        }
        Assert.That(interactor.Flags.Contains("objective.laptop"), Is.True);
        Assert.That(slip.Prompt, Is.Null, "Finishing the laptop task alone must not unlock the slip.");
        director.SamplePosition(6.1f);
        director.SamplePosition(4.4f);
        yield return new WaitForSeconds(1f);
        Physics.SyncTransforms();
        Vector3 point = slip.GetComponent<Collider>().bounds.center;
        Assert.That(PlayerInteractor.FindTarget(new Ray(camera.transform.position, (point - camera.transform.position).normalized), player.transform), Is.SameAs(slip), "Standing ray should reach slip past the player's own capsule.");
        camera.transform.rotation = Quaternion.LookRotation(point - camera.transform.position);
        interactor.Interact();
        Assert.That(interactor.IsInspecting, Is.True, "Standing E should open the slip.");
        Assert.That(interactor.Flags.Contains("objective.overtime"), Is.True);
        interactor.CloseInspection();
        motor.Simulate(new FirstPersonFrame { CrouchPressed = true }, .02f);
        for (int i = 0; i < 100; i++) motor.Simulate(default, .02f);
        Physics.SyncTransforms();
        Assert.That(motor.IsCrouched, Is.True);
        Assert.That(PlayerInteractor.FindTarget(new Ray(camera.transform.position, (point - camera.transform.position).normalized), player.transform), Is.SameAs(slip), "Crouched ray should reach slip past the player's own capsule.");
        camera.transform.rotation = Quaternion.LookRotation(point - camera.transform.position);
        interactor.Interact();
        Assert.That(interactor.IsInspecting, Is.True, "Crouched E should open the slip.");
        interactor.CloseInspection();
    }

    [UnityTest] public IEnumerator CrouchingRakaBodyRigKeepsFeetGroundedAndCameraClearOfLanyardAndChest()
    {
        yield return SceneManager.LoadSceneAsync("Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity");
        yield return null;

        var player = Object.FindAnyObjectByType<FirstPersonPlayer>();
        var motor = player.GetComponent<FirstPersonMotor>();
        var body = player.GetComponent<FirstPersonBodyVisual>();
        var cam = motor.ViewCamera;

        var sessionType = Type.GetType("NoRainbowAfter.UI.FirstPersonSession, Assembly-CSharp");
        if (sessionType != null)
        {
            var session = GameObject.Find("UI_FirstPerson")?.GetComponent(sessionType);
            var pauseFocus = sessionType.GetMethod("PauseForFocusLoss", BindingFlags.Instance | BindingFlags.NonPublic);
            if (session != null && pauseFocus != null)
                player.FocusLost -= (Action)Delegate.CreateDelegate(typeof(Action), session, pauseFocus);
        }
        player.enabled = false;
        player.SetPaused(false);
        Time.timeScale = 1f;

        Assert.That(body, Is.Not.Null);
        Assert.That(body.BodyInstance, Is.Not.Null);
        Assert.That(body.HasArmatureRig, Is.True, "ACT_Raka_StudentBody must use Generic Armature bones.");
        Assert.That(body.PelvisBone, Is.Not.Null);
        Assert.That(body.ChestBone, Is.Not.Null);
        Assert.That(body.LanyardBone, Is.Not.Null);
        Assert.That(body.LeftFootBone, Is.Not.Null);
        Assert.That(body.RightFootBone, Is.Not.Null);
        Assert.That(body.BodyInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length, Is.GreaterThan(0),
            "Rigged FPP body must render via SkinnedMeshRenderer.");

        // 1) Verify upright Standing pose (0.88m pelvis, ~1.18m chest, grounded feet) before crouching
        Assert.That(motor.CrouchBlend, Is.LessThan(0.01f));
        body.ApplyPoseNow(0.02f);

        float leftFootStandRelY = body.LeftFootBone.position.y - player.transform.position.y;
        float rightFootStandRelY = body.RightFootBone.position.y - player.transform.position.y;
        Assert.That(leftFootStandRelY, Is.InRange(0.04f, 0.14f), "Standing left foot must remain grounded on floor.");
        Assert.That(rightFootStandRelY, Is.InRange(0.04f, 0.14f), "Standing right foot must remain grounded on floor.");

        Vector3 pelvisStandLocal = player.transform.InverseTransformPoint(body.PelvisBone.position);
        Vector3 chestStandLocal = player.transform.InverseTransformPoint(body.ChestBone.position);
        Vector3 lanyardStandLocal = player.transform.InverseTransformPoint(body.LanyardBone.position);
        Assert.That(pelvisStandLocal.y, Is.InRange(0.82f, 0.94f), "Standing pelvis rest pose must stay at 0.88m.");
        Assert.That(chestStandLocal.y, Is.InRange(1.12f, 1.24f), "Standing chest rest pose must stay at ~1.18m.");
        Assert.That(lanyardStandLocal.y, Is.InRange(1.18f, 1.30f), "Standing lanyard must sit on upright chest (~1.24m).");
        Assert.That(lanyardStandLocal.z, Is.GreaterThan(chestStandLocal.z + 0.05f),
            "Standing lanyard must be on front chest (+Z ahead of spine/chest centerline).");

        // 2) Transition to full Crouch and verify true squat heights + camera clearance across 0/45/75 deg pitch
        motor.Simulate(new FirstPersonFrame { CrouchPressed = true }, 0.02f);
        for (int i = 0; i < 60; i++)
            motor.Simulate(default, 0.02f);

        Assert.That(motor.IsCrouched, Is.True);
        Assert.That(motor.CrouchBlend, Is.GreaterThan(0.99f));

        body.ApplyPoseNow(0.02f);

        float leftFootCrouchRelY = body.LeftFootBone.position.y - player.transform.position.y;
        float rightFootCrouchRelY = body.RightFootBone.position.y - player.transform.position.y;
        Assert.That(leftFootCrouchRelY, Is.InRange(0.04f, 0.14f), "Crouched left foot must stay grounded above floor.");
        Assert.That(rightFootCrouchRelY, Is.InRange(0.04f, 0.14f), "Crouched right foot must stay grounded above floor.");

        Vector3 camLocal = player.transform.InverseTransformPoint(cam.transform.position);
        Vector3 pelvisCrouchLocal = player.transform.InverseTransformPoint(body.PelvisBone.position);
        Vector3 chestLocal = player.transform.InverseTransformPoint(body.ChestBone.position);
        Vector3 lanyardLocal = player.transform.InverseTransformPoint(body.LanyardBone.position);
        Assert.That(pelvisCrouchLocal.y, Is.InRange(0.32f, 0.44f), "Crouched pelvis must sit at ~0.38m (no double-squat).");
        Assert.That(chestLocal.y, Is.InRange(0.58f, 0.76f), "Crouched chest must sit naturally at ~0.68m.");
        Assert.That(lanyardLocal.y, Is.InRange(0.60f, 0.78f), "Crouched lanyard must stay tucked safely below camera.");
        Assert.That(chestLocal.y, Is.LessThan(camLocal.y - 0.12f), "Chest must sit safely below crouched eye height.");
        Assert.That(chestLocal.z, Is.LessThan(-0.05f), "Chest must remain behind the crouched camera lens.");
        Assert.That(lanyardLocal.y, Is.LessThan(camLocal.y - 0.16f), "Lanyard must sit safely below crouched eye height.");
        Assert.That(lanyardLocal.z, Is.LessThan(-0.05f), "Lanyard must remain behind the crouched camera lens.");

        float[] testPitches = { 0f, 45f, 75f };
        foreach (float targetPitch in testPitches)
        {
            motor.Simulate(new FirstPersonFrame
            {
                Look = new Vector2(0f, -(targetPitch - motor.Pitch) / motor.MouseSensitivity)
            }, 0.02f);
            body.ApplyPoseNow(0.02f);

            Assert.That(Vector3.Distance(cam.transform.position, body.LanyardBone.position), Is.GreaterThan(0.22f),
                $"Lanyard must stay clear of camera at pitch {targetPitch}.");

            foreach (Transform bone in new[] { body.LanyardBone, body.ChestBone })
            {
                Vector3 vp = cam.WorldToViewportPoint(bone.position);
                bool blockingCenter = vp.z > 0f && vp.z < 0.25f &&
                                      vp.x >= 0.15f && vp.x <= 0.85f &&
                                      vp.y >= 0.15f && vp.y <= 0.85f;
                Assert.That(blockingCenter, Is.False,
                    $"Bone {bone.name} must not block center viewport at crouch pitch {targetPitch} (vp={vp}).");
            }
        }
    }

}
