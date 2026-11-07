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
    [UnityTest] public IEnumerator SceneHasThreeUniqueTargetsAndEscapeClosesBeforePause()
    {
        yield return SceneManager.LoadSceneAsync("Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity");
        yield return null;
        var targets = Object.FindObjectsByType<SceneInteractable>(FindObjectsInactive.Exclude);
        CollectionAssert.AreEquivalent(new[] { "INT_BOOK", "INT_OVERTIME", "INT_C1_DOOR" }, targets.Select(x => x.StableId).ToArray());
        var player = Object.FindAnyObjectByType<FirstPersonPlayer>();
        var interactor = player.GetComponent<PlayerInteractor>();
        Assert.That(interactor, Is.Not.Null);
        var session = GameObject.Find("UI_FirstPerson").GetComponent(Type.GetType("NoRainbowAfter.UI.FirstPersonSession, Assembly-CSharp"));
        var escape = session.GetType().GetMethod("TogglePause", BindingFlags.Instance | BindingFlags.NonPublic);
        var start = player.transform.position;
        interactor.ShowInspection("Uji", "Teks");
        Assert.That(player.IsInspecting, Is.True);
        Assert.That(player.IsPaused, Is.False);
        yield return null;
        Assert.That(player.transform.position, Is.EqualTo(start));
        escape.Invoke(session, null);
        Assert.That(interactor.IsInspecting, Is.False);
        Assert.That(player.IsInspecting, Is.False);
        Assert.That(player.IsPaused, Is.False);
        escape.Invoke(session, null);
        Assert.That(player.IsPaused, Is.True);
        escape.Invoke(session, null);
        Assert.That(player.IsPaused, Is.False);
    }
    [UnityTest] public IEnumerator InteractionRecastsWhenNewWallAppears()
    {
        yield return SceneManager.LoadSceneAsync("Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity");
        yield return null;
        var player = Object.FindAnyObjectByType<FirstPersonPlayer>();
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
