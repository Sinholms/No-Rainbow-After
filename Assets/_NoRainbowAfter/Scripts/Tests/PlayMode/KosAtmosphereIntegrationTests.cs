using System;
using System.Collections;
using NoRainbowAfter.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace NoRainbowAfter.Tests
{
    public sealed class KosAtmosphereIntegrationTests
    {
        private const string ScenePath = "Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity";
        private const string EnabledKey = "NRA.CircularViewEnabled";
        private bool hadEnabledPreference;
        private int oldEnabledPreference;

        [SetUp]
        public void RememberPreferences()
        {
            hadEnabledPreference = PlayerPrefs.HasKey(EnabledKey);
            oldEnabledPreference = PlayerPrefs.GetInt(EnabledKey, 1);
            PlayerPrefs.SetInt(EnabledKey, 1);
            Time.timeScale = 1;
        }

        [TearDown]
        public void RestorePreferences()
        {
            Time.timeScale = 1;
            if (hadEnabledPreference) PlayerPrefs.SetInt(EnabledKey, oldEnabledPreference);
            else PlayerPrefs.DeleteKey(EnabledKey);
            PlayerPrefs.Save();
        }

        [UnityTest]
        public IEnumerator GameplaySceneHasOnePlayerAndPassableDoorway()
        {
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            Assert.That(cameras.Length, Is.EqualTo(1), "Gameplay should have one active camera.");
            Assert.That(GameObject.Find("ENV_KosAtmosphere"), Is.Not.Null);
            Assert.That(GameObject.Find("T02_BlenderLab"), Is.Null);
            var driver = Object.FindAnyObjectByType<FirstPersonPlayer>();
            Assert.That(driver, Is.Not.Null);
            Assert.That(driver.isActiveAndEnabled, Is.True, "Gameplay input must start enabled.");
            var motor = driver.GetComponent<FirstPersonMotor>();
            driver.enabled = false;
            var controller = motor.GetComponent<CharacterController>();
            controller.enabled = false;
            motor.transform.SetPositionAndRotation(new Vector3(0.15f, 0.04f, 1.1f), Quaternion.identity);
            controller.enabled = true;
            motor.StopMotion();
            Physics.SyncTransforms();
            for (int i = 0; i < 450; i++) motor.Simulate(new FirstPersonFrame { Move = Vector2.up }, 0.02f);
            Assert.That(motor.transform.position.z, Is.GreaterThan(10f), "The doorway and corridor should be traversable from the room spawn.");
        }

        [UnityTest]
        public IEnumerator PauseStopsPlayerAndHidesCircularMask()
        {
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            var driver = Object.FindAnyObjectByType<FirstPersonPlayer>();
            Assert.That(driver, Is.Not.Null);
            var ui = GameObject.Find("UI_FirstPerson");
            Assert.That(ui, Is.Not.Null);
            Type sessionType = Type.GetType("NoRainbowAfter.UI.FirstPersonSession, Assembly-CSharp");
            Type maskType = Type.GetType("NoRainbowAfter.UI.CircularViewMask, Assembly-CSharp");
            Assert.That(sessionType, Is.Not.Null); Assert.That(maskType, Is.Not.Null);
            var session = ui.GetComponent(sessionType);
            var mask = ui.GetComponentInChildren(maskType, true);
            Assert.That(session, Is.Not.Null); Assert.That(mask, Is.Not.Null);
            var setPaused = sessionType.GetMethod("SetPaused");
            var effectEnabled = maskType.GetProperty("EffectEnabled");
            Assert.That(setPaused, Is.Not.Null); Assert.That(effectEnabled, Is.Not.Null);
            setPaused.Invoke(session, new object[] { true });
            Assert.That(Time.timeScale, Is.EqualTo(0)); Assert.That(driver.IsPaused, Is.True);
            Assert.That((bool)effectEnabled.GetValue(mask), Is.False);
            setPaused.Invoke(session, new object[] { false });
            Assert.That(Time.timeScale, Is.EqualTo(1)); Assert.That(driver.IsPaused, Is.False);
            Assert.That((bool)effectEnabled.GetValue(mask), Is.True);
        }
    }
}
