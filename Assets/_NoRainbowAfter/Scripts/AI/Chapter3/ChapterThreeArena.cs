using System;
using NoRainbowAfter.Player;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace NoRainbowAfter.AI
{
    /// <summary>
    /// Open underground basement parking arena for Chapter 3's 2-minute chase.
    /// Manages NavMesh registration, dark foggy atmosphere, flickering overhead lights,
    /// ambient hum audio, and clean spawn/cleanup.
    /// </summary>
    public sealed class ChapterThreeArena : MonoBehaviour
    {
        public NavMeshData Navigation;
        public Transform PlayerSpawn;
        public Transform MonsterSpawn;
        public Transform[] PatrolWaypoints;
        public Light[] FlickerLights;
        public Renderer[] FlickerEmissives;
        public float ArenaHalfExtent = 23.5f;

        private NavMeshDataInstance navInstance;
        private AudioSource humAudio;
        private AudioClip humClip;
        private float[] baseIntensities;
        private float[] nextToggle;
        private bool[] lightOn;
        private MaterialPropertyBlock emissiveBlock;

        private bool atmosphereSaved;
        private bool savedFog;
        private Color savedFogColor;
        private FogMode savedFogMode;
        private float savedFogDensity;
        private AmbientMode savedAmbientMode;
        private Color savedAmbientSky, savedAmbientEquator, savedAmbientGround;

        public bool IsEntered { get; private set; }

        public void Stage()
        {
            transform.position = new Vector3(480f, 0f, 180f);
            gameObject.SetActive(true);
            EnsureNavMesh();
            InitFlicker();
            Physics.SyncTransforms();
        }

        public bool Enter(FirstPersonMotor target)
        {
            if (target == null) return false;
            Stage();
            Vector3 spawnPos = PlayerSpawn != null ? PlayerSpawn.position : transform.position + Vector3.up * 0.08f;
            Quaternion spawnRot = PlayerSpawn != null ? PlayerSpawn.rotation : Quaternion.identity;
            if (!target.TryRestorePose(spawnPos, spawnRot)) return false;

            IsEntered = true;
            ApplyBasementAtmosphere();
            StartHumAudio();
            Physics.SyncTransforms();
            return true;
        }

        public void Leave()
        {
            IsEntered = false;
            RestoreAtmosphere();
            if (humAudio != null && humAudio.isPlaying) humAudio.Stop();
            if (navInstance.valid) navInstance.Remove();
            gameObject.SetActive(false);
        }

        public void SetAudioPaused(bool paused)
        {
            if (humAudio == null) return;
            if (paused && humAudio.isPlaying) humAudio.Pause();
            else if (!paused && IsEntered && !humAudio.isPlaying) humAudio.UnPause();
        }

        private void OnDestroy()
        {
            RestoreAtmosphere();
            if (navInstance.valid) navInstance.Remove();
            if (humClip != null) Destroy(humClip);
        }

        private void Update()
        {
            if (!IsEntered || FlickerLights == null) return;
            float now = Time.time;
            for (int i = 0; i < FlickerLights.Length; i++)
            {
                Light l = FlickerLights[i];
                if (l == null || nextToggle == null || i >= nextToggle.Length) continue;
                if (now < nextToggle[i]) continue;

                lightOn[i] = !lightOn[i];
                l.intensity = lightOn[i] ? baseIntensities[i] : baseIntensities[i] * 0.14f;
                if (FlickerEmissives != null && i < FlickerEmissives.Length && FlickerEmissives[i] != null)
                {
                    emissiveBlock ??= new MaterialPropertyBlock();
                    emissiveBlock.SetColor("_EmissionColor",
                        lightOn[i] ? new Color(0.45f, 0.54f, 0.56f) : new Color(0.06f, 0.08f, 0.09f));
                    FlickerEmissives[i].SetPropertyBlock(emissiveBlock);
                }
                nextToggle[i] = now + (lightOn[i] ? UnityEngine.Random.Range(1.8f, 5.2f) : UnityEngine.Random.Range(0.06f, 0.24f));
            }
        }

        private void EnsureNavMesh()
        {
            if (Navigation != null && !navInstance.valid)
                navInstance = NavMesh.AddNavMeshData(Navigation, transform.position, transform.rotation);
        }

        private void InitFlicker()
        {
            if (FlickerLights == null || baseIntensities != null) return;
            baseIntensities = new float[FlickerLights.Length];
            nextToggle = new float[FlickerLights.Length];
            lightOn = new bool[FlickerLights.Length];
            for (int i = 0; i < FlickerLights.Length; i++)
            {
                baseIntensities[i] = FlickerLights[i] != null ? FlickerLights[i].intensity : 0f;
                nextToggle[i] = Time.time + 1.2f + i * 0.9f;
                lightOn[i] = true;
            }
        }

        private void ApplyBasementAtmosphere()
        {
            if (!atmosphereSaved)
            {
                savedFog = RenderSettings.fog;
                savedFogColor = RenderSettings.fogColor;
                savedFogMode = RenderSettings.fogMode;
                savedFogDensity = RenderSettings.fogDensity;
                savedAmbientMode = RenderSettings.ambientMode;
                savedAmbientSky = RenderSettings.ambientSkyColor;
                savedAmbientEquator = RenderSettings.ambientEquatorColor;
                savedAmbientGround = RenderSettings.ambientGroundColor;
                atmosphereSaved = true;
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.014f, 0.019f, 0.026f);
            RenderSettings.fogDensity = 0.052f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.036f, 0.048f, 0.064f);
            RenderSettings.ambientEquatorColor = new Color(0.022f, 0.028f, 0.036f);
            RenderSettings.ambientGroundColor = new Color(0.012f, 0.014f, 0.018f);
        }

        private void RestoreAtmosphere()
        {
            if (!atmosphereSaved) return;
            RenderSettings.fog = savedFog;
            RenderSettings.fogColor = savedFogColor;
            RenderSettings.fogMode = savedFogMode;
            RenderSettings.fogDensity = savedFogDensity;
            RenderSettings.ambientMode = savedAmbientMode;
            RenderSettings.ambientSkyColor = savedAmbientSky;
            RenderSettings.ambientEquatorColor = savedAmbientEquator;
            RenderSettings.ambientGroundColor = savedAmbientGround;
            atmosphereSaved = false;
        }

        private void StartHumAudio()
        {
            if (humAudio == null)
            {
                humAudio = gameObject.AddComponent<AudioSource>();
                humAudio.playOnAwake = false;
                humAudio.loop = true;
                humAudio.spatialBlend = 0f;
                humAudio.volume = 0.22f;
                humClip = CreateBasementHumClip();
                humAudio.clip = humClip;
            }
            if (!humAudio.isPlaying) humAudio.Play();
        }

        private static AudioClip CreateBasementHumClip()
        {
            const int rate = 22050;
            const int seconds = 4;
            int count = rate * seconds;
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate;
                float pulse = 0.78f + 0.22f * Mathf.Sin(2f * Mathf.PI * 0.25f * t);
                float sub = Mathf.Sin(2f * Mathf.PI * 38.0f * t) * 0.52f
                          + Mathf.Sin(2f * Mathf.PI * 57.0f * t) * 0.28f
                          + Mathf.Sin(2f * Mathf.PI * 114.0f * t) * 0.10f;
                data[i] = Mathf.Clamp(sub * pulse * 0.65f, -0.9f, 0.9f);
            }
            var clip = AudioClip.Create("BasementDreadHum", count, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
