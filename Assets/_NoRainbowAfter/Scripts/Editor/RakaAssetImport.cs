using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace NoRainbowAfter.Editor
{
    /// <summary>
    /// Imports the generated Raka FBX and prepares its reusable visual prefab.
    /// This utility deliberately does not touch gameplay scenes or AI.
    /// </summary>
    public static class RakaAssetImport
    {
        private const string ModelPath = "Assets/_NoRainbowAfter/Art/Models/Characters/ACT_Shadow_Raka.fbx";
        private const string TextureDir = "Assets/_NoRainbowAfter/Art/Textures/Raka/";
        private const string MaterialDir = "Assets/_NoRainbowAfter/Art/Materials/Characters/Raka/";
        private const string ControllerPath = MaterialDir + "ACT_Shadow_Raka.controller";
        private const string PrefabDir = "Assets/_NoRainbowAfter/Art/Prefabs/Characters/";
        private const string PrefabPath = PrefabDir + "ACT_Shadow_Raka.prefab";

        private static readonly string[] ClipNames =
        {
            "Idle_Breath", "Walk_Lurch", "Pursue", "Search_Cover",
            "Fight_Reaction", "Hide_Pass", "Approach_Reveal", "Despawn"
        };

        private static readonly HashSet<string> LoopingClips = new HashSet<string>
        {
            "Idle_Breath", "Walk_Lurch", "Pursue", "Search_Cover", "Hide_Pass"
        };

        [MenuItem("No Rainbow After/Art/Prepare Raka Generated Asset")]
        public static void Prepare()
        {
            if (!File.Exists(ModelPath))
                throw new InvalidOperationException("Generated Raka FBX is missing: " + ModelPath);

            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            ConfigureModelImport();

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null) throw new InvalidOperationException("Could not load generated Raka FBX.");

            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)).ToArray();
            Dictionary<string, AnimationClip> clipsByName = ValidateClips(clips);
            ValidateRigAndMeshes(model);

            Directory.CreateDirectory(MaterialDir);
            Directory.CreateDirectory(PrefabDir);
            AssetDatabase.Refresh();

            Dictionary<string, Material> materials = CreateMaterials(model);
            AnimatorController controller = CreateController(clipsByName);
            CreatePrefab(model, materials, controller);
            AssetDatabase.SaveAssets();
            Debug.Log("[NRA] Prepared standalone ACT_Shadow_Raka prefab and animation controller.");
        }

        private static void ConfigureModelImport()
        {
            ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Could not configure ModelImporter for " + ModelPath);

            // Keep the FBX's named slots available to remap the generated materials below.
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.importLights = false;
            importer.importCameras = false;
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

            ModelImporterClipAnimation[] sourceClips = importer.defaultClipAnimations;
            if (sourceClips == null || sourceClips.Length == 0)
                throw new InvalidOperationException("The Raka FBX has no exported animation takes.");
            var configuredClips = new List<ModelImporterClipAnimation>(sourceClips.Length);
            foreach (ModelImporterClipAnimation source in sourceClips)
            {
                bool loop = LoopingClips.Contains(NormalizeClipName(source.name));
                var clip = new ModelImporterClipAnimation
                {
                    name = NormalizeClipName(source.name),
                    takeName = source.takeName,
                    firstFrame = source.firstFrame,
                    lastFrame = source.lastFrame,
                    loop = loop,
                    loopTime = loop,
                    loopPose = loop,
                    wrapMode = loop ? WrapMode.Loop : WrapMode.Default
                };
                configuredClips.Add(clip);
            }
            importer.clipAnimations = configuredClips.ToArray();
            importer.SaveAndReimport();
        }

        private static Dictionary<string, AnimationClip> ValidateClips(AnimationClip[] clips)
        {
            var found = clips.Select(c => new { Clip = c, Name = NormalizeClipName(c.name) })
                .Where(x => ClipNames.Contains(x.Name)).ToDictionary(x => x.Name, x => x.Clip);
            string[] missing = ClipNames.Where(n => !found.ContainsKey(n)).ToArray();
            if (missing.Length > 0)
                throw new InvalidOperationException("Raka FBX is missing required animation clips: " + string.Join(", ", missing));
            return found;
        }

        private static string NormalizeClipName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            int separator = name.LastIndexOf('|');
            return separator >= 0 && separator + 1 < name.Length ? name.Substring(separator + 1) : name;
        }

        private static void ValidateRigAndMeshes(GameObject model)
        {
            if (model.GetComponentInChildren<SkinnedMeshRenderer>(true) == null)
                throw new InvalidOperationException("Raka FBX has no skinned mesh renderer.");
            if (model.GetComponentInChildren<Animator>(true) == null)
                throw new InvalidOperationException("Raka FBX has no imported Generic rig/Animator.");
            if (model.GetComponentsInChildren<Renderer>(true).Length == 0)
                throw new InvalidOperationException("Raka FBX has no renderers.");
        }

        private static Dictionary<string, Material> CreateMaterials(GameObject model)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("Universal Render Pipeline/Lit shader is required.");

            var names = new HashSet<string>();
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
                foreach (Material source in renderer.sharedMaterials)
                    if (source != null && source.name.StartsWith("Raka_", StringComparison.Ordinal)) names.Add(source.name);
            if (names.Count == 0) throw new InvalidOperationException("No Raka_* material slots were found in the FBX.");

            var result = new Dictionary<string, Material>();
            foreach (string name in names)
            {
                string basePath = TextureDir + name + "_BaseColor.png";
                string normalPath = TextureDir + name + "_Normal.png";
                string roughnessPath = TextureDir + name + "_Roughness.png";
                Texture2D baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath);
                Texture2D normalMap = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                Texture2D roughnessMap = AssetDatabase.LoadAssetAtPath<Texture2D>(roughnessPath);
                if (baseMap == null || normalMap == null || roughnessMap == null)
                    throw new InvalidOperationException("Missing generated texture set for " + name + " (BaseColor, Normal, Roughness required).");

                var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
                if (normalImporter == null) throw new InvalidOperationException("Could not configure normal texture: " + normalPath);
                if (normalImporter.textureType != TextureImporterType.NormalMap)
                {
                    normalImporter.textureType = TextureImporterType.NormalMap;
                    normalImporter.SaveAndReimport();
                    normalMap = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                }

                float smoothness = name.IndexOf("Skin", StringComparison.OrdinalIgnoreCase) >= 0 ? 0.25f :
                    name.IndexOf("Hair", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Ink", StringComparison.OrdinalIgnoreCase) >= 0 ? 0.35f : 0.18f;
                var material = new Material(shader) { name = name };
                material.SetTexture("_BaseMap", baseMap);
                material.SetTexture("_BumpMap", normalMap);
                material.SetFloat("_BumpScale", 1f);
                material.SetFloat("_Metallic", 0f);
                material.SetFloat("_Smoothness", smoothness);
                material.EnableKeyword("_NORMALMAP");
                string path = MaterialDir + name + ".mat";
                Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (existing == null) AssetDatabase.CreateAsset(material, path);
                else
                {
                    EditorUtility.CopySerialized(material, existing);
                    UnityEngine.Object.DestroyImmediate(material);
                    EditorUtility.SetDirty(existing);
                    material = existing;
                }
                result.Add(name, material);
            }
            return result;
        }

        private static AnimatorController CreateController(Dictionary<string, AnimationClip> clips)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }
            else
            {
                foreach (AnimatorControllerLayer layer in controller.layers)
                    foreach (ChildAnimatorState state in layer.stateMachine.states)
                        layer.stateMachine.RemoveState(state.state);
            }

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idle = null;
            foreach (string clipName in ClipNames)
            {
                AnimatorState state = machine.AddState(clipName);
                state.motion = clips[clipName];
                if (clipName == "Idle_Breath") idle = state;
            }
            machine.defaultState = idle;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void CreatePrefab(GameObject model, Dictionary<string, Material> materials, AnimatorController controller)
        {
            var root = new GameObject("ACT_Shadow_Raka");
            try
            {
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
                visual.name = "ACT_Shadow_Raka_Model";
                visual.transform.SetParent(root.transform, false);
                Animator animator = visual.GetComponentInChildren<Animator>(true);
                if (animator == null) throw new InvalidOperationException("Imported Raka Animator is missing.");
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] source = renderer.sharedMaterials;
                    var mapped = new Material[source.Length];
                    for (int i = 0; i < source.Length; i++)
                    {
                        string name = source[i] == null ? null : source[i].name;
                        if (name == null || !materials.TryGetValue(name, out mapped[i]))
                            throw new InvalidOperationException("Unmapped FBX material slot '" + (name ?? "<null>") + "' on " + renderer.name + ".");
                    }
                    renderer.sharedMaterials = mapped;
                }

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
                if (!success || saved == null) throw new InvalidOperationException("Could not save standalone Raka prefab at " + PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
