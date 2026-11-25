using System;
using System.IO;
using NoRainbowAfter.Core;
using NoRainbowAfter.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NoRainbowAfter.Editor
{
    /// <summary>
    /// Idempotent organic psychological-horror UI redesign pass for No Rainbow After.
    /// Eliminates rectangular dashboard boxes/cards in favor of translucent wet-glass raindrops,
    /// creeping black ink shadows, a lurking shadow silhouette in Pause, and floating editorial typography,
    /// while preserving every serialized reference and PlayMode test contract.
    /// </summary>
    public static class MenuUiRedesignPass
    {
        private const string Root = "Assets/_NoRainbowAfter/";
        private const string ArtPath = Root + "Art/Textures/Menu/T_Menu_KosRain_v1.png";
        private const string RainAudioPath = Root + "Audio/Ambience/AUD_KosRain.wav";
        private const string TitleFontPath = Root + "UI/Fonts/RubikDistressed/RubikDistressed-Regular.ttf";
        private const string NavFontPath = Root + "UI/Fonts/AmaticSC/AmaticSC-Bold.ttf";
        private const string BodyFontPath = Root + "UI/Fonts/BarlowCondensed/BarlowCondensed-Medium.ttf";
        private const string BodyBoldFontPath = Root + "UI/Fonts/BarlowCondensed/BarlowCondensed-SemiBold.ttf";
        private const string SpriteDir = Root + "UI/Sprites/";
        private const string MenuAtmosphereMatPath = Root + "Art/Materials/MAT_MenuAtmosphere.mat";
        private const string PauseScrimMatPath = Root + "Art/Materials/MAT_PauseScrim.mat";
        private const string SettingsScrimMatPath = Root + "Art/Materials/MAT_SettingsScrim.mat";

        // Art-bible palette derived from T_Menu_KosRain_v1.png
        private static readonly Color VoidSlate = new Color32(7, 14, 19, 255);
        private static readonly Color IvoryInk = new Color32(238, 240, 232, 255);
        private static readonly Color SoftPaper = new Color32(216, 224, 222, 255);
        private static readonly Color ColdMist = new Color32(152, 180, 190, 255);
        private static readonly Color MutedSlate = new Color32(130, 156, 166, 255);
        private static readonly Color DisabledMist = new Color32(96, 120, 130, 255);
        private static readonly Color DoorwayAmber = new Color32(226, 188, 128, 255);
        private static readonly Color HoverIvory = new Color32(250, 244, 228, 255);
        private static readonly Color QuietCrimsonHover = new Color32(236, 188, 180, 255);
        private static readonly Color SliderTrackCol = new Color(0.22f, 0.34f, 0.40f, 0.52f);
        private static readonly Color SliderFillCol = new Color(0.87f, 0.71f, 0.46f, 0.94f);

        private sealed class UiFonts
        {
            public Font TitleFont;
            public Font NavFont;
            public Font BodyFont;
            public Font BodyBoldFont;
        }

        private sealed class UiSprites
        {
            public Sprite MistHighlight;
            public Sprite SoftRule;
            public Sprite VerticalFadeRule;
            public Sprite SliderKnob;
            public Sprite ToggleRing;
            public Sprite Checkmark;
            public Sprite CheckboxBox;
        }

        [MenuItem("No Rainbow After/Apply Menu & Pause UI Redesign")]
        public static void ApplyAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before applying menu UI redesign.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save open scenes before applying menu UI redesign.");

            UiSprites sprites = EnsureUiSprites();
            UiFonts fonts = LoadUiFonts();

            Material menuWeatherMat = EnsureMaterial(MenuAtmosphereMatPath, "NoRainbowAfter/UI/MenuAtmosphere");

            Material pauseScrimMat = EnsureMaterial(PauseScrimMatPath, "NoRainbowAfter/UI/PauseScrim");
            pauseScrimMat.SetFloat("_ShowShadowFigure", 1f);
            pauseScrimMat.SetFloat("_StressLevel", 0.5f);
            EditorUtility.SetDirty(pauseScrimMat);

            Material settingsScrimMat = EnsureMaterial(SettingsScrimMatPath, "NoRainbowAfter/UI/PauseScrim");
            settingsScrimMat.SetFloat("_ShowShadowFigure", 0.28f);
            settingsScrimMat.SetFloat("_StressLevel", 0.25f);
            EditorUtility.SetDirty(settingsScrimMat);

            ApplyMainMenu(fonts, sprites, menuWeatherMat, settingsScrimMat);
            ApplyPauseScene(SceneFlow.PathFor(GameScene.MasterGameplay), fonts, sprites, pauseScrimMat, hasCheckpointAndQuality: true);
            ApplyPauseScene(SceneFlow.PathFor(GameScene.FirstPersonTest), fonts, sprites, pauseScrimMat, hasCheckpointAndQuality: false);

            AssetDatabase.SaveAssets();
            Debug.Log("[NRA] Reference-inspired distressed title, organic hand-lettered navigation/objective, and hairline UI redesign applied cleanly.");
        }

        private static UiFonts LoadUiFonts()
        {
            Font titleFont = AssetDatabase.LoadAssetAtPath<Font>(TitleFontPath);
            Font navFont = AssetDatabase.LoadAssetAtPath<Font>(NavFontPath);
            Font bodyFont = AssetDatabase.LoadAssetAtPath<Font>(BodyFontPath);
            Font bodyBoldFont = AssetDatabase.LoadAssetAtPath<Font>(BodyBoldFontPath);
            if (titleFont == null || navFont == null || bodyFont == null || bodyBoldFont == null)
            {
                throw new InvalidOperationException(
                    $"Missing required UI font(s): Title={TitleFontPath} ({titleFont != null}), Nav={NavFontPath} ({navFont != null}), Body={BodyFontPath} ({bodyFont != null}), BodyBold={BodyBoldFontPath} ({bodyBoldFont != null}).");
            }
            return new UiFonts
            {
                TitleFont = titleFont,
                NavFont = navFont,
                BodyFont = bodyFont,
                BodyBoldFont = bodyBoldFont
            };
        }

        private static void ApplyMainMenu(UiFonts fonts, UiSprites sprites, Material weatherMat, Material settingsScrimMat)
        {
            AssetDatabase.ImportAsset(ArtPath);
            var artwork = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtPath);
            var rainAudio = AssetDatabase.LoadAssetAtPath<AudioClip>(RainAudioPath);
            if (artwork == null)
                throw new InvalidOperationException("Missing menu artwork at " + ArtPath);

            Scene scene = EditorSceneManager.OpenScene(SceneFlow.PathFor(GameScene.MainMenu), OpenSceneMode.Single);
            GameObject canvas = FindRoot(scene, "UI_Root");
            GameObject systems = FindRoot(scene, "Systems");
            if (canvas == null || systems == null)
                throw new InvalidOperationException("UI_Root or Systems missing in MainMenu.");

            var shell = systems.GetComponent<ShellScreen>();
            Transform content = canvas.transform.Find("ScreenContent");
            if (shell == null || content == null)
                throw new InvalidOperationException("ShellScreen or ScreenContent missing in MainMenu.");

            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            Stretch((RectTransform)content);

            // 1. Living Atmospheric Backdrop (wet raindrops + creeping shadow + kos doorway flicker)
            Transform oldBackdrop = canvas.transform.Find("Backdrop");
            if (oldBackdrop != null) UnityEngine.Object.DestroyImmediate(oldBackdrop.gameObject);
            Transform oldAtmos = canvas.transform.Find("MenuAtmosphere");
            if (oldAtmos != null) UnityEngine.Object.DestroyImmediate(oldAtmos.gameObject);

            var atmosRoot = new GameObject("MenuAtmosphere", typeof(RectTransform));
            atmosRoot.transform.SetParent(canvas.transform, false);
            atmosRoot.transform.SetAsFirstSibling();
            Stretch((RectTransform)atmosRoot.transform);

            RawImage artImg = CreateRawImage("GeneratedArtwork", atmosRoot.transform);
            artImg.texture = artwork;
            RawImage weatherImg = CreateRawImage("AnimatedRainAndMist", atmosRoot.transform);
            weatherImg.material = weatherMat;

            var atmos = atmosRoot.AddComponent<MenuAtmosphere>();
            var atmosSo = new SerializedObject(atmos);
            atmosSo.FindProperty("artwork").objectReferenceValue = artImg;
            atmosSo.FindProperty("weather").objectReferenceValue = weatherImg;
            atmosSo.FindProperty("motionAmount").floatValue = 0.42f;
            atmosSo.FindProperty("rainAmbienceClip").objectReferenceValue = rainAudio;
            atmosSo.FindProperty("rainTargetVolume").floatValue = 0.24f;
            atmosSo.ApplyModifiedPropertiesWithoutUndo();

            // 2. Editorial Title & Narrative Framing (floating cleanly in dark creeping rain shadow)
            Transform eyebrowTr = content.Find("Eyebrow");
            if (eyebrowTr != null)
            {
                eyebrowTr.gameObject.SetActive(true);
                Text eyebrow = eyebrowTr.GetComponent<Text>();
                StyleText(eyebrow, fonts.BodyBoldFont, 17, ColdMist, TextAnchor.MiddleLeft, true);
                eyebrow.text = "HOROR PSIKOLOGIS   ·   ORANG PERTAMA";
                PlaceMenuLeft(eyebrow.rectTransform, 376f, new Vector2(620f, 30f), 16f);
            }

            Image eyebrowAccent = UpsertImage(content, "EyebrowAccent", null, DoorwayAmber);
            PlaceMenuLeft(eyebrowAccent.rectTransform, 376f, new Vector2(2.5f, 15f), 0f);
            eyebrowAccent.raycastTarget = false;

            Text title = content.Find("Title").GetComponent<Text>();
            StyleText(title, fonts.TitleFont, 74, IvoryInk, TextAnchor.MiddleLeft, true);
            title.text = "NO RAINBOW\nAFTER";
            title.lineSpacing = 0.90f;
            PlaceMenuLeft(title.rectTransform, 248f, new Vector2(720f, 196f));
            var titleShadow = title.GetComponent<Shadow>();
            if (titleShadow != null)
            {
                titleShadow.effectColor = new Color(0.0f, 0.01f, 0.02f, 0.92f);
                titleShadow.effectDistance = new Vector2(2.5f, -3.5f);
            }

            Transform ruleTr = content.Find("TitleRule");
            if (ruleTr != null)
            {
                ruleTr.gameObject.SetActive(true);
                Image ruleImg = ruleTr.GetComponent<Image>();
                ruleImg.sprite = sprites.SoftRule;
                ruleImg.color = new Color(DoorwayAmber.r, DoorwayAmber.g, DoorwayAmber.b, 0.88f);
                ruleImg.raycastTarget = false;
                PlaceMenuLeft(ruleImg.rectTransform, 144f, new Vector2(310f, 2f));
            }

            Text tagline = UpsertText(content, "TitleTagline", fonts.BodyFont, 22, new Color(0.75f, 0.83f, 0.86f, 0.95f), TextAnchor.MiddleLeft, true);
            tagline.text = "Di balik pintu yang terbuka, hujan tak pernah benar-benar reda.";
            PlaceMenuLeft(tagline.rectTransform, 112f, new Vector2(660f, 36f));

            // 3. Navigation Stack (feathered smoky mist focus — zero boxes!)
            StyleEditorialButton(content.Find("NewGameButton").GetComponent<Button>(), "Mulai Baru", fonts.NavFont, sprites, 22f, 460f, 56f, 44, false);
            StyleEditorialButton(content.Find("ContinueButton").GetComponent<Button>(), "Lanjutkan", fonts.NavFont, sprites, -40f, 460f, 56f, 44, false);
            StyleEditorialButton(content.Find("SettingsButton").GetComponent<Button>(), "Pengaturan", fonts.NavFont, sprites, -102f, 460f, 56f, 44, false);
            StyleEditorialButton(content.Find("CreditsButton").GetComponent<Button>(), "Kredit", fonts.NavFont, sprites, -164f, 460f, 56f, 44, false);
            StyleEditorialButton(content.Find("QuitButton").GetComponent<Button>(), "Keluar", fonts.NavFont, sprites, -226f, 460f, 56f, 44, true);

            Text hint = content.Find("SaveHint").GetComponent<Text>();
            StyleText(hint, fonts.BodyFont, 18, MutedSlate, TextAnchor.MiddleLeft, true);
            hint.text = "Belum ada permainan tersimpan.";
            PlaceMenuLeft(hint.rectTransform, -296f, new Vector2(540f, 30f), 20f);

            Text status = content.Find("LoadingStatus").GetComponent<Text>();
            StyleText(status, fonts.BodyBoldFont, 20, DoorwayAmber, TextAnchor.MiddleLeft, true);
            PlaceMenuLeft(status.rectTransform, -344f, new Vector2(640f, 36f), 20f);

            Image footerRule = UpsertImage(content, "FooterRule", sprites.SoftRule, new Color(0.45f, 0.62f, 0.69f, 0.42f));
            PlaceMenuLeft(footerRule.rectTransform, -432f, new Vector2(460f, 1.5f));
            footerRule.raycastTarget = false;

            Text footer = content.Find("Footer").GetComponent<Text>();
            StyleText(footer, fonts.BodyFont, 17, MutedSlate, TextAnchor.MiddleLeft, true);
            footer.text = "W / S / Panah  Navigasi      ·      Enter  Pilih      ·      Esc  Kembali";
            PlaceMenuLeft(footer.rectTransform, -464f, new Vector2(660f, 32f));

            Text edition = UpsertText(content, "EditionStamp", fonts.BodyBoldFont, 15, new Color(0.62f, 0.74f, 0.79f, 0.78f), TextAnchor.MiddleRight, true);
            edition.text = "NO RAINBOW AFTER   ·   BAB I–III";
            edition.rectTransform.anchorMin = edition.rectTransform.anchorMax = new Vector2(0.94f, 0.07f);
            edition.rectTransform.pivot = new Vector2(1f, 0.5f);
            edition.rectTransform.anchoredPosition = Vector2.zero;
            edition.rectTransform.sizeDelta = new Vector2(420f, 28f);

            // 4. Organic Hairline SettingsPanel (floating over translucent wet-rain + shadow scrim with minimal Ref-2 hairline frame)
            Transform settings = canvas.transform.Find("SettingsPanel");
            if (settings == null) settings = content.Find("SettingsPanel");
            if (settings == null) throw new InvalidOperationException("SettingsPanel missing in MainMenu.");
            settings.SetParent(canvas.transform, false);
            Stretch((RectTransform)settings);

            // Transparent raycast catch on panel itself; visual scrim comes from living wet-rain shader
            Image settingsRootImg = settings.GetComponent<Image>();
            settingsRootImg.color = new Color(0f, 0f, 0f, 0.004f);
            settingsRootImg.raycastTarget = true;

            var settingsCg = settings.GetComponent<CanvasGroup>();
            if (settingsCg == null) settingsCg = settings.gameObject.AddComponent<CanvasGroup>();
            settingsCg.alpha = 1f;

            // Remove old rectangular SettingsCard box!
            Transform oldCard = settings.Find("SettingsCard");
            if (oldCard != null) UnityEngine.Object.DestroyImmediate(oldCard.gameObject);

            // Add translucent wet-rain + dark-shadow backdrop
            Transform oldSettingsScrim = settings.Find("SettingsScrimBackdrop");
            if (oldSettingsScrim != null) UnityEngine.Object.DestroyImmediate(oldSettingsScrim.gameObject);
            RawImage settingsScrimImg = CreateRawImage("SettingsScrimBackdrop", settings);
            settingsScrimImg.material = settingsScrimMat;
            settingsScrimImg.color = Color.white;
            settingsScrimImg.transform.SetAsFirstSibling();

            // Remove any hairline frame box so SettingsPanel stays completely box-free
            Transform oldSettingsFrame = settings.Find("SettingsHairlineFrame");
            if (oldSettingsFrame != null) UnityEngine.Object.DestroyImmediate(oldSettingsFrame.gameObject);

            Text settingsEyebrow = UpsertText(settings, "SettingsEyebrow", fonts.BodyBoldFont, 17, ColdMist, TextAnchor.MiddleLeft, true);
            settingsEyebrow.text = "NO RAINBOW AFTER   ·   SISTEM";
            PlaceMenuLeft(settingsEyebrow.rectTransform, 326f, new Vector2(640f, 26f), 16f);

            Image settingsEyebrowBar = UpsertImage(settings, "SettingsEyebrowAccent", null, DoorwayAmber);
            PlaceMenuLeft(settingsEyebrowBar.rectTransform, 326f, new Vector2(2.5f, 14f), 0f);
            settingsEyebrowBar.raycastTarget = false;

            Text settingsTitle = settings.Find("SettingsTitle").GetComponent<Text>();
            StyleText(settingsTitle, fonts.TitleFont, 50, IvoryInk, TextAnchor.MiddleLeft, true);
            settingsTitle.text = "PENGATURAN";
            PlaceMenuLeft(settingsTitle.rectTransform, 270f, new Vector2(680f, 70f));

            Image settingsRule = UpsertImage(settings, "SettingsRule", sprites.SoftRule, new Color(DoorwayAmber.r, DoorwayAmber.g, DoorwayAmber.b, 0.86f));
            PlaceMenuLeft(settingsRule.rectTransform, 218f, new Vector2(280f, 2f));
            settingsRule.raycastTarget = false;

            Text settingsSub = UpsertText(settings, "SettingsSubtitle", fonts.BodyFont, 22, ColdMist, TextAnchor.MiddleLeft, true);
            settingsSub.text = "Sesuaikan audio dan kualitas visual sebelum memasuki malam.";
            PlaceMenuLeft(settingsSub.rectTransform, 182f, new Vector2(640f, 34f));

            Text masterLabel = settings.Find("MasterLabel").GetComponent<Text>();
            StyleText(masterLabel, fonts.BodyBoldFont, 24, SoftPaper, TextAnchor.MiddleLeft, true);
            masterLabel.text = "Volume Utama";
            PlaceMenuLeft(masterLabel.rectTransform, 102f, new Vector2(440f, 40f));

            Text valueLabel = settings.Find("VolumeValue").GetComponent<Text>();
            StyleText(valueLabel, fonts.BodyBoldFont, 22, DoorwayAmber, TextAnchor.MiddleRight, true);
            PlaceMenuLeft(valueLabel.rectTransform, 102f, new Vector2(180f, 40f), 440f);

            Slider masterSlider = settings.Find("MasterVolumeSlider").GetComponent<Slider>();
            PlaceMenuLeft((RectTransform)masterSlider.transform, 52f, new Vector2(620f, 30f));
            StyleSlider(masterSlider, sprites);

            Text qualitySection = UpsertText(settings, "QualitySectionLabel", fonts.BodyBoldFont, 24, SoftPaper, TextAnchor.MiddleLeft, true);
            qualitySection.text = "Kualitas Grafis";
            PlaceMenuLeft(qualitySection.rectTransform, -26f, new Vector2(620f, 40f));

            Text qualityPreset = UpsertText(settings, "QualityPresetLabel", fonts.BodyFont, 18, ColdMist, TextAnchor.MiddleLeft, true);
            qualityPreset.text = "Kualitas Grafis  —  Tinggi";
            PlaceMenuLeft(qualityPreset.rectTransform, -60f, new Vector2(620f, 28f));

            // Borderless floating horror quality tabs (ZERO rectangular boxes!)
            Button lowBtn = StyleOrganicQualityTab(settings.Find("QualityLowButton").GetComponent<Button>(), "Rendah", fonts.BodyBoldFont, sprites, new Vector2(0.075f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, -118f), new Vector2(190f, 48f));
            Button medBtn = StyleOrganicQualityTab(settings.Find("QualityMediumButton").GetComponent<Button>(), "Sedang", fonts.BodyBoldFont, sprites, new Vector2(0.075f, 0.5f), new Vector2(0f, 0.5f), new Vector2(215f, -118f), new Vector2(190f, 48f));
            Button highBtn = StyleOrganicQualityTab(settings.Find("QualityHighButton").GetComponent<Button>(), "Tinggi", fonts.BodyBoldFont, sprites, new Vector2(0.075f, 0.5f), new Vector2(0f, 0.5f), new Vector2(430f, -118f), new Vector2(190f, 48f));

            Button backBtn = settings.Find("SettingsBackButton").GetComponent<Button>();
            StyleEditorialButton(backBtn, "Kembali", fonts.NavFont, sprites, -224f, 360f, 56f, 42, false);

            Text settingsHint = settings.Find("SettingsHint").GetComponent<Text>();
            StyleText(settingsHint, fonts.BodyFont, 17, MutedSlate, TextAnchor.MiddleLeft, true);
            settingsHint.text = "ESC   ·   Kembali ke Menu Utama";
            PlaceMenuLeft(settingsHint.rectTransform, -290f, new Vector2(620f, 30f), 20f);

            // Smooth dark intro fade overlay
            Image introFade = UpsertImage(canvas.transform, "IntroFadeOverlay", null, new Color(VoidSlate.r, VoidSlate.g, VoidSlate.b, 0f));
            Stretch(introFade.rectTransform);
            introFade.raycastTarget = false;

            settings.SetAsLastSibling();
            introFade.transform.SetAsLastSibling();

            AssignObjectRef(shell, "qualityPresetLabel", qualityPreset);
            AssignObjectRef(shell, "qualityLowButton", lowBtn);
            AssignObjectRef(shell, "qualityMediumButton", medBtn);
            AssignObjectRef(shell, "qualityHighButton", highBtn);
            AssignObjectRef(shell, "settingsCanvasGroup", settingsCg);
            AssignObjectRef(shell, "settingsScrim", settingsScrimImg);
            AssignObjectRef(shell, "introFadeOverlay", introFade);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Failed to save redesigned MainMenu.unity.");
        }

        private static void ApplyPauseScene(string scenePath, UiFonts fonts, UiSprites sprites, Material pauseScrimMat, bool hasCheckpointAndQuality)
        {
            if (!File.Exists(scenePath)) return;

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject uiRoot = FindRoot(scene, "UI_FirstPerson");
            if (uiRoot == null)
                throw new InvalidOperationException("UI_FirstPerson missing in " + scenePath);

            var session = uiRoot.GetComponent<FirstPersonSession>();
            Transform pause = uiRoot.transform.Find("PausePanel");
            if (session == null || pause == null)
                throw new InvalidOperationException("FirstPersonSession or PausePanel missing in " + scenePath);

            var scaler = uiRoot.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            }

            Stretch((RectTransform)pause);
            Image pauseRootImg = pause.GetComponent<Image>();
            if (pauseRootImg != null)
            {
                pauseRootImg.color = new Color(0f, 0f, 0f, 0.004f);
                pauseRootImg.raycastTarget = true;
            }
            var pauseCg = pause.GetComponent<CanvasGroup>();
            if (pauseCg == null) pauseCg = pause.gameObject.AddComponent<CanvasGroup>();
            pauseCg.alpha = 1f;

            // Layer 0: Translucent wet-raindrops + creeping black shadows + lurking shadow figure shader!
            Transform oldScrim = pause.Find("PauseScrimBackdrop");
            if (oldScrim != null) UnityEngine.Object.DestroyImmediate(oldScrim.gameObject);
            RawImage scrimImg = CreateRawImage("PauseScrimBackdrop", pause);
            scrimImg.material = pauseScrimMat;
            scrimImg.color = Color.white;
            scrimImg.transform.SetAsFirstSibling();

            // Remove old boxy PauseSettingsCard / PauseSettingsFrame completely!
            Transform oldSettingsFrame = pause.Find("PauseSettingsFrame");
            if (oldSettingsFrame != null) UnityEngine.Object.DestroyImmediate(oldSettingsFrame.gameObject);
            Transform oldRightCard = pause.Find("PauseSettingsCard");
            if (oldRightCard != null) UnityEngine.Object.DestroyImmediate(oldRightCard.gameObject);

            // Convert PauseControlsCard & PauseContextCard into 100% invisible borderless containers (ZERO boxes!)
            Transform controlsContainer = MakeBorderlessContainer(pause, "PauseControlsCard", new Vector2(-505f, -206f), new Vector2(450f, 250f));
            controlsContainer.SetSiblingIndex(1);
            PopulateControlsLegend(controlsContainer, fonts, sprites);

            Transform contextContainer = MakeBorderlessContainer(pause, "PauseContextCard", new Vector2(510f, 406f), new Vector2(440f, 92f));
            contextContainer.SetSiblingIndex(2);
            BuildContextReadout(contextContainer, fonts, sprites, out Text chapterStatusLabel, out Text stressStatusLabel, out RectTransform stressFillBar, out Image stressFillImg);

            // Delicate fading mist hairlines (never closed boxes!)
            Image headerLine = UpsertImage(pause, "PauseHeaderRule", sprites.SoftRule, new Color(0.45f, 0.62f, 0.69f, 0.28f));
            PlaceCenterLeft(headerLine.rectTransform, new Vector2(-730f, 340f), new Vector2(1460f, 1.5f));
            headerLine.raycastTarget = false;
            headerLine.transform.SetSiblingIndex(3);

            Image verticalDivider = UpsertImage(pause, "PauseColumnMistLine", sprites.VerticalFadeRule, new Color(0.45f, 0.62f, 0.69f, 0.26f));
            PlaceCenter(verticalDivider.rectTransform, new Vector2(8f, -34f), new Vector2(1.5f, 640f));
            verticalDivider.raycastTarget = false;
            verticalDivider.transform.SetSiblingIndex(4);

            Image footerLine = UpsertImage(pause, "PauseFooterRule", sprites.SoftRule, new Color(0.45f, 0.62f, 0.69f, 0.26f));
            PlaceCenterLeft(footerLine.rectTransform, new Vector2(-730f, -418f), new Vector2(1460f, 1.5f));
            footerLine.raycastTarget = false;
            footerLine.transform.SetSiblingIndex(5);

            // Layer 2: Top Header
            Text eyebrow = UpsertText(pause, "PauseEyebrow", fonts.BodyBoldFont, 17, ColdMist, TextAnchor.MiddleLeft, true);
            eyebrow.text = "JEDA PERMAINAN   ·   NO RAINBOW AFTER";
            PlaceCenterLeft(eyebrow.rectTransform, new Vector2(-714f, 444f), new Vector2(560f, 26f));

            Image eyebrowBar = UpsertImage(pause, "PauseEyebrowAccent", null, DoorwayAmber);
            PlaceCenterLeft(eyebrowBar.rectTransform, new Vector2(-730f, 444f), new Vector2(2.5f, 14f));
            eyebrowBar.raycastTarget = false;

            Text pauseTitle = pause.Find("PauseTitle").GetComponent<Text>();
            StyleText(pauseTitle, fonts.TitleFont, 56, IvoryInk, TextAnchor.MiddleLeft, true);
            pauseTitle.text = "JEDA";
            PlaceCenterLeft(pauseTitle.rectTransform, new Vector2(-730f, 392f), new Vector2(520f, 70f));

            Image titleAccentRule = UpsertImage(pause, "PauseTitleRule", sprites.SoftRule, new Color(DoorwayAmber.r, DoorwayAmber.g, DoorwayAmber.b, 0.90f));
            PlaceCenterLeft(titleAccentRule.rectTransform, new Vector2(-730f, 340f), new Vector2(270f, 2f));
            titleAccentRule.raycastTarget = false;

            // Layer 3: Left Column — Floating Actions (x = -730 .. -240, leaving open dark mist center for the lurking shadow!)
            Text actionsHeader = UpsertText(pause, "ActionsSectionHeader", fonts.BodyBoldFont, 18, ColdMist, TextAnchor.MiddleLeft, true);
            actionsHeader.text = "TINDAKAN";
            PlaceCenterLeft(actionsHeader.rectTransform, new Vector2(-730f, 286f), new Vector2(460f, 26f));

            Button resumeBtn = pause.Find("ResumeButton").GetComponent<Button>();
            StylePauseActionButton(resumeBtn, "Lanjutkan Permainan", fonts.NavFont, sprites, new Vector2(-730f, 224f), new Vector2(480f, 58f), 40, false);

            Transform checkpointTr = pause.Find("CheckpointButton");
            float menuBtnY = 152f;
            if (checkpointTr != null)
            {
                StylePauseActionButton(checkpointTr.GetComponent<Button>(), "Kembali ke Titik Awal", fonts.NavFont, sprites, new Vector2(-730f, 152f), new Vector2(480f, 58f), 40, false);
                menuBtnY = 80f;
            }

            Button menuBtn = pause.Find("ReturnToMenuButton").GetComponent<Button>();
            StylePauseActionButton(menuBtn, "Kembali ke Menu Utama", fonts.NavFont, sprites, new Vector2(-730f, menuBtnY), new Vector2(480f, 58f), 40, true);

            // Layer 4: Right Column — Floating Controls inside Ref-2 hairline frame (x = +84 .. +730, width = 646)
            const float rightX = 84f;
            const float rightW = 646f;

            Text settingsHeader = UpsertText(pause, "SettingsSectionHeader", fonts.BodyBoldFont, 18, ColdMist, TextAnchor.MiddleLeft, true);
            settingsHeader.text = "PENGATURAN KONTROL & VISUAL";
            PlaceCenterLeft(settingsHeader.rectTransform, new Vector2(rightX, 286f), new Vector2(rightW, 26f));

            Image settingsHeaderRule = UpsertImage(pause, "SettingsSectionRule", sprites.SoftRule, new Color(0.45f, 0.62f, 0.69f, 0.34f));
            PlaceCenterLeft(settingsHeaderRule.rectTransform, new Vector2(rightX, 262f), new Vector2(320f, 1.5f));
            settingsHeaderRule.raycastTarget = false;

            // Master Volume
            Text volLabel = UpsertText(pause, "MasterVolumeLabel", fonts.BodyFont, 21, SoftPaper, TextAnchor.MiddleLeft, true);
            volLabel.text = "Volume utama  80%";
            PlaceCenterLeft(volLabel.rectTransform, new Vector2(rightX, 222f), new Vector2(rightW, 30f));

            Slider volSlider = UpsertSliderFromTemplate(pause, pause.Find("FovSlider").gameObject, "MasterVolumeSlider", 0f, 1f, 0.8f);
            PlaceCenterLeft((RectTransform)volSlider.transform, new Vector2(rightX, 186f), new Vector2(rightW, 28f));
            StyleSlider(volSlider, sprites);

            // Sensitivity
            Text sensLabel = pause.Find("SensitivityLabel").GetComponent<Text>();
            StyleText(sensLabel, fonts.BodyFont, 21, SoftPaper, TextAnchor.MiddleLeft, true);
            sensLabel.text = "Sensitivitas mouse  0.08";
            PlaceCenterLeft(sensLabel.rectTransform, new Vector2(rightX, 134f), new Vector2(rightW, 30f));

            Slider sensSlider = pause.Find("SensitivitySlider").GetComponent<Slider>();
            PlaceCenterLeft((RectTransform)sensSlider.transform, new Vector2(rightX, 98f), new Vector2(rightW, 28f));
            StyleSlider(sensSlider, sprites);

            // FOV
            Text fovLabel = pause.Find("FovLabel").GetComponent<Text>();
            StyleText(fovLabel, fonts.BodyFont, 21, SoftPaper, TextAnchor.MiddleLeft, true);
            fovLabel.text = "Bidang pandang  75°";
            PlaceCenterLeft(fovLabel.rectTransform, new Vector2(rightX, 46f), new Vector2(rightW, 30f));

            Slider fovSlider = pause.Find("FovSlider").GetComponent<Slider>();
            PlaceCenterLeft((RectTransform)fovSlider.transform, new Vector2(rightX, 10f), new Vector2(rightW, 28f));
            StyleSlider(fovSlider, sprites);

            // Circular View (MasterGameplay)
            Transform viewRadiusLabelTr = pause.Find("ViewRadiusLabel");
            Transform viewRadiusSliderTr = pause.Find("ViewRadiusSlider");
            if (viewRadiusLabelTr != null && viewRadiusSliderTr != null)
            {
                Text vrLabel = viewRadiusLabelTr.GetComponent<Text>();
                StyleText(vrLabel, fonts.BodyFont, 21, SoftPaper, TextAnchor.MiddleLeft, true);
                vrLabel.text = "Lebar pandang  50%";
                PlaceCenterLeft(vrLabel.rectTransform, new Vector2(rightX, -42f), new Vector2(rightW, 30f));

                Slider vrSlider = viewRadiusSliderTr.GetComponent<Slider>();
                PlaceCenterLeft((RectTransform)vrSlider.transform, new Vector2(rightX, -78f), new Vector2(rightW, 28f));
                StyleSlider(vrSlider, sprites);
            }

            Transform toggleTr = pause.Find("CircularViewToggle");
            Transform toggleCapTr = pause.Find("CircularViewCaption");
            if (toggleTr != null)
            {
                StyleOrganicToggle(toggleTr.GetComponent<Toggle>(), sprites, new Vector2(rightX + 14f, -134f));
            }
            if (toggleCapTr != null)
            {
                Text capText = toggleCapTr.GetComponent<Text>();
                StyleText(capText, fonts.BodyFont, 21, SoftPaper, TextAnchor.MiddleLeft, true);
                capText.text = "Tepi pandangan melingkar";
                PlaceCenterLeft(capText.rectTransform, new Vector2(rightX + 42f, -134f), new Vector2(rightW - 42f, 34f));
            }

            // Borderless floating Quality Preset Tabs (MasterGameplay)
            if (hasCheckpointAndQuality)
            {
                Transform qLabelTr = pause.Find("QualityPresetLabel");
                if (qLabelTr != null)
                {
                    Text qLabel = qLabelTr.GetComponent<Text>();
                    StyleText(qLabel, fonts.BodyFont, 21, SoftPaper, TextAnchor.MiddleLeft, true);
                    qLabel.text = "Kualitas grafis  —  Tinggi";
                    PlaceCenterLeft(qLabel.rectTransform, new Vector2(rightX, -196f), new Vector2(rightW, 30f));
                }

                Transform qLow = pause.Find("QualityLowButton");
                Transform qMed = pause.Find("QualityMediumButton");
                Transform qHigh = pause.Find("QualityHighButton");
                // Three evenly spaced floating tabs spanning rightX (84) .. rightX + 646 (730)
                if (qLow != null) StyleOrganicQualityTab(qLow.GetComponent<Button>(), "Rendah", fonts.BodyBoldFont, sprites, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(rightX + 100f, -252f), new Vector2(200f, 48f));
                if (qMed != null) StyleOrganicQualityTab(qMed.GetComponent<Button>(), "Sedang", fonts.BodyBoldFont, sprites, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(rightX + 323f, -252f), new Vector2(200f, 48f));
                if (qHigh != null) StyleOrganicQualityTab(qHigh.GetComponent<Button>(), "Tinggi", fonts.BodyBoldFont, sprites, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(rightX + 546f, -252f), new Vector2(200f, 48f));
            }

            // Bottom Footer
            Text footer = UpsertText(pause, "PauseFooter", fonts.BodyFont, 17, MutedSlate, TextAnchor.MiddleLeft, true);
            footer.text = "ESC  Lanjutkan Permainan       ·       Perubahan pengaturan disimpan otomatis";
            PlaceCenterLeft(footer.rectTransform, new Vector2(-730f, -454f), new Vector2(1460f, 30f));

            AssignObjectRef(session, "masterVolumeSlider", volSlider);
            AssignObjectRef(session, "masterVolumeLabel", volLabel);
            AssignObjectRef(session, "pauseCanvasGroup", pauseCg);
            AssignObjectRef(session, "pauseScrim", scrimImg);
            AssignObjectRef(session, "chapterStatusLabel", chapterStatusLabel);
            AssignObjectRef(session, "stressStatusLabel", stressStatusLabel);
            AssignObjectRef(session, "stressFillBar", stressFillBar);
            AssignObjectRef(session, "stressFillImage", stressFillImg);

            ApplyGameplayObjectiveStyle(uiRoot, fonts);

            pause.gameObject.SetActive(false);
            EditorUtility.SetDirty(session);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Failed to save redesigned pause UI in " + scenePath);
        }

        private static void ApplyGameplayObjectiveStyle(GameObject uiRoot, UiFonts fonts)
        {
            Transform hud = uiRoot.transform.Find("GameplayHUD");
            if (hud == null) return;

            Transform objTr = hud.Find("TestObjective");
            if (objTr != null)
            {
                Text objText = objTr.GetComponent<Text>();
                if (objText != null)
                {
                    objText.font = fonts.NavFont;
                    objText.fontSize = 44;
                    objText.fontStyle = FontStyle.Normal;
                    objText.color = IvoryInk;
                    objText.alignment = TextAnchor.MiddleLeft;
                    objText.horizontalOverflow = HorizontalWrapMode.Overflow;
                    objText.verticalOverflow = VerticalWrapMode.Overflow;
                    objText.raycastTarget = false;

                    RectTransform rect = objText.rectTransform;
                    rect.sizeDelta = new Vector2(Mathf.Max(560f, rect.sizeDelta.x), Mathf.Max(58f, rect.sizeDelta.y));
                    if (rect.anchoredPosition.y > 405f)
                        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, 405f);

                    var shadow = objTr.GetComponent<Shadow>() ?? objTr.gameObject.AddComponent<Shadow>();
                    shadow.effectColor = new Color(0f, 0.01f, 0.02f, 0.92f);
                    shadow.effectDistance = new Vector2(1.8f, -2.0f);

                    var outline = objTr.GetComponent<Outline>() ?? objTr.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(0f, 0f, 0f, 0.65f);
                    outline.effectDistance = new Vector2(0.8f, -0.8f);

                    EditorUtility.SetDirty(objText);
                }
            }

            Transform hintTr = hud.Find("ControlHint");
            if (hintTr != null)
            {
                Text hintText = hintTr.GetComponent<Text>();
                if (hintText != null)
                {
                    hintText.font = fonts.BodyFont;
                    hintText.fontSize = 24;
                    hintText.fontStyle = FontStyle.Normal;
                    hintText.color = SoftPaper;
                    hintText.verticalOverflow = VerticalWrapMode.Overflow;
                    hintText.raycastTarget = false;

                    var shadow = hintTr.GetComponent<Shadow>() ?? hintTr.gameObject.AddComponent<Shadow>();
                    shadow.effectColor = new Color(0f, 0.01f, 0.02f, 0.90f);
                    shadow.effectDistance = new Vector2(1.2f, -1.5f);

                    EditorUtility.SetDirty(hintText);
                }
            }
        }

        private static Transform MakeBorderlessContainer(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            Transform tr = parent.Find(name);
            GameObject go = tr != null ? tr.gameObject : new GameObject(name, typeof(RectTransform));
            if (tr == null) go.transform.SetParent(parent, false);

            // Destroy any boxy Image/Border/TopAccent left from previous layout
            Image img = go.GetComponent<Image>();
            if (img != null) UnityEngine.Object.DestroyImmediate(img);
            for (int i = go.transform.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(go.transform.GetChild(i).gameObject);

            PlaceCenter((RectTransform)go.transform, pos, size);
            return go.transform;
        }

        private static void PopulateControlsLegend(Transform container, UiFonts fonts, UiSprites sprites)
        {
            Text header = UpsertText(container, "ControlsTitle", fonts.BodyBoldFont, 17, ColdMist, TextAnchor.MiddleLeft, true);
            header.text = "KONTROL SINGKAT";
            header.rectTransform.anchorMin = header.rectTransform.anchorMax = new Vector2(0f, 1f);
            header.rectTransform.pivot = new Vector2(0f, 1f);
            header.rectTransform.anchoredPosition = new Vector2(0f, 0f);
            header.rectTransform.sizeDelta = new Vector2(440f, 24f);

            Image rule = UpsertImage(container, "ControlsRule", sprites.SoftRule, new Color(0.45f, 0.62f, 0.69f, 0.36f));
            rule.rectTransform.anchorMin = rule.rectTransform.anchorMax = new Vector2(0f, 1f);
            rule.rectTransform.pivot = new Vector2(0f, 1f);
            rule.rectTransform.anchoredPosition = new Vector2(0f, -28f);
            rule.rectTransform.sizeDelta = new Vector2(260f, 1.5f);
            rule.raycastTarget = false;

            (string key, string action)[] rows =
            {
                ("WASD", "Bergerak"),
                ("SHIFT", "Berlari"),
                ("CTRL", "Merunduk / Bersembunyi"),
                ("E", "Interaksi / Periksa"),
                ("F / TAB", "Tutup Catatan / Laptop"),
                ("ESC", "Jeda Permainan")
            };

            for (int i = 0; i < rows.Length; i++)
            {
                float y = -46f - i * 31f;
                Text k = UpsertText(container, "Key_" + i, fonts.BodyBoldFont, 17, DoorwayAmber, TextAnchor.MiddleLeft, true);
                k.text = rows[i].key;
                k.rectTransform.anchorMin = k.rectTransform.anchorMax = new Vector2(0f, 1f);
                k.rectTransform.pivot = new Vector2(0f, 1f);
                k.rectTransform.anchoredPosition = new Vector2(0f, y);
                k.rectTransform.sizeDelta = new Vector2(118f, 26f);

                Text a = UpsertText(container, "Action_" + i, fonts.BodyFont, 18, SoftPaper, TextAnchor.MiddleLeft, true);
                a.text = rows[i].action;
                a.rectTransform.anchorMin = a.rectTransform.anchorMax = new Vector2(0f, 1f);
                a.rectTransform.pivot = new Vector2(0f, 1f);
                a.rectTransform.anchoredPosition = new Vector2(126f, y);
                a.rectTransform.sizeDelta = new Vector2(320f, 26f);
            }
        }

        private static void BuildContextReadout(
            Transform container,
            UiFonts fonts,
            UiSprites sprites,
            out Text chapterLabel,
            out Text stressLabel,
            out RectTransform stressFillBar,
            out Image stressFillImg)
        {
            // Floating right-aligned editorial status — NO card box, just a thin vertical accent sliver and stress bar
            Image rightAccent = UpsertImage(container, "RightAccent", null, new Color(DoorwayAmber.r, DoorwayAmber.g, DoorwayAmber.b, 0.85f));
            rightAccent.rectTransform.anchorMin = new Vector2(1f, 0.14f);
            rightAccent.rectTransform.anchorMax = new Vector2(1f, 0.88f);
            rightAccent.rectTransform.pivot = new Vector2(1f, 0.5f);
            rightAccent.rectTransform.anchoredPosition = Vector2.zero;
            rightAccent.rectTransform.sizeDelta = new Vector2(2f, 0f);
            rightAccent.raycastTarget = false;

            chapterLabel = UpsertText(container, "ChapterStatusLabel", fonts.BodyBoldFont, 17, DoorwayAmber, TextAnchor.MiddleRight, true);
            chapterLabel.text = "BAB I  ·  KOS RAKA";
            chapterLabel.rectTransform.anchorMin = chapterLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            chapterLabel.rectTransform.pivot = new Vector2(1f, 1f);
            chapterLabel.rectTransform.anchoredPosition = new Vector2(-16f, -8f);
            chapterLabel.rectTransform.sizeDelta = new Vector2(410f, 22f);

            stressLabel = UpsertText(container, "StressStatusLabel", fonts.BodyFont, 20, IvoryInk, TextAnchor.MiddleRight, true);
            stressLabel.text = "Kondisi Mental  ·  Gelisah (50%)";
            stressLabel.rectTransform.anchorMin = stressLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            stressLabel.rectTransform.pivot = new Vector2(1f, 1f);
            stressLabel.rectTransform.anchoredPosition = new Vector2(-16f, -35f);
            stressLabel.rectTransform.sizeDelta = new Vector2(410f, 26f);

            Image track = UpsertImage(container, "StressBarTrack", null, new Color(0.20f, 0.30f, 0.35f, 0.58f));
            track.raycastTarget = false;
            track.rectTransform.anchorMin = track.rectTransform.anchorMax = new Vector2(1f, 0f);
            track.rectTransform.pivot = new Vector2(1f, 0f);
            track.rectTransform.anchoredPosition = new Vector2(-16f, 12f);
            track.rectTransform.sizeDelta = new Vector2(360f, 3.5f);

            var fillGo = new GameObject("StressBarFill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(track.transform, false);
            stressFillBar = (RectTransform)fillGo.transform;
            stressFillBar.anchorMin = Vector2.zero;
            stressFillBar.anchorMax = new Vector2(0.5f, 1f);
            stressFillBar.offsetMin = stressFillBar.offsetMax = Vector2.zero;
            stressFillImg = fillGo.GetComponent<Image>();
            stressFillImg.color = new Color(0.84f, 0.65f, 0.39f, 0.94f);
            stressFillImg.raycastTarget = false;
        }

        private static void StyleEditorialButton(
            Button button,
            string captionText,
            Font font,
            UiSprites sprites,
            float y,
            float width,
            float height,
            int fontSize,
            bool isCautionAction)
        {
            PlaceMenuLeft((RectTransform)button.transform, y, new Vector2(width, height));
            ConfigureFocusButtonVisuals(button, captionText, font, sprites, fontSize, isCautionAction);
        }

        private static void StylePauseActionButton(
            Button button,
            string captionText,
            Font font,
            UiSprites sprites,
            Vector2 pos,
            Vector2 size,
            int fontSize,
            bool isCautionAction)
        {
            PlaceCenterLeft((RectTransform)button.transform, pos, size);
            ConfigureFocusButtonVisuals(button, captionText, font, sprites, fontSize, isCautionAction);
        }

        private static void ConfigureFocusButtonVisuals(
            Button button,
            string captionText,
            Font font,
            UiSprites sprites,
            int fontSize,
            bool isCautionAction)
        {
            var hitArea = button.GetComponent<Image>();
            hitArea.sprite = null;
            hitArea.color = Color.clear;
            hitArea.raycastTarget = true;

            Transform oldFill = button.transform.Find("FocusFill");
            if (oldFill != null) UnityEngine.Object.DestroyImmediate(oldFill.gameObject);
            var fillGo = new GameObject("FocusFill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(button.transform, false);
            fillGo.transform.SetAsFirstSibling();
            var fillRect = (RectTransform)fillGo.transform;
            Stretch(fillRect);
            fillRect.offsetMin = new Vector2(-12f, -6f);
            fillRect.offsetMax = new Vector2(20f, 6f);
            Image fillImg = fillGo.GetComponent<Image>();
            fillImg.sprite = sprites.MistHighlight;
            fillImg.color = isCautionAction
                ? new Color(0.52f, 0.16f, 0.16f, 0f)
                : new Color(0.62f, 0.48f, 0.30f, 0f);
            fillImg.raycastTarget = false;
            fillImg.enabled = false;

            Transform captionTr = button.transform.Find("Caption");
            Text caption = captionTr != null ? captionTr.GetComponent<Text>() : button.GetComponentInChildren<Text>(true);
            if (caption != null)
            {
                caption.gameObject.name = "Caption";
                StyleText(caption, font, fontSize, Color.white, TextAnchor.MiddleLeft, true);
                caption.text = captionText;
                Stretch(caption.rectTransform);
                caption.rectTransform.offsetMin = new Vector2(20f, 0f);
                caption.rectTransform.offsetMax = new Vector2(-16f, 0f);
                button.targetGraphic = caption;
            }

            var colors = button.colors;
            colors.normalColor = isCautionAction ? new Color32(168, 176, 180, 255) : new Color32(178, 196, 202, 255);
            colors.highlightedColor = isCautionAction ? QuietCrimsonHover : HoverIvory;
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = IvoryInk;
            colors.disabledColor = DisabledMist;
            colors.fadeDuration = 0.15f;
            button.colors = colors;

            Transform oldMarker = button.transform.Find("FocusMarker");
            if (oldMarker != null) UnityEngine.Object.DestroyImmediate(oldMarker.gameObject);
            var markerGo = new GameObject("FocusMarker", typeof(RectTransform), typeof(Image));
            markerGo.transform.SetParent(button.transform, false);
            var mr = (RectTransform)markerGo.transform;
            mr.anchorMin = mr.anchorMax = new Vector2(0f, 0.5f);
            mr.pivot = new Vector2(0f, 0.5f);
            mr.anchoredPosition = new Vector2(0f, 0f);
            mr.sizeDelta = new Vector2(18f, 2f);
            Image markerImg = markerGo.GetComponent<Image>();
            markerImg.color = isCautionAction ? new Color32(208, 114, 106, 255) : DoorwayAmber;
            markerImg.raycastTarget = false;
            markerImg.enabled = false;

            var focus = button.GetComponent<MenuItemFocus>();
            if (focus == null) focus = button.gameObject.AddComponent<MenuItemFocus>();
            var so = new SerializedObject(focus);
            so.FindProperty("marker").objectReferenceValue = markerImg;
            so.FindProperty("highlightFill").objectReferenceValue = fillImg;
            so.FindProperty("captionRect").objectReferenceValue = caption != null ? caption.rectTransform : null;
            so.FindProperty("hoverShiftX").floatValue = 12f;
            so.FindProperty("markerExpandedWidth").floatValue = 20f;
            so.FindProperty("fillTargetAlpha").floatValue = isCautionAction ? 0.36f : 0.32f;
            so.FindProperty("playAudio").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Button StyleOrganicQualityTab(
            Button button,
            string captionText,
            Font font,
            UiSprites sprites,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 pos,
            Vector2 size)
        {
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var oldFocus = button.GetComponent<MenuItemFocus>();
            if (oldFocus != null) UnityEngine.Object.DestroyImmediate(oldFocus);
            Transform oldMarker = button.transform.Find("FocusMarker");
            if (oldMarker != null) UnityEngine.Object.DestroyImmediate(oldMarker.gameObject);
            Transform oldFill = button.transform.Find("FocusFill");
            if (oldFill != null) UnityEngine.Object.DestroyImmediate(oldFill.gameObject);
            Transform oldBorder = button.transform.Find("Border");
            if (oldBorder != null) UnityEngine.Object.DestroyImmediate(oldBorder.gameObject);

            // Soft feathered mist wash instead of a hard rectangular box!
            Image bg = button.GetComponent<Image>();
            bg.sprite = sprites.MistHighlight;
            bg.type = Image.Type.Simple;
            bg.color = captionText == "Tinggi"
                ? new Color(0.70f, 0.55f, 0.34f, 0.24f)
                : new Color(0f, 0f, 0f, 0f);
            bg.raycastTarget = true;
            button.targetGraphic = bg;

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.25f, 1.22f, 1.18f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.92f, 0.90f, 0.86f, 1f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
            colors.fadeDuration = 0.12f;
            button.colors = colors;

            // Delicate glowing bottom mist-underline when active (no closed box!)
            Image activeBar = UpsertImage(button.transform, "ActiveBar", sprites.SoftRule, DoorwayAmber);
            activeBar.rectTransform.anchorMin = new Vector2(0.16f, 0f);
            activeBar.rectTransform.anchorMax = new Vector2(0.84f, 0f);
            activeBar.rectTransform.pivot = new Vector2(0.5f, 0f);
            activeBar.rectTransform.anchoredPosition = new Vector2(0f, 3f);
            activeBar.rectTransform.sizeDelta = new Vector2(0f, 2f);
            activeBar.raycastTarget = false;
            activeBar.gameObject.SetActive(captionText == "Tinggi");

            Text caption = button.GetComponentInChildren<Text>(true);
            if (caption != null)
            {
                StyleText(caption, font, 22, SoftPaper, TextAnchor.MiddleCenter, true);
                caption.text = captionText;
                Stretch(caption.rectTransform);
                caption.rectTransform.offsetMin = Vector2.zero;
                caption.rectTransform.offsetMax = Vector2.zero;
            }

            button.interactable = true;
            return button;
        }

        private static void StyleSlider(Slider slider, UiSprites sprites)
        {
            if (slider == null) return;
            Transform root = slider.transform;

            Transform track = root.Find("Track");
            if (track == null) track = root.Find("Background");
            if (track == null)
            {
                track = new GameObject("Track", typeof(RectTransform), typeof(Image)).transform;
                track.SetParent(root, false);
            }
            var trackRect = (RectTransform)track;
            trackRect.anchorMin = new Vector2(0f, 0.5f);
            trackRect.anchorMax = new Vector2(1f, 0.5f);
            trackRect.pivot = new Vector2(0.5f, 0.5f);
            trackRect.offsetMin = new Vector2(0f, -1.5f);
            trackRect.offsetMax = new Vector2(0f, 1.5f);
            Image trackImg = track.GetComponent<Image>();
            if (trackImg == null) trackImg = track.gameObject.AddComponent<Image>();
            trackImg.sprite = null;
            trackImg.color = SliderTrackCol;
            trackImg.raycastTarget = false;
            track.SetAsFirstSibling();

            Transform fillArea = root.Find("FillArea");
            if (fillArea == null)
            {
                fillArea = new GameObject("FillArea", typeof(RectTransform)).transform;
                fillArea.SetParent(root, false);
            }
            var fillAreaRect = (RectTransform)fillArea;
            fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
            fillAreaRect.pivot = new Vector2(0.5f, 0.5f);
            fillAreaRect.offsetMin = new Vector2(0f, -1.75f);
            fillAreaRect.offsetMax = new Vector2(0f, 1.75f);
            fillArea.SetSiblingIndex(1);

            Transform fill = fillArea.Find("Fill");
            if (fill == null)
            {
                fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).transform;
                fill.SetParent(fillArea, false);
            }
            var fillRect = (RectTransform)fill;
            float norm = Mathf.InverseLerp(slider.minValue, slider.maxValue, slider.value);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(Mathf.Clamp01(norm), 1f);
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            Image fillImg = fill.GetComponent<Image>();
            if (fillImg == null) fillImg = fill.gameObject.AddComponent<Image>();
            fillImg.sprite = null;
            fillImg.color = SliderFillCol;
            fillImg.raycastTarget = false;
            slider.fillRect = fillRect;

            Transform handleArea = root.Find("HandleArea");
            if (handleArea == null) handleArea = root.Find("Handle Slide Area");
            if (handleArea == null)
            {
                handleArea = new GameObject("HandleArea", typeof(RectTransform)).transform;
                handleArea.SetParent(root, false);
            }
            var handleAreaRect = (RectTransform)handleArea;
            handleAreaRect.anchorMin = new Vector2(0f, 0.5f);
            handleAreaRect.anchorMax = new Vector2(1f, 0.5f);
            handleAreaRect.pivot = new Vector2(0.5f, 0.5f);
            handleAreaRect.offsetMin = new Vector2(11f, -11f);
            handleAreaRect.offsetMax = new Vector2(-11f, 11f);
            handleArea.SetAsLastSibling();

            Transform handle = handleArea.Find("Handle");
            if (handle == null && slider.handleRect != null) handle = slider.handleRect;
            if (handle == null)
            {
                handle = new GameObject("Handle", typeof(RectTransform), typeof(Image)).transform;
                handle.SetParent(handleArea, false);
            }
            var handleRect = (RectTransform)handle;
            handleRect.anchorMin = new Vector2(Mathf.Clamp01(norm), 0f);
            handleRect.anchorMax = new Vector2(Mathf.Clamp01(norm), 1f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new Vector2(22f, 0f);
            Image handleImg = handle.GetComponent<Image>();
            if (handleImg == null) handleImg = handle.gameObject.AddComponent<Image>();
            handleImg.sprite = sprites.SliderKnob;
            handleImg.color = IvoryInk;
            handleImg.raycastTarget = true;

            slider.handleRect = handleRect;
            slider.targetGraphic = handleImg;
            var colors = slider.colors;
            colors.normalColor = IvoryInk;
            colors.highlightedColor = DoorwayAmber;
            colors.selectedColor = DoorwayAmber;
            colors.pressedColor = Color.white;
            colors.fadeDuration = 0.12f;
            slider.colors = colors;
        }

        private static void StyleOrganicToggle(Toggle toggle, UiSprites sprites, Vector2 pos)
        {
            if (toggle == null) return;
            var rect = (RectTransform)toggle.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(26f, 26f);

            Transform oldBorder = toggle.transform.Find("Border");
            if (oldBorder != null) UnityEngine.Object.DestroyImmediate(oldBorder.gameObject);

            // Reference-2 crisp thin square hairline checkbox
            Image bg = toggle.GetComponent<Image>();
            bg.sprite = sprites.CheckboxBox;
            bg.type = Image.Type.Simple;
            bg.color = new Color(0.82f, 0.88f, 0.90f, 0.85f);

            Transform markTr = toggle.transform.Find("Checkmark");
            if (markTr != null)
            {
                var mr = (RectTransform)markTr;
                Stretch(mr);
                mr.offsetMin = new Vector2(4f, 4f);
                mr.offsetMax = new Vector2(-4f, -4f);
                Image markImg = markTr.GetComponent<Image>();
                markImg.sprite = sprites.Checkmark;
                markImg.color = DoorwayAmber;
                markImg.raycastTarget = false;
                toggle.graphic = markImg;
            }
            toggle.targetGraphic = bg;
        }

        private static Slider UpsertSliderFromTemplate(Transform parent, GameObject template, string name, float min, float max, float val)
        {
            Transform existing = parent.Find(name);
            if (existing == null)
            {
                GameObject clone = UnityEngine.Object.Instantiate(template, parent, false);
                clone.name = name;
                existing = clone.transform;
            }
            var slider = existing.GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = val;
            return slider;
        }

        private static UiSprites EnsureUiSprites()
        {
            Directory.CreateDirectory(SpriteDir);
            return new UiSprites
            {
                MistHighlight = SaveProceduralSprite("UI_MistHighlight.png", BuildOrganicMistHighlightTex(256, 64), Vector4.zero),
                SoftRule = SaveProceduralSprite("UI_SoftRule.png", BuildSoftRuleTex(256, 8), Vector4.zero),
                VerticalFadeRule = SaveProceduralSprite("UI_VerticalFadeRule.png", BuildVerticalFadeRuleTex(8, 256), Vector4.zero),
                SliderKnob = SaveProceduralSprite("UI_SliderKnob.png", BuildRaindropOrbKnobTex(48), Vector4.zero),
                ToggleRing = SaveProceduralSprite("UI_ToggleRing.png", BuildSoftRingTex(48), Vector4.zero),
                Checkmark = SaveProceduralSprite("UI_Checkmark.png", BuildCheckmarkTex(48), Vector4.zero),
                CheckboxBox = SaveProceduralSprite("UI_CheckboxBox.png", BuildCheckboxBoxTex(48), Vector4.zero)
            };
        }

        private static Sprite SaveProceduralSprite(string fileName, Texture2D tex, Vector4 border)
        {
            string path = SpriteDir + fileName;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // Organic smoky mist highlight feathered on all 4 edges (never a hard rectangle!)
        private static Texture2D BuildOrganicMistHighlightTex(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                float v = (float)y / (height - 1);
                float verticalBell = Mathf.Pow(Mathf.Sin(v * Mathf.PI), 1.45f);
                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / (width - 1);
                    float leftFade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / 0.14f));
                    float rightFade = Mathf.Pow(1f - u, 1.75f);
                    float wisp = 0.84f + 0.16f * Mathf.Sin(u * 14f + v * 5.2f) * Mathf.Cos(u * 7.3f - v * 9.1f);
                    float alpha = Mathf.Clamp01(leftFade * rightFade * verticalBell * wisp);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D BuildSoftRuleTex(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                float v = (float)y / (height - 1);
                float vy = Mathf.Sin(v * Mathf.PI);
                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / (width - 1);
                    float alpha = Mathf.Pow(1f - u, 1.75f) * vy;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D BuildVerticalFadeRuleTex(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                float v = (float)y / (height - 1);
                float vy = Mathf.Pow(Mathf.Sin(v * Mathf.PI), 1.35f);
                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / (width - 1);
                    float vx = Mathf.Sin(u * Mathf.PI);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, vx * vy));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D BuildRaindropOrbKnobTex(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            float outerGlowR = half - 1.0f;
            float coreR = half * 0.62f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) - half;
                    float dy = (y + 0.5f) - half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float core = Mathf.Clamp01(coreR - d + 0.8f);
                    float halo = Mathf.Pow(Mathf.Clamp01(1f - d / outerGlowR), 2.4f) * 0.42f;
                    float alpha = Mathf.Clamp01(Mathf.Max(core, halo));
                    float shade = Mathf.Lerp(0.86f, 1f, core);
                    tex.SetPixel(x, y, new Color(shade, shade, shade, alpha));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D BuildSoftRingTex(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            float ringR = half - 4.5f;
            float halfThick = 1.6f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) - half;
                    float dy = (y + 0.5f) - half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float ring = Mathf.Clamp01(halfThick - Mathf.Abs(d - ringR) + 0.85f);
                    float innerTint = Mathf.Clamp01(ringR - d) * 0.14f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(Mathf.Max(ring, innerTint))));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D BuildCheckmarkTex(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 p0 = new Vector2(0.22f, 0.50f) * size;
            Vector2 p1 = new Vector2(0.43f, 0.28f) * size;
            Vector2 p2 = new Vector2(0.80f, 0.74f) * size;
            float thickness = size * 0.082f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = Mathf.Min(DistToSegment(p, p0, p1), DistToSegment(p, p1, p2));
                    float alpha = Mathf.Clamp01(thickness - d + 0.8f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D BuildCheckboxBoxTex(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            int pad = 4;
            int innerMin = pad;
            int innerMax = size - 1 - pad;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (x < innerMin || x > innerMax || y < innerMin || y > innerMax)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }
                    int edgeDist = Mathf.Min(Mathf.Min(x - innerMin, innerMax - x), Mathf.Min(y - innerMin, innerMax - y));
                    if (edgeDist == 0)
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.90f));
                    else if (edgeDist == 1)
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.45f));
                    else
                        tex.SetPixel(x, y, new Color(0.04f, 0.07f, 0.09f, 0.36f));
                }
            }
            tex.Apply();
            return tex;
        }

        private static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, Vector2.Dot(ab, ab)));
            return Vector2.Distance(p, a + ab * t);
        }

        private static Material EnsureMaterial(string path, string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
                throw new InvalidOperationException("Shader not found: " + shaderName);

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static RawImage CreateRawImage(string name, Transform parent)
        {
            var raw = new GameObject(name, typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            raw.transform.SetParent(parent, false);
            Stretch(raw.rectTransform);
            raw.raycastTarget = false;
            return raw;
        }

        private static Image UpsertImage(Transform parent, string name, Sprite sprite, Color color)
        {
            Transform tr = parent.Find(name);
            GameObject go = tr != null ? tr.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image));
            if (tr == null) go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            if (img == null) img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            return img;
        }

        private static Text UpsertText(Transform parent, string name, Font font, int fontSize, Color color, TextAnchor align, bool addShadow = false)
        {
            Transform tr = parent.Find(name);
            GameObject go = tr != null ? tr.gameObject : new GameObject(name, typeof(RectTransform), typeof(Text));
            if (tr == null) go.transform.SetParent(parent, false);
            var txt = go.GetComponent<Text>();
            if (txt == null) txt = go.AddComponent<Text>();
            StyleText(txt, font, fontSize, color, align, addShadow);
            return txt;
        }

        private static void StyleText(Text text, Font font, int size, Color color, TextAnchor align, bool addShadow = false)
        {
            text.font = font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Normal;
            text.color = color;
            text.alignment = align;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            if (addShadow)
            {
                var shadow = text.GetComponent<Shadow>();
                if (shadow == null) shadow = text.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0.012f, 0.024f, 0.86f);
                shadow.effectDistance = new Vector2(1.2f, -1.6f);
            }
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void PlaceMenuLeft(RectTransform rect, float y, Vector2 size, float xOffset = 0f)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.075f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(xOffset, y);
        }

        private static void PlaceCenter(RectTransform rect, Vector2 pos, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;
        }

        private static void PlaceCenterLeft(RectTransform rect, Vector2 pos, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;
        }

        private static GameObject FindRoot(Scene scene, string rootName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == rootName) return root;
            return null;
        }

        private static void AssignObjectRef(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(target);
            }
        }
    }
}
