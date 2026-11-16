using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NoRainbowAfter.Core;
using NoRainbowAfter.Quality;
using NoRainbowAfter.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NoRainbowAfter.Editor
{
    /// <summary>
    /// Idempotent installer for T07 Visual Vertical Slice:
    /// - Imports upgraded KosAtmosphere PBR textures + AO + wet/damp/rain decals + VFX sprites
    /// - Updates Kos materials with physically aligned normal/mask/AO and alpha-blended wet decals
    /// - Creates 3 distinct URP Renderer + Pipeline Asset pairs (Low / Medium / High) and configures QualitySettings
    /// - Polishes MasterGameplay lighting, silhouette contrast (8-12m and 2-3m), reflection probes, post-processing,
    ///   and controlled atmospheric dust/water-drip ParticleSystems with QualityAtmosphereController
    /// - Wires Low / Medium / High quality selector buttons into MasterGameplay PausePanel and MainMenu SettingsPanel
    ///   while preserving all T02-T06 gameplay, collider, doorway, cover, LoopAtmosphere, and clue invariants.
    /// </summary>
    public static class T07VisualSliceInstaller
    {
        private const string Root = "Assets/_NoRainbowAfter/";
        private const string ModelPath = Root + "Art/Models/Environment/ENV_KosAtmosphere_v1.fbx";
        private const string ManifestPath = "BlenderSources/Environment/KosAtmosphere_v1_manifest.json";
        private const string TextureDir = Root + "Art/Textures/KosAtmosphere/";
        private const string VfxTextureDir = Root + "Art/Textures/VFX/";
        private const string MaterialDir = Root + "Art/Materials/KosAtmosphere/";
        private const string VfxMaterialDir = Root + "Art/Materials/VFX/";
        private const string QualityAssetDir = Root + "Settings/Quality/";
        private const string VolumeProfilePath = MaterialDir + "KosAtmosphereVolume.asset";
        private const string MasterGameplayPath = Root + "Scenes/MasterGameplay.unity";
        private const string MainMenuPath = Root + "Scenes/MainMenu.unity";
        private const string BaseRendererTemplatePath = "Assets/Settings/PC_Renderer.asset";
        private const string BasePipelineTemplatePath = "Assets/Settings/PC_RPAsset.asset";
        private const string CrimsonFontPath = Root + "UI/Fonts/CrimsonText/CrimsonText-Regular.ttf";

        [Serializable]
        private sealed class Manifest
        {
            public Part[] parts;
            public MaterialSpec[] materials;
        }

        [Serializable]
        private sealed class Part
        {
            public string name;
            public string material;
            public string collision;
        }

        [Serializable]
        private sealed class MaterialSpec
        {
            public string name;
            public string texturePrefix;
            public float[] baseColor;
            public float metallic;
            public float smoothness;
            public float[] emission;
        }

        private static readonly HashSet<string> TransparentDecalMaterials = new HashSet<string>(StringComparer.Ordinal)
        {
            "MAT_H_Glass",
            "MAT_H_WetPuddle",
            "MAT_H_DampStain",
            "MAT_H_WindowRain"
        };

        private static readonly HashSet<string> NoShadowCastMaterials = new HashSet<string>(StringComparer.Ordinal)
        {
            "MAT_H_Glass",
            "MAT_H_WetPuddle",
            "MAT_H_DampStain",
            "MAT_H_WindowRain",
            "MAT_H_LampWarm",
            "MAT_H_LampCold"
        };

        [MenuItem("No Rainbow After/Install T07 Visual Vertical Slice")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before installing T07 Visual Vertical Slice.");

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save open scenes before installing T07 Visual Vertical Slice.");
            }

            if (!File.Exists(ManifestPath) || !File.Exists(ModelPath))
                throw new InvalidOperationException("Missing Kos FBX or manifest required for T07.");

            Manifest manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath));
            if (manifest == null || manifest.parts == null || manifest.materials == null)
                throw new InvalidOperationException("KosAtmosphere_v1_manifest.json is incomplete.");

            Directory.CreateDirectory(MaterialDir);
            Directory.CreateDirectory(VfxMaterialDir);
            Directory.CreateDirectory(QualityAssetDir);

            ConfigureEnvironmentModelImporter();
            Dictionary<string, Material> kosMaterials = BuildOrUpdateKosMaterials(manifest.materials);
            Material dustMaterial = BuildOrUpdateParticleMaterial("MAT_VFX_DustMote", "T_VFX_DustMote.png", new Color(1f, 0.96f, 0.88f, 0.42f));
            Material dripMaterial = BuildOrUpdateParticleMaterial("MAT_VFX_WaterDrip", "T_VFX_WaterDrip.png", new Color(0.76f, 0.88f, 1.0f, 0.62f));

            BuildAndRegisterQualityPresets();
            UpgradeMainMenuSettingsUi();
            UpgradeMasterGameplayScene(manifest.parts, kosMaterials, dustMaterial, dripMaterial);

            AssetDatabase.SaveAssets();
            GraphicsQualityService.ApplyPreset(QualityPreset.High, savePreference: false);
            Debug.Log("[NRA] T07 Visual Vertical Slice installed cleanly across URP presets, MainMenu, and MasterGameplay.");
        }

        private static void ConfigureEnvironmentModelImporter()
        {
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null)
                throw new InvalidOperationException("Could not load ModelImporter for " + ModelPath);

            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importLights = false;
            importer.importCameras = false;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.SaveAndReimport();
        }

        private static Texture2D ImportPbrTexture(string folder, string fileName, bool isNormal, bool isLinear, bool clampWrap, bool alphaTransparency)
        {
            string path = folder + fileName;
            if (!File.Exists(path))
                return null;

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException("Could not load TextureImporter for " + path);

            importer.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !isLinear && !isNormal;
            importer.mipmapEnabled = true;
            importer.streamingMipmaps = false;
            importer.wrapMode = clampWrap ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = clampWrap ? 4 : 8;
            importer.npotScale = TextureImporterNPOTScale.ToNearest;
            importer.maxTextureSize = 1024;
            importer.alphaIsTransparency = !isNormal && !isLinear && alphaTransparency;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Dictionary<string, Material> BuildOrUpdateKosMaterials(MaterialSpec[] specs)
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
                throw new InvalidOperationException("Universal Render Pipeline/Lit shader not found.");

            var result = new Dictionary<string, Material>(StringComparer.Ordinal);
            foreach (MaterialSpec spec in specs)
            {
                bool isTransparent = TransparentDecalMaterials.Contains(spec.name);
                bool isClampDecal = spec.name == "MAT_H_WetPuddle" || spec.name == "MAT_H_DampStain" || spec.name == "MAT_H_WindowRain";

                var temp = new Material(urpLit);
                temp.name = spec.name;
                Color baseCol = ColorFrom(spec.baseColor, Color.white);
                if (spec.name == "MAT_H_Glass")
                    baseCol = new Color(0.14f, 0.22f, 0.28f, 0.42f);

                temp.SetColor("_BaseColor", baseCol);
                temp.SetFloat("_Metallic", spec.metallic);
                temp.SetFloat("_Smoothness", spec.smoothness);
                temp.SetFloat("_EnvironmentReflections", 1f);
                temp.SetFloat("_SpecularHighlights", 1f);
                if (spec.name == "MAT_H_Cabinet" || spec.name.StartsWith("MAT_H_Fan", StringComparison.Ordinal))
                    temp.SetFloat("_Cull", 0f);

                if (!string.IsNullOrEmpty(spec.texturePrefix))
                {
                    Texture2D baseTex = ImportPbrTexture(TextureDir, spec.texturePrefix + "_BaseColor.png", false, false, isClampDecal, isClampDecal);
                    Texture2D normalTex = ImportPbrTexture(TextureDir, spec.texturePrefix + "_Normal.png", true, false, isClampDecal, false);
                    Texture2D maskTex = ImportPbrTexture(TextureDir, spec.texturePrefix + "_Mask.png", false, true, isClampDecal, false);
                    Texture2D aoTex = ImportPbrTexture(TextureDir, spec.texturePrefix + "_AO.png", false, true, isClampDecal, false);

                    if (baseTex != null)
                        temp.SetTexture("_BaseMap", baseTex);

                    if (normalTex != null)
                    {
                        temp.SetTexture("_BumpMap", normalTex);
                        temp.SetFloat("_BumpScale", spec.name == "MAT_H_Wall" ? 1.15f : 1.0f);
                        temp.EnableKeyword("_NORMALMAP");
                    }

                    if (maskTex != null)
                    {
                        temp.SetTexture("_MetallicGlossMap", maskTex);
                        temp.SetFloat("_Smoothness", 1.0f);
                        temp.EnableKeyword("_METALLICSPECGLOSSMAP");
                    }

                    if (aoTex != null && !isTransparent)
                    {
                        temp.SetTexture("_OcclusionMap", aoTex);
                        temp.SetFloat("_OcclusionStrength", 0.62f);
                        temp.EnableKeyword("_OCCLUSIONMAP");
                    }
                }

                Color emission = ColorFrom(spec.emission, Color.black);
                if (spec.name == "MAT_H_LampWarm")
                    emission = new Color(1.55f, 1.26f, 0.88f, 1f);
                else if (spec.name == "MAT_H_LampCold")
                    emission = new Color(0.68f, 1.08f, 1.55f, 1f);

                if (emission.maxColorComponent > 0f)
                {
                    temp.SetColor("_EmissionColor", emission);
                    temp.EnableKeyword("_EMISSION");
                }

                if (isTransparent)
                {
                    temp.SetFloat("_Surface", 1f);
                    temp.SetFloat("_Blend", 0f);
                    temp.SetFloat("_BlendModePreserveSpecular", 0f);
                    temp.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                    temp.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                    temp.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                    temp.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                    temp.SetFloat("_ZWrite", 0f);
                    temp.SetFloat("_AlphaClip", 0f);
                    temp.SetFloat("_ReceiveShadows", 1f);
                    temp.SetOverrideTag("RenderType", "Transparent");
                    temp.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    temp.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    temp.DisableKeyword("_ALPHATEST_ON");
                    temp.renderQueue = spec.name == "MAT_H_WetPuddle"
                        ? (int)RenderQueue.Transparent + 2
                        : (int)RenderQueue.Transparent;
                }
                else
                {
                    temp.SetFloat("_Surface", 0f);
                    temp.SetFloat("_SrcBlend", (float)BlendMode.One);
                    temp.SetFloat("_DstBlend", (float)BlendMode.Zero);
                    temp.SetFloat("_ZWrite", 1f);
                    temp.SetOverrideTag("RenderType", "Opaque");
                    temp.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    temp.renderQueue = -1;
                }

                string matPath = MaterialDir + spec.name + ".mat";
                Material saved = UpsertMaterialAsset(temp, matPath);
                result[spec.name] = saved;
            }

            return result;
        }

        private static Material BuildOrUpdateParticleMaterial(string matName, string texFileName, Color tint)
        {
            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (particleShader == null)
                throw new InvalidOperationException("Universal Render Pipeline/Particles/Unlit shader not found.");

            Texture2D spriteTex = ImportPbrTexture(VfxTextureDir, texFileName, false, false, true, true);
            if (spriteTex == null)
                throw new InvalidOperationException("Missing VFX texture: " + VfxTextureDir + texFileName);

            var temp = new Material(particleShader);
            temp.name = matName;
            temp.SetTexture("_BaseMap", spriteTex);
            temp.SetColor("_BaseColor", tint);
            temp.SetFloat("_Surface", 1f);
            temp.SetFloat("_Blend", 0f);
            temp.SetFloat("_ColorMode", 0f);
            temp.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            temp.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            temp.SetFloat("_ZWrite", 0f);
            temp.SetFloat("_Cull", (float)CullMode.Off);
            temp.SetOverrideTag("RenderType", "Transparent");
            temp.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            temp.renderQueue = (int)RenderQueue.Transparent + 10;

            return UpsertMaterialAsset(temp, VfxMaterialDir + matName + ".mat");
        }

        private static Material UpsertMaterialAsset(Material temp, string assetPath)
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(temp, assetPath);
                return temp;
            }

            EditorUtility.CopySerialized(temp, existing);
            UnityEngine.Object.DestroyImmediate(temp);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static void BuildAndRegisterQualityPresets()
        {
            UniversalRendererData lowRenderer = BuildOrUpdateRendererAsset("NRA_Renderer_Low", enableSsao: false, ssaoDownsample: true, ssaoIntensity: 0f, ssaoSamples: 0, transparentReceiveShadows: false);
            UniversalRendererData medRenderer = BuildOrUpdateRendererAsset("NRA_Renderer_Medium", enableSsao: true, ssaoDownsample: true, ssaoIntensity: 0.42f, ssaoSamples: 1, transparentReceiveShadows: true);
            UniversalRendererData highRenderer = BuildOrUpdateRendererAsset("NRA_Renderer_High", enableSsao: true, ssaoDownsample: false, ssaoIntensity: 0.56f, ssaoSamples: 2, transparentReceiveShadows: true);

            UniversalRenderPipelineAsset lowPipe = BuildOrUpdatePipelineAsset(
                "NRA_URP_Low", lowRenderer,
                renderScale: 0.85f, msaa: 1, shadowDistance: 12f, shadowCascades: 1,
                mainShadowRes: 512, additionalLightShadows: false, additionalShadowRes: 512,
                softShadows: false, softShadowQuality: 1, probeBlending: false, probeBoxProj: false);

            UniversalRenderPipelineAsset medPipe = BuildOrUpdatePipelineAsset(
                "NRA_URP_Medium", medRenderer,
                renderScale: 1.00f, msaa: 2, shadowDistance: 22f, shadowCascades: 2,
                mainShadowRes: 1024, additionalLightShadows: true, additionalShadowRes: 1024,
                softShadows: true, softShadowQuality: 2, probeBlending: true, probeBoxProj: true);

            UniversalRenderPipelineAsset highPipe = BuildOrUpdatePipelineAsset(
                "NRA_URP_High", highRenderer,
                renderScale: 1.00f, msaa: 4, shadowDistance: 35f, shadowCascades: 4,
                mainShadowRes: 2048, additionalLightShadows: true, additionalShadowRes: 2048,
                softShadows: true, softShadowQuality: 3, probeBlending: true, probeBoxProj: true);

            GraphicsSettings.defaultRenderPipeline = highPipe;
            EditorUtility.SetDirty(GraphicsSettings.GetGraphicsSettings());

            UnityEngine.Object[] qualityAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
            if (qualityAssets == null || qualityAssets.Length == 0)
                throw new InvalidOperationException("Could not load ProjectSettings/QualitySettings.asset.");

            var qs = new SerializedObject(qualityAssets[0]);
            SerializedProperty levels = qs.FindProperty("m_QualitySettings");
            levels.arraySize = 3;

            ConfigureQualityElement(levels.GetArrayElementAtIndex(0), "Low", lowPipe,
                mipmapLimit: 1, anisotropic: 0, antiAliasing: 0, softParticles: false,
                realtimeProbes: false, shadowCascades: 1, shadowDistance: 12f, shadowsMode: 1, lodBias: 1.0f);

            ConfigureQualityElement(levels.GetArrayElementAtIndex(1), "Medium", medPipe,
                mipmapLimit: 0, anisotropic: 1, antiAliasing: 2, softParticles: true,
                realtimeProbes: true, shadowCascades: 2, shadowDistance: 22f, shadowsMode: 2, lodBias: 1.5f);

            ConfigureQualityElement(levels.GetArrayElementAtIndex(2), "High", highPipe,
                mipmapLimit: 0, anisotropic: 2, antiAliasing: 4, softParticles: true,
                realtimeProbes: true, shadowCascades: 4, shadowDistance: 35f, shadowsMode: 2, lodBias: 2.0f);

            qs.FindProperty("m_CurrentQuality").intValue = 2;
            SerializedProperty defaults = qs.FindProperty("m_PerPlatformDefaultQuality");
            if (defaults != null)
            {
                SerializedProperty standaloneProp = defaults.FindPropertyRelative("Standalone");
                if (standaloneProp != null) standaloneProp.intValue = 2;
            }

            qs.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(qualityAssets[0]);
        }

        private static UniversalRendererData BuildOrUpdateRendererAsset(
            string assetName,
            bool enableSsao,
            bool ssaoDownsample,
            float ssaoIntensity,
            int ssaoSamples,
            bool transparentReceiveShadows)
        {
            string targetPath = QualityAssetDir + assetName + ".asset";
            if (!File.Exists(targetPath))
            {
                if (!AssetDatabase.CopyAsset(BaseRendererTemplatePath, targetPath))
                    throw new InvalidOperationException("Could not copy renderer template to " + targetPath);
            }

            AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceSynchronousImport);
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(targetPath);
            if (renderer == null)
                throw new InvalidOperationException("Could not load UniversalRendererData at " + targetPath);

            renderer.name = assetName;
            var rendererSo = new SerializedObject(renderer);
            SetBool(rendererSo, "m_ShadowTransparentReceive", transparentReceiveShadows);
            SetInt(rendererSo, "m_RenderingMode", 2); // Forward+
            rendererSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(renderer);

            foreach (UnityEngine.Object subAsset in AssetDatabase.LoadAllAssetsAtPath(targetPath))
            {
                if (subAsset == null || subAsset == renderer) continue;
                if (subAsset.GetType().Name.IndexOf("ScreenSpaceAmbientOcclusion", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var ssaoSo = new SerializedObject(subAsset);
                    SetBool(ssaoSo, "m_Active", enableSsao);
                    SerializedProperty settings = ssaoSo.FindProperty("m_Settings");
                    if (settings != null && enableSsao)
                    {
                        SerializedProperty ds = settings.FindPropertyRelative("Downsample");
                        if (ds != null) ds.boolValue = ssaoDownsample;
                        SerializedProperty intensity = settings.FindPropertyRelative("Intensity");
                        if (intensity != null) intensity.floatValue = ssaoIntensity;
                        SerializedProperty radius = settings.FindPropertyRelative("Radius");
                        if (radius != null) radius.floatValue = 0.30f;
                        SerializedProperty samples = settings.FindPropertyRelative("Samples");
                        if (samples != null) samples.intValue = ssaoSamples;
                    }
                    ssaoSo.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(subAsset);
                }
            }

            return renderer;
        }

        private static UniversalRenderPipelineAsset BuildOrUpdatePipelineAsset(
            string assetName,
            UniversalRendererData rendererData,
            float renderScale,
            int msaa,
            float shadowDistance,
            int shadowCascades,
            int mainShadowRes,
            bool additionalLightShadows,
            int additionalShadowRes,
            bool softShadows,
            int softShadowQuality,
            bool probeBlending,
            bool probeBoxProj)
        {
            string targetPath = QualityAssetDir + assetName + ".asset";
            if (!File.Exists(targetPath))
            {
                if (!AssetDatabase.CopyAsset(BasePipelineTemplatePath, targetPath))
                    throw new InvalidOperationException("Could not copy pipeline template to " + targetPath);
            }

            AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceSynchronousImport);
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(targetPath);
            if (pipeline == null)
                throw new InvalidOperationException("Could not load UniversalRenderPipelineAsset at " + targetPath);

            pipeline.name = assetName;
            var so = new SerializedObject(pipeline);
            SerializedProperty rendererList = so.FindProperty("m_RendererDataList");
            if (rendererList != null && rendererList.arraySize > 0)
                rendererList.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;

            SetInt(so, "m_DefaultRendererIndex", 0);
            SetBool(so, "m_SupportsHDR", true);
            SetInt(so, "m_MSAA", msaa);
            SetFloat(so, "m_RenderScale", renderScale);
            SetFloat(so, "m_ShadowDistance", shadowDistance);
            SetInt(so, "m_ShadowCascadeCount", shadowCascades);
            SetInt(so, "m_MainLightShadowmapResolution", mainShadowRes);
            SetInt(so, "m_AdditionalLightsRenderingMode", 1);
            SetInt(so, "m_AdditionalLightsPerObjectLimit", 8);
            SetBool(so, "m_AdditionalLightShadowsSupported", additionalLightShadows);
            SetInt(so, "m_AdditionalLightsShadowmapResolution", additionalShadowRes);
            SetInt(so, "m_AdditionalLightsShadowResolutionTierLow", 256);
            SetInt(so, "m_AdditionalLightsShadowResolutionTierMedium", 512);
            SetInt(so, "m_AdditionalLightsShadowResolutionTierHigh", additionalShadowRes >= 2048 ? 1024 : 512);
            SetBool(so, "m_SoftShadowsSupported", softShadows);
            SetInt(so, "m_SoftShadowQuality", softShadowQuality);
            SetBool(so, "m_ReflectionProbeBlending", probeBlending);
            SetBool(so, "m_ReflectionProbeBoxProjection", probeBoxProj);
            SetBool(so, "m_UseSRPBatcher", true);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
            return pipeline;
        }

        private static void ConfigureQualityElement(
            SerializedProperty elem,
            string presetName,
            UniversalRenderPipelineAsset pipeline,
            int mipmapLimit,
            int anisotropic,
            int antiAliasing,
            bool softParticles,
            bool realtimeProbes,
            int shadowCascades,
            float shadowDistance,
            int shadowsMode,
            float lodBias)
        {
            elem.FindPropertyRelative("name").stringValue = presetName;
            elem.FindPropertyRelative("customRenderPipeline").objectReferenceValue = pipeline;
            elem.FindPropertyRelative("pixelLightCount").intValue = 4;
            elem.FindPropertyRelative("shadows").intValue = shadowsMode;
            elem.FindPropertyRelative("shadowResolution").intValue = presetName == "High" ? 2 : (presetName == "Medium" ? 1 : 0);
            elem.FindPropertyRelative("shadowProjection").intValue = 1;
            elem.FindPropertyRelative("shadowCascades").intValue = shadowCascades;
            elem.FindPropertyRelative("shadowDistance").floatValue = shadowDistance;
            elem.FindPropertyRelative("globalTextureMipmapLimit").intValue = mipmapLimit;
            elem.FindPropertyRelative("anisotropicTextures").intValue = anisotropic;
            elem.FindPropertyRelative("antiAliasing").intValue = antiAliasing;
            elem.FindPropertyRelative("softParticles").boolValue = softParticles;
            elem.FindPropertyRelative("realtimeReflectionProbes").boolValue = realtimeProbes;
            elem.FindPropertyRelative("billboardsFaceCameraPosition").boolValue = true;
            elem.FindPropertyRelative("vSyncCount").intValue = 0;
            elem.FindPropertyRelative("lodBias").floatValue = lodBias;
            elem.FindPropertyRelative("maximumLODLevel").intValue = 0;
            elem.FindPropertyRelative("enableLODCrossFade").boolValue = presetName != "Low";
            elem.FindPropertyRelative("streamingMipmapsActive").boolValue = false;

            SerializedProperty excluded = elem.FindPropertyRelative("excludedTargetPlatforms");
            if (excluded != null)
                excluded.arraySize = 0;
        }

        private static void UpgradeMainMenuSettingsUi()
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(CrimsonFontPath);
            if (font == null)
                throw new InvalidOperationException("Missing licensed font at " + CrimsonFontPath);

            Scene scene = EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
            GameObject uiRoot = FindRoot(scene, "UI_Root");
            GameObject systemsRoot = FindRoot(scene, "Systems");
            if (uiRoot == null || systemsRoot == null)
                throw new InvalidOperationException("UI_Root or Systems missing in MainMenu.");

            var shell = systemsRoot.GetComponent<ShellScreen>();
            Transform settings = uiRoot.transform.Find("SettingsPanel");
            if (settings == null)
                settings = FindInScene(scene, "SettingsPanel");
            if (shell == null || settings == null)
                throw new InvalidOperationException("ShellScreen or SettingsPanel missing in MainMenu.");

            PlaceMenuLeft((RectTransform)settings.Find("SettingsTitle"), 260f, new Vector2(660f, 76f));
            PlaceMenuLeft((RectTransform)settings.Find("MasterLabel"), 162f, new Vector2(660f, 50f));
            PlaceMenuLeft((RectTransform)settings.Find("VolumeValue"), 112f, new Vector2(660f, 40f));
            PlaceMenuLeft((RectTransform)settings.Find("MasterVolumeSlider"), 56f, new Vector2(620f, 44f));

            Text sectionLabel = UpsertMenuText(settings, "QualitySectionLabel", font, 30, new Color32(185, 199, 198, 255));
            sectionLabel.text = "Kualitas grafis";
            PlaceMenuLeft(sectionLabel.rectTransform, -18f, new Vector2(660f, 48f));

            Text presetLabel = UpsertMenuText(settings, "QualityPresetLabel", font, 20, new Color32(171, 189, 190, 255));
            presetLabel.text = "Kualitas grafis: Tinggi";
            PlaceMenuLeft(presetLabel.rectTransform, -66f, new Vector2(660f, 38f));

            Button backButton = settings.Find("SettingsBackButton").GetComponent<Button>();
            Button lowBtn = UpsertMenuQualityButton(settings, backButton, "QualityLowButton", "Rendah", font, new Vector2(0f, -124f), new Vector2(190f, 50f));
            Button medBtn = UpsertMenuQualityButton(settings, backButton, "QualityMediumButton", "Sedang", font, new Vector2(205f, -124f), new Vector2(190f, 50f));
            Button highBtn = UpsertMenuQualityButton(settings, backButton, "QualityHighButton", "Tinggi", font, new Vector2(410f, -124f), new Vector2(190f, 50f));

            PlaceMenuLeft((RectTransform)backButton.transform, -215f, new Vector2(520f, 58f));
            PlaceMenuLeft((RectTransform)settings.Find("SettingsHint"), -292f, new Vector2(660f, 40f));

            AssignObjectRef(shell, "qualityPresetLabel", presetLabel);
            AssignObjectRef(shell, "qualityLowButton", lowBtn);
            AssignObjectRef(shell, "qualityMediumButton", medBtn);
            AssignObjectRef(shell, "qualityHighButton", highBtn);

            settings.SetAsLastSibling();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Failed to save MainMenu.unity.");
        }

        private static void UpgradeMasterGameplayScene(
            Part[] manifestParts,
            Dictionary<string, Material> kosMaterials,
            Material dustMaterial,
            Material dripMaterial)
        {
            Scene scene = EditorSceneManager.OpenScene(MasterGameplayPath, OpenSceneMode.Single);
            Transform chapterRoot = FindInScene(scene, "Chapter1Root");
            Transform envRoot = FindInScene(scene, "ENV_KosAtmosphere");
            Transform playerRoot = FindInScene(scene, "Player_Raka_FPP");
            GameObject uiRoot = FindRoot(scene, "UI_FirstPerson");

            if (chapterRoot == null || envRoot == null || playerRoot == null || uiRoot == null)
                throw new InvalidOperationException("MasterGameplay missing Chapter1Root, ENV_KosAtmosphere, Player_Raka_FPP, or UI_FirstPerson.");

            ApplyKosMaterialsAndFlags(envRoot.gameObject, manifestParts, kosMaterials);

            Camera viewCamera = playerRoot.GetComponentInChildren<Camera>(true);
            if (viewCamera == null)
                throw new InvalidOperationException("ViewCamera missing under Player_Raka_FPP.");

            Light playerViewLight = TuneLightingAndReadability(scene, viewCamera);
            ReflectionProbe[] probes = ConfigureReflectionProbes(scene);
            Volume postVolume = UpgradePostProcessVolume(scene, viewCamera);
            BuildAtmosphericParticles(chapterRoot, dustMaterial, dripMaterial, out ParticleSystem[] drips, out ParticleSystem[] dust);

            GameObject qualityRig = FindOrCreateChild(chapterRoot, "QualityAtmosphereRig");
            var controller = qualityRig.GetComponent<QualityAtmosphereController>();
            if (controller == null)
                controller = qualityRig.AddComponent<QualityAtmosphereController>();
            controller.Configure(viewCamera, playerViewLight, probes, postVolume, drips, dust);
            EditorUtility.SetDirty(controller);

            UpgradePauseMenuUi(uiRoot);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Failed to save MasterGameplay.unity.");
        }

        private static void ApplyKosMaterialsAndFlags(
            GameObject envRoot,
            Part[] parts,
            Dictionary<string, Material> materials)
        {
            var lookup = new Dictionary<string, Part>(StringComparer.Ordinal);
            foreach (Part p in parts)
                lookup[p.name] = p;

            foreach (MeshRenderer renderer in envRoot.GetComponentsInChildren<MeshRenderer>(true))
            {
                string meshName = renderer.name;
                if (!lookup.TryGetValue(meshName, out Part part))
                    throw new InvalidOperationException("Unspecified Kos mesh in ENV_KosAtmosphere: " + meshName);

                if (!materials.TryGetValue(part.material, out Material mat))
                    throw new InvalidOperationException("Missing Kos material: " + part.material);

                renderer.sharedMaterial = mat;
                renderer.receiveShadows = true;
                renderer.shadowCastingMode = NoShadowCastMaterials.Contains(part.material)
                    ? ShadowCastingMode.Off
                    : ShadowCastingMode.On;

                bool isMovingLoopPaper =
                    meshName == "SM_PROP_OpenNotes" ||
                    meshName == "SM_PROP_NotesHeading" ||
                    meshName.StartsWith("SM_PROP_NotesLine", StringComparison.Ordinal) ||
                    meshName == "SM_PROP_WorkSlip" ||
                    meshName == "SM_PROP_WorkSlipHeading";

                bool isSpinningFanBlade = meshName == "SM_PROP_FanBlade";
                renderer.gameObject.isStatic = !(isMovingLoopPaper || isSpinningFanBlade);
                if (isSpinningFanBlade)
                {
                    if (renderer.GetComponent<FanBladeSpinner>() == null)
                        renderer.gameObject.AddComponent<FanBladeSpinner>();
                }

                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                    throw new InvalidOperationException("Missing MeshFilter/sharedMesh on " + meshName);

                if (part.collision == "none")
                {
                    if (meshName.StartsWith("SM_T07_", StringComparison.Ordinal) ||
                        meshName.StartsWith("SM_DEC_", StringComparison.Ordinal))
                    {
                        foreach (Collider col in renderer.GetComponents<Collider>())
                            UnityEngine.Object.DestroyImmediate(col);
                    }
                }
                else if (part.collision == "box")
                {
                    var box = renderer.GetComponent<BoxCollider>();
                    if (box == null)
                        box = renderer.gameObject.AddComponent<BoxCollider>();
                    box.center = filter.sharedMesh.bounds.center;
                    box.size = filter.sharedMesh.bounds.size;
                }
                else if (part.collision == "mesh" && renderer.GetComponent<Collider>() == null)
                {
                    renderer.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
                }

                EditorUtility.SetDirty(renderer);
                EditorUtility.SetDirty(renderer.gameObject);
            }
        }

        private static Light TuneLightingAndReadability(Scene scene, Camera viewCamera)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.skybox = null;
            RenderSettings.ambientSkyColor = new Color(0.075f, 0.088f, 0.102f);
            RenderSettings.ambientEquatorColor = new Color(0.042f, 0.048f, 0.055f);
            RenderSettings.ambientGroundColor = new Color(0.020f, 0.024f, 0.028f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.026f, 0.038f, 0.050f);
            RenderSettings.fogDensity = 0.0115f;

            TunePointLight(scene, "Desk Lamp", new Vector3(1.61f, 1.11f, 3.70f), new Color(1.00f, 0.66f, 0.36f), 1.55f, 2.75f, LightShadows.None);
            TuneSpotLight(scene, "Desk Task Spot", new Vector3(1.61f, 1.13f, 3.70f), new Vector3(1.20f, 0.78f, 3.78f), new Color(1.00f, 0.68f, 0.38f), 2.55f, 2.65f, 96f, 44f, LightShadows.Soft);
            TunePointLight(scene, "Window Spill", new Vector3(-1.84f, 1.78f, 3.40f), new Color(0.24f, 0.49f, 0.76f), 1.68f, 4.65f, LightShadows.None);
            TunePointLight(scene, "Room Fluorescent", new Vector3(0.00f, 2.42f, 2.60f), new Color(0.64f, 0.72f, 0.63f), 0.72f, 4.60f, LightShadows.None);
            TunePointLight(scene, "Bed Corner Fill", new Vector3(-1.12f, 1.08f, 1.38f), new Color(0.25f, 0.38f, 0.52f), 0.50f, 3.35f, LightShadows.None);

            TunePointLight(scene, "Corridor Near", new Vector3(0.00f, 2.42f, 6.30f), new Color(0.64f, 0.72f, 0.55f), 1.08f, 5.35f, LightShadows.None);
            TunePointLight(scene, "Corridor Middle", new Vector3(0.00f, 2.42f, 10.80f), new Color(0.76f, 0.48f, 0.28f), 1.04f, 5.75f, LightShadows.None);
            TunePointLight(scene, "Corridor Far", new Vector3(0.00f, 2.42f, 17.10f), new Color(0.30f, 0.50f, 0.72f), 1.20f, 5.10f, LightShadows.None);
            TunePointLight(scene, "Corridor End Backlight", new Vector3(0.00f, 1.56f, 18.58f), new Color(0.28f, 0.47f, 0.68f), 1.02f, 3.65f, LightShadows.None);

            Transform viewLightTr = viewCamera.transform.Find("Player View Light");
            Light viewSpot = viewLightTr != null ? viewLightTr.GetComponent<Light>() : null;
            if (viewSpot == null)
                throw new InvalidOperationException("Player View Light missing under ViewCamera.");

            viewSpot.type = LightType.Spot;
            viewSpot.range = 11.5f;
            viewSpot.spotAngle = 66f;
            viewSpot.innerSpotAngle = 28f;
            viewSpot.intensity = 3.15f;
            viewSpot.color = new Color(1.00f, 0.91f, 0.79f);
            viewSpot.shadows = LightShadows.Soft;
            viewSpot.shadowBias = 0.02f;
            viewSpot.shadowNormalBias = 0.06f;
            EditorUtility.SetDirty(viewSpot);

            return viewSpot;
        }

        private static void TunePointLight(
            Scene scene,
            string lightName,
            Vector3 position,
            Color color,
            float intensity,
            float range,
            LightShadows shadows)
        {
            Transform tr = FindInScene(scene, lightName);
            GameObject go;
            if (tr == null)
            {
                go = new GameObject(lightName, typeof(Light));
                SceneManager.MoveGameObjectToScene(go, scene);
            }
            else
            {
                go = tr.gameObject;
            }

            go.transform.position = position;
            var light = go.GetComponent<Light>();
            if (light == null) light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = shadows;
            light.shadowBias = 0.018f;
            light.shadowNormalBias = 0.055f;
            EditorUtility.SetDirty(light);
        }

        private static void TuneSpotLight(
            Scene scene,
            string lightName,
            Vector3 position,
            Vector3 lookTarget,
            Color color,
            float intensity,
            float range,
            float spotAngle,
            float innerSpotAngle,
            LightShadows shadows)
        {
            Transform tr = FindInScene(scene, lightName);
            GameObject go;
            if (tr == null)
            {
                go = new GameObject(lightName, typeof(Light));
                SceneManager.MoveGameObjectToScene(go, scene);
            }
            else
            {
                go = tr.gameObject;
            }

            go.transform.position = position;
            go.transform.rotation = Quaternion.LookRotation((lookTarget - position).normalized, Vector3.up);
            var light = go.GetComponent<Light>();
            if (light == null) light = go.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.spotAngle = spotAngle;
            light.innerSpotAngle = innerSpotAngle;
            light.shadows = shadows;
            light.shadowBias = 0.015f;
            light.shadowNormalBias = 0.050f;
            EditorUtility.SetDirty(light);
        }

        private static ReflectionProbe[] ConfigureReflectionProbes(Scene scene)
        {
            ReflectionProbe roomProbe = UpsertReflectionProbe(
                scene, "Room Reflection Probe",
                new Vector3(0f, 1.35f, 2.50f),
                new Vector3(4.02f, 2.72f, 5.02f),
                256,
                0.45f);

            ReflectionProbe corridorProbe = UpsertReflectionProbe(
                scene, "Corridor Reflection Probe",
                new Vector3(0f, 1.35f, 12.00f),
                new Vector3(1.82f, 2.72f, 14.05f),
                256,
                0.42f);

            return new[] { roomProbe, corridorProbe };
        }

        private static ReflectionProbe UpsertReflectionProbe(
            Scene scene,
            string probeName,
            Vector3 position,
            Vector3 boxSize,
            int resolution,
            float intensity)
        {
            Transform tr = FindInScene(scene, probeName);
            GameObject go = tr != null ? tr.gameObject : new GameObject(probeName, typeof(ReflectionProbe));
            if (tr == null)
                SceneManager.MoveGameObjectToScene(go, scene);

            go.transform.position = position;
            var probe = go.GetComponent<ReflectionProbe>();
            if (probe == null) probe = go.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.clearFlags = ReflectionProbeClearFlags.SolidColor;
            probe.backgroundColor = new Color(0.010f, 0.016f, 0.022f, 1f);
            probe.size = boxSize;
            probe.resolution = resolution;
            probe.boxProjection = true;
            probe.intensity = intensity;
            EditorUtility.SetDirty(probe);
            return probe;
        }

        private static Volume UpgradePostProcessVolume(Scene scene, Camera viewCamera)
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
                throw new InvalidOperationException("Missing KosAtmosphereVolume.asset at " + VolumeProfilePath);

            if (!profile.TryGet<Tonemapping>(out var tone))
            {
                tone = profile.Add<Tonemapping>();
                AssetDatabase.AddObjectToAsset(tone, profile);
            }
            if (!profile.TryGet<FilmGrain>(out var grain))
            {
                grain = profile.Add<FilmGrain>();
                AssetDatabase.AddObjectToAsset(grain, profile);
            }
            if (!profile.TryGet<ColorAdjustments>(out var color))
            {
                color = profile.Add<ColorAdjustments>();
                AssetDatabase.AddObjectToAsset(color, profile);
            }
            if (!profile.TryGet<Bloom>(out var bloom))
            {
                bloom = profile.Add<Bloom>();
                AssetDatabase.AddObjectToAsset(bloom, profile);
            }

            tone.active = true;
            tone.mode.overrideState = true;
            tone.mode.value = TonemappingMode.ACES;

            grain.active = true;
            grain.intensity.overrideState = true;
            grain.intensity.value = 0.11f;
            grain.response.overrideState = true;
            grain.response.value = 0.72f;

            color.active = true;
            color.postExposure.overrideState = true;
            color.postExposure.value = 0.08f;
            color.contrast.overrideState = true;
            color.contrast.value = 8f;
            color.saturation.overrideState = true;
            color.saturation.value = -10f;

            bloom.active = true;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 1.02f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.22f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.62f;
            bloom.highQualityFiltering.overrideState = true;
            bloom.highQualityFiltering.value = true;

            EditorUtility.SetDirty(tone);
            EditorUtility.SetDirty(grain);
            EditorUtility.SetDirty(color);
            EditorUtility.SetDirty(bloom);
            EditorUtility.SetDirty(profile);

            Transform volTr = FindInScene(scene, "Room Post Processing");
            if (volTr == null)
                throw new InvalidOperationException("Room Post Processing Volume missing in MasterGameplay.");

            var volume = volTr.GetComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            EditorUtility.SetDirty(volume);

            var camData = viewCamera.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null)
                camData = viewCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camData.antialiasingQuality = AntialiasingQuality.High;
            EditorUtility.SetDirty(camData);

            return volume;
        }

        private static void BuildAtmosphericParticles(
            Transform chapterRoot,
            Material dustMaterial,
            Material dripMaterial,
            out ParticleSystem[] primaryDrips,
            out ParticleSystem[] secondaryDust)
        {
            GameObject vfxRoot = FindOrCreateChild(chapterRoot, "VFX_KosAtmosphere");
            vfxRoot.transform.localPosition = Vector3.zero;
            vfxRoot.transform.localRotation = Quaternion.identity;

            ParticleSystem dormDust = ConfigureDustMotes(
                FindOrCreateChild(vfxRoot.transform, "VFX_DormDustMotes"),
                new Vector3(0.05f, 1.50f, 3.15f),
                new Vector3(3.1f, 1.45f, 2.4f),
                dustMaterial,
                maxParticles: 42,
                rateOverTime: 6.5f);

            ParticleSystem corridorDust = ConfigureDustMotes(
                FindOrCreateChild(vfxRoot.transform, "VFX_CorridorMistMotes"),
                new Vector3(0.00f, 1.45f, 11.80f),
                new Vector3(1.35f, 1.55f, 11.0f),
                dustMaterial,
                maxParticles: 48,
                rateOverTime: 7.5f);

            ParticleSystem dripNear = ConfigureCeilingWaterDrip(
                FindOrCreateChild(vfxRoot.transform, "VFX_CorridorWaterDrip_Near"),
                new Vector3(-0.18f, 2.62f, 8.02f),
                dripMaterial,
                rateOverTime: 0.55f);

            ParticleSystem dripFar = ConfigureCeilingWaterDrip(
                FindOrCreateChild(vfxRoot.transform, "VFX_CorridorWaterDrip_Far"),
                new Vector3(-0.10f, 2.62f, 14.58f),
                dripMaterial,
                rateOverTime: 0.45f);

            primaryDrips = new[] { dripNear, dripFar };
            secondaryDust = new[] { dormDust, corridorDust };
        }

        private static ParticleSystem ConfigureDustMotes(
            GameObject go,
            Vector3 worldPos,
            Vector3 boxSize,
            Material mat,
            int maxParticles,
            float rateOverTime)
        {
            go.transform.position = worldPos;
            go.isStatic = false;
            var ps = go.GetComponent<ParticleSystem>();
            if (ps == null) ps = go.AddComponent<ParticleSystem>();
            var renderer = go.GetComponent<ParticleSystemRenderer>();

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6.0f, 10.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.01f, 0.035f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.009f, 0.020f);
            main.startColor = new Color(0.92f, 0.94f, 0.96f, 0.24f);
            main.gravityModifier = -0.002f;
            main.maxParticles = maxParticles;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = rateOverTime;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = boxSize;

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.015f, 0.015f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.010f, 0.018f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.015f, 0.015f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.75f, 0.25f),
                    new GradientAlphaKey(0.75f, 0.75f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            EditorUtility.SetDirty(ps);
            EditorUtility.SetDirty(renderer);
            return ps;
        }

        private static ParticleSystem ConfigureCeilingWaterDrip(
            GameObject go,
            Vector3 worldPos,
            Material mat,
            float rateOverTime)
        {
            go.transform.position = worldPos;
            go.isStatic = false;
            var ps = go.GetComponent<ParticleSystem>();
            if (ps == null) ps = go.AddComponent<ParticleSystem>();
            var renderer = go.GetComponent<ParticleSystemRenderer>();

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.prewarm = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 0.73f; // 0.5 * 9.81 * 0.73^2 ~= 2.61m drop to floor puddle
            main.startSpeed = 0.05f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.016f, 0.024f);
            main.startColor = new Color(0.78f, 0.89f, 1.0f, 0.58f);
            main.gravityModifier = 1.0f;
            main.maxParticles = 6;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = rateOverTime;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.04f;

            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.035f;
            renderer.lengthScale = 1.8f;
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            EditorUtility.SetDirty(ps);
            EditorUtility.SetDirty(renderer);
            return ps;
        }

        private static void UpgradePauseMenuUi(GameObject uiRoot)
        {
            var session = uiRoot.GetComponent<FirstPersonSession>();
            Transform pause = uiRoot.transform.Find("PausePanel");
            if (session == null || pause == null)
                throw new InvalidOperationException("FirstPersonSession or PausePanel missing in UI_FirstPerson.");

            SetPauseY(pause, "PauseTitle", 412f);
            SetPauseY(pause, "SensitivityLabel", 332f);
            SetPauseY(pause, "SensitivitySlider", 284f);
            SetPauseY(pause, "FovLabel", 216f);
            SetPauseY(pause, "FovSlider", 168f);
            SetPauseY(pause, "ViewRadiusLabel", 100f);
            SetPauseY(pause, "ViewRadiusSlider", 52f);
            SetPauseY(pause, "CircularViewToggle", -14f);
            SetPauseY(pause, "CircularViewCaption", -14f);

            Transform fovLabelTemplate = pause.Find("FovLabel");
            Transform resumeTemplate = pause.Find("ResumeButton");
            if (fovLabelTemplate == null || resumeTemplate == null)
                throw new InvalidOperationException("PausePanel missing FovLabel or ResumeButton template.");

            Transform presetLabelTr = pause.Find("QualityPresetLabel");
            if (presetLabelTr == null)
            {
                GameObject clone = UnityEngine.Object.Instantiate(fovLabelTemplate.gameObject, pause, false);
                clone.name = "QualityPresetLabel";
                presetLabelTr = clone.transform;
            }
            var presetRect = (RectTransform)presetLabelTr;
            presetRect.anchoredPosition = new Vector2(-380f, -78f);
            presetRect.sizeDelta = new Vector2(760f, 45f);
            Text presetLabel = presetLabelTr.GetComponent<Text>();
            presetLabel.text = "Kualitas grafis: Tinggi";

            Button lowBtn = UpsertPauseQualityButton(pause, resumeTemplate.gameObject, "QualityLowButton", "Rendah", new Vector2(-653f, -134f), new Vector2(214f, 50f));
            Button medBtn = UpsertPauseQualityButton(pause, resumeTemplate.gameObject, "QualityMediumButton", "Sedang", new Vector2(-420f, -134f), new Vector2(214f, 50f));
            Button highBtn = UpsertPauseQualityButton(pause, resumeTemplate.gameObject, "QualityHighButton", "Tinggi", new Vector2(-187f, -134f), new Vector2(214f, 50f));

            SetPauseY(pause, "ResumeButton", -232f);
            SetPauseY(pause, "ReturnToMenuButton", -302f);
            if (pause.Find("CheckpointButton") != null)
                SetPauseY(pause, "CheckpointButton", -372f);

            AssignObjectRef(session, "qualityPresetLabel", presetLabel);
            AssignObjectRef(session, "qualityLowButton", lowBtn);
            AssignObjectRef(session, "qualityMediumButton", medBtn);
            AssignObjectRef(session, "qualityHighButton", highBtn);
            EditorUtility.SetDirty(session);
        }

        private static Button UpsertPauseQualityButton(
            Transform pause,
            GameObject templateButton,
            string buttonName,
            string labelText,
            Vector2 anchoredPos,
            Vector2 size)
        {
            Transform tr = pause.Find(buttonName);
            if (tr == null)
            {
                GameObject clone = UnityEngine.Object.Instantiate(templateButton, pause, false);
                clone.name = buttonName;
                tr = clone.transform;
            }

            var rect = (RectTransform)tr;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            Text txt = tr.GetComponentInChildren<Text>(true);
            if (txt != null)
            {
                txt.text = labelText;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.fontSize = 22;
            }

            var btn = tr.GetComponent<Button>();
            btn.interactable = true;
            return btn;
        }

        private static Text UpsertMenuText(Transform parent, string objectName, Font font, int fontSize, Color color)
        {
            Transform tr = parent.Find(objectName);
            GameObject go = tr != null ? tr.gameObject : new GameObject(objectName, typeof(RectTransform), typeof(Text));
            if (tr == null)
                go.transform.SetParent(parent, false);

            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Normal;
            text.color = color;
            text.raycastTarget = false;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static Button UpsertMenuQualityButton(
            Transform settingsPanel,
            Button templateButton,
            string buttonName,
            string captionText,
            Font font,
            Vector2 anchoredPos,
            Vector2 size)
        {
            Transform tr = settingsPanel.Find(buttonName);
            if (tr == null)
            {
                GameObject clone = UnityEngine.Object.Instantiate(templateButton.gameObject, settingsPanel, false);
                clone.name = buttonName;
                tr = clone.transform;
            }

            var rect = (RectTransform)tr;
            rect.anchorMin = rect.anchorMax = new Vector2(0.08f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            Transform captionTr = tr.Find("Caption");
            Text caption = captionTr != null ? captionTr.GetComponent<Text>() : tr.GetComponentInChildren<Text>(true);
            if (caption != null)
            {
                caption.font = font;
                caption.fontSize = 26;
                caption.text = captionText;
                caption.alignment = TextAnchor.MiddleLeft;
            }

            var btn = tr.GetComponent<Button>();
            btn.interactable = true;
            return btn;
        }

        private static void PlaceMenuLeft(RectTransform rect, float y, Vector2 size)
        {
            if (rect == null) return;
            rect.anchorMin = rect.anchorMax = new Vector2(0.08f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(0f, y);
        }

        private static void SetPauseY(Transform pause, string childName, float y)
        {
            var rect = pause.Find(childName) as RectTransform;
            if (rect != null)
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
        }

        private static GameObject FindOrCreateChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
                return existing.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject FindRoot(Scene scene, string rootName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == rootName)
                    return root;
            return null;
        }

        private static Transform FindInScene(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == objectName)
                        return t;
            }
            return null;
        }

        private static void AssignObjectRef(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
                throw new InvalidOperationException("Missing SerializedProperty '" + fieldName + "' on " + target.GetType().Name);
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetInt(SerializedObject so, string propName, int val)
        {
            SerializedProperty p = so.FindProperty(propName);
            if (p != null) p.intValue = val;
        }

        private static void SetFloat(SerializedObject so, string propName, float val)
        {
            SerializedProperty p = so.FindProperty(propName);
            if (p != null) p.floatValue = val;
        }

        private static void SetBool(SerializedObject so, string propName, bool val)
        {
            SerializedProperty p = so.FindProperty(propName);
            if (p != null) p.boolValue = val;
        }

        private static Color ColorFrom(float[] v, Color fallback)
        {
            if (v == null || v.Length < 3) return fallback;
            return new Color(v[0], v[1], v[2], v.Length > 3 ? v[3] : 1f);
        }
    }
}
