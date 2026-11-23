using System;
using System.Collections;
using System.IO;
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
