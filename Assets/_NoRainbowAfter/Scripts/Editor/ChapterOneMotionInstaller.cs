using System;
using System.IO;
using System.Linq;
using NoRainbowAfter.AI;
using UnityEditor;
using UnityEngine;

namespace NoRainbowAfter.Editor
{
    public static class ChapterOneMotionInstaller
    {
        public static void Install()
        {
            const string path = "Assets/_NoRainbowAfter/Art/Models/Characters/ACT_C1_ShadowMotion.fbx";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.importAnimation = true;
            importer.importCameras = importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
            var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
            File.WriteAllText("Docs/QA/C1MotionClips.txt", string.Join("\n", clips.Select(c => c.name + " " + c.length)));
            AnimationClip Clip(string name) => clips.First(c => c.name.EndsWith("|" + name, StringComparison.Ordinal) || c.name == name);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            try
            {
                var animator = instance.GetComponentInChildren<Animator>();
                if (animator != null) animator.enabled = false;
                var playback = instance.AddComponent<ShadowMotionPlayback>();
                playback.ChestHit = Clip("Hit_Chest"); playback.FinalHit = Clip("Hit_Final");
                playback.Grab = Clip("Grab_Player"); playback.Rest = Clip("Rest");
                playback.Spine = instance.GetComponentsInChildren<Transform>().First(t => t.name == "Spine");
                Directory.CreateDirectory("Assets/_NoRainbowAfter/Resources");
                AssetDatabase.Refresh();
                PrefabUtility.SaveAsPrefabAsset(instance, "Assets/_NoRainbowAfter/Resources/C1ShadowMotion.prefab");
                AssetDatabase.SaveAssets();
                Debug.Log("[NRA] C1 motion prepared: " + string.Join(", ", clips.Select(c => c.name)));
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }
    }
}
