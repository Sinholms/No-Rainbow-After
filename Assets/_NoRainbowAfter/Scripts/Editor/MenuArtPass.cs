using System;
using NoRainbowAfter.Core;
using NoRainbowAfter.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NoRainbowAfter.Editor
{
    /// <summary>Explicit visual update of MainMenu; keeps the existing screen's serialized actions.</summary>
    public static class MenuArtPass
    {
        private const string Root = "Assets/_NoRainbowAfter/";
        private const string ArtPath = Root + "Art/Textures/Menu/T_Menu_KosRain_v1.png";
        private const string MaterialPath = Root + "Art/Materials/MAT_MenuAtmosphere.mat";

        [MenuItem("No Rainbow After/Apply Cinematic Menu Art")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before changing menu assets.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save open scenes before applying the menu art pass.");

            AssetDatabase.ImportAsset(ArtPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(ArtPath);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
            var artwork = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtPath);
            var font = AssetDatabase.LoadAssetAtPath<Font>(Root + "UI/Fonts/CrimsonText/CrimsonText-Regular.ttf");
            var shader = Shader.Find("NoRainbowAfter/UI/MenuAtmosphere");
            if (artwork == null || font == null || shader == null)
                throw new InvalidOperationException("The menu image, licensed font, and weather shader must import first.");
            var weatherMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (weatherMaterial == null)
            {
                weatherMaterial = new Material(shader);
                AssetDatabase.CreateAsset(weatherMaterial, MaterialPath);
            }

            Scene scene = EditorSceneManager.OpenScene(SceneFlow.PathFor(GameScene.MainMenu));
            var canvas = GameObject.Find("UI_Root");
            var content = canvas.transform.Find("ScreenContent");
            if (content == null) throw new InvalidOperationException("The existing T01 MainMenu UI is required.");
            canvas.GetComponent<CanvasScaler>().screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            Stretch((RectTransform)content);
            var oldBackdrop = canvas.transform.Find("Backdrop");
            if (oldBackdrop != null) UnityEngine.Object.DestroyImmediate(oldBackdrop.gameObject);
            var oldAtmosphere = canvas.transform.Find("MenuAtmosphere");
            if (oldAtmosphere != null) UnityEngine.Object.DestroyImmediate(oldAtmosphere.gameObject);

            var atmosphereRoot = new GameObject("MenuAtmosphere", typeof(RectTransform));
            atmosphereRoot.transform.SetParent(canvas.transform, false);
            atmosphereRoot.transform.SetAsFirstSibling();
            Stretch((RectTransform)atmosphereRoot.transform);
            RawImage image = Image("GeneratedArtwork", atmosphereRoot.transform);
            image.texture = artwork;
            RawImage weather = Image("AnimatedRainAndMist", atmosphereRoot.transform);
            weather.material = weatherMaterial;
            var atmosphere = atmosphereRoot.AddComponent<MenuAtmosphere>();
            var atmosphereData = new SerializedObject(atmosphere);
            atmosphereData.FindProperty("artwork").objectReferenceValue = image;
            atmosphereData.FindProperty("weather").objectReferenceValue = weather;
            atmosphereData.ApplyModifiedPropertiesWithoutUndo();

            content.Find("Eyebrow").gameObject.SetActive(false);
            content.Find("TitleRule").gameObject.SetActive(false);
            var title = content.Find("Title").GetComponent<Text>();
            StyleText(title, font, 70, new Color32(217, 224, 218, 255));
            title.text = "NO RAINBOW\nAFTER";
            title.lineSpacing = 0.95f;
            PlaceLeft(title.rectTransform, 257, new Vector2(650, 178));
            var shadow = title.GetComponent<Shadow>() ?? title.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0.025f, 0.04f, 0.7f);
            shadow.effectDistance = new Vector2(1, -2);

            StyleButton(content.Find("NewGameButton").GetComponent<Button>(), font, 48, 32);
            StyleButton(content.Find("ContinueButton").GetComponent<Button>(), font, -20, 32);
            StyleButton(content.Find("SettingsButton").GetComponent<Button>(), font, -88, 32);
            StyleButton(content.Find("CreditsButton").GetComponent<Button>(), font, -156, 32);
            StyleButton(content.Find("QuitButton").GetComponent<Button>(), font, -224, 32);
            var hint = content.Find("SaveHint").GetComponent<Text>();
            hint.text = "Belum ada permainan tersimpan.";
            hint.fontSize = 17;
            hint.color = new Color32(137, 158, 166, 255);
            PlaceLeft(hint.rectTransform, -315, new Vector2(660, 36));
            var footer = content.Find("Footer").GetComponent<Text>();
            footer.text = "↑ ↓  Pilih       Enter  Konfirmasi";
            footer.fontSize = 16;
            footer.color = new Color32(137, 158, 166, 255);
            PlaceLeft(footer.rectTransform, -480, new Vector2(660, 36));
            var status = content.Find("LoadingStatus").GetComponent<Text>();
            status.fontSize = 20;
            status.color = new Color32(208, 216, 209, 255);

            var settings = canvas.transform.Find("SettingsPanel") ?? content.Find("SettingsPanel");
            settings.SetParent(canvas.transform, false);
            Stretch((RectTransform)settings);
            settings.GetComponent<UnityEngine.UI.Image>().color = new Color(0.015f, 0.03f, 0.045f, 0.72f);
            var settingsTitle = settings.Find("SettingsTitle").GetComponent<Text>();
            StyleText(settingsTitle, font, 47, new Color32(221, 225, 215, 255));
            settingsTitle.text = "Pengaturan";
            PlaceLeft(settingsTitle.rectTransform, 200, new Vector2(660, 76));
            var masterLabel = settings.Find("MasterLabel").GetComponent<Text>();
            StyleText(masterLabel, font, 30, new Color32(185, 199, 198, 255));
            PlaceLeft(masterLabel.rectTransform, 85, new Vector2(660, 50));
            var valueLabel = settings.Find("VolumeValue").GetComponent<Text>();
            valueLabel.fontSize = 20;
            valueLabel.color = new Color32(171, 189, 190, 255);
            PlaceLeft(valueLabel.rectTransform, 30, new Vector2(660, 40));
            var slider = (RectTransform)settings.Find("MasterVolumeSlider");
            PlaceLeft(slider, -35, new Vector2(620, 44));
            var back = settings.Find("SettingsBackButton").GetComponent<Button>();
            StyleButton(back, font, -140, 32);
            var settingsHint = settings.Find("SettingsHint").GetComponent<Text>();
            settingsHint.color = new Color32(137, 158, 166, 255);
            settingsHint.fontSize = 17;
            PlaceLeft(settingsHint.rectTransform, -228, new Vector2(660, 40));

            // Keep Settings above every base-screen element, including the load status.
            settings.SetAsLastSibling();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save MainMenu.");
            AssetDatabase.SaveAssets();
            Debug.Log("[NRA] Original imagegen artwork and animated weather applied to MainMenu. Existing actions preserved.");
        }

        private static void StyleButton(Button button, Font font, float y, int size)
        {
            PlaceLeft((RectTransform)button.transform, y, new Vector2(520, 58));
            var hitArea = button.GetComponent<UnityEngine.UI.Image>();
            hitArea.color = Color.clear;
            hitArea.raycastTarget = true;
            var caption = button.transform.Find("Caption").GetComponent<Text>();
            StyleText(caption, font, size, Color.white);
            Stretch(caption.rectTransform);
            caption.rectTransform.offsetMin = new Vector2(34, 0);
            caption.rectTransform.offsetMax = new Vector2(-14, 0);
            button.targetGraphic = caption;
            var colors = button.colors;
            colors.normalColor = new Color32(160, 179, 184, 255);
            colors.highlightedColor = new Color32(240, 234, 212, 255);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = Color.white;
            colors.disabledColor = new Color32(85, 111, 125, 255);
            colors.fadeDuration = 0.18f;
            button.colors = colors;
            var previous = button.transform.Find("FocusMarker");
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var marker = new GameObject("FocusMarker", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            marker.transform.SetParent(button.transform, false);
            var rect = (RectTransform)marker.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 0.5f);
            rect.anchoredPosition = new Vector2(12, 0);
            rect.sizeDelta = new Vector2(14, 1.5f);
            var graphic = marker.GetComponent<UnityEngine.UI.Image>();
            graphic.color = new Color32(223, 215, 189, 255);
            graphic.raycastTarget = false;
            graphic.enabled = false;
            var focus = button.GetComponent<MenuItemFocus>() ?? button.gameObject.AddComponent<MenuItemFocus>();
            var data = new SerializedObject(focus);
            data.FindProperty("marker").objectReferenceValue = graphic;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RawImage Image(string name, Transform parent)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            image.transform.SetParent(parent, false);
            Stretch(image.rectTransform);
            image.raycastTarget = false;
            return image;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void PlaceLeft(RectTransform rect, float y, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.08f, 0.5f);
            rect.pivot = new Vector2(0, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(0, y);
        }

        private static void StyleText(Text text, Font font, int size, Color color)
        {
            text.font = font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Normal;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }
    }
}
