using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NoRainbowAfter.Quality
{
    /// <summary>
    /// Mengatur penskalaan atmosfer URP tingkat scene (Low / Medium / High) untuk partikel kabut/debu,
    /// tetesan air langit-langit, ReflectionProbe, bayangan lampu pemain, anti-aliasing kamera,
    /// dan override VolumeProfile runtime tanpa mengotori aset di disk.
    ///
    /// [GRAFIKA KOMPUTER]
    /// Mengendalikan parameter render per-tier kualitas:
    /// - Camera AA (None -> FXAA -> SMAA), LightShadows (None -> Soft),
    /// - Box-projected ReflectionProbe (mati -> 128px -> 256px),
    /// - ParticleSystem emission scaling, serta ACES Tonemapping, Bloom, &amp; FilmGrain.
    /// </summary>
    public sealed class QualityAtmosphereController : MonoBehaviour
    {
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private Light playerViewLight;
        [SerializeField] private ReflectionProbe[] reflectionProbes;
        [SerializeField] private Volume postProcessVolume;
        [SerializeField] private ParticleSystem[] primaryDripSystems;
        [SerializeField] private ParticleSystem[] secondaryDustSystems;

        private VolumeProfile sourceSharedProfile;
        private VolumeProfile runtimeProfile;
        private float[] baseDustEmissionRates;

        public QualityPreset AppliedPreset { get; private set; } = GraphicsQualityService.DefaultPreset;

        public int ActiveSecondaryParticleSystemCount
        {
            get
            {
                if (secondaryDustSystems == null) return 0;
                int count = 0;
                for (int i = 0; i < secondaryDustSystems.Length; i++)
                {
                    ParticleSystem ps = secondaryDustSystems[i];
                    if (ps != null && ps.gameObject.activeInHierarchy && ps.isPlaying)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        public int ActivePrimaryDripSystemCount
        {
            get
            {
                if (primaryDripSystems == null) return 0;
                int count = 0;
                for (int i = 0; i < primaryDripSystems.Length; i++)
                {
                    ParticleSystem ps = primaryDripSystems[i];
                    if (ps != null && ps.gameObject.activeInHierarchy && ps.isPlaying)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        public void Configure(
            Camera targetCamera,
            Light viewLight,
            ReflectionProbe[] probes,
            Volume volume,
            ParticleSystem[] drips,
            ParticleSystem[] dust)
        {
            gameplayCamera = targetCamera;
            playerViewLight = viewLight;
            reflectionProbes = probes;
            postProcessVolume = volume;
            primaryDripSystems = drips;
            secondaryDustSystems = dust;
            CacheDustBaseRates();
            if (Application.isPlaying && isActiveAndEnabled)
            {
                ApplySceneQuality(GraphicsQualityService.CurrentPreset);
            }
        }

        private void Awake()
        {
            CacheDustBaseRates();
            EnsureRuntimeVolumeProfile();
        }

        private void OnEnable()
        {
            EnsureRuntimeVolumeProfile();
            GraphicsQualityService.PresetChanged += ApplySceneQuality;
            ApplySceneQuality(GraphicsQualityService.CurrentPreset);
        }

        private void OnDisable() => GraphicsQualityService.PresetChanged -= ApplySceneQuality;

        private void OnDestroy()
        {
            if (postProcessVolume != null && sourceSharedProfile != null)
            {
                postProcessVolume.sharedProfile = sourceSharedProfile;
            }
            if (runtimeProfile != null)
            {
                Destroy(runtimeProfile);
                runtimeProfile = null;
            }
        }

        public void ApplySceneQuality(QualityPreset preset)
        {
            AppliedPreset = preset;

            if (gameplayCamera != null)
            {
                GraphicsQualityService.ApplyCameraAntialiasing(gameplayCamera, preset);
            }

            if (playerViewLight != null)
            {
                playerViewLight.shadows = preset == QualityPreset.Low
                    ? LightShadows.None
                    : LightShadows.Soft;
            }

            ApplyReflectionProbes(preset);
            ApplyParticles(preset);
            ApplyPostProcessing(preset);
        }

        private void CacheDustBaseRates()
        {
            int length = secondaryDustSystems != null ? secondaryDustSystems.Length : 0;
            baseDustEmissionRates = new float[length];
            for (int i = 0; i < length; i++)
            {
                ParticleSystem ps = secondaryDustSystems[i];
                baseDustEmissionRates[i] = ps != null ? ps.emission.rateOverTimeMultiplier : 0f;
            }
        }

        private void EnsureRuntimeVolumeProfile()
        {
            if (!Application.isPlaying || postProcessVolume == null || runtimeProfile != null)
            {
                return;
            }
            sourceSharedProfile = postProcessVolume.sharedProfile;
            if (sourceSharedProfile == null)
            {
                return;
            }
            runtimeProfile = Instantiate(sourceSharedProfile);
            runtimeProfile.name = sourceSharedProfile.name + "_RuntimeT07";
            postProcessVolume.profile = runtimeProfile;
        }

        private void ApplyReflectionProbes(QualityPreset preset)
        {
            if (reflectionProbes == null)
            {
                return;
            }
            bool enableProbes = preset != QualityPreset.Low;
            int targetRes = preset == QualityPreset.High ? 256 : 128;

            for (int i = 0; i < reflectionProbes.Length; i++)
            {
                ReflectionProbe probe = reflectionProbes[i];
                if (probe == null) continue;
                bool enabledChanged = probe.enabled != enableProbes;
                probe.enabled = enableProbes;
                if (enableProbes)
                {
                    bool resChanged = probe.resolution != targetRes;
                    probe.resolution = targetRes;
                    probe.boxProjection = true;
                    if (Application.isPlaying && (enabledChanged || resChanged) && probe.mode == ReflectionProbeMode.Realtime)
                    {
                        probe.RenderProbe();
                    }
                }
            }
        }

        private void ApplyParticles(QualityPreset preset)
        {
            if (primaryDripSystems != null)
            {
                bool enableDrips = preset != QualityPreset.Low;
                for (int i = 0; i < primaryDripSystems.Length; i++)
                {
                    ParticleSystem ps = primaryDripSystems[i];
                    if (ps == null) continue;
                    if (enableDrips)
                    {
                        if (!ps.gameObject.activeSelf) ps.gameObject.SetActive(true);
                        if (!ps.isPlaying) ps.Play(true);
                    }
                    else
                    {
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        ps.gameObject.SetActive(false);
                    }
                }
            }

            if (secondaryDustSystems != null)
            {
                if (baseDustEmissionRates == null || baseDustEmissionRates.Length != secondaryDustSystems.Length)
                {
                    CacheDustBaseRates();
                }

                for (int i = 0; i < secondaryDustSystems.Length; i++)
                {
                    ParticleSystem ps = secondaryDustSystems[i];
                    if (ps == null) continue;

                    bool enableSystem = preset == QualityPreset.High || (preset == QualityPreset.Medium && i == 0);
                    if (enableSystem)
                    {
                        float rateMultiplier = preset == QualityPreset.High ? 1.0f : 0.55f;
                        var emission = ps.emission;
                        emission.rateOverTimeMultiplier = baseDustEmissionRates[i] * rateMultiplier;
                        if (!ps.gameObject.activeSelf) ps.gameObject.SetActive(true);
                        if (!ps.isPlaying) ps.Play(true);
                    }
                    else
                    {
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        ps.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void ApplyPostProcessing(QualityPreset preset)
        {
            if (!Application.isPlaying)
            {
                return;
            }
            EnsureRuntimeVolumeProfile();
            VolumeProfile profile = runtimeProfile != null
                ? runtimeProfile
                : (postProcessVolume != null ? postProcessVolume.sharedProfile : null);
            if (profile == null)
            {
                return;
            }

            if (profile.TryGet<Tonemapping>(out var tone))
            {
                tone.active = true;
                tone.mode.overrideState = true;
                tone.mode.value = TonemappingMode.ACES;
            }
            if (profile.TryGet<ColorAdjustments>(out var color))
            {
                color.active = true;
            }
            if (profile.TryGet<FilmGrain>(out var grain))
            {
                grain.active = preset != QualityPreset.Low;
                grain.intensity.overrideState = true;
                grain.intensity.value = preset == QualityPreset.High ? 0.11f : 0.07f;
            }
            if (profile.TryGet<Bloom>(out var bloom))
            {
                bloom.active = preset != QualityPreset.Low;
                bloom.intensity.overrideState = true;
                bloom.intensity.value = preset == QualityPreset.High ? 0.22f : 0.14f;
                bloom.highQualityFiltering.overrideState = true;
                bloom.highQualityFiltering.value = preset == QualityPreset.High;
            }
        }
    }
}
