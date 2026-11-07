using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NoRainbowAfter.AI;
using NoRainbowAfter.Narrative;
using NoRainbowAfter.Player;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace NoRainbowAfter.Quality
{
    [Serializable]
    public sealed class BenchmarkScenarioResult
    {
        public string preset;
        public string scenario;
        public int qualityLevelIndex;
        public string urpAssetName;
        public float renderScale;
        public int msaaSamples;
        public float shadowDistance;
        public int shadowCascades;
        public bool softShadows;
        public bool additionalLightShadows;
        public int activeSecondaryParticles;
        public int activeDripParticles;
        public FrameTimeSummary metrics;
        public string screenshotPath;
    }

    [Serializable]
    public sealed class BenchmarkPresetAggregate
    {
        public string preset;
        public int totalSamples;
        public float averageFrameTimeMs;
        public float averageFps;
        public float onePercentLowFps;
        public float p95FrameTimeMs;
        public float maxFrameTimeMs;
    }

    [Serializable]
    public sealed class BenchmarkReport
    {
        public string timestampUtc;
        public string environment;
        public string unityVersion;
        public string platform;
        public string gpuName;
        public string graphicsApi;
        public int vramMb;
        public string cpuName;
        public int ramMb;
        public int screenWidth;
        public int screenHeight;
        public string fullScreenMode;
        public int vSyncCount;
        public float warmupSecondsPerScenario;
        public float sampleSecondsPerScenario;
        public List<BenchmarkPresetAggregate> presetAggregates = new List<BenchmarkPresetAggregate>();
        public List<BenchmarkScenarioResult> scenarios = new List<BenchmarkScenarioResult>();
    }

    /// <summary>
    /// Non-intrusive benchmark runner for T07.
    /// Completely dormant during normal play; activates automatically only when the Windows standalone
    /// executable is launched with "--nra-benchmark" (or when invoked explicitly for Editor comparison).
    /// Isolates clue persistence to a temporary file and restores PlayerPrefs upon completion.
    /// </summary>
    public sealed class StandaloneBenchmarkRunner : MonoBehaviour
    {
        public const string BenchmarkFlag = "--nra-benchmark";
        private const string MasterGameplayPath = "Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity";

        private static StandaloneBenchmarkRunner instance;

        private string outputJsonPath;
        private string captureDirectory;
        private int targetWidth = 1920;
        private int targetHeight = 1080;
        private float warmupSeconds = 1.0f;
        private float sampleSeconds = 2.5f;
        private bool quitOnComplete = true;

        private bool hadSavedPreset;
        private int savedPresetValue;
        private string tempClueDirectory;
        private string previousClueOverride;

        public bool IsRunning { get; private set; }
        public BenchmarkReport LastReport { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoBootstrapFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (!HasFlag(args, BenchmarkFlag))
                return;

            if (instance != null)
                return;

            var host = new GameObject("NRA_StandaloneBenchmarkRunner");
            DontDestroyOnLoad(host);
            instance = host.AddComponent<StandaloneBenchmarkRunner>();
            instance.ConfigureFromArgs(args);
            instance.BeginBenchmark();
        }

        public static StandaloneBenchmarkRunner CreateForEditorRun(
            string outputJson,
            string screenshotDir,
            float warmupSec = 0.6f,
            float sampleSec = 1.8f)
        {
            if (instance != null)
                Destroy(instance.gameObject);

            var host = new GameObject("NRA_EditorBenchmarkRunner");
            DontDestroyOnLoad(host);
            instance = host.AddComponent<StandaloneBenchmarkRunner>();
            instance.outputJsonPath = outputJson;
            instance.captureDirectory = screenshotDir;
            instance.targetWidth = Screen.width;
            instance.targetHeight = Screen.height;
            instance.warmupSeconds = warmupSec;
            instance.sampleSeconds = sampleSec;
            instance.quitOnComplete = false;
            instance.IsolatePersistence();
            instance.BeginBenchmark();
            return instance;
        }

        private void ConfigureFromArgs(string[] args)
        {
            outputJsonPath = GetArgValue(args, "--nra-output")
                             ?? Path.Combine(Path.GetTempPath(), "NRA_T07_Benchmark", "T07_Standalone_Benchmark.json");
            captureDirectory = GetArgValue(args, "--nra-captures");
            if (int.TryParse(GetArgValue(args, "--nra-width"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int w) && w >= 640)
                targetWidth = w;
            if (int.TryParse(GetArgValue(args, "--nra-height"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int h) && h >= 480)
                targetHeight = h;
            if (float.TryParse(GetArgValue(args, "--nra-warmup"), NumberStyles.Float, CultureInfo.InvariantCulture, out float wu) && wu >= 0.1f)
                warmupSeconds = wu;
            if (float.TryParse(GetArgValue(args, "--nra-seconds"), NumberStyles.Float, CultureInfo.InvariantCulture, out float sec) && sec >= 0.5f)
                sampleSeconds = sec;

            quitOnComplete = !Application.isEditor;
            IsolatePersistence();

            if (!Application.isEditor)
            {
                Screen.SetResolution(targetWidth, targetHeight, FullScreenMode.FullScreenWindow);
            }
        }

        private void IsolatePersistence()
        {
            hadSavedPreset = PlayerPrefs.HasKey(GraphicsQualityService.PreferenceKey);
            savedPresetValue = PlayerPrefs.GetInt(GraphicsQualityService.PreferenceKey, (int)GraphicsQualityService.DefaultPreset);

            previousClueOverride = ChapterDirector.StoragePathOverride;
            tempClueDirectory = Path.Combine(Path.GetTempPath(), "NRA_T07_BenchClues_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempClueDirectory);
            ChapterDirector.StoragePathOverride = Path.Combine(tempClueDirectory, "clues-benchmark.json");
            Application.runInBackground = true;
        }

        private void BeginBenchmark()
        {
            if (IsRunning) return;
            IsRunning = true;
            StartCoroutine(RunBenchmarkRoutine());
        }

        private IEnumerator RunBenchmarkRoutine()
        {
            // Allow boot/initial scene settle before forcing MasterGameplay.
            yield return null;
            yield return null;

            if (SceneManager.GetActiveScene().path != MasterGameplayPath)
            {
                AsyncOperation load = SceneManager.LoadSceneAsync(MasterGameplayPath, LoadSceneMode.Single);
                while (load != null && !load.isDone)
                    yield return null;
            }

            yield return null;
            yield return null;

            if (!Application.isEditor && (Screen.width != targetWidth || Screen.height != targetHeight))
            {
                Screen.SetResolution(targetWidth, targetHeight, FullScreenMode.FullScreenWindow);
                for (int i = 0; i < 6; i++) yield return null;
            }

            Time.timeScale = 1f;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;

            FirstPersonPlayer player = FindAnyObjectByType<FirstPersonPlayer>();
            FirstPersonMotor motor = FindAnyObjectByType<FirstPersonMotor>();
            CharacterController playerCc = motor != null ? motor.GetComponent<CharacterController>() : null;
            ShadowEncounterController shadow = FindAnyObjectByType<ShadowEncounterController>();
            ShadowPlaceholderVisual shadowVisual = shadow != null ? shadow.GetComponent<ShadowPlaceholderVisual>() : null;
            QualityAtmosphereController atmosphere = FindAnyObjectByType<QualityAtmosphereController>();

            // Disable human input & focus-loss pause during deterministic benchmark run.
            if (player != null)
                player.enabled = false;

            if (!string.IsNullOrEmpty(captureDirectory))
                Directory.CreateDirectory(captureDirectory);

            var report = new BenchmarkReport
            {
                timestampUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                environment = Application.isEditor ? "UnityEditorPlayMode" : "WindowsStandalone64",
                unityVersion = Application.unityVersion,
                platform = Application.platform.ToString(),
                gpuName = SystemInfo.graphicsDeviceName,
                graphicsApi = SystemInfo.graphicsDeviceType.ToString(),
                vramMb = SystemInfo.graphicsMemorySize,
                cpuName = SystemInfo.processorType,
                ramMb = SystemInfo.systemMemorySize,
                screenWidth = Screen.width,
                screenHeight = Screen.height,
                fullScreenMode = Screen.fullScreenMode.ToString(),
                vSyncCount = QualitySettings.vSyncCount,
                warmupSecondsPerScenario = warmupSeconds,
                sampleSecondsPerScenario = sampleSeconds
            };

            var sampler = new FrameTimeSampler(32768);
            QualityPreset[] presets = { QualityPreset.Low, QualityPreset.Medium, QualityPreset.High };

            foreach (QualityPreset preset in presets)
            {
                GraphicsQualityService.ApplyPreset(preset, savePreference: false);
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = -1;
                yield return null;
                yield return null;

                var aggregateSampler = new FrameTimeSampler(65536);

                // Scenario 1: Hero Room Desk, Bed Edge & Doorway Vista
                PositionShadow(shadow, shadowVisual, false, new Vector3(0f, 0.02f, 17.5f));
                PositionPlayer(motor, playerCc, new Vector3(0.15f, 0.04f, 1.10f), 16f, 6f);
                yield return WarmupAndSample(sampler, aggregateSampler, null);
                string dormShot = null;
                if (!string.IsNullOrEmpty(captureDirectory))
                {
                    dormShot = Path.Combine(captureDirectory, $"Standalone_{GraphicsQualityService.GetCanonicalName(preset)}_DormDesk_1080p.png");
                    yield return CapturePngEndOfFrame(dormShot);
                }
                report.scenarios.Add(BuildScenarioResult(preset, "DormHeroRoom", atmosphere, sampler.Summarize(), dormShot));

                // Scenario 2: Corridor Wet Reflections & Monster Silhouette at 10.6 m
                PositionShadow(shadow, shadowVisual, true, new Vector3(0f, 0.02f, 16.60f));
                PositionPlayer(motor, playerCc, new Vector3(0.00f, 0.04f, 6.00f), 0f, 1.5f);
                yield return WarmupAndSample(sampler, aggregateSampler, null);
                string corridorShot = null;
                if (!string.IsNullOrEmpty(captureDirectory))
                {
                    corridorShot = Path.Combine(captureDirectory, $"Standalone_{GraphicsQualityService.GetCanonicalName(preset)}_CorridorSilhouette10m_1080p.png");
                    yield return CapturePngEndOfFrame(corridorShot);
                }
                report.scenarios.Add(BuildScenarioResult(preset, "CorridorSilhouette10m", atmosphere, sampler.Summarize(), corridorShot));

                // Extra hero captures (Bed/Window, 2.4m Monster Reveal, and Pause Quality UI) when capture directory is requested
                if (!string.IsNullOrEmpty(captureDirectory) && preset == QualityPreset.High)
                {
                    PositionShadow(shadow, shadowVisual, false, new Vector3(0f, 0.02f, 17.50f));
                    PositionPlayer(motor, playerCc, new Vector3(0.85f, 0.04f, 1.05f), -38f, 10f);
                    yield return null;
                    yield return null;
                    yield return CapturePngEndOfFrame(Path.Combine(captureDirectory, "Standalone_High_DormBedWindow_1080p.png"));

                    PositionShadow(shadow, shadowVisual, true, new Vector3(0f, 0.02f, 17.50f));
                    PositionPlayer(motor, playerCc, new Vector3(0.00f, 0.04f, 15.10f), 0f, 2.0f);
                    yield return null;
                    yield return null;
                    yield return CapturePngEndOfFrame(Path.Combine(captureDirectory, "Standalone_High_MonsterReveal2_4m_1080p.png"));

                    GameObject uiGo = GameObject.Find("UI_FirstPerson");
                    Type sessionType = Type.GetType("NoRainbowAfter.UI.FirstPersonSession, Assembly-CSharp");
                    object session = (uiGo != null && sessionType != null) ? uiGo.GetComponent(sessionType) : null;
                    var setPaused = sessionType != null ? sessionType.GetMethod("SetPaused") : null;
                    if (session != null && setPaused != null)
                    {
                        PositionPlayer(motor, playerCc, new Vector3(0.15f, 0.04f, 1.10f), 16f, 6f);
                        setPaused.Invoke(session, new object[] { true });
                        yield return null;
                        yield return CapturePngEndOfFrame(Path.Combine(captureDirectory, "Standalone_High_PauseQualityUI_1080p.png"));
                        setPaused.Invoke(session, new object[] { false });
                        Time.timeScale = 1f;
                        yield return null;
                    }
                }

                // Scenario 3: Dynamic FPP Traversal (Dorm -> Doorway -> Corridor)
                PositionShadow(shadow, shadowVisual, true, new Vector3(0f, 0.02f, 17.20f));
                PositionPlayer(motor, playerCc, new Vector3(0.12f, 0.04f, 1.20f), 0f, 1f);
                yield return WarmupAndSample(sampler, aggregateSampler, t =>
                {
                    float z = Mathf.Lerp(1.20f, 14.20f, Mathf.PingPong(t * 0.42f, 1f));
                    float x = Mathf.Lerp(0.12f, 0.0f, Mathf.Clamp01((z - 1.2f) / 3.2f));
                    float yaw = Mathf.Sin(t * 1.3f) * 5.5f;
                    PositionPlayer(motor, playerCc, new Vector3(x, 0.04f, z), yaw, 1.0f);
                });
                report.scenarios.Add(BuildScenarioResult(preset, "DynamicCorridorTraversal", atmosphere, sampler.Summarize(), null));

                FrameTimeSummary agg = aggregateSampler.Summarize();
                report.presetAggregates.Add(new BenchmarkPresetAggregate
                {
                    preset = GraphicsQualityService.GetCanonicalName(preset),
                    totalSamples = agg.sampleCount,
                    averageFrameTimeMs = agg.averageFrameTimeMs,
                    averageFps = agg.averageFps,
                    onePercentLowFps = agg.onePercentLowFps,
                    p95FrameTimeMs = agg.p95FrameTimeMs,
                    maxFrameTimeMs = agg.maxFrameTimeMs
                });
            }

            PositionShadow(shadow, shadowVisual, false, new Vector3(0f, 0.02f, 17.5f));
            report.screenWidth = Screen.width;
            report.screenHeight = Screen.height;
            LastReport = report;

            string dir = Path.GetDirectoryName(outputJsonPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(outputJsonPath, JsonUtility.ToJson(report, true));
            Debug.Log("[NRA Benchmark] Wrote benchmark report to: " + outputJsonPath);

            RestorePersistence();
            IsRunning = false;

            if (quitOnComplete)
            {
                Application.Quit(0);
            }
        }

        private IEnumerator WarmupAndSample(
            FrameTimeSampler scenarioSampler,
            FrameTimeSampler aggregateSampler,
            Action<float> perFrameMotion)
        {
            float warmupElapsed = 0f;
            while (warmupElapsed < warmupSeconds)
            {
                yield return null;
                float dt = Time.unscaledDeltaTime;
                warmupElapsed += dt;
                perFrameMotion?.Invoke(warmupElapsed);
            }

            scenarioSampler.Reset();
            float sampleElapsed = 0f;
            while (sampleElapsed < sampleSeconds)
            {
                yield return null;
                float dt = Time.unscaledDeltaTime;
                sampleElapsed += dt;
                perFrameMotion?.Invoke(warmupSeconds + sampleElapsed);
                scenarioSampler.RecordFrame(dt);
                aggregateSampler.RecordFrame(dt);
            }
        }

        private static IEnumerator CapturePngEndOfFrame(string filePath)
        {
            yield return new WaitForEndOfFrame();
            Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();
            if (tex != null)
            {
                byte[] bytes = tex.EncodeToPNG();
                Destroy(tex);
                if (bytes != null && bytes.Length > 0)
                    File.WriteAllBytes(filePath, bytes);
            }
        }

        private static BenchmarkScenarioResult BuildScenarioResult(
            QualityPreset preset,
            string scenarioName,
            QualityAtmosphereController atmosphere,
            FrameTimeSummary summary,
            string shotPath)
        {
            UniversalRenderPipelineAsset urp = GraphicsQualityService.GetActiveUrpAsset();
            return new BenchmarkScenarioResult
            {
                preset = GraphicsQualityService.GetCanonicalName(preset),
                scenario = scenarioName,
                qualityLevelIndex = QualitySettings.GetQualityLevel(),
                urpAssetName = urp != null ? urp.name : "null",
                renderScale = urp != null ? urp.renderScale : 1f,
                msaaSamples = urp != null ? urp.msaaSampleCount : 1,
                shadowDistance = urp != null ? urp.shadowDistance : 0f,
                shadowCascades = urp != null ? urp.shadowCascadeCount : 0,
                softShadows = urp != null && urp.supportsSoftShadows,
                additionalLightShadows = urp != null && urp.supportsAdditionalLightShadows,
                activeSecondaryParticles = atmosphere != null ? atmosphere.ActiveSecondaryParticleSystemCount : 0,
                activeDripParticles = atmosphere != null ? atmosphere.ActivePrimaryDripSystemCount : 0,
                metrics = summary,
                screenshotPath = shotPath ?? string.Empty
            };
        }

        private static void PositionPlayer(FirstPersonMotor motor, CharacterController cc, Vector3 pos, float yaw, float pitch)
        {
            if (motor == null) return;
            if (cc != null) cc.enabled = false;
            motor.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            if (cc != null) cc.enabled = true;
            motor.StopMotion();
            if (motor.ViewCamera != null)
            {
                motor.ViewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
            Physics.SyncTransforms();
        }

        private static void PositionShadow(
            ShadowEncounterController shadow,
            ShadowPlaceholderVisual visual,
            bool visible,
            Vector3 pos)
        {
            if (shadow == null) return;
            var cc = shadow.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            shadow.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, 180f, 0f));
            if (visual != null)
            {
                visual.SetVisible(visible);
                if (visible)
                    visual.ResetPostureImmediate();
            }
            Physics.SyncTransforms();
        }

        private void RestorePersistence()
        {
            ChapterDirector.StoragePathOverride = previousClueOverride;

            if (hadSavedPreset)
            {
                PlayerPrefs.SetInt(GraphicsQualityService.PreferenceKey, savedPresetValue);
                GraphicsQualityService.ApplyPreset((QualityPreset)savedPresetValue, savePreference: false);
            }
            else
            {
                PlayerPrefs.DeleteKey(GraphicsQualityService.PreferenceKey);
                GraphicsQualityService.ApplyPreset(GraphicsQualityService.DefaultPreset, savePreference: false);
            }
            PlayerPrefs.Save();

            try
            {
                if (!string.IsNullOrEmpty(tempClueDirectory) && Directory.Exists(tempClueDirectory))
                    Directory.Delete(tempClueDirectory, recursive: true);
            }
            catch
            {
                // Ignore temp cleanup contention.
            }
        }

        private static bool HasFlag(string[] args, string flag)
        {
            if (args == null) return false;
            for (int i = 0; i < args.Length; i++)
                if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static string GetArgValue(string[] args, string key)
        {
            if (args == null) return null;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }
            return null;
        }
    }
}
