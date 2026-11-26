using System;
using System.Collections;
using System.Linq;
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
    public sealed class ChapterThreeEndingTests
    {
        private string previousStoragePath;
        private string isolatedFolder;

        [SetUp]
        public void SetUp()
        {
            previousStoragePath = ChapterDirector.StoragePathOverride;
            isolatedFolder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "NRA-C3-" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(isolatedFolder);
            ChapterDirector.StoragePathOverride = System.IO.Path.Combine(isolatedFolder, "clues.json");
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            ChapterDirector.CancelChapterRestart();
            ChapterDirector.StoragePathOverride = previousStoragePath;
            if (System.IO.Directory.Exists(isolatedFolder))
                System.IO.Directory.Delete(isolatedFolder, true);
        }

        [UnityTest]
        public IEnumerator ChapterTwoEssayHasNoPeriodsAndWaterGallonSkipsEssay()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_NoRainbowAfter/Scenes/Chapter2.unity", LoadSceneMode.Single);
            float deadline = Time.realtimeSinceStartup + 10f;
            ChapterDirector director = null;
            FirstPersonPlayer player = null;
            while (Time.realtimeSinceStartup < deadline)
            {
                director = Object.FindAnyObjectByType<ChapterDirector>();
                player = Object.FindAnyObjectByType<FirstPersonPlayer>();
                if (director?.State != null && player != null && !player.IsTransitionLocked &&
                    director.State.Chapter2Beat == ChapterTwoBeat.WokeInDorm)
                    break;
                yield return null;
            }
            Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Chapter 2 preview did not reach WokeInDorm.");

            player.SetPaused(false);
            Time.timeScale = 1f;

            var interactor = player.GetComponent<PlayerInteractor>();
            var laptop = Object.FindAnyObjectByType<LaptopTypingMinigame>();

            Assert.That(laptop.TargetSentence.EndsWith("."), Is.False, "Chapter 2 essay sentences must not end with a period.");

            var gallon = Object.FindObjectsByType<SceneInteractable>(FindObjectsInactive.Exclude)
                .SingleOrDefault(x => x.StableId == "INT_C2_GALLON");
            Assert.That(gallon, Is.Not.Null, "Water gallon must be interactable in Chapter 2.");

            gallon.Interact(interactor);
            Assert.That(interactor.IsInspecting, Is.True);
            Assert.That(laptop.IsEssayComplete, Is.True, "Inspecting the water gallon must skip the essay.");

            interactor.CloseInspection();
            yield return null;

            Assert.That(director.State.Chapter2Beat, Is.EqualTo(ChapterTwoBeat.OvertimeRequested),
                "Closing gallon inspection must advance Chapter 2 to OvertimeRequested.");
            Assert.That(interactor.IsInspecting, Is.True, "Overtime message inspection should open automatically.");
            interactor.CloseInspection();
        }

        [UnityTest]
        public IEnumerator ChapterThreeArenaHasBarriersMysteryObjectiveAndAllThreeEndings()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity");
            yield return null;

            var player = Object.FindAnyObjectByType<FirstPersonPlayer>();
            var motor = player.GetComponent<FirstPersonMotor>();
            var director = player.GetComponent<ChapterDirector>();
            var tracker = player.GetComponent<ObjectiveTracker>();
            var chapterThree = player.GetComponent<ChapterThreeFlow>();
            Assert.That(chapterThree, Is.Not.Null);

            player.SetPaused(false);
            Time.timeScale = 1f;

            chapterThree.AutoTick = false;
            Assert.That(chapterThree.BeginChapterThree(), Is.True, "ChapterThreeArena prefab must load and enter.");
            yield return null;

            var objective = (UnityEngine.UI.Text)typeof(ObjectiveTracker)
                .GetField("objective", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tracker);
            Assert.That(objective.text, Is.EqualTo("???"));
            Assert.That(chapterThree.FaintTimerLabel, Is.Not.Null);
            Assert.That(chapterThree.FaintTimerLabel.color.a, Is.LessThan(0.5f), "Timer must be faint/low-opacity.");

            // Invisible perimeter barrier blocks player from leaving the arena
            Vector3 arenaOrigin = chapterThree.Arena.transform.position;
            float extent = chapterThree.Arena.ArenaHalfExtent;
            Vector3 nearWall = arenaOrigin + new Vector3(0f, 0.08f, extent - 0.8f);
            Assert.That(motor.TryRestorePose(nearWall, Quaternion.identity, false), Is.True);
            for (int i = 0; i < 60; i++)
                motor.Simulate(new FirstPersonFrame { Move = Vector2.up }, 0.033f);
            Assert.That(motor.transform.position.z - arenaOrigin.z, Is.LessThanOrEqualTo(extent + 0.05f),
                "Invisible barrier must prevent exiting the Chapter 3 arena.");

            var ellaProp = typeof(GameState).GetProperty("EllaMessageSent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var interactor = player.GetComponent<PlayerInteractor>();

            // 1. Bad Ending: hide in wardrobe until 2 minutes expire without sending Ella message
            var wardrobe = chapterThree.Arena.GetComponentInChildren<HidingWardrobe>();
            Assert.That(wardrobe, Is.Not.Null);
            wardrobe.Interact(interactor);
            Assert.That(player.IsHiding, Is.True);
            ellaProp.SetValue(director.State, false);
            for (int i = 0; i < 1210 && chapterThree.ResolvedEnding == StoryEnding.None; i++)
                chapterThree.TickChase(0.1f);
            Assert.That(chapterThree.ResolvedEnding, Is.EqualTo(StoryEnding.Bad));
            Assert.That(player.IsHiding, Is.False);

            // Reset & test 2. Hidden Ending: hide in wardrobe until 2 minutes expire after sending Ella message in C2
            var presenter = player.GetComponent<EndingPresentationController>();
            if (presenter != null) Object.Destroy(presenter);
            Object.Destroy(chapterThree);
            yield return null;
            player.SetInspecting(false);
            player.SetTransitionLocked(false);
            chapterThree = player.gameObject.AddComponent<ChapterThreeFlow>();
            chapterThree.AutoTick = false;
            Assert.That(chapterThree.BeginChapterThree(), Is.True);
            wardrobe = chapterThree.Arena.GetComponentInChildren<HidingWardrobe>();
            wardrobe.Interact(interactor);
            Assert.That(player.IsHiding, Is.True);
            ellaProp.SetValue(director.State, true);
            for (int i = 0; i < 1210 && chapterThree.ResolvedEnding == StoryEnding.None; i++)
                chapterThree.TickChase(0.1f);
            Assert.That(chapterThree.ResolvedEnding, Is.EqualTo(StoryEnding.Hidden));
            Assert.That(player.IsHiding, Is.False);

            // Reset & test 3. Good Ending (Reality): approaching monster closely
            presenter = player.GetComponent<EndingPresentationController>();
            if (presenter != null) Object.Destroy(presenter);
            Object.Destroy(chapterThree);
            yield return null;
            player.SetInspecting(false);
            player.SetTransitionLocked(false);
            chapterThree = player.gameObject.AddComponent<ChapterThreeFlow>();
            chapterThree.AutoTick = false;
            Assert.That(chapterThree.BeginChapterThree(), Is.True);
            Vector3 mPos = chapterThree.MonsterTransform.position;
            Assert.That(motor.TryRestorePose(mPos + Vector3.back * 1.1f, Quaternion.identity, false), Is.True);
            chapterThree.TickChase(0.05f);
            Assert.That(chapterThree.ResolvedEnding, Is.EqualTo(StoryEnding.Good));
        }
    }
}
