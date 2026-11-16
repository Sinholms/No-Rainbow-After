using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NoRainbowAfter.AI;
using NoRainbowAfter.Level;
using NoRainbowAfter.Narrative;
using NoRainbowAfter.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace NoRainbowAfter.Editor
{
    public static class T06ShadowEncounterInstaller
    {
        private const string ScenePath = "Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity";
        private const string ModelPath = "Assets/_NoRainbowAfter/Art/Models/Characters/ACT_001_ShadowPlaceholder_v1.fbx";
        private const string HeroPrefabPath = "Assets/_NoRainbowAfter/Art/Prefabs/Characters/ACT_Shadow_Raka.prefab";
        private const string MaterialDir = "Assets/_NoRainbowAfter/Art/Materials/Characters/";
        private const string PrefabPath = "Assets/_NoRainbowAfter/Art/Prefabs/PFB_ACT_001_ShadowPlaceholder.prefab";
        private const string SceneInstanceName = "ACT_001_ShadowPlaceholder";
        private static readonly Vector3 CorridorMonsterSpawn = new Vector3(90f, 0.12f, -1.65f);
        private static readonly Quaternion CorridorMonsterRotation = Quaternion.Euler(0f, -90f, 0f);

        private static readonly Dictionary<string, string> MeshMaterials = new Dictionary<string, string>
        {
            { "SM_ACT_ShadowLower", "MAT_Shadow_Skin" },
            { "SM_ACT_ShadowLowerCloth", "MAT_Shadow_Cloth" },
            { "SM_ACT_ShadowTorso", "MAT_Shadow_Skin" },
            { "SM_ACT_ShadowJacket", "MAT_Shadow_Cloth" },
            { "SM_ACT_ShadowHead", "MAT_Shadow_Skin" },
            { "SM_ACT_ShadowFaceWrap", "MAT_Shadow_Cloth" },
            { "SM_ACT_ShadowArmLeft", "MAT_Shadow_Skin" },
            { "SM_ACT_ShadowArmRight", "MAT_Shadow_Skin" },
            { "SM_ACT_ShadowPapers", "MAT_Shadow_Paper" }
        };

        [MenuItem("No Rainbow After/Install T06 Shadow Encounter")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode first.");

            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save open scenes before installation.");

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.isLoaded || SceneManager.GetActiveScene() != scene)
                throw new InvalidOperationException("Open and activate MasterGameplay first.");

            if (!File.Exists(ModelPath))
                throw new InvalidOperationException("Missing Blender FBX placeholder at " + ModelPath);

            Transform playerTransform = Find(scene, "Player_Raka_FPP");
            Transform chapterRoot = Find(scene, "Chapter1Root");
            Transform leftCoverObj = Find(scene, "C1_COVER_LEFT");
            Transform rightCoverObj = Find(scene, "C1_COVER_RIGHT");

            if (playerTransform == null || chapterRoot == null || leftCoverObj == null || rightCoverObj == null)
                throw new InvalidOperationException("Required T04/T05 scene objects missing; no changes made.");

            ChapterDirector director = playerTransform.GetComponent<ChapterDirector>();
            FirstPersonPlayer player = playerTransform.GetComponent<FirstPersonPlayer>();
            FirstPersonMotor motor = playerTransform.GetComponent<FirstPersonMotor>();
            DormCheckpoint checkpoint = playerTransform.GetComponent<DormCheckpoint>();
            SubtitlePlaceholder subtitles = playerTransform.GetComponent<SubtitlePlaceholder>();
            CoverZone leftCover = leftCoverObj.GetComponent<CoverZone>();
            CoverZone rightCover = rightCoverObj.GetComponent<CoverZone>();

            if (director == null || player == null || motor == null || checkpoint == null || subtitles == null ||
                leftCover == null || rightCover == null)
                throw new InvalidOperationException("Required T04/T05 components missing; no changes made.");

            Directory.CreateDirectory("Temp/T06Baseline");
            string backup = "Temp/T06Baseline/MasterGameplay.before-t06.unity";
            if (!File.Exists(backup))
                File.Copy(ScenePath, backup);

            GameObject fbxModel = ConfigureModelImport();
            Dictionary<string, Material> materials = CreateOrUpdateMaterials();
            GameObject prefab = BuildOrUpdatePrefab(fbxModel, materials);

            GameObject instance = InstallOrReuseSceneInstance(scene, chapterRoot, prefab);
            ShadowPlaceholderVisual visual = instance.GetComponent<ShadowPlaceholderVisual>();
            ShadowEncounterController encounter = instance.GetComponent<ShadowEncounterController>();

            encounter.Configure(director, player, motor, checkpoint, new[] { leftCover, rightCover }, visual);
            encounter.EnableFirstEscape();
            var encounterSo = new SerializedObject(encounter);
            encounterSo.FindProperty("dormantPosition").vector3Value = CorridorMonsterSpawn;
            encounterSo.FindProperty("allowFirstEscape").boolValue = true;
            encounterSo.ApplyModifiedPropertiesWithoutUndo();
            subtitles.Configure(director, player, encounter);

            EditorUtility.SetDirty(instance);
            EditorUtility.SetDirty(encounter);
            EditorUtility.SetDirty(visual);
            EditorUtility.SetDirty(subtitles);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save MasterGameplay.");
            AssetDatabase.SaveAssets();

            Debug.Log("[NRA] T06 shadow placeholder encounter installed in MasterGameplay.");
        }

        private static GameObject ConfigureModelImport()
        {
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null)
                throw new InvalidOperationException("Could not configure ModelImporter for " + ModelPath);

            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importLights = false;
            importer.importCameras = false;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.SaveAndReimport();

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
                throw new InvalidOperationException("Failed to load imported FBX at " + ModelPath);
            return model;
        }

        private static Dictionary<string, Material> CreateOrUpdateMaterials()
        {
            Directory.CreateDirectory(MaterialDir);
            Directory.CreateDirectory("Assets/_NoRainbowAfter/Art/Prefabs");
            AssetDatabase.Refresh();

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
                throw new InvalidOperationException("URP Lit shader is required.");

            var result = new Dictionary<string, Material>
            {
                { "MAT_Shadow_Skin", UpsertMaterial(urpLit, "MAT_Shadow_Skin", new Color(0.048f, 0.052f, 0.060f, 1f), 0f, 0.28f) },
                { "MAT_Shadow_Cloth", UpsertMaterial(urpLit, "MAT_Shadow_Cloth", new Color(0.072f, 0.082f, 0.096f, 1f), 0f, 0.20f) },
                { "MAT_Shadow_Paper", UpsertMaterial(urpLit, "MAT_Shadow_Paper", new Color(0.520f, 0.490f, 0.430f, 1f), 0f, 0.24f) }
            };
            return result;
        }

        private static Material UpsertMaterial(Shader shader, string name, Color baseColor, float metallic, float smoothness)
        {
            var temp = new Material(shader);
            temp.SetColor("_BaseColor", baseColor);
            temp.SetFloat("_Metallic", metallic);
            temp.SetFloat("_Smoothness", smoothness);

            string path = MaterialDir + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(temp, path);
                return temp;
            }

            EditorUtility.CopySerialized(temp, existing);
            UnityEngine.Object.DestroyImmediate(temp);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static GameObject BuildOrUpdatePrefab(GameObject fbxModel, Dictionary<string, Material> materials)
        {
            var root = new GameObject(SceneInstanceName);
            try
            {
                var cc = root.AddComponent<CharacterController>();
                cc.radius = 0.23f;
                cc.height = 1.80f;
                cc.center = new Vector3(0f, 0.90f, 0f);
                cc.stepOffset = 0.20f;
                cc.slopeLimit = 45f;
                cc.skinWidth = 0.02f;
                cc.minMoveDistance = 0f;

                var visual = root.AddComponent<ShadowPlaceholderVisual>();
                root.AddComponent<ShadowEncounterController>();

                var wrapper = new GameObject("VisualWrapper").transform;
                wrapper.SetParent(root.transform, false);
                wrapper.localPosition = Vector3.zero;
                wrapper.localRotation = Quaternion.identity;
                wrapper.localScale = Vector3.one;

                var modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(fbxModel);
                modelInstance.name = fbxModel.name;
                modelInstance.transform.SetParent(wrapper, false);
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localScale = Vector3.one;

                Transform upperPivot = null;
                foreach (Transform t in modelInstance.GetComponentsInChildren<Transform>(true))
                {
                    t.gameObject.isStatic = false;
                    if (t.name == "PIV_ShadowUpper")
                        upperPivot = t;
                }

                foreach (MeshRenderer renderer in modelInstance.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (!MeshMaterials.TryGetValue(renderer.name, out string matName))
                        throw new InvalidOperationException("Unspecified shadow placeholder mesh: " + renderer.name);
                    renderer.sharedMaterial = materials[matName];
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                    renderer.receiveShadows = true;
                }

                if (!File.Exists(HeroPrefabPath))
                    RakaAssetImport.Prepare();
                GameObject heroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeroPrefabPath);
                if (heroPrefab != null)
                {
                    var heroInstance = (GameObject)PrefabUtility.InstantiatePrefab(heroPrefab);
                    heroInstance.name = "ACT_Shadow_Raka";
                    heroInstance.transform.SetParent(wrapper, false);
                    heroInstance.transform.localPosition = Vector3.zero;
                    heroInstance.transform.localRotation = Quaternion.identity;
                    heroInstance.transform.localScale = Vector3.one;
                    foreach (MeshRenderer placeholderRenderer in modelInstance.GetComponentsInChildren<MeshRenderer>(true))
                        placeholderRenderer.enabled = false;
                }

                if (upperPivot == null)
                    throw new InvalidOperationException("PIV_ShadowUpper missing in " + ModelPath);

                visual.Configure(wrapper, upperPivot);

                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
                if (!success || savedPrefab == null)
                    throw new InvalidOperationException("Failed to save prefab at " + PrefabPath);
                return savedPrefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject InstallOrReuseSceneInstance(Scene scene, Transform chapterRoot, GameObject prefab)
        {
            var existingMatches = chapterRoot
                .GetComponentsInChildren<Transform>(true)
                .Where(t => t.parent == chapterRoot && t.name == SceneInstanceName)
                .Select(t => t.gameObject)
                .ToList();

            GameObject instance = null;
            for (int i = 0; i < existingMatches.Count; i++)
            {
                GameObject candidate = existingMatches[i];
                bool isValidPrefabInstance =
                    instance == null &&
                    PrefabUtility.GetPrefabInstanceStatus(candidate) == PrefabInstanceStatus.Connected &&
                    PrefabUtility.GetCorrespondingObjectFromSource(candidate) == prefab;

                if (isValidPrefabInstance)
                    instance = candidate;
                else
                    UnityEngine.Object.DestroyImmediate(candidate);
            }

            if (instance == null)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.name = SceneInstanceName;
                instance.transform.SetParent(chapterRoot, false);
            }
            else
            {
                PrefabUtility.RevertPrefabInstance(instance, InteractionMode.AutomatedAction);
                instance.name = SceneInstanceName;
                instance.transform.SetParent(chapterRoot, false);
            }

            instance.transform.SetPositionAndRotation(CorridorMonsterSpawn, CorridorMonsterRotation);
            return instance;
        }

        private static Transform Find(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name)
                        return t;
            return null;
        }
    }
}
