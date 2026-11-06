using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NoRainbowAfter.Core;
using NoRainbowAfter.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NoRainbowAfter.Editor
{
    /// <summary>Explicit, one-time setup. Never runs on import and refuses to overwrite scenes.</summary>
    public static class FoundationBuilder
    {
        private const string Root = "Assets/_NoRainbowAfter";
        private static readonly Color Ink = new Color32(225, 232, 226, 255);
        private static readonly Color Muted = new Color32(151, 172, 172, 255);
        private static readonly Color Panel = new Color32(26, 43, 50, 255);
        private static Font font;
        private static readonly GameScene[] FoundationScenes =
            { GameScene.Boot, GameScene.MainMenu, GameScene.MasterGameplay, GameScene.Credits };

        [MenuItem("No Rainbow After/Create T01 Foundation")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before creating the foundation.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save your open scenes before running setup.");
            foreach (GameScene scene in FoundationScenes)
                if (File.Exists(SceneFlow.PathFor(scene)))
                    throw new InvalidOperationException("Setup cancelled: existing scene " + SceneFlow.PathFor(scene));

            CreateFolders();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach (GameScene id in FoundationScenes) CreateScene(id);
            var paths = FoundationScenes.Select(SceneFlow.PathFor).ToArray();
            var entries = paths.Select(p => new EditorBuildSettingsScene(p, true)).ToList();
            entries.AddRange(EditorBuildSettings.scenes.Where(s => !paths.Contains(s.path)));
            EditorBuildSettings.scenes = entries.ToArray();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(SceneFlow.PathFor(GameScene.Boot));
            Debug.Log("[NRA] T01 foundation saved. Boot is first in Build Settings; SampleScene is preserved.");
        }

        private static void CreateFolders()
        {
            string[] paths = {
                "Art/Models/Environment", "Art/Models/Props", "Art/Models/Characters",
                "Art/Materials", "Art/Textures", "Art/Decals", "Art/Animations", "Art/Prefabs",
                "Audio/Ambience", "Audio/SFX", "Audio/Voice", "Audio/Music", "Audio/Mixers",
                "Data/ScriptableObjects", "Data/Localization", "Data/Dialogue", "Data/Clues",
                "Scenes", "Scripts/Core", "Scripts/Player", "Scripts/Interaction", "Scripts/Narrative",
                "Scripts/AI", "Scripts/UI", "Scripts/Editor", "Scripts/Tests/EditMode", "Scripts/Tests/PlayMode",
                "UI/Prefabs", "UI/Fonts", "UI/Sprites", "VFX", "Settings", "ThirdParty",
                "Documentation/GDD", "Documentation/Tasks", "Documentation/Licenses", "Documentation/PlaytestLogs"
            };
            foreach (string relative in paths)
            {
                string path = Root + "/" + relative;
                Directory.CreateDirectory(path);
                if (!Directory.EnumerateFileSystemEntries(path).Any()) File.WriteAllText(path + "/.gitkeep", "");
            }
            AssetDatabase.Refresh();
        }

        private static void CreateScene(GameScene id)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 1.65f, -4);
            var view = camera.GetComponent<Camera>();
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = new Color32(12, 24, 31, 255);
            view.fieldOfView = 75;
            var light = new GameObject("Directional Light", typeof(Light));
            light.GetComponent<Light>().type = LightType.Directional;
            light.GetComponent<Light>().intensity = 0.5f;
            light.transform.rotation = Quaternion.Euler(45, -30, 0);
            var systems = new GameObject("Systems");
            if (id == GameScene.Boot)
            {
                systems.AddComponent<BootEntry>();
            }
            else
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                ShellScreen screen = systems.AddComponent<ShellScreen>();
                BuildScreen(screen, id);
                if (id == GameScene.MasterGameplay)
                    foreach (string name in new[] { "PrologueRoot", "Chapter1Root", "Chapter2Root", "Chapter3Root", "EndingsRoot" })
                        new GameObject(name).SetActive(false);
            }
            if (!EditorSceneManager.SaveScene(scene, SceneFlow.PathFor(id)))
                throw new IOException("Could not save " + id);
        }

        private static void BuildScreen(ShellScreen screen, GameScene id)
        {
            var canvas = new GameObject("UI_Root", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            RectTransform backdrop = Rect("Backdrop", canvas.transform, Vector2.zero, new Vector2(1920, 1080));
            backdrop.gameObject.AddComponent<Image>().color = new Color32(12, 24, 31, 255);
            // Restrained corridor-like graphic, built from original UI geometry.
            for (int i = 0; i < 5; i++)
            {
                float scale = 1f - i * 0.16f;
                var frame = Rect("RainFrame_" + i, backdrop, new Vector2(480, 0), new Vector2(720 * scale, 900 * scale));
                var stroke = frame.gameObject.AddComponent<Image>();
                stroke.color = new Color(0.3f, 0.48f, 0.51f, 0.018f + i * 0.009f);
            }
            var rain = new System.Random(21);
            for (int i = 0; i < 34; i++)
            {
                var line = Rect("Rain_" + i, backdrop, new Vector2(rain.Next(180, 920), rain.Next(-530, 530)), new Vector2(1, rain.Next(30, 140)));
                line.gameObject.AddComponent<Image>().color = new Color(0.6f, 0.72f, 0.75f, 0.09f);
            }
            RectTransform content = Rect("ScreenContent", canvas.transform, Vector2.zero, new Vector2(1920, 1080));
            var group = content.gameObject.AddComponent<CanvasGroup>();
            var serialized = new SerializedObject(screen);
            serialized.FindProperty("kind").enumValueIndex = id == GameScene.MainMenu ? 0 : id == GameScene.MasterGameplay ? 1 : 2;
            Set(serialized, "controls", group);
            Label("Eyebrow", content, "HOROR PSIKOLOGIS  /  ORANG PERTAMA", new Vector2(-480, 398), new Vector2(660, 36), 18, Muted);
            Label("Title", content, "NO RAINBOW\nAFTER", new Vector2(-470, 245), new Vector2(680, 242), 83, Ink, FontStyle.Normal);
            var rule = Rect("TitleRule", content, new Vector2(-758, 96), new Vector2(104, 2));
            rule.gameObject.AddComponent<Image>().color = new Color32(151, 181, 176, 255);
            Button primary;
            if (id == GameScene.MainMenu)
            {
                primary = Button("NewGameButton", content, "Mulai Baru", 10);
                var resume = Button("ContinueButton", content, "Lanjutkan", -60);
                resume.interactable = false;
                Set(serialized, "continueButton", resume);
                Set(serialized, "settingsButton", Button("SettingsButton", content, "Pengaturan", -130));
                Set(serialized, "creditsButton", Button("CreditsButton", content, "Kredit", -200));
                Set(serialized, "quitButton", Button("QuitButton", content, "Keluar", -270));
                Label("SaveHint", content, "Belum ada checkpoint tersimpan.", new Vector2(-480, -338), new Vector2(660, 34), 17, Muted);
                BuildSettings(serialized, content);
            }
            else if (id == GameScene.MasterGameplay)
            {
                Label("ChapterPlaceholder", content, "PERJALANAN RAKA", new Vector2(-480, 12), new Vector2(660, 44), 25, Ink);
                Label("PrototypeNotice", content, "Area permainan sedang disiapkan.\nTerima kasih sudah mencoba prototipe ini.", new Vector2(-480, -68), new Vector2(660, 90), 23, Muted);
                Set(serialized, "creditsButton", Button("CreditsButton", content, "Lihat Kredit", -200));
                primary = Button("MainMenuButton", content, "Kembali ke Menu", -270);
            }
            else
            {
                Label("CreditsHeading", content, "KREDIT", new Vector2(-480, 16), new Vector2(660, 44), 27, Ink);
                Label("CreditsBody", content, "No Rainbow After\nProyek game mahasiswa\n\nDibuat dengan Unity\nDaftar kontributor dan atribusi aset akan dilengkapi.", new Vector2(-480, -112), new Vector2(660, 196), 23, Muted);
                primary = Button("MainMenuButton", content, "Kembali ke Menu", -270);
            }
            Set(serialized, "primaryButton", primary);
            Set(serialized, "statusLabel", Label("LoadingStatus", content, "", new Vector2(0, -450), new Vector2(1600, 44), 22, Ink));
            Label("Footer", content, "NO RAINBOW AFTER   ·   PROTOTIPE", new Vector2(-480, -488), new Vector2(660, 32), 16, Muted);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildSettings(SerializedObject screen, RectTransform content)
        {
            var overlay = Rect("SettingsPanel", content.parent, Vector2.zero, new Vector2(1920, 1080));
            overlay.gameObject.AddComponent<Image>().color = new Color(0.025f, 0.05f, 0.065f, 0.98f);
            Label("SettingsTitle", overlay, "PENGATURAN", new Vector2(0, 180), new Vector2(620, 70), 42, Ink);
            Label("MasterLabel", overlay, "Volume utama", new Vector2(0, 65), new Vector2(620, 50), 25, Ink);
            var value = Label("VolumeValue", overlay, "80%", new Vector2(0, 8), new Vector2(620, 40), 22, Muted);
            var sliderRect = Rect("MasterVolumeSlider", overlay, new Vector2(0, -65), new Vector2(620, 44));
            var slider = sliderRect.gameObject.AddComponent<Slider>();
            var track = Rect("Track", sliderRect, Vector2.zero, new Vector2(620, 6));
            track.gameObject.AddComponent<Image>().color = Panel;
            var handleArea = Rect("HandleArea", sliderRect, Vector2.zero, new Vector2(596, 44));
            var handle = Rect("Handle", handleArea, Vector2.zero, new Vector2(24, 36));
            var graphic = handle.gameObject.AddComponent<Image>();
            graphic.color = Ink;
            slider.handleRect = handle;
            slider.targetGraphic = graphic;
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 0.8f;
            var back = Button("SettingsBackButton", overlay, "Kembali", -180);
            back.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -180);
            Label("SettingsHint", overlay, "ESC untuk kembali", new Vector2(0, -265), new Vector2(620, 40), 20, Muted);
            Set(screen, "settingsPanel", overlay.gameObject);
            Set(screen, "settingsBackButton", back);
            Set(screen, "masterVolume", slider);
            Set(screen, "volumeValue", value);
            overlay.gameObject.SetActive(false);
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Text Label(string name, Transform parent, string text, Vector2 position, Vector2 size, int fontSize, Color color, FontStyle style = FontStyle.Normal)
        {
            var label = Rect(name, parent, position, size).gameObject.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAnchor.MiddleLeft;
            label.raycastTarget = false;
            return label;
        }

        private static Button Button(string name, Transform parent, string caption, float y)
        {
            var rect = Rect(name, parent, new Vector2(-560, y), new Vector2(500, 58));
            var image = rect.gameObject.AddComponent<Image>();
            image.color = Panel;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.4f, 1.55f, 1.55f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.7f, 0.85f, 0.85f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
            button.colors = colors;
            Label("Caption", rect, caption, new Vector2(18, 0), new Vector2(450, 54), 24, Ink);
            return button;
        }

        private static void Set(SerializedObject target, string property, UnityEngine.Object value)
            => target.FindProperty(property).objectReferenceValue = value;
    }
}
