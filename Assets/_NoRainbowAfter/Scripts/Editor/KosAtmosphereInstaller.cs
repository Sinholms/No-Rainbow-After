using System;
using System.Collections.Generic;
using System.IO;
using NoRainbowAfter.Core;
using NoRainbowAfter.Player;
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
    public static class KosAtmosphereInstaller
    {
        private const string Root = "Assets/_NoRainbowAfter/";
        private const string Model = Root + "Art/Models/Environment/ENV_KosAtmosphere_v1.fbx";
        private const string ManifestPath = "BlenderSources/Environment/KosAtmosphere_v1_manifest.json";
        private const string TextureDir = Root + "Art/Textures/KosAtmosphere/";
        private const string MaterialDir = Root + "Art/Materials/KosAtmosphere/";
        private const string Prefab = Root + "Art/Prefabs/PFB_Raka_FirstPerson.prefab";

        [Serializable] private sealed class Manifest { public Part[] parts; public MaterialSpec[] materials; public LightSpec[] lights; public float[] spawn; }
        [Serializable] private sealed class Part { public string name; public string material; public string collision; }
        [Serializable] private sealed class MaterialSpec { public string name; public string texturePrefix; public float[] baseColor; public float metallic; public float smoothness; public float[] emission; }
        [Serializable] private sealed class LightSpec { public string name; public float[] position; public float[] color; public float intensity; public float range; }

        [MenuItem("No Rainbow After/Install Kos Atmosphere")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scenes before installing the room.");
            if (!File.Exists(ManifestPath) || !File.Exists(Model)) throw new InvalidOperationException("Kos FBX and manifest are required.");
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath));
            if (manifest == null || manifest.parts == null || manifest.materials == null || manifest.lights == null || manifest.spawn == null || manifest.spawn.Length < 3)
                throw new InvalidOperationException("Kos manifest is incomplete.");
            AssetDatabase.ImportAsset(Model, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(Model) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Could not import Kos FBX.");
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importLights = false; importer.importCameras = false; importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Model);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            var shader = Shader.Find("NoRainbowAfter/UI/CircularViewMask");
            if (model == null || prefab == null || shader == null) throw new InvalidOperationException("Missing model, player prefab, or circular mask shader.");
            var materials = CreateMaterials(manifest.materials);

            string sourcePath = SceneFlow.PathFor(GameScene.FirstPersonTest);
            string destinationPath = SceneFlow.PathFor(GameScene.MasterGameplay);
            Scene source = SceneManager.GetSceneByPath(sourcePath);
            bool openedSource = !source.isLoaded;
            if (openedSource) source = EditorSceneManager.OpenScene(sourcePath, OpenSceneMode.Additive);
            GameObject sourceUi = FindRoot(source, "UI_FirstPerson");
            GameObject sourceEvents = FindRoot(source, "EventSystem");
            if (sourceUi == null || sourceEvents == null) throw new InvalidOperationException("FirstPersonTest UI template is incomplete.");
            Scene previousDestination = SceneManager.GetSceneByPath(destinationPath);
            if (previousDestination.isLoaded) EditorSceneManager.CloseScene(previousDestination, true);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var ui = UnityEngine.Object.Instantiate(sourceUi); ui.name = sourceUi.name;
            var events = UnityEngine.Object.Instantiate(sourceEvents); events.name = sourceEvents.name;
            SceneManager.MoveGameObjectToScene(ui, scene); SceneManager.MoveGameObjectToScene(events, scene);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene other = SceneManager.GetSceneAt(i);
                if (other != scene) EditorSceneManager.CloseScene(other, true);
            }
            SceneManager.SetActiveScene(scene);
            var geometry = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
            geometry.name = "ENV_KosAtmosphere"; geometry.transform.rotation = Quaternion.Euler(0, 180, 0);
            ConfigureGeometry(geometry, manifest.parts, materials);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            player.name = "Player_Raka_FPP"; player.transform.position = Vec(manifest.spawn); player.transform.rotation = Quaternion.Euler(0, 10, 0);
            ConfigureLights(manifest.lights, player.GetComponentInChildren<Camera>());
            ConfigureRainAmbience();
            ConfigureUi(ui, player, shader);
            if (!EditorSceneManager.SaveScene(scene, destinationPath)) throw new InvalidOperationException("Could not save MasterGameplay.");
            AssetDatabase.SaveAssets();
            Debug.Log("[NRA] Kos atmosphere installed in MasterGameplay; inspect lighting and collision in Play Mode.");
        }

        private static Dictionary<string, Material> CreateMaterials(MaterialSpec[] specs)
        {
            Directory.CreateDirectory(MaterialDir);
            AssetDatabase.Refresh();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader is required.");
            var result = new Dictionary<string, Material>();
            foreach (var spec in specs)
            {
                var material = new Material(shader);
                material.SetColor("_BaseColor", ColorFrom(spec.baseColor, Color.white));
                material.SetFloat("_Metallic", spec.metallic); material.SetFloat("_Smoothness", spec.smoothness);
                if (!string.IsNullOrEmpty(spec.texturePrefix))
                {
                    var color = ImportTexture(spec.texturePrefix + "_BaseColor.png", false, false);
                    var normal = ImportTexture(spec.texturePrefix + "_Normal.png", true, false);
                    var mask = ImportTexture(spec.texturePrefix + "_Mask.png", false, true);
                    material.SetTexture("_BaseMap", color); material.SetTexture("_BumpMap", normal);
                    material.EnableKeyword("_NORMALMAP"); material.SetTexture("_MetallicGlossMap", mask);
                    material.EnableKeyword("_METALLICSPECGLOSSMAP");
                    material.SetFloat("_Smoothness", 1f);
                }
                Color emission = ColorFrom(spec.emission, Color.black);
                if (emission.maxColorComponent > 0) { material.SetColor("_EmissionColor", emission); material.EnableKeyword("_EMISSION"); }
                string path = MaterialDir + spec.name + ".mat";
                var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (existing == null) { AssetDatabase.CreateAsset(material, path); existing = material; }
                else { EditorUtility.CopySerialized(material, existing); UnityEngine.Object.DestroyImmediate(material); EditorUtility.SetDirty(existing); }
                result.Add(spec.name, existing);
            }
            return result;
        }

        private static Texture2D ImportTexture(string name, bool normal, bool linear)
        {
            string path = TextureDir + name;
            if (!File.Exists(path)) throw new InvalidOperationException("Missing texture: " + path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !linear && !normal; importer.wrapMode = TextureWrapMode.Repeat;
            importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 1024;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void ConfigureGeometry(GameObject root, Part[] parts, Dictionary<string, Material> materials)
        {
            var lookup = new Dictionary<string, Part>();
            foreach (var part in parts) lookup.Add(part.name, part);
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!lookup.TryGetValue(renderer.name, out var part)) throw new InvalidOperationException("Unspecified Kos mesh: " + renderer.name);
                renderer.sharedMaterial = materials[part.material];
                renderer.gameObject.isStatic = true;
                if (part.material == "MAT_H_Glass") renderer.shadowCastingMode = ShadowCastingMode.Off;
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) throw new InvalidOperationException("Missing mesh: " + renderer.name);
                if (part.collision == "box") { var box = renderer.gameObject.AddComponent<BoxCollider>(); box.center = filter.sharedMesh.bounds.center; box.size = filter.sharedMesh.bounds.size; }
                else if (part.collision == "mesh") renderer.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
                else if (part.collision != "none") throw new InvalidOperationException("Unknown collision: " + part.collision);
            }
        }

        private static void ConfigureLights(LightSpec[] lights, Camera camera)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.skybox = null;
            RenderSettings.ambientSkyColor = new Color(0.07f, 0.08f, 0.09f);
            RenderSettings.ambientEquatorColor = new Color(0.035f, 0.04f, 0.045f);
            RenderSettings.ambientGroundColor = new Color(0.015f, 0.018f, 0.02f);
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(0.025f, 0.035f, 0.045f);
            RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = 0.012f;
            foreach (var spec in lights)
            {
                var go = new GameObject(spec.name, typeof(Light)); go.transform.position = Vec(spec.position);
                var light = go.GetComponent<Light>(); light.type = LightType.Point;
                light.color = ColorFrom(spec.color, Color.white); light.intensity = spec.intensity; light.range = spec.range;
            }
            if (camera == null) throw new InvalidOperationException("Player camera is missing.");
            var lamp = new GameObject("Player View Light", typeof(Light)); lamp.transform.SetParent(camera.transform, false);
            var spot = lamp.GetComponent<Light>(); spot.type = LightType.Spot; spot.range = 10; spot.spotAngle = 65; spot.innerSpotAngle = 25;
            spot.intensity = 3; spot.color = new Color(1f, 0.91f, 0.78f); spot.shadows = LightShadows.Soft; spot.shadowBias = 0.02f; spot.shadowNormalBias = 0.06f;
            var probeGo = new GameObject("Room Reflection Probe", typeof(ReflectionProbe));
            probeGo.transform.position = new Vector3(0, 1.3f, 2.5f);
            var probe = probeGo.GetComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime; probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.clearFlags = ReflectionProbeClearFlags.SolidColor; probe.backgroundColor = new Color(0.005f, 0.008f, 0.01f, 1f);
            probe.resolution = 128; probe.size = new Vector3(5, 3, 6);
            ConfigurePostProcessing(camera);
        }

        private static void ConfigurePostProcessing(Camera camera)
        {
            const string profilePath = Root + "Art/Materials/KosAtmosphere/KosAtmosphereVolume.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }
            if (!profile.TryGet<Tonemapping>(out var tone)) { tone = profile.Add<Tonemapping>(); AssetDatabase.AddObjectToAsset(tone, profile); }
            if (!profile.TryGet<FilmGrain>(out var grain)) { grain = profile.Add<FilmGrain>(); AssetDatabase.AddObjectToAsset(grain, profile); }
            if (!profile.TryGet<ColorAdjustments>(out var color)) { color = profile.Add<ColorAdjustments>(); AssetDatabase.AddObjectToAsset(color, profile); }
            tone.mode.overrideState = true; tone.mode.value = TonemappingMode.ACES;
            grain.intensity.overrideState = true; grain.intensity.value = 0.10f;
            color.saturation.overrideState = true; color.saturation.value = -10;
            EditorUtility.SetDirty(tone); EditorUtility.SetDirty(grain); EditorUtility.SetDirty(color); EditorUtility.SetDirty(profile);
            var volumeGo = new GameObject("Room Post Processing", typeof(Volume));
            var volume = volumeGo.GetComponent<Volume>(); volume.isGlobal = true; volume.sharedProfile = profile;
            var data = camera.GetComponent<UniversalAdditionalCameraData>();
            if (data == null) data = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
        }

        private static void ConfigureRainAmbience()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Audio/Ambience/AUD_KosRain.wav");
            if (clip == null) return;
            var go = new GameObject("Rain Ambience", typeof(AudioSource));
            go.transform.position = new Vector3(-1.9f, 1.6f, 3.4f);
            var source = go.GetComponent<AudioSource>();
            source.clip = clip; source.loop = true; source.playOnAwake = true; source.volume = 0.13f;
            source.spatialBlend = 0.65f; source.minDistance = 1; source.maxDistance = 9;
        }

        private static void ConfigureUi(GameObject ui, GameObject player, Shader shader)
        {
            var session = ui.GetComponent<FirstPersonSession>();
            var hud = ui.transform.Find("GameplayHUD"); var pause = ui.transform.Find("PausePanel");
            if (session == null || hud == null || pause == null) throw new InvalidOperationException("First-person UI template is incomplete.");
            Assign(session, "player", player.GetComponent<FirstPersonPlayer>()); Assign(session, "motor", player.GetComponent<FirstPersonMotor>());
            var maskGo = new GameObject("CircularViewMask", typeof(RectTransform), typeof(RawImage), typeof(CircularViewMask));
            maskGo.transform.SetParent(ui.transform, false); maskGo.transform.SetAsFirstSibling();
            var maskRect = (RectTransform)maskGo.transform; maskRect.anchorMin = Vector2.zero; maskRect.anchorMax = Vector2.one; maskRect.offsetMin = maskRect.offsetMax = Vector2.zero;
            var raw = maskGo.GetComponent<RawImage>(); raw.raycastTarget = false;
            var mask = maskGo.GetComponent<CircularViewMask>(); Assign(mask, "target", raw); Assign(mask, "maskShader", shader); Assign(session, "viewMask", mask);
            hud.Find("TestObjective").GetComponent<Text>().text = "Kos Raka";
            hud.Find("ControlHint").GetComponent<Text>().text = "WASD  Bergerak    Mouse  Melihat    Shift  Berlari    Ctrl  Merunduk    Esc  Jeda";
            Move(pause, "PauseTitle", 370); Move(pause, "SensitivityLabel", 290); Move(pause, "SensitivitySlider", 240);
            Move(pause, "FovLabel", 170); Move(pause, "FovSlider", 120);
            Move(pause, "ResumeButton", -250); Move(pause, "ReturnToMenuButton", -325);
            var radiusLabel = UnityEngine.Object.Instantiate(pause.Find("FovLabel").gameObject, pause); radiusLabel.name = "ViewRadiusLabel";
            ((RectTransform)radiusLabel.transform).anchoredPosition = new Vector2(-380, 30);
            var sliderGo = UnityEngine.Object.Instantiate(pause.Find("FovSlider").gameObject, pause); sliderGo.name = "ViewRadiusSlider";
            ((RectTransform)sliderGo.transform).anchoredPosition = new Vector2(-380, -20);
            var slider = sliderGo.GetComponent<Slider>(); slider.minValue = 0.45f; slider.maxValue = 0.80f; slider.value = 0.50f;
            var toggleGo = new GameObject("CircularViewToggle", typeof(RectTransform), typeof(Image), typeof(Toggle)); toggleGo.transform.SetParent(pause, false);
            var tr = (RectTransform)toggleGo.transform; tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.5f); tr.anchoredPosition = new Vector2(-740, -130); tr.sizeDelta = new Vector2(32, 32);
            var bg = toggleGo.GetComponent<Image>(); bg.color = new Color(0.2f, 0.27f, 0.29f, 1);
            var mark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image)); mark.transform.SetParent(toggleGo.transform, false);
            var mr = (RectTransform)mark.transform; mr.anchorMin = Vector2.zero; mr.anchorMax = Vector2.one; mr.offsetMin = new Vector2(5, 5); mr.offsetMax = new Vector2(-5, -5);
            mark.GetComponent<Image>().color = new Color(0.85f, 0.9f, 0.85f, 1);
            var toggle = toggleGo.GetComponent<Toggle>(); toggle.targetGraphic = bg; toggle.graphic = mark.GetComponent<Image>(); toggle.isOn = true;
            var caption = UnityEngine.Object.Instantiate(pause.Find("FovLabel").gameObject, pause); caption.name = "CircularViewCaption";
            ((RectTransform)caption.transform).anchoredPosition = new Vector2(-340, -130); caption.GetComponent<Text>().text = "Tepi pandangan melingkar";
            Assign(session, "circularViewToggle", toggle); Assign(session, "viewRadius", slider); Assign(session, "viewRadiusLabel", radiusLabel.GetComponent<Text>());
        }

        private static void Move(Transform parent, string name, float y) { var r = (RectTransform)parent.Find(name); r.anchoredPosition = new Vector2(r.anchoredPosition.x, y); }
        private static void Assign(UnityEngine.Object target, string field, UnityEngine.Object value) { var data = new SerializedObject(target); data.FindProperty(field).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
        private static GameObject FindRoot(Scene scene, string name) { foreach (var root in scene.GetRootGameObjects()) if (root.name == name) return root; return null; }
        private static Vector3 Vec(float[] v) => new Vector3(v[0], v[1], v[2]);
        private static Color ColorFrom(float[] v, Color fallback) => v != null && v.Length >= 3 ? new Color(v[0], v[1], v[2], v.Length > 3 ? v[3] : 1) : fallback;
    }
}
