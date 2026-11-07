using NoRainbowAfter.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NoRainbowAfter.Tests
{
    public sealed class FirstPersonInputTests : InputTestFixture
    {
        private InputActionAsset asset;
        private FirstPersonInputSource source;

        [SetUp]
        public void CreateActions()
        {
            asset = FirstPersonInputSource.CreateDefaultAsset();
            source = new FirstPersonInputSource(asset);
            source.Enable();
        }

        [TearDown]
        public void DestroyActions()
        {
            source.Dispose();
            Object.DestroyImmediate(asset);
        }

        [Test]
        public void WasdShiftAndLeftCtrlReachTheMotorFrame()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            Press(keyboard.wKey);
            Assert.That(source.ReadFrame().Move, Is.EqualTo(Vector2.up));
            Press(keyboard.dKey);
            Assert.That(source.ReadFrame().Move.magnitude, Is.EqualTo(1).Within(0.001f));
            Press(keyboard.leftShiftKey);
            Assert.That(source.ReadFrame().SprintHeld, Is.True);
            Press(keyboard.leftCtrlKey);
            Assert.That(source.ReadFrame().CrouchPressed, Is.True);
            Release(keyboard.leftCtrlKey);
            Press(keyboard.cKey);
            Assert.That(source.ReadFrame().CrouchPressed, Is.False, "Template C binding must not replace design.md's Ctrl.");
        }

        [Test]
        public void InteractUsesEAndCannotFireWhileGameplayLocked()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            int requests = 0;
            source.InteractRequested += () => requests++;
            Press(keyboard.eKey);
            Assert.That(requests, Is.EqualTo(1));
            source.SetGameplayEnabled(false);
            Release(keyboard.eKey); Press(keyboard.eKey);
            Assert.That(requests, Is.EqualTo(1));
            source.SetGameplayEnabled(true);
            Release(keyboard.eKey); Press(keyboard.eKey);
            Assert.That(requests, Is.EqualTo(2));
        }

        [Test]
        public void MouseDeltaReachesLookWithoutSpeedScaling()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            Set(mouse.delta, new Vector2(21, -7));
            Assert.That(source.ReadFrame().Look, Is.EqualTo(new Vector2(21, -7)));
        }

        [Test]
        public void PauseStopsGameplayInputButEscapeStillWorksForResume()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            int pauseRequests = 0;
            source.PauseRequested += () => pauseRequests++;
            Press(keyboard.escapeKey);
            Assert.That(pauseRequests, Is.EqualTo(1));
            source.SetGameplayEnabled(false);
            Press(keyboard.wKey);
            Assert.That(source.ReadFrame().Move, Is.EqualTo(Vector2.zero));
            Release(keyboard.escapeKey);
            Press(keyboard.escapeKey);
            Assert.That(pauseRequests, Is.EqualTo(2));
            source.SetGameplayEnabled(true);
            Release(keyboard.wKey); Press(keyboard.wKey);
            Assert.That(source.ReadFrame().Move, Is.EqualTo(Vector2.up));
        }

        [Test]
        public void FightUsesMouseLeftButtonAndCannotFireWhileGameplayLocked()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            int fightRequests = 0;
            source.FightRequested += () => fightRequests++;
            Press(mouse.leftButton);
            Assert.That(fightRequests, Is.EqualTo(1));
            source.SetGameplayEnabled(false);
            Release(mouse.leftButton);
            Press(mouse.leftButton);
            Assert.That(fightRequests, Is.EqualTo(1));
            source.SetGameplayEnabled(true);
            Release(mouse.leftButton);
            Press(mouse.leftButton);
            Assert.That(fightRequests, Is.EqualTo(2));
        }
    }
}
