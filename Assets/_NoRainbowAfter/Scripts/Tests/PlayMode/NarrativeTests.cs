using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NoRainbowAfter.AI;
using NoRainbowAfter.Interaction;
using NoRainbowAfter.Level;
using NoRainbowAfter.Narrative;
using NoRainbowAfter.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

[SetUpFixture]
public sealed class NarrativeStorageIsolation
{
    private string folder;
    [OneTimeSetUp] public void SetUp()
    {
        folder = Path.Combine(Path.GetTempPath(), "NRA-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        ChapterDirector.StoragePathOverride = Path.Combine(folder, "clues.json");
    }
    [OneTimeTearDown] public void TearDown()
    {
        ChapterDirector.StoragePathOverride = null;
        if (File.Exists(Path.Combine(folder, "clues.json"))) File.Delete(Path.Combine(folder, "clues.json"));
        if (File.Exists(Path.Combine(folder, "clues.json.tmp"))) File.Delete(Path.Combine(folder, "clues.json.tmp"));
        if (Directory.Exists(folder)) Directory.Delete(folder);
    }
}

public sealed class NarrativeTests
{
    private sealed class Encounter : IShadowEncounter
    {
        public bool IsReady { get; set; }
        public event Action Ready;
        public Action<EncounterResponse> Callback;
        public int Begins;
        public EncounterResponse Immediate;
        public void Begin(Action<EncounterResponse> callback) { Begins++; Callback = callback; if (Immediate != EncounterResponse.None) callback(Immediate); }
        public void Signal() => Ready?.Invoke();
    }
    private const string Scene = "Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity";
    [TearDown] public void RestoreTime() { Time.timeScale = 1f; }

    [UnityTest] public IEnumerator BlockedEarlySlipAndDeferredFightRemainOrdered()
    {
        yield return RunEncounter(EncounterResponse.Fight);
    }
    [UnityTest] public IEnumerator BlockedEarlySlipAndDeferredHideRemainOrdered()
    {
        yield return RunEncounter(EncounterResponse.Hide);
    }
    private IEnumerator RunEncounter(EncounterResponse response)
    {
        var beats = new List<string>();
        UnityEngine.Events.UnityAction<Scene, LoadSceneMode> capture = (scene, mode) =>
        {
            var loaded = Object.FindAnyObjectByType<ChapterDirector>();
            loaded.BeatCompleted += beats.Add;
        };
        SceneManager.sceneLoaded += capture;
        yield return SceneManager.LoadSceneAsync(Scene);
        SceneManager.sceneLoaded -= capture;
        yield return null;
        var player = Object.FindAnyObjectByType<FirstPersonPlayer>();
        player.SetPaused(false);
        var interactor = player.GetComponent<PlayerInteractor>();
        var director = player.GetComponent<ChapterDirector>();
        Assert.That(director, Is.Not.Null);
        Assert.That(director.BeatIndex, Is.EqualTo(1));
        Assert.That(beats, Is.EqualTo(new[] { "C1-01" }));
        int transitions = 0; director.ChapterTransitionRequested += () => transitions++;
        var slip = Object.FindObjectsByType<SceneInteractable>(FindObjectsInactive.Exclude).Single(x => x.StableId == "INT_OVERTIME");
        slip.Interact(interactor);
        Assert.That(interactor.Flags.Contains("objective.overtime"), Is.False);
        Assert.That(interactor.IsInspecting, Is.False);
        Assert.That(slip.Prompt, Is.Null);
        Assert.That(director.BeatIndex, Is.EqualTo(1));
        interactor.CloseInspection();
        director.SamplePosition(6.1f); director.SamplePosition(4.4f);
        Assert.That(director.LoopIndex, Is.Zero, "Book gates narrative crossing.");
        var book = Object.FindObjectsByType<SceneInteractable>(FindObjectsInactive.Exclude).Single(x => x.StableId == "INT_BOOK");
        book.Interact(interactor); interactor.CloseInspection();
        director.SamplePosition(5.1f); director.SamplePosition(4.9f);
        Assert.That(director.LoopIndex, Is.Zero, "Doorway jitter is not a trip.");
        slip.Interact(interactor);
        Assert.That(interactor.Flags.Contains("objective.overtime"), Is.False, "Reading the book alone must not unlock the slip.");
        director.SamplePosition(6.1f);
        Assert.That(slip.Prompt, Is.Null, "The player must return to the room first.");
        director.SamplePosition(4.4f);
        CollectionAssert.AreEqual(new[] { "C1-01", "C1-02" }, beats);
        Assert.That(slip.Prompt, Is.Not.Null);
        Assert.That(director.HasReadOvertimeSlip, Is.False);
        slip.Interact(interactor);
        Assert.That(interactor.IsInspecting, Is.True);
        interactor.CloseInspection();
        Assert.That(director.State.HasClue("CLUE_02"), Is.True);
        CollectionAssert.AreEqual(new[] { "C1-01", "C1-02", "C1-03" }, beats);
        Assert.That(director.AwaitingEncounter, Is.True);
        var old = new Encounter { IsReady = true };
        director.SetEncounter(old);
        director.SamplePosition(6.1f);
        Assert.That(old.Begins, Is.EqualTo(1));
        var replacement = new Encounter { IsReady = true };
        director.SetEncounter(replacement);
        Assert.That(replacement.Begins, Is.EqualTo(1));
        old.Callback(response);
        Assert.That(director.BeatIndex, Is.EqualTo(4));
        replacement.Callback(EncounterResponse.None);
        Assert.That(director.BeatIndex, Is.EqualTo(4));
        replacement.Callback(response); replacement.Callback(response);
        CollectionAssert.AreEqual(new[] { "C1-01", "C1-02", "C1-03", "C1-04", "C1-05", "C1-06" }, beats);
        Assert.That(transitions, Is.Zero);
        Assert.That(director.FinishChapterOne(), Is.True);
        Assert.That(director.FinishChapterOne(), Is.False);
        Assert.That(transitions, Is.EqualTo(1));
        Assert.That(director.State.Chapter1Response, Is.EqualTo(response));
    }
    [UnityTest] public IEnumerator EarlyOvertimeFlagCannotSkipReadingAfterFirstReturn()
    {
        yield return SceneManager.LoadSceneAsync(Scene); yield return null;
        var player = Object.FindAnyObjectByType<FirstPersonPlayer>(); player.SetPaused(false);
        var director = player.GetComponent<ChapterDirector>();
        var interactor = player.GetComponent<PlayerInteractor>();
        var tracker = player.GetComponent<ObjectiveTracker>();
        var slip = Object.FindObjectsByType<SceneInteractable>(FindObjectsInactive.Exclude).Single(target => target.StableId == "INT_OVERTIME");
        var exit = Object.FindAnyObjectByType<SharedCorridorDoor>();
        Assert.That(exit, Is.Not.Null);
        // Older or externally supplied flags must not replay an out-of-order read.
        interactor.Flags.Set("objective.overtime");
        interactor.NotifyInteraction("INT_OVERTIME");
        interactor.Flags.Set("objective.book");
        yield return null;
        Assert.That(tracker.IsCompleted("objective.overtime"), Is.False);
        director.SamplePosition(6.1f); director.SamplePosition(4.4f);
        Assert.That(director.BeatIndex, Is.EqualTo(2));
        Assert.That(director.HasReadOvertimeSlip, Is.False);
        yield return null;
        var hint = (UnityEngine.UI.Text)typeof(ObjectiveTracker)
            .GetField("hint", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(tracker);
        Assert.That(hint.text, Is.EqualTo("Periksa slip lembur di kamar."));
        Assert.That(tracker.IsCompleted("objective.overtime"), Is.False);
        exit.Interact(interactor);
        Assert.That(exit.IsOpen, Is.False, "The imported flag must not unlock the exit.");
        slip.Interact(interactor); interactor.CloseInspection();
        yield return null;
        Assert.That(director.BeatIndex, Is.EqualTo(3));
        Assert.That(tracker.IsCompleted("objective.overtime"), Is.True);
        exit.Interact(interactor);
        Assert.That(exit.IsOpen, Is.True, "Reading the slip must unlock the exit.");
        slip.Interact(interactor); interactor.CloseInspection();
        Assert.That(director.BeatIndex, Is.EqualTo(3), "Rereading must not repeat the story beat.");
    }

    [UnityTest] public IEnumerator LoopCapPauseAndCheckpointReset()
    {
        yield return SceneManager.LoadSceneAsync(Scene); yield return null;
        var player = Object.FindAnyObjectByType<FirstPersonPlayer>(); player.SetPaused(false);
        var director = player.GetComponent<ChapterDirector>(); var interactor = player.GetComponent<PlayerInteractor>();
        interactor.Flags.Set("objective.book");
        player.SetPaused(true); director.SamplePosition(6.1f); player.SetPaused(false); director.SamplePosition(4.4f);
        Assert.That(director.LoopIndex, Is.Zero);
        interactor.ShowInspection("Test", "Test"); director.SamplePosition(6.1f); interactor.CloseInspection(); director.SamplePosition(4.4f);
        Assert.That(director.LoopIndex, Is.Zero);
        director.SamplePosition(6.1f); Assert.That(player.GetComponent<DormCheckpoint>().TryRestore(), Is.True); director.SamplePosition(4.4f);
        Assert.That(director.LoopIndex, Is.Zero);
        for (int i=0; i<5; i++) { director.SamplePosition(6.1f); director.SamplePosition(4.4f); }
        Assert.That(director.LoopIndex, Is.EqualTo(3));
    }
    [Test] public void ClueStorageDeduplicatesAndProtectsUnknownSchema()
    {
        string path = Path.Combine(Path.GetTempPath(), "NRA-clues-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(path, "{\"schemaVersion\":1,\"clueIds\":[\"CLUE_02\",\"CLUE_02\",\"BAD\"]}");
            var state = new GameState(path);
            Assert.That(state.ClueIds.Count, Is.EqualTo(1));
            Assert.That(state.AddClue("CLUE_02"), Is.False);
            Assert.That(state.AddClue("CLUE_03"), Is.True);
            Assert.That(state.AddClue("CLUE_01"), Is.True);
            Assert.That(state.AddClue("CLUE_04"), Is.True);
            Assert.That(state.AddClue(null), Is.False);
            Assert.That(state.AddClue("CLUE_05"), Is.False);
            Assert.That(new GameState(path).ClueIds.Count, Is.EqualTo(4));
            const string future = "{\"schemaVersion\":999,\"clueIds\":[\"CLUE_01\"]}";
            File.WriteAllText(path, future);
            state = new GameState(path); state.AddClue("CLUE_04");
            Assert.That(state.StorageStatus, Is.EqualTo(ClueStorageStatus.InvalidOrUnsupported));
            Assert.That(File.ReadAllText(path), Is.EqualTo(future));
        }
        finally { if (File.Exists(path)) File.Delete(path); if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp"); }
    }
    [UnityTest] public IEnumerator AtmosphereMovesImportedPaperAndRestoresIt()
    {
        yield return SceneManager.LoadSceneAsync(Scene); yield return null;
        var player = Object.FindAnyObjectByType<FirstPersonPlayer>(); player.SetPaused(false);
        var director = player.GetComponent<ChapterDirector>();
        var atmosphere = player.GetComponent<LoopAtmosphere>();
        var note = Resources.FindObjectsOfTypeAll<Transform>().First(t => t.gameObject.scene.IsValid() && t.name == "SM_PROP_OpenNotes");
        var heading = Resources.FindObjectsOfTypeAll<Transform>().First(t => t.gameObject.scene.IsValid() && t.name == "SM_PROP_NotesHeading");
        var light = GameObject.Find("Corridor Near").GetComponent<Light>();
        Vector3 original = note.position, headingOriginal = heading.position;
        Quaternion rotation = note.rotation;
        float intensity = light.intensity, fog = RenderSettings.fogDensity;
        player.GetComponent<PlayerInteractor>().Flags.Set("objective.book");
        director.SamplePosition(6.1f); director.SamplePosition(4.4f);
        yield return new WaitForSeconds(1f);
        Assert.That(Vector3.Distance(note.position, original), Is.GreaterThan(.005f));
        Assert.That(Vector3.Angle(note.rotation * Vector3.forward, rotation * Vector3.forward), Is.LessThan(10f));
        Assert.That(Mathf.Abs(Vector3.Distance(note.position, heading.position) - Vector3.Distance(original, headingOriginal)), Is.LessThan(.001f));
        Assert.That(light.intensity, Is.LessThan(intensity));
        Assert.That(RenderSettings.fogDensity, Is.GreaterThan(fog));
        atmosphere.enabled = false;
        Assert.That(Vector3.Distance(note.position, original), Is.LessThan(.0001f));
        Assert.That(Vector3.Distance(heading.position, headingOriginal), Is.LessThan(.0001f));
        Assert.That(light.intensity, Is.EqualTo(intensity).Within(.0001f));
        Assert.That(RenderSettings.fogDensity, Is.EqualTo(fog).Within(.0001f));
    }

    [UnityTest] public IEnumerator SubtitlesSuspendWhileInspectingAndPaused()
    {
        yield return SceneManager.LoadSceneAsync(Scene); yield return null;
        var player = Object.FindAnyObjectByType<FirstPersonPlayer>(); player.SetPaused(false);
        var subtitles = player.GetComponent<SubtitlePlaceholder>();
        string first = subtitles.CurrentText;
        Assert.That(first, Does.Contain("Hari ini"));
        var interactor = player.GetComponent<PlayerInteractor>();
        interactor.ShowInspection("Test", "Test");
        yield return new WaitForSeconds(.1f);
        Assert.That(subtitles.CurrentText, Is.EqualTo(first));
        Assert.That(subtitles.PendingCount, Is.EqualTo(1));
        interactor.CloseInspection(); player.SetPaused(true);
        yield return new WaitForSecondsRealtime(.1f);
        Assert.That(subtitles.CurrentText, Is.EqualTo(first));
        Assert.That(subtitles.PendingCount, Is.EqualTo(1));
        player.SetPaused(false);
    }
    [UnityTest] public IEnumerator ProviderReadinessAndSynchronousResolutionAreExplicit()
    {
        yield return SceneManager.LoadSceneAsync(Scene); yield return null;
        var player = Object.FindAnyObjectByType<FirstPersonPlayer>(); player.SetPaused(false);
        var interactor = player.GetComponent<PlayerInteractor>();
        var director = player.GetComponent<ChapterDirector>();
        // Clear the scene's installed T06 encounter first so no-provider and unready-provider gating can be tested explicitly.
        director.SetEncounter(null);
        var audio = new List<string>(); director.CueRequested += cue => audio.Add(cue.AudioCueId);
        interactor.Flags.Set("objective.book");
        var slip = Object.FindObjectsByType<SceneInteractable>(FindObjectsInactive.Exclude).Single(x => x.StableId == "INT_OVERTIME");
        director.SamplePosition(6.1f); director.SamplePosition(4.4f);
        slip.Interact(interactor); interactor.CloseInspection();
        director.SamplePosition(6.1f);
        Assert.That(director.AwaitingEncounter, Is.True);
        Assert.That(audio, Does.Not.Contain("AUD_C1_SHADOW"));
        var provider = new Encounter { Immediate = EncounterResponse.Hide };
        director.SetEncounter(provider);
        Assert.That(provider.Begins, Is.Zero);
        player.SetPaused(true); provider.IsReady = true; provider.Signal();
        Assert.That(provider.Begins, Is.Zero);
        player.SetPaused(false); provider.Signal();
        Assert.That(provider.Begins, Is.EqualTo(1));
        Assert.That(director.BeatIndex, Is.EqualTo(6));
        Assert.That(director.State.Chapter1Response, Is.EqualTo(EncounterResponse.Hide));
        Assert.That(audio.Count(x => x == "AUD_C1_SHADOW"), Is.EqualTo(1));
    }

    [UnityTest] public IEnumerator MotorExcursionAcrossDoorwayCountsOnce()
    {
        yield return SceneManager.LoadSceneAsync(Scene); yield return null;
        var player = Object.FindAnyObjectByType<FirstPersonPlayer>(); player.SetPaused(false);
        var director = player.GetComponent<ChapterDirector>();
        player.GetComponent<PlayerInteractor>().Flags.Set("objective.book");
        var motor = player.GetComponent<FirstPersonMotor>();
        var sessionType = Type.GetType("NoRainbowAfter.UI.FirstPersonSession, Assembly-CSharp");
        ((Behaviour)GameObject.Find("UI_FirstPerson").GetComponent(sessionType)).enabled = false;
        Time.timeScale = 1f;
        player.SetPaused(false); player.enabled = false;
        yield return Walk(motor, new Vector2(0, 6.3f));
        yield return Walk(motor, new Vector2(0, 4.2f));
        yield return null;
        Assert.That(director.LoopIndex, Is.EqualTo(1));
    }
    private static IEnumerator Walk(FirstPersonMotor motor, Vector2 destination)
    {
        motor.StopMotion();
        for (int i = 0; i < 900; i++)
        {
            Vector3 p = motor.transform.position;
            Vector2 delta = destination - new Vector2(p.x, p.z);
            if (delta.magnitude < .13f) { motor.StopMotion(); yield break; }
            Vector3 local = motor.transform.InverseTransformDirection(new Vector3(delta.x, 0, delta.y));
            motor.Simulate(new FirstPersonFrame { Move = new Vector2(local.x, local.z).normalized }, .02f);
            if (i % 5 == 0) yield return new WaitForFixedUpdate();
        }
        Assert.Fail("Unable to walk to " + destination);
    }
}
