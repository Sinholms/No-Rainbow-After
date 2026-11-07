using System;
using System.IO;
using NoRainbowAfter.Interaction;
using NoRainbowAfter.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NoRainbowAfter.Editor
{
    public static class InteractionInstaller
    {
        private const string Path = "Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity";
        [MenuItem("No Rainbow After/Install T03 Interaction")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scenes before installing interaction.");
            var scene = SceneManager.GetSceneByPath(Path);
            if (!scene.isLoaded) throw new InvalidOperationException("Open MasterGameplay first.");
            GameObject environment = null, player = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                var env = Find(root.transform, "ENV_KosAtmosphere");
                if (env != null) environment = env.gameObject;
                if (root.name == "Player_Raka_FPP") player = root;
            }
            if (environment == null || player == null || player.GetComponent<FirstPersonPlayer>() == null) throw new InvalidOperationException("Kos environment or player missing.");
            var book = Find(environment.transform, "SM_PROP_StackedTextbook");
            var slip = Find(environment.transform, "SM_PROP_WorkSlip");
            var door = Find(environment.transform, "SM_PROP_HallClosedDoor");
            if (book == null || slip == null || door == null) throw new InvalidOperationException("Interaction artwork missing; no changes made.");
            int layer = EnsureInteractableLayer();
            Directory.CreateDirectory("Temp/T03Baseline");
            string backup = "Temp/T03Baseline/MasterGameplay.before-interaction.unity";
            if (!File.Exists(backup)) File.Copy(Path, backup);
            if (player.GetComponent<PlayerInteractor>() == null) player.AddComponent<PlayerInteractor>();
            book.gameObject.layer = layer; slip.gameObject.layer = layer; door.gameObject.layer = layer;
            Setup(book, "INT_BOOK", "Buku", "Ambil Buku", "objective.book", "Buku kuliah", "Buku ini masih harus kubaca. Setidaknya aku bisa mulai dari satu halaman.", true);
            Setup(slip, "INT_OVERTIME", "Slip lembur", "Periksa Slip", "objective.overtime", "Slip lembur", "Catatan lembur\n\nGiliran tambahan dicatat setelah jam kerja.\nUpahnya akan membantu biaya kuliah bulan ini.", false);
            Setup(door, "INT_C1_DOOR", "Pintu kamar", "Periksa Pintu", "objective.doorChecked", "Pintu kamar", "Terkunci. Pintu ini belum bisa dibuka.", false);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save MasterGameplay.");
            Debug.Log("[NRA] T03 interaction installed. Verify reach and readability in Play Mode.");
        }
        private static int EnsureInteractableLayer()
        {
            var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (asset.Length == 0) throw new InvalidOperationException("Could not load layer settings.");
            var serialized = new SerializedObject(asset[0]);
            var layers = serialized.FindProperty("layers");
            int empty = -1;
            for (int i = 8; i < layers.arraySize; i++)
            {
                var entry = layers.GetArrayElementAtIndex(i);
                if (entry.stringValue == "Interactable") return i;
                if (empty < 0 && string.IsNullOrEmpty(entry.stringValue)) empty = i;
            }
            if (empty < 0) throw new InvalidOperationException("No free user layer for Interactable.");
            layers.GetArrayElementAtIndex(empty).stringValue = "Interactable";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return empty;
        }
        private static Transform Find(Transform root, string name)
        { foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t; return null; }
        private static void Setup(Transform target, string id, string name, string prompt, string flag, string title, string body, bool once)
        {
            var collider = target.GetComponent<Collider>();
            if (collider == null)
            {
                var mesh = target.GetComponent<MeshFilter>();
                if (mesh == null || mesh.sharedMesh == null) throw new InvalidOperationException("Missing target mesh: " + target.name);
                var box = target.gameObject.AddComponent<BoxCollider>(); box.center = mesh.sharedMesh.bounds.center; box.size = mesh.sharedMesh.bounds.size;
            }
            var interactable = target.GetComponent<SceneInteractable>() ?? target.gameObject.AddComponent<SceneInteractable>();
            interactable.Configure(id, name, prompt, flag, title, body, once);
            EditorUtility.SetDirty(interactable);
        }
    }
}
