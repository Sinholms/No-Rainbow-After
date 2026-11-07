using System;
using System.Collections;
using System.IO;
using System.Linq;
using NoRainbowAfter.AI;
using NoRainbowAfter.Interaction;
using NoRainbowAfter.Level;
using NoRainbowAfter.Narrative;
using NoRainbowAfter.Player;
using NoRainbowAfter.Quality;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace NoRainbowAfter.Tests
{
    public sealed class VisualQualitySliceTests
    {
        private const string MasterGameplayPath = "Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity";
        private const string MainMenuPath = "Assets/_NoRainbowAfter/Scenes/MainMenu.unity";

        private static readonly string[] PrefKeysToRestore =
        {
            GraphicsQualityService.PreferenceKey,
            "NRA.MouseSensitivity",
            "NRA.FieldOfView",
            "NRA.CircularViewEnabled",
            "NRA.CircularViewRadius"
        };

        private bool[] hadKey;
        private int[] intVals;
        private float[] floatVals;
        private string previousClueOverride;
        private string tempFolder;
        private int initialQualityLevel;

        [SetUp]
        public void SetUpIsolation()
        {
            Time.timeScale = 1f;
            initialQualityLevel = QualitySettings.GetQualityLevel();

            hadKey = new bool[PrefKeysToRestore.Length];
            intVals = new int[PrefKeysToRestore.Length];
            floatVals = new float[PrefKeysToRestore.Length];
            for (int i = 0; i < PrefKeysToRestore.Length; i++)
            {
                string key = PrefKeysToRestore[i];
                hadKey[i] = PlayerPrefs.HasKey(key);
                if (key == GraphicsQualityService.PreferenceKey || key == "NRA.CircularViewEnabled")
                    intVals[i] = PlayerPrefs.GetInt(key, 0);
                else
                    floatVals[i] = PlayerPrefs.GetFloat(key, 0f);
            }

            previousClueOverride = ChapterDirector.StoragePathOverride;
            tempFolder = Path.Combine(Path.GetTempPath(), "NRA-T07-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempFolder);
            ChapterDirector.StoragePathOverride = Path.Combine(tempFolder, "clues.json");
        }

        [TearDown]
        public void TearDownIsolation()
        {
            Time.timeScale = 1f;
            ChapterDirector.StoragePathOverride = previousClueOverride;

            if (!string.IsNullOrEmpty(tempFolder) && Directory.Exists(tempFolder))
            {
                foreach (string file in Directory.GetFiles(tempFolder))
                    File.Delete(file);
                Directory.Delete(tempFolder, true);
            }

            for (int i = 0; i < PrefKeysToRestore.Length; i++)
            {
                string key = PrefKeysToRestore[i];
                if (hadKey[i])
                {
                    if (key == GraphicsQualityService.PreferenceKey || key == "NRA.CircularViewEnabled")
                        PlayerPrefs.SetInt(key, intVals[i]);
                    else
                        PlayerPrefs.SetFloat(key, floatVals[i]);
                }
                else
                {
                    PlayerPrefs.DeleteKey(key);
                }
            }
            PlayerPrefs.Save();

            if (initialQualityLevel >= 0 && initialQualityLevel < QualitySettings.names.Length)
                QualitySettings.SetQualityLevel(initialQualityLevel, applyExpensiveChanges: true);
        }

        [Test]
        public void QualitySettingsDefineThreeDistinctPresets_LowMediumHigh()
        {
            string[] names = QualitySettings.names;
            Assert.That(names, Is.EqualTo(new[] { "Low", "Medium", "High" }));

            GraphicsQualityService.ApplyPreset(QualityPreset.Low, savePreference: false);
            var lowAsset = GraphicsQualityService.GetActiveUrpAsset();
            int lowMipLimit = QualitySettings.globalTextureMipmapLimit;
            bool lowProbes = QualitySettings.realtimeReflectionProbes;

            GraphicsQualityService.ApplyPreset(QualityPreset.Medium, savePreference: false);
            var medAsset = GraphicsQualityService.GetActiveUrpAsset();
            int medMipLimit = QualitySettings.globalTextureMipmapLimit;
            bool medProbes = QualitySettings.realtimeReflectionProbes;

            GraphicsQualityService.ApplyPreset(QualityPreset.High, savePreference: false);
            var highAsset = GraphicsQualityService.GetActiveUrpAsset();
            int highMipLimit = QualitySettings.globalTextureMipmapLimit;
            bool highProbes = QualitySettings.realtimeReflectionProbes;

            Assert.That(lowAsset, Is.Not.Null);
            Assert.That(medAsset, Is.Not.Null);
            Assert.That(highAsset, Is.Not.Null);
            Assert.That(lowAsset, Is.Not.SameAs(medAsset));
            Assert.That(medAsset, Is.Not.SameAs(highAsset));

            Assert.That(lowAsset.renderScale, Is.LessThan(medAsset.renderScale));
            Assert.That(medAsset.renderScale, Is.EqualTo(1.0f).Within(0.01f));
            Assert.That(highAsset.renderScale, Is.EqualTo(1.0f).Within(0.01f));

            Assert.That(lowAsset.shadowDistance, Is.LessThan(medAsset.shadowDistance));
            Assert.That(medAsset.shadowDistance, Is.LessThan(highAsset.shadowDistance));
            Assert.That(lowAsset.shadowCascadeCount, Is.LessThan(medAsset.shadowCascadeCount));
            Assert.That(medAsset.shadowCascadeCount, Is.LessThan(highAsset.shadowCascadeCount));
            Assert.That(lowAsset.msaaSampleCount, Is.LessThan(medAsset.msaaSampleCount));
            Assert.That(medAsset.msaaSampleCount, Is.LessThan(highAsset.msaaSampleCount));

            Assert.That(lowAsset.supportsAdditionalLightShadows, Is.False);
            Assert.That(medAsset.supportsAdditionalLightShadows, Is.True);
            Assert.That(highAsset.supportsAdditionalLightShadows, Is.True);

            Assert.That(lowMipLimit, Is.EqualTo(1));
            Assert.That(medMipLimit, Is.EqualTo(0));
            Assert.That(highMipLimit, Is.EqualTo(0));

            Assert.That(lowProbes, Is.False);
            Assert.That(medProbes, Is.True);
            Assert.That(highProbes, Is.True);
        }

        [UnityTest]
        public IEnumerator MasterGameplayAtmosphereScalesCorrectlyAcrossPresets()
        {
            GraphicsQualityService.ApplyPreset(QualityPreset.High, savePreference: true);
            yield return SceneManager.LoadSceneAsync(MasterGameplayPath, LoadSceneMode.Single);
            yield return null;

            var controller = Object.FindAnyObjectByType<QualityAtmosphereController>();
            Assert.That(controller, Is.Not.Null, "QualityAtmosphereController must exist in MasterGameplay.");

            var viewLight = GameObject.Find("Player View Light").GetComponent<Light>();
            var viewCamData = GameObject.Find("ViewCamera").GetComponent<UniversalAdditionalCameraData>();
            var postVol = GameObject.Find("Room Post Processing").GetComponent<Volume>();
            var corridorProbe = GameObject.Find("Corridor Reflection Probe").GetComponent<ReflectionProbe>();
            VolumeProfile diskSharedProfile = postVol.sharedProfile;

            GraphicsQualityService.ApplyPreset(QualityPreset.Low, savePreference: false);
            yield return null;
            Assert.That(controller.AppliedPreset, Is.EqualTo(QualityPreset.Low));
            Assert.That(controller.ActiveSecondaryParticleSystemCount, Is.EqualTo(0), "Secondary dust particles must turn off on Low.");
            Assert.That(controller.ActivePrimaryDripSystemCount, Is.EqualTo(0), "Ceiling drip particles must turn off on Low.");
            Assert.That(viewLight.shadows, Is.EqualTo(LightShadows.None));
            Assert.That(viewCamData.antialiasing, Is.EqualTo(AntialiasingMode.None));
            Assert.That(corridorProbe.enabled, Is.False);

            GraphicsQualityService.ApplyPreset(QualityPreset.Medium, savePreference: false);
            yield return null;
            Assert.That(controller.AppliedPreset, Is.EqualTo(QualityPreset.Medium));
            Assert.That(controller.ActivePrimaryDripSystemCount, Is.EqualTo(2));
            Assert.That(controller.ActiveSecondaryParticleSystemCount, Is.EqualTo(1));
            Assert.That(viewLight.shadows, Is.EqualTo(LightShadows.Soft));
            Assert.That(viewCamData.antialiasing, Is.EqualTo(AntialiasingMode.FastApproximateAntialiasing));
            Assert.That(corridorProbe.enabled, Is.True);
            Assert.That(corridorProbe.boxProjection, Is.True);

            GraphicsQualityService.ApplyPreset(QualityPreset.High, savePreference: false);
            yield return null;
            Assert.That(controller.AppliedPreset, Is.EqualTo(QualityPreset.High));
            Assert.That(controller.ActivePrimaryDripSystemCount, Is.EqualTo(2));
            Assert.That(controller.ActiveSecondaryParticleSystemCount, Is.EqualTo(2));
            Assert.That(viewLight.shadows, Is.EqualTo(LightShadows.Soft));
            Assert.That(viewCamData.antialiasing, Is.EqualTo(AntialiasingMode.SubpixelMorphologicalAntiAliasing));
            Assert.That(corridorProbe.enabled, Is.True);
            Assert.That(corridorProbe.boxProjection, Is.True);
            Assert.That(postVol.sharedProfile, Is.SameAs(diskSharedProfile), "Shared VolumeProfile on disk must remain intact.");
        }

        [UnityTest]
        public IEnumerator PauseMenuQualityButtonsSwitchAndPersistWithoutBreakingViewSettings()
        {
            PlayerPrefs.SetFloat("NRA.MouseSensitivity", 0.18f);
            PlayerPrefs.SetFloat("NRA.FieldOfView", 79f);
            PlayerPrefs.SetInt("NRA.CircularViewEnabled", 1);
            PlayerPrefs.SetFloat("NRA.CircularViewRadius", 0.61f);
            GraphicsQualityService.ApplyPreset(QualityPreset.High, savePreference: true);

            yield return SceneManager.LoadSceneAsync(MasterGameplayPath, LoadSceneMode.Single);
            yield return null;

            GameObject ui = GameObject.Find("UI_FirstPerson");
            Assert.That(ui, Is.Not.Null);
            Type sessionType = Type.GetType("NoRainbowAfter.UI.FirstPersonSession, Assembly-CSharp");
            Assert.That(sessionType, Is.Not.Null);
            object session = ui.GetComponent(sessionType);
            Assert.That(session, Is.Not.Null);

            sessionType.GetMethod("SetPaused").Invoke(session, new object[] { true });
            Transform pausePanel = ui.transform.Find("PausePanel");
            Assert.That(pausePanel.gameObject.activeSelf, Is.True);

            var lowBtn = pausePanel.Find("QualityLowButton").GetComponent<Button>();
            var medBtn = pausePanel.Find("QualityMediumButton").GetComponent<Button>();
            var highBtn = pausePanel.Find("QualityHighButton").GetComponent<Button>();
            var label = pausePanel.Find("QualityPresetLabel").GetComponent<Text>();

            lowBtn.onClick.Invoke();
            Assert.That(GraphicsQualityService.CurrentPreset, Is.EqualTo(QualityPreset.Low));
            Assert.That(QualitySettings.GetQualityLevel(), Is.EqualTo(0));
            Assert.That(label.text, Does.Contain("Rendah"));
            Assert.That(lowBtn.GetComponentInChildren<Text>().fontStyle, Is.EqualTo(FontStyle.Bold));
            Assert.That(medBtn.GetComponentInChildren<Text>().fontStyle, Is.EqualTo(FontStyle.Normal));

            medBtn.onClick.Invoke();
            Assert.That(GraphicsQualityService.CurrentPreset, Is.EqualTo(QualityPreset.Medium));
            Assert.That(QualitySettings.GetQualityLevel(), Is.EqualTo(1));
            Assert.That(label.text, Does.Contain("Sedang"));

            sessionType.GetMethod("SetPaused").Invoke(session, new object[] { false });

            // Reload MasterGameplay and confirm preset + existing FPP settings all persisted
            yield return SceneManager.LoadSceneAsync(MasterGameplayPath, LoadSceneMode.Single);
            yield return null;

            Assert.That(GraphicsQualityService.CurrentPreset, Is.EqualTo(QualityPreset.Medium));
            Assert.That(QualitySettings.GetQualityLevel(), Is.EqualTo(1));
            var motor = Object.FindAnyObjectByType<FirstPersonMotor>();
            Assert.That(motor.MouseSensitivity, Is.EqualTo(0.18f).Within(0.005f));
            Assert.That(motor.FieldOfView, Is.EqualTo(79f).Within(0.1f));
            Assert.That(PlayerPrefs.GetInt("NRA.CircularViewEnabled"), Is.EqualTo(1));
            Assert.That(PlayerPrefs.GetFloat("NRA.CircularViewRadius"), Is.EqualTo(0.61f).Within(0.005f));

            // Switch back to High via button
            ui = GameObject.Find("UI_FirstPerson");
            pausePanel = ui.transform.Find("PausePanel");
            highBtn = pausePanel.Find("QualityHighButton").GetComponent<Button>();
            highBtn.onClick.Invoke();
            Assert.That(GraphicsQualityService.CurrentPreset, Is.EqualTo(QualityPreset.High));
        }

        [UnityTest]
        public IEnumerator MainMenuSettingsQualitySelectorPersistsIntoGameplay()
        {
            GraphicsQualityService.ApplyPreset(QualityPreset.High, savePreference: true);
            yield return SceneManager.LoadSceneAsync(MainMenuPath, LoadSceneMode.Single);
            yield return null;

            GameObject uiRoot = GameObject.Find("UI_Root");
            Assert.That(uiRoot, Is.Not.Null);
            var settingsButton = uiRoot.transform.Find("ScreenContent/SettingsButton").GetComponent<Button>();
            settingsButton.onClick.Invoke();

            Transform settingsPanel = uiRoot.transform.Find("SettingsPanel");
            Assert.That(settingsPanel.gameObject.activeSelf, Is.True);

            var lowBtn = settingsPanel.Find("QualityLowButton").GetComponent<Button>();
            var presetLabel = settingsPanel.Find("QualityPresetLabel").GetComponent<Text>();
            lowBtn.onClick.Invoke();

            Assert.That(GraphicsQualityService.CurrentPreset, Is.EqualTo(QualityPreset.Low));
            Assert.That(presetLabel.text, Does.Contain("Rendah"));

            yield return SceneManager.LoadSceneAsync(MasterGameplayPath, LoadSceneMode.Single);
            yield return null;

            Assert.That(GraphicsQualityService.CurrentPreset, Is.EqualTo(QualityPreset.Low));
            Assert.That(QualitySettings.GetQualityLevel(), Is.EqualTo(0));
            var controller = Object.FindAnyObjectByType<QualityAtmosphereController>();
            Assert.That(controller.AppliedPreset, Is.EqualTo(QualityPreset.Low));
        }

        [UnityTest]
        public IEnumerator SceneIntegrityAndDoorwayClearanceHoldAcrossAllPresets()
        {
            yield return SceneManager.LoadSceneAsync(MasterGameplayPath, LoadSceneMode.Single);
            yield return null;

            QualityPreset[] presets = { QualityPreset.Low, QualityPreset.Medium, QualityPreset.High };
            foreach (QualityPreset preset in presets)
            {
                GraphicsQualityService.ApplyPreset(preset, savePreference: false);
                yield return null;

                Camera[] activeCams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude);
                Assert.That(activeCams.Length, Is.EqualTo(1), "Must maintain exactly 1 active Camera on " + preset);

                GameObject env = GameObject.Find("ENV_KosAtmosphere");
                GameObject shadow = GameObject.Find("ACT_001_ShadowPlaceholder");
                Assert.That(env, Is.Not.Null);
                Assert.That(shadow, Is.Not.Null);
                Assert.That(env.transform.parent.name, Is.EqualTo("Chapter1Root"));
                Assert.That(shadow.transform.parent.name, Is.EqualTo("Chapter1Root"));

                // Verify all 9 LoopAtmosphere moving paper meshes remain non-static
                Transform[] envTransforms = env.GetComponentsInChildren<Transform>(true);
                Transform[] movingLoopMeshes = envTransforms.Where(t =>
                    t.name == "SM_PROP_OpenNotes" ||
                    t.name == "SM_PROP_NotesHeading" ||
                    t.name.StartsWith("SM_PROP_NotesLine", StringComparison.Ordinal) ||
                    t.name == "SM_PROP_WorkSlip" ||
                    t.name == "SM_PROP_WorkSlipHeading").ToArray();
                Assert.That(movingLoopMeshes.Length, Is.EqualTo(9));
                foreach (Transform m in movingLoopMeshes)
                    Assert.That(m.gameObject.isStatic, Is.False, "Moving LoopAtmosphere mesh must remain non-static: " + m.name);

                // Verify all T07 polish & decal meshes have zero colliders so monster stoop spherecasts & player routes are untouched
                foreach (Transform t in envTransforms)
                {
                    if (t.name.StartsWith("SM_T07_", StringComparison.Ordinal) ||
                        t.name.StartsWith("SM_DEC_", StringComparison.Ordinal))
                    {
                        Assert.That(t.GetComponent<Collider>(), Is.Null, "T07 visual detail/decal must not add colliders: " + t.name);
                    }
                }

                // Verify both cover niches remain intact
                CoverZone[] covers = Object.FindObjectsByType<CoverZone>(FindObjectsInactive.Exclude);
                Assert.That(covers.Select(c => c.StableId), Is.EquivalentTo(new[] { "C1_COVER_LEFT", "C1_COVER_RIGHT" }));
            }

            // Verify physical doorway traversal (0.66m gap, 2.05m lintel) still passes cleanly
            var player = Object.FindAnyObjectByType<FirstPersonPlayer>();
            var motor = player.GetComponent<FirstPersonMotor>();
            var cc = motor.GetComponent<CharacterController>();
            player.enabled = false;
            cc.enabled = false;
            motor.transform.SetPositionAndRotation(new Vector3(0.15f, 0.04f, 1.10f), Quaternion.identity);
            cc.enabled = true;
            motor.StopMotion();
            Physics.SyncTransforms();
            for (int i = 0; i < 320; i++)
                motor.Simulate(new FirstPersonFrame { Move = Vector2.up }, 0.02f);
            Assert.That(motor.transform.position.z, Is.GreaterThan(8.5f), "Player must traverse open kos doorway into corridor cleanly.");
        }

        [UnityTest]
        public IEnumerator InteractablesAndMonsterSilhouetteRemainReadableAcrossPresets()
        {
            PlayerPrefs.SetInt("NRA.CircularViewEnabled", 1);
            yield return SceneManager.LoadSceneAsync(MasterGameplayPath, LoadSceneMode.Single);
            yield return null;

            var player = Object.FindAnyObjectByType<FirstPersonPlayer>();
            player.enabled = false;
            Camera cam = GameObject.Find("ViewCamera").GetComponent<Camera>();
            GameObject shadow = GameObject.Find("ACT_001_ShadowPlaceholder");

            SceneInteractable[] interactables = Object.FindObjectsByType<SceneInteractable>(FindObjectsInactive.Include);
            SceneInteractable book = interactables.FirstOrDefault(i => i.StableId == "INT_BOOK");
            SceneInteractable slip = interactables.FirstOrDefault(i => i.StableId == "INT_OVERTIME");
            SceneInteractable door = interactables.FirstOrDefault(i => i.StableId == "INT_C1_DOOR");
            Assert.That(book, Is.Not.Null);
            Assert.That(slip, Is.Not.Null);
            Assert.That(door, Is.Not.Null);

            Light farLight = GameObject.Find("Corridor Far").GetComponent<Light>();
            Light backLight = GameObject.Find("Corridor End Backlight").GetComponent<Light>();
            Assert.That(farLight.intensity, Is.GreaterThan(0.8f));
            Assert.That(backLight.intensity, Is.GreaterThan(0.7f));

            QualityPreset[] presets = { QualityPreset.Low, QualityPreset.Medium, QualityPreset.High };
            foreach (QualityPreset preset in presets)
            {
                GraphicsQualityService.ApplyPreset(preset, savePreference: false);
                yield return null;

                // 1) Check unobstructed FPP sightline to INT_BOOK & INT_OVERTIME from desk standing position
                AssertUnobstructedTarget(cam, new Vector3(0.65f, 1.62f, 3.35f), book.GetComponent<Collider>(), "INT_BOOK on " + preset);
                AssertUnobstructedTarget(cam, new Vector3(1.15f, 1.62f, 3.35f), slip.GetComponent<Collider>(), "INT_OVERTIME on " + preset);

                // 2) Move shadow aside temporarily to verify unobstructed sightline to INT_C1_DOOR
                shadow.transform.position = new Vector3(0f, 0.02f, 12.0f);
                Physics.SyncTransforms();
                AssertUnobstructedTarget(cam, new Vector3(0f, 1.62f, 17.6f), door.GetComponent<Collider>(), "INT_C1_DOOR on " + preset);

                // 3) Verify monster silhouette inside Circular POV center aperture at 10.6m (8-12m) and 2.4m (2-3m)
                CharacterController shadowCc = shadow.GetComponent<CharacterController>();
                shadowCc.enabled = false;
                shadow.transform.position = new Vector3(0f, 0.02f, 17.50f);
                shadowCc.enabled = true;
                Physics.SyncTransforms();

                AssertMonsterReadableInCircularAperture(cam, new Vector3(0f, 1.66f, 6.90f), shadow.transform.position + Vector3.up * 1.15f, shadowCc, "10.6m silhouette on " + preset);
                AssertMonsterReadableInCircularAperture(cam, new Vector3(0f, 1.66f, 15.10f), shadow.transform.position + Vector3.up * 1.15f, shadowCc, "2.4m silhouette on " + preset);
            }
        }

        [Test]
        public void FrameTimeSamplerComputesAccurateFrameMetrics()
        {
            var sampler = new FrameTimeSampler(256);
            for (int i = 0; i < 99; i++)
                sampler.RecordFrame(0.010f); // 10 ms = 100 FPS
            sampler.RecordFrame(0.025f);     // 25 ms = 40 FPS (worst 1% out of 100 frames)

            FrameTimeSummary summary = sampler.Summarize();
            Assert.That(summary.sampleCount, Is.EqualTo(100));
            Assert.That(summary.averageFrameTimeMs, Is.EqualTo(10.15f).Within(0.02f));
            Assert.That(summary.averageFps, Is.EqualTo(1000f / 10.15f).Within(0.25f));
            Assert.That(summary.maxFrameTimeMs, Is.EqualTo(25.0f).Within(0.01f));
            Assert.That(summary.onePercentLowFps, Is.EqualTo(40.0f).Within(0.2f));
        }

        private static void AssertUnobstructedTarget(Camera cam, Vector3 eyePos, Collider targetCollider, string context)
        {
            Vector3 targetCenter = targetCollider.bounds.center;
            cam.transform.position = eyePos;
            cam.transform.rotation = Quaternion.LookRotation((targetCenter - eyePos).normalized, Vector3.up);
            Physics.SyncTransforms();

            Vector3 dir = targetCenter - eyePos;
            float dist = dir.magnitude;
            Assert.That(Physics.Raycast(eyePos, dir.normalized, out RaycastHit hit, dist + 0.5f, ~0, QueryTriggerInteraction.Ignore),
                Is.True, "Expected raycast hit for " + context);
            Assert.That(hit.collider, Is.SameAs(targetCollider), "Expected unobstructed sightline to " + context + ", but hit " + hit.collider.name);
        }

        private static void AssertMonsterReadableInCircularAperture(
            Camera cam,
            Vector3 eyePos,
            Vector3 monsterChestWorld,
            Collider monsterCollider,
            string context)
        {
            cam.transform.position = eyePos;
            cam.transform.rotation = Quaternion.LookRotation((monsterChestWorld - eyePos).normalized, Vector3.up);
            Physics.SyncTransforms();

            Vector3 vp = cam.WorldToViewportPoint(monsterChestWorld);
            Assert.That(vp.z, Is.GreaterThan(0f), context + " should be in front of camera.");
            float radialDistFromCenter = Vector2.Distance(new Vector2(vp.x, vp.y), new Vector2(0.5f, 0.5f));
            Assert.That(radialDistFromCenter, Is.LessThan(0.15f), context + " must sit cleanly inside the Circular POV aperture.");

            Vector3 dir = monsterChestWorld - eyePos;
            Assert.That(Physics.Raycast(eyePos, dir.normalized, out RaycastHit hit, dir.magnitude + 0.5f, ~0, QueryTriggerInteraction.Ignore),
                Is.True, "Expected unobstructed raycast to monster for " + context);
            Assert.That(hit.collider, Is.SameAs(monsterCollider), "Monster silhouette occluded by " + hit.collider.name + " during " + context);
        }
    }
}
