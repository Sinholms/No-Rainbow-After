using System;
using System.IO;
using NoRainbowAfter.Interaction;
using NoRainbowAfter.Level;
using NoRainbowAfter.Player;
using NoRainbowAfter.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NoRainbowAfter.Editor
{
    public static class ChapterOneInstaller
    {
        private const string ScenePath = "Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity";
        [MenuItem("No Rainbow After/Install T04 Chapter One Readiness")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scenes before installation.");
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.isLoaded || SceneManager.GetActiveScene() != scene) throw new InvalidOperationException("Open and activate MasterGameplay first.");
            Transform env = null, player = null, chapter = null, objective = null, hint = null, ui = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                env = env != null ? env : Find(root.transform, "ENV_KosAtmosphere");
                player = player != null ? player : Find(root.transform, "Player_Raka_FPP");
                chapter = chapter != null ? chapter : Find(root.transform, "Chapter1Root");
                objective = objective != null ? objective : Find(root.transform, "TestObjective");
                hint = hint != null ? hint : Find(root.transform, "ControlHint");
                ui = ui != null ? ui : Find(root.transform, "UI_FirstPerson");
            }
            var motor = player != null ? player.GetComponent<FirstPersonMotor>() : null;
            var interactor = player != null ? player.GetComponent<PlayerInteractor>() : null;
            if (env == null || motor == null || interactor == null || objective == null || hint == null ||
                ui == null || ui.GetComponent<FirstPersonSession>() == null ||
                objective.GetComponent<Text>() == null || hint.GetComponent<Text>() == null)
                throw new InvalidOperationException("Required T03 scene objects missing; no changes made.");
            Directory.CreateDirectory("Temp/T04Baseline");
            string backup = "Temp/T04Baseline/MasterGameplay.before-t04.unity";
            if (!File.Exists(backup)) File.Copy(ScenePath, backup);
            if (chapter == null) chapter = new GameObject("Chapter1Root").transform;
            env.SetParent(chapter, true);
            Transform spawn = Find(chapter, "DormSpawn_T04");
            if (spawn == null)
            {
                spawn = new GameObject("DormSpawn_T04").transform;
                spawn.SetParent(chapter, false);
                spawn.SetPositionAndRotation(player.position, player.rotation);
            }
            var checkpoint = player.GetComponent<DormCheckpoint>();
            if (checkpoint == null) checkpoint = player.gameObject.AddComponent<DormCheckpoint>();
            checkpoint.Configure(motor, spawn);
            ConfigureCheckpointButton(ui, checkpoint);
            var bridge = player.GetComponent<ChapterOneObjective>();
            if (bridge == null) bridge = player.gameObject.AddComponent<ChapterOneObjective>();
            bridge.Configure(interactor, objective.GetComponent<Text>(), hint.GetComponent<Text>());
            SetupCover(chapter, "C1_COVER_LEFT", new Vector3(-1.45f, 0.9f, 9f));
            SetupCover(chapter, "C1_COVER_RIGHT", new Vector3(1.45f, 0.9f, 13f));
            foreach (var mesh in env.GetComponentsInChildren<MeshFilter>(true))
            {
                string n = mesh.name;
                if (!(n.StartsWith("SM_ENV_NicheBack") || n.StartsWith("SM_ENV_NicheFloor") ||
                      n.StartsWith("SM_ENV_NicheEnd") || n.StartsWith("SM_PROP_UtilityCabinet") ||
                      n.StartsWith("SM_ENV_CorridorCeiling"))) continue;
                var box = mesh.GetComponent<BoxCollider>();
                if (box != null && mesh.sharedMesh != null) { box.center = mesh.sharedMesh.bounds.center; box.size = mesh.sharedMesh.bounds.size; }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save MasterGameplay.");
            Debug.Log("[NRA] T04 installed. Validate route, cover and checkpoint in Play Mode.");
        }
        private static Transform Find(Transform root, string name)
        { foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t; return null; }
        private static void ConfigureCheckpointButton(Transform ui, DormCheckpoint checkpoint)
        {
            var pause = ui.Find("PausePanel");
            var resume = pause != null ? pause.Find("ResumeButton") : null;
            if (pause == null || resume == null || resume.GetComponent<Button>() == null)
                throw new InvalidOperationException("Pause panel template is incomplete.");
            var buttonTransform = pause.Find("CheckpointButton");
            if (buttonTransform == null)
            {
                var clone = UnityEngine.Object.Instantiate(resume.gameObject, pause, false);
                clone.name = "CheckpointButton";
                buttonTransform = clone.transform;
            }
            var rect = buttonTransform.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -400f);
            var label = buttonTransform.GetComponentInChildren<Text>(true);
            if (label != null) label.text = "Kembali ke titik awal";
            var session = ui.GetComponent<FirstPersonSession>();
            Assign(session, "checkpointButton", buttonTransform.GetComponent<Button>());
            Assign(session, "checkpoint", checkpoint);
        }
        private static void Assign(UnityEngine.Object target, string field, UnityEngine.Object value)
        { var data = new SerializedObject(target); data.FindProperty(field).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
        private static void SetupCover(Transform chapter, string id, Vector3 position)
        {
            var target = Find(chapter, id);
            if (target == null) { target = new GameObject(id).transform; target.SetParent(chapter, false); }
            target.position = position; target.rotation = Quaternion.identity;
            var box = target.GetComponent<BoxCollider>();
            if (box == null) box = target.gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true; box.size = new Vector3(.8f, 1.8f, 1.1f);
            var zone = target.GetComponent<CoverZone>();
            if (zone == null) zone = target.gameObject.AddComponent<CoverZone>();
            zone.Configure(id);
        }
    }
}
