using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NoRainbowAfter.Quality
{
    public enum QualityPreset
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    /// <summary>
    /// Owns the three canonical T07 graphics quality presets (Low / Medium / High),
    /// synchronises Unity QualitySettings + active URP pipeline assets, and persists
    /// the player's selection in PlayerPrefs ("NRA.QualityPreset").
    /// </summary>
    public static class GraphicsQualityService
    {
        public const string PreferenceKey = "NRA.QualityPreset";
        public const QualityPreset DefaultPreset = QualityPreset.High;

        private static readonly string[] CanonicalLevelNames = { "Low", "Medium", "High" };
        private static readonly string[] DisplayLabels = { "Rendah", "Sedang", "Tinggi" };

        private static QualityPreset currentPreset = DefaultPreset;
        private static bool initialized;

        public static event Action<QualityPreset> PresetChanged;

        public static QualityPreset CurrentPreset
        {
            get
            {
                if (!initialized)
                    currentPreset = LoadSavedPreset();
                return currentPreset;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            currentPreset = DefaultPreset;
            initialized = false;
            PresetChanged = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyOnStartup()
        {
            ApplySavedPreset();
        }

        public static QualityPreset LoadSavedPreset()
        {
            int raw = PlayerPrefs.GetInt(PreferenceKey, (int)DefaultPreset);
            return ClampPreset(raw);
        }

        public static QualityPreset ApplySavedPreset()
        {
            QualityPreset saved = LoadSavedPreset();
            ApplyPreset(saved, savePreference: false);
            return saved;
        }

        public static void ApplyPreset(QualityPreset preset, bool savePreference = true)
        {
            QualityPreset clamped = ClampPreset((int)preset);
            int qualityIndex = ResolveQualitySettingsIndex(clamped);
            if (qualityIndex >= 0 && QualitySettings.GetQualityLevel() != qualityIndex)
            {
                QualitySettings.SetQualityLevel(qualityIndex, applyExpensiveChanges: true);
            }

            ApplyCameraAntialiasingToLoadedCameras(clamped);

            currentPreset = clamped;
            initialized = true;

            if (savePreference)
            {
                PlayerPrefs.SetInt(PreferenceKey, (int)clamped);
                PlayerPrefs.Save();
            }

            PresetChanged?.Invoke(clamped);
        }

        public static string GetDisplayLabel(QualityPreset preset)
        {
            int index = (int)ClampPreset((int)preset);
            return DisplayLabels[index];
        }

        public static string GetCanonicalName(QualityPreset preset)
        {
            int index = (int)ClampPreset((int)preset);
            return CanonicalLevelNames[index];
        }

        public static UniversalRenderPipelineAsset GetActiveUrpAsset()
        {
            return GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset
                   ?? QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
        }

        public static void ApplyCameraAntialiasing(Camera camera, QualityPreset preset)
        {
            if (camera == null) return;
            var data = camera.GetComponent<UniversalAdditionalCameraData>();
            if (data == null) return;

            switch (ClampPreset((int)preset))
            {
                case QualityPreset.Low:
                    data.antialiasing = AntialiasingMode.None;
                    data.antialiasingQuality = AntialiasingQuality.Low;
                    break;
                case QualityPreset.Medium:
                    data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                    data.antialiasingQuality = AntialiasingQuality.Medium;
                    break;
                default:
                    data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                    data.antialiasingQuality = AntialiasingQuality.High;
                    break;
            }
        }

        public static int ResolveQualitySettingsIndex(QualityPreset preset)
        {
            string[] names = QualitySettings.names;
            if (names == null || names.Length == 0)
                return -1;

            string targetName = GetCanonicalName(preset);
            for (int i = 0; i < names.Length; i++)
            {
                if (string.Equals(names[i], targetName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return Mathf.Clamp((int)preset, 0, names.Length - 1);
        }

        private static void ApplyCameraAntialiasingToLoadedCameras(QualityPreset preset)
        {
            var cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera cam = cameras[i];
                if (cam != null && cam.cameraType == CameraType.Game)
                    ApplyCameraAntialiasing(cam, preset);
            }
        }

        private static QualityPreset ClampPreset(int raw)
        {
            if (raw <= (int)QualityPreset.Low) return QualityPreset.Low;
            if (raw >= (int)QualityPreset.High) return QualityPreset.High;
            return QualityPreset.Medium;
        }
    }
}
