using System.Collections;
using NoRainbowAfter.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NoRainbowAfter.Tests
{
    public sealed class FirstPersonLabTests
    {
        [UnityTest]
        public IEnumerator BlenderLabSupportsCrouchRampStairsAndDoorway()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_NoRainbowAfter/Scenes/FirstPersonTest.unity");
            yield return null;
            var motor = Object.FindAnyObjectByType<FirstPersonMotor>();
            Assert.That(motor, Is.Not.Null);
            motor.GetComponent<FirstPersonPlayer>().enabled = false;
            var cc = motor.GetComponent<CharacterController>();
            float standingHeight = cc.height;
            Assert.That(standingHeight, Is.EqualTo(1.70f).Within(.001f), "Raka's standing capsule must measure 170 cm.");
            Time.timeScale = 1;
            Warp(motor, new Vector3(3, 0.05f, 0));
            Move(motor, 100);
            Assert.That(motor.transform.position.z, Is.LessThan(1f), "Standing capsule must stop at the imported low roof.");
            motor.Simulate(new FirstPersonFrame { CrouchPressed = true }, 0.02f);
            Move(motor, 95);
            Assert.That(motor.transform.position.z, Is.InRange(2f, 4.5f), "Ctrl must permit entering the passage.");
            motor.Simulate(new FirstPersonFrame { CrouchPressed = true }, 0.02f);
            for (int i = 0; i < 20; i++) motor.Simulate(default, 0.02f);
            Assert.That(cc.height, Is.LessThan(1.12f), "Do not expand the capsule into the Blender roof.");
            Move(motor, 150);
            Assert.That(motor.transform.position.z, Is.GreaterThan(5.4f));
            Assert.That(motor.IsCrouched, Is.False);
            Assert.That(cc.height, Is.EqualTo(standingHeight).Within(.001f), "The capsule must return to standing height after exiting the passage.");

            Warp(motor, new Vector3(-3, 0.05f, 3.8f));
            float rampPeak = Move(motor, 180);
            Assert.That(rampPeak, Is.GreaterThan(0.55f), "Player must climb the Blender ramp rather than pass through it.");
            Assert.That(motor.transform.position.z, Is.GreaterThan(8.5f));

            Warp(motor, new Vector3(0, 0.05f, 5));
            float stairPeak = Move(motor, 140);
            Assert.That(stairPeak, Is.GreaterThan(0.55f));
            Assert.That(motor.transform.position.z, Is.GreaterThan(10.8f), "The 0.90m x 2.05m doorway must remain traversable.");
        }

        private static void Warp(FirstPersonMotor motor, Vector3 position)
        {
            var cc = motor.GetComponent<CharacterController>();
            cc.enabled = false;
            motor.transform.SetPositionAndRotation(position, Quaternion.identity);
            cc.enabled = true;
            motor.StopMotion();
            Physics.SyncTransforms();
        }

        private static float Move(FirstPersonMotor motor, int count)
        {
            float peak = motor.transform.position.y;
            for (int i = 0; i < count; i++)
            {
                motor.Simulate(new FirstPersonFrame { Move = Vector2.up }, 0.02f);
                peak = Mathf.Max(peak, motor.transform.position.y);
            }
            return peak;
        }
    }
}
