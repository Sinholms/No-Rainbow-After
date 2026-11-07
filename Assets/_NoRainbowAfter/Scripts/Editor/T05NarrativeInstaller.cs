using System;
using System.IO;
using System.Linq;
using NoRainbowAfter.Interaction;
using NoRainbowAfter.Level;
using NoRainbowAfter.Narrative;
using NoRainbowAfter.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NoRainbowAfter.Editor
{
    public static class T05NarrativeInstaller
    {
        private const string ScenePath = "Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity";
        [MenuItem("No Rainbow After/Install T05 Narrative")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scenes before installation.");
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.isLoaded || SceneManager.GetActiveScene() != scene) throw new InvalidOperationException("Open and activate MasterGameplay first.");
            Transform player = Find(scene, "Player_Raka_FPP"), chapter = Find(scene, "Chapter1Root");
            Transform note = Find(scene, "SM_PROP_OpenNotes"), slip = Find(scene, "SM_PROP_WorkSlip");
            Transform light = Find(scene, "Corridor Near"), title = Find(scene, "TestObjective"), hint = Find(scene, "ControlHint");
            if (player == null || chapter == null || note == null || slip == null || light == null || title == null || hint == null ||
                player.GetComponent<PlayerInteractor>() == null || player.GetComponent<FirstPersonPlayer>() == null ||
                player.GetComponent<FirstPersonMotor>() == null || light.GetComponent<Light>() == null ||
                title.GetComponent<Text>() == null || hint.GetComponent<Text>() == null)
                throw new InvalidOperationException("Required T03/T04 scene objects missing; no changes made.");
            var parts = chapter.GetComponentsInChildren<Transform>(true);
            var noteParts = parts.Where(t => t.name == "SM_PROP_OpenNotes" || t.name == "SM_PROP_NotesHeading" || t.name.StartsWith("SM_PROP_NotesLine", StringComparison.Ordinal)).ToArray();
            var slipParts = parts.Where(t => t.name == "SM_PROP_WorkSlip" || t.name == "SM_PROP_WorkSlipHeading").ToArray();
            if (noteParts.Length < 3 || slipParts.Length != 2) throw new InvalidOperationException("Missing note or slip visual parts; no changes made.");
            Directory.CreateDirectory("Temp/T05Baseline");
            string backup = "Temp/T05Baseline/MasterGameplay.before-t05.unity";
            if (!File.Exists(backup)) File.Copy(ScenePath, backup);
            var director = GetOrAdd<ChapterDirector>(player.gameObject);
            director.Configure(player.GetComponent<PlayerInteractor>(), player.GetComponent<FirstPersonPlayer>(), player.GetComponent<FirstPersonMotor>(), player, 5f);
            var old = player.GetComponent<ChapterOneObjective>();
            if (old != null) old.enabled = false;
            GetOrAdd<ObjectiveTracker>(player.gameObject).Configure(director, player.GetComponent<PlayerInteractor>(), title.GetComponent<Text>(), hint.GetComponent<Text>());
            GetOrAdd<SubtitlePlaceholder>(player.gameObject).Configure(director, player.GetComponent<FirstPersonPlayer>());
            var notes = Group(chapter, note, "C1_LoopNotes");
            var slips = Group(chapter, slip, "C1_LoopSlip");
            foreach (var part in noteParts.Concat(slipParts))
            {
                part.gameObject.isStatic = false;
                if (part.gameObject.isStatic) throw new InvalidOperationException("Moving visual part remains static: " + part.name);
            }
            GetOrAdd<LoopAtmosphere>(player.gameObject).Configure(director, noteParts, slipParts, notes, slips, light.GetComponent<Light>());
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save MasterGameplay.");
            Debug.Log("[NRA] T05 installed. Encounter remains pending a provider.");
        }
        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }
        private static Transform Find(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
        private static Transform Group(Transform chapter, Transform pivot, string name)
        {
            Transform group = null;
            foreach (var t in chapter.GetComponentsInChildren<Transform>(true)) if (t.name == name) { group = t; break; }
            if (group == null) { group = new GameObject(name).transform; group.SetParent(chapter, true); }
            group.position = pivot.position; group.rotation = Quaternion.identity;
            return group;
        }
    }
}
