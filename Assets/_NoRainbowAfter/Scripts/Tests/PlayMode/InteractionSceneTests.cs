using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NoRainbowAfter.Interaction;
using NoRainbowAfter.Player;
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
        Assert.That(stress.CurrentStress, Is.EqualTo(50f).Within(0.01f), "Stress must start at 50%.");
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
        Assert.That(stress.CurrentStress, Is.EqualTo(52f).Within(0.01f));

        // Expiration auto-submits incomplete text and raises stress (+15% -> 67%)
        laptop.Tick(60.5f, false);
        Assert.That(laptop.SubmittedAttempts, Is.EqualTo(1));
        Assert.That(stress.CurrentStress, Is.EqualTo(67f).Within(0.01f));

        // Another two typos push >= 70% triggering blinking warning; backspace them before typing cleanly
        laptop.ProcessTypedCharacter(laptop.TargetSentence[0] == 'z' ? 'x' : 'z');
        laptop.ProcessTypedCharacter(laptop.TargetSentence[0] == 'z' ? 'x' : 'z');
        Assert.That(stress.CurrentStress, Is.EqualTo(71f).Within(0.01f));
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
        stress.SetStress(74f);
        Assert.That(stress.IsWarningActive, Is.True, "Warning must activate at >= 70% stress.");
        Assert.That(stress.WarningLabel.enabled, Is.True);

        Assert.That(breathing.StartBreathing(), Is.True);
        Assert.That(breathing.IsActive, Is.True);
        Assert.That(player.IsBreathing, Is.True);

        // Inhale while matching inside the green zone: stress drops below 70% and camera nods UP (negative X Euler offset)
        for (int i = 0; i < 15; i++)
            breathing.Tick(0.10f, true);

        Assert.That(breathing.CurrentCameraPitchOffset, Is.LessThan(-0.5f), "Camera must pitch UP when inhaling.");
        Assert.That(breathing.IsInGreenZone, Is.True, "Holding breath cleanly tracks the rising green zone.");
        Assert.That(stress.CurrentStress, Is.LessThan(70f), "Keeping parameter in the green bar must calm stress below 70%.");
        Assert.That(stress.IsWarningActive, Is.False, "Warning must clear once stress drops below 70%.");
        Assert.That(stress.WarningLabel.enabled, Is.False);

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
        var camera = player.GetComponentInChildren<Camera>();
        var interactor = player.GetComponent<PlayerInteractor>();
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

}
