using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NoRainbowAfter.Core;
using NoRainbowAfter.Player;
using NoRainbowAfter.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NoRainbowAfter.Editor
{
    public static class FirstPersonTestBuilder
    {
        private const string Root = "Assets/_NoRainbowAfter/";
        private const string ModelPath = Root + "Art/Models/Environment/T02_ControllerLab.fbx";
        private const string PrefabPath = Root + "Art/Prefabs/PFB_Raka_FirstPerson.prefab";
        private const string ActionsPath = Root + "Settings/NRA_FirstPersonActions.inputactions";
        private static Font font;

        [MenuItem("No Rainbow After/Create T02 First Person Test")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scenes first.");
            if (File.Exists(SceneFlow.PathFor(GameScene.FirstPersonTest)) || File.Exists(PrefabPath) || File.Exists(ActionsPath))
                throw new InvalidOperationException("T02 assets already exist; this builder does not overwrite them.");
            AssetDatabase.ImportAsset(ModelPath);
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            if (importer == null) throw new InvalidOperationException("The Blender FBX must exist and import first.");
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importCameras = false;
            importer.importLights = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.SaveAndReimport();
            var actions = FirstPersonInputSource.CreateDefaultAsset();
            File.WriteAllText(ActionsPath, actions.ToJson());
            UnityEngine.Object.DestroyImmediate(actions);
            AssetDatabase.ImportAsset(ActionsPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            if (actions == null) throw new InvalidOperationException("Input action asset import must complete before saving the player prefab.");
            font = AssetDatabase.LoadAssetAtPath<Font>(Root + "UI/Fonts/CrimsonText/CrimsonText-Regular.ttf");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateEnvironment();
            GameObject player = CreatePlayer(actions);
            PrefabUtility.SaveAsPrefabAsset(player, PrefabPath);
            UnityEngine.Object.DestroyImmediate(player);
            player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            player.name = "Player_Raka_FPP";
            player.transform.position = new Vector3(-3, 0.08f, -2);
            CreateHud(player);
            EditorSceneManager.SaveScene(scene, SceneFlow.PathFor(GameScene.FirstPersonTest));
            var entries = EditorBuildSettings.scenes.ToList();
            entries.Add(new EditorBuildSettingsScene(SceneFlow.PathFor(GameScene.FirstPersonTest), true));
            EditorBuildSettings.scenes = entries.ToArray();
            AddLaunchButton();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(SceneFlow.PathFor(GameScene.FirstPersonTest));
            Debug.Log("[NRA] T02 Blender lab, player prefab, input asset and pause UI saved.");
        }

        private static GameObject CreatePlayer(InputActionAsset actions)
        {
            var root = new GameObject("Player_Raka_FPP");
            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.7f;
            cc.center = new Vector3(0, 0.85f, 0);
            cc.radius = 0.28f;
            cc.skinWidth = 0.025f;
            cc.stepOffset = 0.25f;
            cc.slopeLimit = 45;
            cc.minMoveDistance = 0;
            var camera = new GameObject("ViewCamera", typeof(Camera), typeof(AudioListener));
            camera.transform.SetParent(root.transform, false);
            camera.transform.localPosition = new Vector3(0, 1.58f, 0);
            camera.tag = "MainCamera";
            var view = camera.GetComponent<Camera>();
            view.fieldOfView = 75;
            view.nearClipPlane = 0.06f;
            view.farClipPlane = 80;
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = new Color32(23, 36, 41, 255);
            var motor = root.AddComponent<FirstPersonMotor>();
            Assign(motor, "viewCamera", view);
            Assign(root.AddComponent<FirstPersonPlayer>(), "actions", actions);
            return root;
        }

        private static void CreateEnvironment()
        {
            var geometry = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
            geometry.name = "T02_BlenderLab";
            // Measured FBX import maps Blender X/Y to Unity -X/-Z. Rotate the assembly to match the lab route.
            geometry.transform.rotation = Quaternion.Euler(0, 180, 0);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("The installed URP Lit shader is required.");
            var concrete = Material("Concrete", new Color32(94, 112, 115, 255), 0.18f, shader);
            var floor = Material("Floor", new Color32(46, 66, 71, 255), 0.55f, shader);
            var trim = Material("Trim", new Color32(36, 47, 52, 255), 0.4f, shader);
            var teal = Material("Teal", new Color32(71, 133, 127, 255), 0.45f, shader);
            var amber = Material("Amber", new Color32(146, 125, 85, 255), 0.25f, shader);
            Bounds bounds = new Bounds();
            bool first = true;
            foreach (var renderer in geometry.GetComponentsInChildren<MeshRenderer>())
            {
                string name = renderer.name;
                renderer.sharedMaterial = name.Contains("Tile") || name.Contains("Foundation") ? floor :
                    name.Contains("Wall") || name.Contains("Step") ? concrete :
                    name.Contains("Ramp") || name.Contains("Lane") || name.Contains("Diffuser") ? teal :
                    name.Contains("Roof") ? amber : trim;
                renderer.gameObject.isStatic = true;
                if (first) { bounds = renderer.bounds; first = false; } else bounds.Encapsulate(renderer.bounds);
                bool collision = name.Contains("Foundation") || name.Contains("Wall") || name.Contains("Crouch") ||
                    name.Contains("Ramp") || name.Contains("Step") || name.Contains("Door");
                if (!collision) continue;
                var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                if (name == "SM_ENV_T02_Ramp") renderer.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
                else
                {
                    var box = renderer.gameObject.AddComponent<BoxCollider>();
                    box.center = mesh.bounds.center;
                    box.size = mesh.bounds.size;
                }
            }
            if (bounds.size.x < 11 || bounds.size.x > 13 || bounds.size.z < 17 || bounds.size.z > 20)
                throw new InvalidOperationException("FBX metre scale / axes need review: " + bounds.size);
            var light = new GameObject("Directional Light", typeof(Light));
            light.transform.rotation = Quaternion.Euler(52, -28, 0);
            var sun = light.GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(0.72f, 0.84f, 0.94f);
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;
            foreach (float z in new[] { -1f, 4f, 9f })
            {
                var fixture = new GameObject("Cool Room Light", typeof(Light));
                fixture.transform.position = new Vector3(0, 2.4f, z);
                var point = fixture.GetComponent<Light>();
                point.type = LightType.Point;
                point.range = 8;
                point.intensity = 2.2f;
                point.color = new Color(0.55f, 0.75f, 0.9f);
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.28f, 0.35f, 0.39f);
            RenderSettings.ambientEquatorColor = new Color(0.13f, 0.19f, 0.21f);
            RenderSettings.ambientGroundColor = new Color(0.07f, 0.09f, 0.1f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.09f, 0.14f, 0.16f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.012f;
            WorldLabel("MERUNDUK / CTRL", new Vector3(3, 1.9f, 0.8f));
            WorldLabel("KEMIRINGAN", new Vector3(-3, 1.5f, 4.3f));
            WorldLabel("TANGGA", new Vector3(0, 1.5f, 5.5f));
            Debug.Log("[NRA] Blender lab imported, measured bounds in metres: " + bounds.size);
        }

        private static Material Material(string name, Color color, float smoothness, Shader shader)
        {
            var material = new Material(shader);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, Root + "Art/Materials/MAT_T02_" + name + ".mat");
            return material;
        }

        private static void CreateHud(GameObject player)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var canvas = new GameObject("UI_FirstPerson", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var session = canvas.AddComponent<FirstPersonSession>();
            var hud = Rect("GameplayHUD", canvas.transform, Vector2.zero, new Vector2(1920, 1080));
            var dot = Rect("Crosshair", hud, Vector2.zero, new Vector2(3, 3)).gameObject.AddComponent<Image>();
            dot.color = new Color(0.9f, 0.94f, 0.9f, 0.6f);
            dot.raycastTarget = false;
            Label("ControlHint", hud, "WASD  Bergerak     Mouse  Melihat     Shift  Berlari     Ctrl  Merunduk     Esc  Jeda",
                new Vector2(0, -490), new Vector2(1660, 36), 23);
            Label("TestObjective", hud, "Kenali gerakanmu.", new Vector2(-580, 465), new Vector2(500, 50), 30);
            var stamina = Rect("Stamina", hud, new Vector2(0, -425), new Vector2(240, 3));
            stamina.gameObject.AddComponent<Image>().color = new Color(0.25f, 0.34f, 0.36f, 0.8f);
            var fill = Rect("Fill", stamina, Vector2.zero, Vector2.zero);
            fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one;
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            fill.gameObject.AddComponent<Image>().color = new Color(0.75f, 0.85f, 0.82f);
            var pause = Rect("PausePanel", canvas.transform, Vector2.zero, new Vector2(1920, 1080));
            pause.anchorMin = Vector2.zero; pause.anchorMax = Vector2.one;
            pause.offsetMin = pause.offsetMax = Vector2.zero;
            pause.gameObject.AddComponent<Image>().color = new Color(0.025f, 0.045f, 0.055f, 0.94f);
            Label("PauseTitle", pause, "Jeda", new Vector2(-380, 280), new Vector2(760, 90), 54);
            var sensitivityLabel = Label("SensitivityLabel", pause, "Sensitivitas mouse", new Vector2(-380, 162), new Vector2(760, 45), 28);
            var sensitivity = Slider("SensitivitySlider", pause, 103, 0.01f, 0.3f, 0.08f);
            var fovLabel = Label("FovLabel", pause, "Bidang pandang", new Vector2(-380, 15), new Vector2(760, 45), 28);
            var fov = Slider("FovSlider", pause, -45, 65, 95, 75);
            var resume = Button("ResumeButton", pause, "Lanjutkan", -170);
            var menu = Button("ReturnToMenuButton", pause, "Kembali ke Menu", -245);
            Assign(session, "player", player.GetComponent<FirstPersonPlayer>());
            Assign(session, "motor", player.GetComponent<FirstPersonMotor>());
            Assign(session, "pausePanel", pause.gameObject);
            Assign(session, "resumeButton", resume);
            Assign(session, "menuButton", menu);
            Assign(session, "sensitivity", sensitivity);
            Assign(session, "fov", fov);
            Assign(session, "sensitivityLabel", sensitivityLabel);
            Assign(session, "fovLabel", fovLabel);
            Assign(session, "staminaRoot", stamina.gameObject);
            Assign(session, "staminaFill", fill);
            Assign(session, "gameplayHud", hud.gameObject);
            pause.gameObject.SetActive(false);
        }

        private static void AddLaunchButton()
        {
            Scene scene = EditorSceneManager.OpenScene(SceneFlow.PathFor(GameScene.MasterGameplay));
            var content = GameObject.Find("UI_Root").transform.Find("ScreenContent");
            var button = Button("FirstPersonTestButton", content, "Coba Kontrol", -130);
            button.GetComponent<RectTransform>().anchoredPosition = new Vector2(-560, -130);
            var notice = content.Find("PrototypeNotice").GetComponent<Text>();
            notice.text = "Kenali kontrol first person sebelum perjalanan dimulai.";
            notice.rectTransform.anchoredPosition = new Vector2(-480, -48);
            notice.rectTransform.sizeDelta = new Vector2(660, 70);
            Assign(GameObject.Find("Systems").GetComponent<ShellScreen>(), "playTestButton", button);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void WorldLabel(string text, Vector3 position)
        {
            var canvas = new GameObject("Sign_" + text, typeof(RectTransform), typeof(Canvas));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            canvas.transform.position = position;
            canvas.transform.localScale = Vector3.one * 0.005f;
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(420, 80);
            Label("Caption", canvas.transform, text, Vector2.zero, new Vector2(420, 80), 34);
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            return rect;
        }

        private static Text Label(string name, Transform parent, string value, Vector2 position, Vector2 size, int fontSize)
        {
            var text = Rect(name, parent, position, size).gameObject.AddComponent<Text>();
            text.font = font; text.fontSize = fontSize; text.text = value;
            text.color = new Color32(215, 225, 220, 255);
            text.alignment = TextAnchor.MiddleLeft; text.raycastTarget = false;
            return text;
        }

        private static Button Button(string name, Transform parent, string caption, float y)
        {
            var rect = Rect(name, parent, new Vector2(-420, y), new Vector2(680, 58));
            var hit = rect.gameObject.AddComponent<Image>(); hit.color = new Color32(32, 52, 59, 255);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = hit;
            Label("Caption", rect, caption, new Vector2(20, 0), new Vector2(610, 56), 30);
            return button;
        }

        private static Slider Slider(string name, Transform parent, float y, float minimum, float maximum, float value)
        {
            var rect = Rect(name, parent, new Vector2(-380, y), new Vector2(760, 44));
            var slider = rect.gameObject.AddComponent<Slider>();
            Rect("Track", rect, Vector2.zero, new Vector2(760, 4)).gameObject.AddComponent<Image>().color = new Color32(67, 88, 94, 255);
            var area = Rect("HandleArea", rect, Vector2.zero, new Vector2(736, 44));
            var handle = Rect("Handle", area, Vector2.zero, new Vector2(24, 32));
            var graphic = handle.gameObject.AddComponent<Image>(); graphic.color = new Color32(216, 225, 216, 255);
            slider.handleRect = handle; slider.targetGraphic = graphic;
            slider.minValue = minimum; slider.maxValue = maximum; slider.value = value;
            return slider;
        }

        private static void Assign(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(name).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
