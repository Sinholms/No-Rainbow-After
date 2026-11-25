using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NoRainbowAfter.AI;
using NoRainbowAfter.Interaction;
using NoRainbowAfter.Narrative;
using NoRainbowAfter.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace NoRainbowAfter.Tests
{
    public sealed class ChapterSceneEntryTests
    {
        private const string Scenes = "Assets/_NoRainbowAfter/Scenes/";
        private string storagePath;
        private string previousStoragePath;
        private ChapterDirector director;
        private FirstPersonPlayer player;

        [SetUp]
        public void SetUp()
        {
            previousStoragePath = ChapterDirector.StoragePathOverride;
            storagePath = Path.Combine(Path.GetTempPath(), $"nra-preview-{Guid.NewGuid():N}.json");
            new GameState(storagePath).AddClue("CLUE_02");
            ChapterDirector.StoragePathOverride = storagePath;
            Time.timeScale = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            ChapterDirector.StoragePathOverride = previousStoragePath;
            File.Delete(storagePath);
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator ChapterOnePreviewAndMasterHaveSeparateClues()
        {
            yield return OpenPreview(1, ChapterTwoBeat.None);
            Assert.That(director.BeatIndex, Is.EqualTo(1));
            Assert.That(director.ChapterOneFinished, Is.False);
            director.State.AddClue("CLUE_01");

            yield return SceneManager.LoadSceneAsync(Scenes + "MasterGameplay.unity", LoadSceneMode.Single);
            yield return null;
            director = Object.FindAnyObjectByType<ChapterDirector>();
            Assert.That(director.PreviewChapter, Is.Zero);
            Assert.That(director.State.ActiveChapter, Is.EqualTo("Chapter1"));
            Assert.That(director.State.ClueIds, Is.EquivalentTo(new[] { "CLUE_02" }));
            Assert.That(SceneManager.GetSceneByPath(Scenes + "Chapter1.unity").isLoaded, Is.False);
        }

        [UnityTest]
        public IEnumerator ChapterTwoPreviewStartsWithThePlayableEssay()
        {
            yield return OpenPreview(2, ChapterTwoBeat.WokeInDorm);
            Assert.That(director.ChapterOneFinished, Is.True);
            Assert.That(director.BeatIndex, Is.Zero);
            Assert.That(director.State.EllaMessageSent, Is.False);
            var laptop = Object.FindAnyObjectByType<LaptopTypingMinigame>();
            laptop.Interact(player.GetComponent<PlayerInteractor>());
            Assert.That(laptop.IsActive, Is.True);
            Assert.That(laptop.TargetSentence, Does.Contain("halte"));
            Assert.That(Object.FindAnyObjectByType<ChapterTwoPhone>(), Is.Not.Null);
            laptop.CloseLaptop();
        }

        [UnityTest]
        public IEnumerator MaximumStressInDormCrouchesImmediatelyAndBreathingPreservesEssay()
        {
            yield return OpenPreview(2, ChapterTwoBeat.WokeInDorm);
            player.SetPaused(false);
            var stress = player.GetComponent<StressBarometer>();
            var motor = player.GetComponent<FirstPersonMotor>();
            var breathing = player.GetComponent<BreathingMinigame>();
            var laptop = Object.FindAnyObjectByType<LaptopTypingMinigame>();
            var interactor = player.GetComponent<PlayerInteractor>();
            Assert.That(stress.CurrentStress, Is.EqualTo(50f));

            laptop.Interact(interactor);
            yield return null;
            foreach (char character in laptop.TargetSentence) laptop.ProcessTypedCharacter(character);
            laptop.SubmitInput();
            Assert.That(laptop.CompletedSentences, Is.EqualTo(1));
            stress.SetStress(99.8f);
            Assert.That(stress.Label.text, Does.EndWith("99%"));
            Assert.That(player.IsBreathRecoveryRequired, Is.False);
            stress.SetStress(98f);
            laptop.ProcessTypedCharacter(laptop.TargetSentence[0] == 'z' ? 'x' : 'z');

            Assert.That(stress.CurrentStress, Is.EqualTo(100f));
            Assert.That(player.IsBreathRecoveryRequired, Is.True);
            Assert.That(motor.IsCrouched, Is.True);
            Assert.That(motor.CrouchBlend, Is.EqualTo(1f).Within(.01f));
            Vector3 position = motor.transform.position;
            motor.Simulate(new FirstPersonFrame { Move = Vector2.up, CrouchPressed = true, SprintHeld = true }, .1f);
            Assert.That(motor.transform.position, Is.EqualTo(position));
            Assert.That(motor.IsCrouched, Is.True);

            string partialEssay = laptop.CurrentInput;
            float remainingTime = laptop.RemainingTime;
            laptop.Tick(0f, enterPressed: false, tabPressed: true);
            yield return null;
            Assert.That(breathing.StartBreathing(), Is.True);
            for (int step = 0; step < 100 && stress.CurrentStress > 45f; step++)
                breathing.Tick(.1f, breathing.IndicatorValue <= breathing.TargetCenter);
            breathing.StopBreathing();

            Assert.That(stress.CurrentStress, Is.EqualTo(45f));
            Assert.That(stress.IsWarningActive, Is.False);
            Assert.That(player.IsBreathRecoveryRequired, Is.False);
            Assert.That(director.State.Chapter2Beat, Is.EqualTo(ChapterTwoBeat.WokeInDorm));
            motor.Simulate(default, .1f);
            Assert.That(motor.IsCrouched, Is.False);

            laptop.Interact(interactor);
            Assert.That(laptop.JudgedEssaySections, Is.EqualTo(1));
            Assert.That(laptop.CurrentInput, Is.EqualTo(partialEssay));
            Assert.That(laptop.RemainingTime, Is.EqualTo(remainingTime));
            laptop.CloseLaptop();
        }

        [UnityTest]
        public IEnumerator MaximumStressInMarketRestartsChapterTwoAndKeepsPreviewClues()
        {
            yield return OpenPreview(2, ChapterTwoBeat.WokeInDorm);
            director.State.AddClue("CLUE_01");
            Assert.That(director.AdvanceChapterTwo(ChapterTwoBeat.OvertimeRequested), Is.True);
            var flow = player.GetComponent<ChapterTwoFlow>();
            typeof(ChapterTwoFlow).GetMethod("UseStreetExit", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(flow, null);
            float streetDeadline = Time.realtimeSinceStartup + 10f;
            while (Time.realtimeSinceStartup < streetDeadline &&
                   (director.State.Chapter2Beat != ChapterTwoBeat.StreetEntered || player.IsTransitionLocked))
                yield return null;
            Assert.That(director.State.Chapter2Beat, Is.EqualTo(ChapterTwoBeat.StreetEntered));
            var street = Object.FindAnyObjectByType<ChapterTwoStreet>();
            Assert.That(street, Is.Not.Null);
            var encounter = street.Encounter;
            encounter.AutoTick = false;
            encounter.RenderFootprints = false;
            encounter.ManualTick(.02f);
            var lamp = street.Lamps[0];
            Vector3 aim = Quaternion.AngleAxis(15f, Vector3.up) * Vector3.ProjectOnPlane(lamp.forward, Vector3.up).normalized;
            Vector3 exposed = lamp.position + aim * 1.95f;
            exposed.y = street.transform.position.y + .04f;
            Assert.That(player.GetComponent<FirstPersonMotor>().TryRestorePose(exposed, Quaternion.identity, false), Is.True);
            var oldDirector = director;
            player.GetComponent<StressBarometer>().SetStress(StressBarometer.MaxStress - ChapterTwoStreet.StressPerCapture);
            for (int i = 0; i < 100 && encounter.CatchCount == 0; i++) encounter.ManualTick(.033f);
            Assert.That(encounter.CatchCount, Is.EqualTo(1));
            float deadline = Time.realtimeSinceStartup + 15f;
            while (Time.realtimeSinceStartup < deadline)
            {
                director = Object.FindAnyObjectByType<ChapterDirector>();
                player = Object.FindAnyObjectByType<FirstPersonPlayer>();
                if (director != null && director != oldDirector && director.State != null &&
                    director.State.Chapter2Beat == ChapterTwoBeat.WokeInDorm && player != null && !player.IsTransitionLocked)
                    break;
                yield return null;
            }
            Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
            Assert.That(director.State.ClueIds, Does.Contain("CLUE_01"));
            Assert.That(new GameState(storagePath).ClueIds, Is.EquivalentTo(new[] { "CLUE_02" }));
            Assert.That(player.transform.position.x, Is.LessThan(2f));
            Assert.That(player.GetComponent<StressBarometer>().CurrentStress, Is.EqualTo(50f));
        }

        private IEnumerator OpenPreview(int chapter, ChapterTwoBeat expectedBeat)
        {
            string path = Scenes + $"Chapter{chapter}.unity";
            yield return SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
            float deadline = Time.realtimeSinceStartup + 10f;
            while (Time.realtimeSinceStartup < deadline)
            {
                director = Object.FindAnyObjectByType<ChapterDirector>();
                player = Object.FindAnyObjectByType<FirstPersonPlayer>();
                if (director?.State != null && director.PreviewChapter == chapter && player != null &&
                    !player.IsTransitionLocked && director.State.Chapter2Beat == expectedBeat)
                    break;

                yield return null;
            }

            Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), $"Chapter {chapter} did not reach a playable state.");
            Assert.That(SceneManager.GetSceneByPath(path).isLoaded, Is.True);
            Assert.That(SceneManager.GetSceneByPath(Scenes + "MasterGameplay.unity").isLoaded, Is.True);
            Assert.That(Object.FindObjectsByType<FirstPersonPlayer>().Length, Is.EqualTo(1));
            Assert.That(director.State.ActiveChapter, Is.EqualTo($"Chapter{chapter}"));
            Assert.That(director.State.ClueIds, Is.Empty);
        }
    }
}
