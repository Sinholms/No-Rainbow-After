using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NoRainbowAfter.Interaction;
using NoRainbowAfter.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NoRainbowAfter.Editor
{
    /// <summary>
    /// Installs the revamped Chapter 1 experience into MasterGameplay.unity and PFB_Raka_FirstPerson.prefab:
    /// - Updates NRA_FirstPersonActions.inputactions with CloseInspect (&lt;Keyboard&gt;/f)
    /// - Imports PROP_RakaLaptop.fbx and ACT_Raka_StudentBody.fbx with URP Lit materials
    /// - Places PROP_RakaLaptop (INT_LAPTOP / LaptopTypingMinigame) on Raka's study desk
    /// - Wires StressBarometer (top-left red bar, initial 50%) and FirstPersonBodyVisual (MiSide-style FPP body)
    /// - Updates initial HUD objective label to "Mengerjakan tugas"
    /// </summary>
    public static class RakaGameplayRevampInstaller
    {
        private const string MasterScenePath = "Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity";
        private const string TestScenePath = "Assets/_NoRainbowAfter/Scenes/FirstPersonTest.unity";
        private const string ActionsPath = "Assets/_NoRainbowAfter/Settings/NRA_FirstPersonActions.inputactions";
        private const string PrefabPath = "Assets/_NoRainbowAfter/Art/Prefabs/PFB_Raka_FirstPerson.prefab";

        private const string LaptopModelPath = "Assets/_NoRainbowAfter/Art/Models/Props/PROP_RakaLaptop.fbx";
        private const string LaptopTexPath = "Assets/_NoRainbowAfter/Art/Textures/Props/T_RakaLaptop_Screen.png";
        private const string LaptopMatDir = "Assets/_NoRainbowAfter/Art/Materials/Props/";

        private const string BodyModelPath = "Assets/_NoRainbowAfter/Art/Models/Characters/ACT_Raka_StudentBody.fbx";
        private const string BodyTexDir = "Assets/_NoRainbowAfter/Art/Textures/RakaBody/";
        private const string BodyMatDir = "Assets/_NoRainbowAfter/Art/Materials/Characters/RakaBody/";
        private const string BreathInhalePath = "Assets/_NoRainbowAfter/Audio/SFX/AUD_RakaBreath_Inhale.wav";
        private const string BreathExhalePath = "Assets/_NoRainbowAfter/Audio/SFX/AUD_RakaBreath_Exhale.wav";

        [MenuItem("No Rainbow After/Install Laptop, Stress Barometer & Raka Body")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode first.");

            UpdateInputActionsAsset();
            Dictionary<string, Material> laptopMaterials = PrepareLaptopModelAndMaterials();
            Dictionary<string, Material> bodyMaterials = PrepareBodyModelAndMaterials();

            GameObject laptopModel = AssetDatabase.LoadAssetAtPath<GameObject>(LaptopModelPath);
            GameObject bodyModel = AssetDatabase.LoadAssetAtPath<GameObject>(BodyModelPath);
            InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            if (laptopModel == null || bodyModel == null || inputAsset == null)
                throw new InvalidOperationException("Missing PROP_RakaLaptop, ACT_Raka_StudentBody, or InputActionAsset.");

            InstallIntoFirstPersonPrefab(bodyModel, bodyMaterials, inputAsset);
            InstallIntoMasterGameplay(laptopModel, laptopMaterials, bodyModel, bodyMaterials, inputAsset);

            AssetDatabase.SaveAssets();
            Debug.Log("[NRA] Revamp installed: PROP_RakaLaptop (Monkeytype), StressBarometer (50%), [F] CloseInspect, and ACT_Raka_StudentBody.");
        }

        private static void UpdateInputActionsAsset()
        {
            InputActionAsset generated = FirstPersonInputSource.CreateDefaultAsset();
            try
            {
                File.WriteAllText(ActionsPath, generated.ToJson());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(generated);
            }
            AssetDatabase.ImportAsset(ActionsPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static Dictionary<string, Material> PrepareLaptopModelAndMaterials()
        {
            Directory.CreateDirectory(LaptopMatDir);
            AssetDatabase.Refresh();

            Texture2D screenTex = ImportTexture(LaptopTexPath);
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? throw new InvalidOperationException("URP Lit shader missing.");

            var mats = new Dictionary<string, Material>(StringComparer.Ordinal)
            {
                ["MAT_Laptop_Chassis"] = CreateOrUpdateMaterial(LaptopMatDir + "MAT_Laptop_Chassis.mat", urpLit, new Color(0.16f, 0.17f, 0.19f), 0.35f, 0.58f),
                ["MAT_Laptop_DarkTrim"] = CreateOrUpdateMaterial(LaptopMatDir + "MAT_Laptop_DarkTrim.mat", urpLit, new Color(0.05f, 0.055f, 0.06f), 0.1f, 0.35f),
                ["MAT_Laptop_Keys"] = CreateOrUpdateMaterial(LaptopMatDir + "MAT_Laptop_Keys.mat", urpLit, new Color(0.065f, 0.07f, 0.08f), 0.05f, 0.45f),
                ["MAT_Laptop_AccentKey"] = CreateOrUpdateMaterial(LaptopMatDir + "MAT_Laptop_AccentKey.mat", urpLit, new Color(0.22f, 0.30f, 0.34f), 0.05f, 0.50f),
                ["MAT_Laptop_Sticker"] = CreateOrUpdateMaterial(LaptopMatDir + "MAT_Laptop_Sticker.mat", urpLit, new Color(0.38f, 0.55f, 0.52f), 0f, 0.25f),
                ["MAT_Laptop_Screen"] = CreateOrUpdateMaterial(LaptopMatDir + "MAT_Laptop_Screen.mat", urpLit, Color.white, 0f, 0.82f, screenTex, new Color(0.65f, 0.80f, 0.86f))
            };

            ConfigureFbxImport(LaptopModelPath, mats);
            return mats;
        }

        private static Dictionary<string, Material> PrepareBodyModelAndMaterials()
        {
            Directory.CreateDirectory(BodyMatDir);
            AssetDatabase.Refresh();

            Texture2D flannelTex = ImportTexture(BodyTexDir + "T_RakaBody_Flannel.png");
            Texture2D jeansTex = ImportTexture(BodyTexDir + "T_RakaBody_Jeans.png");
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? throw new InvalidOperationException("URP Lit shader missing.");

            var mats = new Dictionary<string, Material>(StringComparer.Ordinal)
            {
                ["MAT_RakaBody_Tee"] = CreateOrUpdateMaterial(BodyMatDir + "MAT_RakaBody_Tee.mat", urpLit, new Color(0.11f, 0.12f, 0.13f), 0f, 0.12f),
                ["MAT_RakaBody_Flannel"] = CreateOrUpdateMaterial(BodyMatDir + "MAT_RakaBody_Flannel.mat", urpLit, Color.white, 0f, 0.16f, flannelTex),
                ["MAT_RakaBody_Jeans"] = CreateOrUpdateMaterial(BodyMatDir + "MAT_RakaBody_Jeans.mat", urpLit, Color.white, 0f, 0.18f, jeansTex),
                ["MAT_RakaBody_Skin"] = CreateOrUpdateMaterial(BodyMatDir + "MAT_RakaBody_Skin.mat", urpLit, new Color(0.55f, 0.38f, 0.28f), 0f, 0.42f),
                ["MAT_RakaBody_Lanyard"] = CreateOrUpdateMaterial(BodyMatDir + "MAT_RakaBody_Lanyard.mat", urpLit, new Color(0.16f, 0.40f, 0.42f), 0f, 0.22f),
                ["MAT_RakaBody_Card"] = CreateOrUpdateMaterial(BodyMatDir + "MAT_RakaBody_Card.mat", urpLit, new Color(0.84f, 0.86f, 0.84f), 0f, 0.60f),
                ["MAT_RakaBody_Watch"] = CreateOrUpdateMaterial(BodyMatDir + "MAT_RakaBody_Watch.mat", urpLit, new Color(0.05f, 0.05f, 0.06f), 0.25f, 0.55f),
                ["MAT_RakaBody_Sneakers"] = CreateOrUpdateMaterial(BodyMatDir + "MAT_RakaBody_Sneakers.mat", urpLit, new Color(0.06f, 0.065f, 0.075f), 0f, 0.22f),
                ["MAT_RakaBody_Sole"] = CreateOrUpdateMaterial(BodyMatDir + "MAT_RakaBody_Sole.mat", urpLit, new Color(0.84f, 0.83f, 0.78f), 0f, 0.30f)
            };

            ConfigureFbxImport(BodyModelPath, mats);
            return mats;
        }

        private static Texture2D ImportTexture(string path)
        {
            if (!File.Exists(path)) return null;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti != null)
            {
                ti.textureType = TextureImporterType.Default;
                ti.sRGBTexture = true;
                ti.wrapMode = TextureWrapMode.Repeat;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material CreateOrUpdateMaterial(string path, Shader shader, Color baseColor, float metallic, float smoothness, Texture2D baseMap = null, Color? emission = null)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }
            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            if (baseMap != null) mat.SetTexture("_BaseMap", baseMap);
            if (emission.HasValue)
            {
                if (baseMap != null) mat.SetTexture("_EmissionMap", baseMap);
                mat.SetColor("_EmissionColor", emission.Value);
                mat.EnableKeyword("_EMISSION");
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void ConfigureFbxImport(string modelPath, Dictionary<string, Material> materials)
        {
            AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Failed to get ModelImporter for " + modelPath);

            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.importLights = false;
            importer.importCameras = false;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.bakeAxisConversion = false;
            foreach (var key in new List<AssetImporter.SourceAssetIdentifier>(importer.GetExternalObjectMap().Keys))
                importer.RemoveRemap(key);
            importer.SaveAndReimport();

            foreach (Material embedded in AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Material>())
            {
                if (embedded == null) continue;
                foreach (var kv in materials)
                {
                    if (embedded.name.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        importer.AddRemap(new AssetImporter.SourceAssetIdentifier(embedded), kv.Value);
                        break;
                    }
                }
            }
            importer.SaveAndReimport();
        }

        private static void InstallIntoMasterGameplay(
            GameObject laptopModel,
            Dictionary<string, Material> laptopMaterials,
            GameObject bodyModel,
            Dictionary<string, Material> bodyMaterials,
            InputActionAsset inputAsset)
        {
            Scene scene = EditorSceneManager.OpenScene(MasterScenePath, OpenSceneMode.Single);
            Transform chapterRoot = FindInScene(scene, "Chapter1Root") ?? throw new InvalidOperationException("Chapter1Root missing.");
            Transform playerTf = FindInScene(scene, "Player_Raka_FPP") ?? throw new InvalidOperationException("Player_Raka_FPP missing.");

            // 1. Place PROP_RakaLaptop on SM_PROP_StudyDesktop without obstructing INT_BOOK or INT_OVERTIME sightlines
            Transform oldLaptop = chapterRoot.Find("PROP_RakaLaptop");
            if (oldLaptop != null) UnityEngine.Object.DestroyImmediate(oldLaptop.gameObject);

            var laptopGo = new GameObject("PROP_RakaLaptop");
            laptopGo.transform.SetParent(chapterRoot, false);
            laptopGo.transform.position = new Vector3(0.86f, 0.803f, 3.53f);
            laptopGo.transform.rotation = Quaternion.identity;

            var laptopVisual = (GameObject)PrefabUtility.InstantiatePrefab(laptopModel, scene);
            laptopVisual.name = "PROP_RakaLaptop_Model";
            laptopVisual.transform.SetParent(laptopGo.transform, false);

            // Preserve the FBX prefab's native axis conversion rotation and orient only around Y toward the chair (-Z).
            Transform trackpad = FindDeep(laptopGo.transform, "SM_PROP_LaptopTrackpad");
            Transform hinge = FindDeep(laptopGo.transform, "SM_PROP_LaptopHinge");
            if (trackpad != null && hinge != null && trackpad.position.z > hinge.position.z)
                laptopGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            foreach (Collider c in laptopGo.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(c);
            AssignRendererMaterials(laptopGo, laptopMaterials, ShadowCastingMode.On);

            int interactableLayer = LayerMask.NameToLayer("Interactable");
            if (interactableLayer < 0) interactableLayer = 8;
            SetLayerRecursively(laptopGo, interactableLayer);

            var box = laptopGo.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.08f, 0f);
            box.size = new Vector3(0.24f, 0.16f, 0.18f);

            laptopGo.AddComponent<LaptopTypingMinigame>();
            EditorUtility.SetDirty(laptopGo);

            // 2. Wire Player_Raka_FPP with updated InputAsset, StressBarometer, and ACT_Raka_StudentBody
            WirePlayerObject(playerTf.gameObject, bodyModel, bodyMaterials, inputAsset, scene);

            // 3. Remove any stale StressBarometer under UI_FirstPerson and update static HUD labels
            Transform uiRoot = FindInScene(scene, "UI_FirstPerson");
            if (uiRoot != null)
            {
                Transform staleStress = FindDeep(uiRoot, "StressBarometer");
                if (staleStress != null)
                    UnityEngine.Object.DestroyImmediate(staleStress.gameObject);

                Transform obj = FindDeep(uiRoot, "TestObjective");
                if (obj != null && obj.GetComponent<Text>() != null)
                {
                    obj.GetComponent<Text>().text = "Mengerjakan tugas";
                    EditorUtility.SetDirty(obj.gameObject);
                }
                Transform hint = FindDeep(uiRoot, "ControlHint");
                if (hint != null && hint.GetComponent<Text>() != null)
                {
                    hint.GetComponent<Text>().text = "WASD  Bergerak    Mouse  Melihat    Shift  Berlari    Ctrl  Merunduk    E  Interaksi    Tab  Atur Napas    F  Tutup    Esc  Jeda";
                    EditorUtility.SetDirty(hint.gameObject);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Failed to save MasterGameplay.unity");
        }

        private static void InstallIntoFirstPersonPrefab(
            GameObject bodyModel,
            Dictionary<string, Material> bodyMaterials,
            InputActionAsset inputAsset)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null) return;
            try
            {
                WirePlayerObject(root, bodyModel, bodyMaterials, inputAsset, default);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void WirePlayerObject(
            GameObject playerGo,
            GameObject bodyModel,
            Dictionary<string, Material> bodyMaterials,
            InputActionAsset inputAsset,
            Scene targetScene)
        {
            if (File.Exists(BreathInhalePath))
                AssetDatabase.ImportAsset(BreathInhalePath, ImportAssetOptions.ForceSynchronousImport);
            if (File.Exists(BreathExhalePath))
                AssetDatabase.ImportAsset(BreathExhalePath, ImportAssetOptions.ForceSynchronousImport);
            AudioClip inhaleClip = AssetDatabase.LoadAssetAtPath<AudioClip>(BreathInhalePath);
            AudioClip exhaleClip = AssetDatabase.LoadAssetAtPath<AudioClip>(BreathExhalePath);

            bool isScenePrefabInstance = targetScene.IsValid() && PrefabUtility.IsPartOfPrefabInstance(playerGo);
            if (isScenePrefabInstance)
            {
                // PFB_Raka_FirstPerson.prefab is wired first and already owns StressBarometer, FirstPersonBodyVisual,
                // BreathingMinigame, and ACT_Raka_StudentBody. Strip any previously added scene overrides to prevent duplicates.
                RemoveAddedComponentOverrides<StressBarometer>(playerGo);
                RemoveAddedComponentOverrides<FirstPersonBodyVisual>(playerGo);
                RemoveAddedComponentOverrides<BreathingMinigame>(playerGo);
                for (int i = playerGo.transform.childCount - 1; i >= 0; i--)
                {
                    GameObject childGo = playerGo.transform.GetChild(i).gameObject;
                    if (childGo.name.StartsWith("ACT_Raka_StudentBody", StringComparison.Ordinal) &&
                        PrefabUtility.IsAddedGameObjectOverride(childGo))
                    {
                        UnityEngine.Object.DestroyImmediate(childGo);
                    }
                }
            }

            var fpPlayer = playerGo.GetComponent<FirstPersonPlayer>();
            if (fpPlayer != null)
            {
                var so = new SerializedObject(fpPlayer);
                so.FindProperty("actions").objectReferenceValue = inputAsset;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            DeduplicateComponent<StressBarometer>(playerGo);
            if (playerGo.GetComponent<StressBarometer>() == null)
                playerGo.AddComponent<StressBarometer>();

            DeduplicateComponent<BreathingMinigame>(playerGo);
            var breathing = playerGo.GetComponent<BreathingMinigame>() ?? playerGo.AddComponent<BreathingMinigame>();
            breathing.ConfigureAudioClips(inhaleClip, exhaleClip);
            EditorUtility.SetDirty(breathing);

            if (!isScenePrefabInstance)
            {
                for (int i = playerGo.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = playerGo.transform.GetChild(i);
                    if (child.name.StartsWith("ACT_Raka_StudentBody", StringComparison.Ordinal))
                        UnityEngine.Object.DestroyImmediate(child.gameObject);
                }

                var bodyGo = new GameObject("ACT_Raka_StudentBody");
                bodyGo.transform.SetParent(playerGo.transform, false);
                bodyGo.transform.localPosition = new Vector3(0f, 0f, -0.10f);
                bodyGo.transform.localRotation = Quaternion.identity;

                GameObject bodyModelInstance = (GameObject)PrefabUtility.InstantiatePrefab(bodyModel);
                bodyModelInstance.name = "ACT_Raka_StudentBody_Model";
                bodyModelInstance.transform.SetParent(bodyGo.transform, false);

                // Preserve FBX axis conversion on bodyModelInstance and rotate wrapper around Y so lanyard faces player forward (+Z).
                Transform card = FindDeep(bodyGo.transform, "SM_Raka_StudentCard");
                Transform collar = FindDeep(bodyGo.transform, "SM_Raka_Collar");
                if (card != null && collar != null &&
                    bodyGo.transform.InverseTransformPoint(card.position).z < bodyGo.transform.InverseTransformPoint(collar.position).z)
                    bodyGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

                foreach (Collider c in bodyGo.GetComponentsInChildren<Collider>(true))
                    UnityEngine.Object.DestroyImmediate(c);
                AssignRendererMaterials(bodyGo, bodyMaterials, ShadowCastingMode.Off);

                DeduplicateComponent<FirstPersonBodyVisual>(playerGo);
                var bodyVisual = playerGo.GetComponent<FirstPersonBodyVisual>() ?? playerGo.AddComponent<FirstPersonBodyVisual>();
                bodyVisual.Configure(bodyGo);
            }
            else
            {
                DeduplicateComponent<FirstPersonBodyVisual>(playerGo);
                Transform prefabBody = playerGo.transform.Find("ACT_Raka_StudentBody");
                if (prefabBody != null)
                {
                    var bodyVisual = playerGo.GetComponent<FirstPersonBodyVisual>();
                    bodyVisual?.Configure(prefabBody.gameObject);
                }
            }

            EditorUtility.SetDirty(playerGo);
        }

        private static void RemoveAddedComponentOverrides<T>(GameObject go) where T : Component
        {
            foreach (T c in go.GetComponents<T>())
            {
                if (c != null && PrefabUtility.IsAddedComponentOverride(c))
                    UnityEngine.Object.DestroyImmediate(c);
            }
        }

        private static void DeduplicateComponent<T>(GameObject go) where T : Component
        {
            T[] list = go.GetComponents<T>();
            for (int i = 1; i < list.Length; i++)
            {
                if (list[i] != null && (!PrefabUtility.IsPartOfPrefabInstance(go) || PrefabUtility.IsAddedComponentOverride(list[i])))
                    UnityEngine.Object.DestroyImmediate(list[i]);
            }
        }

        private static void AssignRendererMaterials(GameObject root, Dictionary<string, Material> lookup, ShadowCastingMode shadowMode)
        {
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = shadowMode;
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    foreach (var kv in lookup)
                    {
                        if (mats[i].name.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase))
                        {
                            mats[i] = kv.Value;
                            break;
                        }
                    }
                }
                r.sharedMaterials = mats;
                EditorUtility.SetDirty(r);
            }
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        private static Transform FindInScene(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform hit = FindDeep(root.transform, name);
                if (hit != null) return hit;
            }
            return null;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }
    }
}
