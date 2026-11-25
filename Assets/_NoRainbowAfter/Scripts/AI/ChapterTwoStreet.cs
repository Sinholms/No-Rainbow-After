using System;
using NoRainbowAfter.Interaction;
using NoRainbowAfter.Player;
using UnityEngine;
using UnityEngine.Rendering;

namespace NoRainbowAfter.AI
{
    public sealed class ChapterTwoStreet : MonoBehaviour
    {
        public const float StressPerCapture = 10f;
        public Transform[] Lamps, Checkpoints;
        public Transform[] GazePivots;
        public Transform PatrolCar, BusSeat;
        public BoxCollider Shelter;
        public SceneInteractable Bag;
        public ChapterTwoBusDoor BusDoor;
        public Light[] FlickerLights;
        public Renderer[] FlickerEmissives;
        public AudioClip RainAmbienceClip;
        public AudioClip ElectricTickClip;

        public event Action SafeZoneReached;
        public event Action BoardRequested;
        public ChapterTwoSpotlightEncounter Encounter { get; private set; }
        public int Segment { get; private set; }
        public bool AtShelter { get; private set; }
        public bool Boarded { get; private set; }

        private FirstPersonMotor motor;
        private FirstPersonPlayer player;
        private AudioSource siren;
        private AudioSource rainAudio;
        private AudioSource droneAudio;
        private AudioSource crackleAudio;
        private AudioClip droneClip;
        private Light[] beacons;
        private float[] flickerBaseIntensity;
        private float[] flickerNextToggle;
        private int[] flickerBurstEdges;
        private bool[] flickerOn;
        private MaterialPropertyBlock flickerBlock;
        private ParticleSystem rainParticles;
        private ParticleSystem mistParticles;
        private Material particleMaterial;
        private Transform[] sentryVisuals;
        private Vector3[] sentryBaseScale;

        private bool sirenPaused;
        private bool atmosphereSaved;
        private bool savedFog;
        private Color savedFogColor;
        private FogMode savedFogMode;
        private float savedFogDensity;
        private AmbientMode savedAmbientMode;
        private Color savedAmbientSky, savedAmbientEquator, savedAmbientGround;

        public void Stage()
        {
            transform.position = new Vector3(240f, 0f, 180f);
            gameObject.SetActive(true);
            beacons ??= PatrolCar != null ? PatrolCar.GetComponentsInChildren<Light>() : Array.Empty<Light>();
            InitFlickerState();
            InitSentryVisuals();
            EnsureWeatherParticles();
            Physics.SyncTransforms();
        }

        public void PlaySiren(float volume)
        {
            Stage();
            if (siren == null)
            {
                siren = gameObject.AddComponent<AudioSource>();
                siren.playOnAwake = false;
                siren.loop = true;
                siren.clip = Resources.Load<AudioClip>("ChapterTwoAudio/TKPSiren");
            }
            siren.volume = volume;
            sirenPaused = false;
            if (siren.clip != null && !siren.isPlaying) siren.Play();
        }

        public bool Enter(FirstPersonMotor target)
        {
            if (motor != null) return true;
            Stage();
            if (!target.TryRestorePose(Checkpoints[0].position, Checkpoints[0].rotation)) return false;

            motor = target;
            player = target.GetComponent<FirstPersonPlayer>();
            Segment = 0;
            AtShelter = Boarded = false;
            Bag.SetAvailable(false);
            BusDoor.Bind(this);

            ApplyHorrorAtmosphere();
            StartAmbienceAudio();

            Encounter = gameObject.AddComponent<ChapterTwoSpotlightEncounter>();
            Encounter.Configure(target, Lamps, Checkpoints, GazePivots);
            Encounter.Caught += OnCaught;
            Encounter.ConfigureBeam(0, 42f, 14f);
            Encounter.ConfigureBeam(1, 42f, 14f);
            // Atur sudut, durasi siklus, dan offset fase supaya kedua sorot menyapu jalan dengan ritme berbeda.
            Encounter.ConfigureSweep(0, 27f, 7.6f);
            Encounter.ConfigureSweep(1, 25f, 8.6f, 1.3f);
            Physics.SyncTransforms();
            if (Encounter.RenderFootprints) Encounter.RebuildFootprints();
            return true;
        }

        private void ApplyHorrorAtmosphere()
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
            RenderSettings.fogColor = new Color(0.022f, 0.034f, 0.048f);
            RenderSettings.fogDensity = 0.028f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.075f, 0.105f, 0.145f);
            RenderSettings.ambientEquatorColor = new Color(0.045f, 0.058f, 0.072f);
            RenderSettings.ambientGroundColor = new Color(0.022f, 0.026f, 0.034f);
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

        private void StartAmbienceAudio()
        {
            if (RainAmbienceClip != null)
            {
                if (rainAudio == null)
                {
                    rainAudio = gameObject.AddComponent<AudioSource>();
                    rainAudio.playOnAwake = false;
                    rainAudio.loop = true;
                    rainAudio.spatialBlend = 0f;
                    rainAudio.clip = RainAmbienceClip;
                }
                rainAudio.volume = 0.24f;
                if (!rainAudio.isPlaying) rainAudio.Play();
            }

            if (droneAudio == null)
            {
                droneAudio = gameObject.AddComponent<AudioSource>();
                droneAudio.playOnAwake = false;
                droneAudio.loop = true;
                droneAudio.spatialBlend = 0f;
                droneAudio.volume = 0.16f;
                droneClip = CreateHorrorDroneClip();
                droneAudio.clip = droneClip;
            }
            if (!droneAudio.isPlaying) droneAudio.Play();

            if (ElectricTickClip != null && crackleAudio == null)
            {
                crackleAudio = gameObject.AddComponent<AudioSource>();
                crackleAudio.playOnAwake = false;
                crackleAudio.spatialBlend = 0f;
                crackleAudio.volume = 0.18f;
            }
        }

        private static AudioClip CreateHorrorDroneClip()
        {
            const int rate = 22050;
            const int seconds = 4;
            int count = rate * seconds;
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate;
                float lfo = 0.72f + 0.28f * Mathf.Sin(2f * Mathf.PI * 0.25f * t);
                float wave = Mathf.Sin(2f * Mathf.PI * 41.2f * t) * 0.55f
                           + Mathf.Sin(2f * Mathf.PI * 43.0f * t) * 0.35f
                           + Mathf.Sin(2f * Mathf.PI * 82.4f * t) * 0.15f;
                data[i] = wave * lfo * 0.65f;
            }
            var clip = AudioClip.Create("Market dread drone", count, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void InitFlickerState()
        {
            if (FlickerLights == null || FlickerLights.Length == 0 || flickerBaseIntensity != null) return;
            int count = FlickerLights.Length;
            flickerBaseIntensity = new float[count];
            flickerNextToggle = new float[count];
            flickerBurstEdges = new int[count];
            flickerOn = new bool[count];
            flickerBlock = new MaterialPropertyBlock();
            for (int i = 0; i < count; i++)
            {
                flickerBaseIntensity[i] = FlickerLights[i] != null ? FlickerLights[i].intensity : 0f;
                flickerNextToggle[i] = 1.4f + i * 1.15f;
                flickerOn[i] = true;
            }
        }

        private void InitSentryVisuals()
        {
            if (Lamps == null || sentryVisuals != null) return;
            sentryVisuals = new Transform[Lamps.Length];
            sentryBaseScale = new Vector3[Lamps.Length];
            for (int i = 0; i < Lamps.Length; i++)
            {
                if (Lamps[i] == null || Lamps[i].parent == null) continue;
                var model = Lamps[i].parent.Find("ACT_TKPSentry");
                if (model != null)
                {
                    sentryVisuals[i] = model;
                    sentryBaseScale[i] = model.localScale;
                }
            }
        }

        private void EnsureWeatherParticles()
        {
            if (rainParticles != null) return;
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) return;

            particleMaterial = new Material(shader);
            particleMaterial.color = Color.white;

            // Night rain sheets slanting slightly in the wind across the market street.
            var rainGo = new GameObject("Hujan malam");
            rainGo.transform.SetParent(transform, false);
            rainGo.transform.localPosition = new Vector3(0f, 10.5f, 20f);
            rainParticles = rainGo.AddComponent<ParticleSystem>();
            var rMain = rainParticles.main;
            rMain.loop = true;
            rMain.prewarm = true;
            rMain.startLifetime = 0.85f;
            rMain.startSpeed = 16f;
            rMain.startSize3D = true;
            rMain.startSizeX = 0.012f;
            rMain.startSizeY = 0.24f;
            rMain.startSizeZ = 0.012f;
            rMain.startColor = new Color(0.58f, 0.70f, 0.82f, 0.18f);
            rMain.maxParticles = 600;
            rMain.simulationSpace = ParticleSystemSimulationSpace.Local;
            var rEmit = rainParticles.emission;
            rEmit.rateOverTime = 460f;
            var rShape = rainParticles.shape;
            rShape.shapeType = ParticleSystemShapeType.Box;
            rShape.scale = new Vector3(16f, 1f, 52f);
            rShape.rotation = new Vector3(101f, 0f, -9f);
            var rRend = rainGo.GetComponent<ParticleSystemRenderer>();
            rRend.sharedMaterial = particleMaterial;
            rRend.renderMode = ParticleSystemRenderMode.Stretch;
            rRend.lengthScale = 1.45f;
            rRend.velocityScale = 0.04f;
            rRend.shadowCastingMode = ShadowCastingMode.Off;
            rRend.receiveShadows = false;

            // Slow low-lying drifting mist wisps creeping along the wet asphalt.
            var mistGo = new GameObject("Kabut jalan pasar");
            mistGo.transform.SetParent(transform, false);
            mistGo.transform.localPosition = new Vector3(0f, 0.95f, 21f);
            mistParticles = mistGo.AddComponent<ParticleSystem>();
            var mMain = mistParticles.main;
            mMain.loop = true;
            mMain.prewarm = true;
            mMain.startLifetime = new ParticleSystem.MinMaxCurve(6.5f, 10f);
            mMain.startSpeed = new ParticleSystem.MinMaxCurve(0.22f, 0.55f);
            mMain.startSize = new ParticleSystem.MinMaxCurve(3.4f, 6.2f);
            mMain.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            mMain.startColor = new Color(0.18f, 0.25f, 0.32f, 0.075f);
            mMain.maxParticles = 48;
            mMain.simulationSpace = ParticleSystemSimulationSpace.Local;
            var mEmit = mistParticles.emission;
            mEmit.rateOverTime = 5.5f;
            var mShape = mistParticles.shape;
            mShape.shapeType = ParticleSystemShapeType.Box;
            mShape.scale = new Vector3(13f, 1.3f, 44f);
            var mCol = mistParticles.colorOverLifetime;
            mCol.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(0.22f, 0.30f, 0.38f), 0f), new GradientColorKey(new Color(0.14f, 0.20f, 0.28f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.85f, 0.35f), new GradientAlphaKey(0.75f, 0.7f), new GradientAlphaKey(0f, 1f) }
            );
            mCol.color = grad;
            var mRend = mistGo.GetComponent<ParticleSystemRenderer>();
            mRend.sharedMaterial = particleMaterial;
            mRend.shadowCastingMode = ShadowCastingMode.Off;
            mRend.receiveShadows = false;
        }

        // Encounter menangani pemulihan posisi. Jalan menambah stres setiap kali pemain tertangkap.
        private void OnCaught(int segment)
        {
            if (player != null) player.GetComponent<StressBarometer>()?.AddStress(StressPerCapture);
        }

        private void Update()
        {
            bool paused = player != null && player.IsPaused;
            SyncAudioPause(paused);
            if (paused) return;

            UpdatePatrolBeacons();
            UpdateFlickerLights(Time.deltaTime);
            UpdateSentryBreathing();

            if (player == null || !player.IsTransitionLocked) CheckProgress();
        }

        private void SyncAudioPause(bool paused)
        {
            if (siren != null)
            {
                if (paused && siren.isPlaying) { siren.Pause(); sirenPaused = true; }
                else if (!paused && sirenPaused && !Boarded) { siren.UnPause(); sirenPaused = false; }
            }
            if (rainAudio != null)
            {
                if (paused && rainAudio.isPlaying) rainAudio.Pause();
                else if (!paused && !rainAudio.isPlaying && motor != null) rainAudio.UnPause();
            }
            if (droneAudio != null)
            {
                if (paused && droneAudio.isPlaying) droneAudio.Pause();
                else if (!paused && !Boarded && !droneAudio.isPlaying && motor != null) droneAudio.UnPause();
            }
        }

        private void UpdatePatrolBeacons()
        {
            if (beacons == null || beacons.Length == 0) return;
            // Realistic sharp double-flash alternating emergency strobe (blue then red).
            float cycle = (Time.time * 1.45f) % 1f;
            float blueFlash = DoublePulse(cycle);
            float redFlash = DoublePulse((cycle + 0.5f) % 1f);
            for (int i = 0; i < beacons.Length; i++)
            {
                if (beacons[i] == null) continue;
                float flash = i == 0 ? blueFlash : redFlash;
                beacons[i].intensity = 0.25f + 16.5f * flash;
            }
        }

        private static float DoublePulse(float phase)
        {
            // Two crisp flashes at phase [0.00..0.11] and [0.16..0.28], then dark until next half-cycle.
            float p1 = phase < 0.11f ? Mathf.Sin(phase / 0.11f * Mathf.PI) : 0f;
            float p2 = (phase > 0.16f && phase < 0.28f) ? Mathf.Sin((phase - 0.16f) / 0.12f * Mathf.PI) : 0f;
            float peak = Mathf.Max(p1, p2);
            return peak * peak;
        }

        private void UpdateFlickerLights(float dt)
        {
            if (FlickerLights == null || flickerBaseIntensity == null) return;
            for (int i = 0; i < FlickerLights.Length; i++)
            {
                var lamp = FlickerLights[i];
                if (lamp == null) continue;
                flickerNextToggle[i] -= dt;
                if (flickerNextToggle[i] > 0f) continue;

                if (flickerBurstEdges[i] <= 0)
                    flickerBurstEdges[i] = UnityEngine.Random.Range(2, 6) * 2;

                flickerOn[i] = !flickerOn[i];
                flickerBurstEdges[i]--;

                bool on = flickerOn[i];
                lamp.intensity = on ? flickerBaseIntensity[i] * UnityEngine.Random.Range(0.75f, 1.08f) : flickerBaseIntensity[i] * 0.04f;

                if (FlickerEmissives != null && i < FlickerEmissives.Length && FlickerEmissives[i] != null)
                {
                    flickerBlock.SetColor("_EmissionColor", on ? new Color(1.1f, 0.88f, 0.56f) : Color.black);
                    FlickerEmissives[i].SetPropertyBlock(flickerBlock);
                }

                if (!on && crackleAudio != null && ElectricTickClip != null && motor != null)
                {
                    float dist = Vector3.Distance(motor.transform.position, lamp.transform.position);
                    if (dist < 14f)
                    {
                        float atten = Mathf.Clamp01(1f - dist / 14f);
                        crackleAudio.pitch = UnityEngine.Random.Range(0.86f, 1.18f);
                        crackleAudio.PlayOneShot(ElectricTickClip, 0.28f * atten * atten);
                    }
                }

                flickerNextToggle[i] = flickerBurstEdges[i] > 0
                    ? UnityEngine.Random.Range(0.05f, 0.17f)
                    : UnityEngine.Random.Range(1.8f, 4.8f);
            }
        }

        private void UpdateSentryBreathing()
        {
            if (sentryVisuals == null) return;
            for (int i = 0; i < sentryVisuals.Length; i++)
            {
                var visual = sentryVisuals[i];
                if (visual == null) continue;
                float breath = Mathf.Sin(Time.time * 1.55f + i * 2.1f);
                Vector3 baseScale = sentryBaseScale[i];
                visual.localScale = new Vector3(
                    baseScale.x * (1f + breath * 0.012f),
                    baseScale.y * (1f + breath * 0.008f),
                    baseScale.z);
            }
        }

        public void CheckProgress()
        {
            if (motor == null || AtShelter) return;
            Vector3 feet = motor.transform.position;
            // Ukur kemajuan pada sumbu Z lokal jalan. Segmen baru menjadi tujuan pemulihan berikutnya.
            if (Checkpoints != null && Segment + 1 < Checkpoints.Length && Checkpoints[Segment + 1] != null &&
                transform.InverseTransformPoint(feet).z >= transform.InverseTransformPoint(Checkpoints[Segment + 1].position).z)
                Encounter.SetSegment(++Segment);
            if (Shelter == null || !Shelter.bounds.Contains(feet + Vector3.up * .5f)) return;

            // Pemeriksaan volume halte menandai akhir area patroli dan mematikan deteksi sorot.
            AtShelter = true;
            if (Encounter != null) Encounter.enabled = false;
            if (Bag != null) Bag.SetAvailable(true);
            SafeZoneReached?.Invoke();
        }

        internal void RequestBoard()
        {
            if (AtShelter && !Boarded) BoardRequested?.Invoke();
        }

        public bool TryBoard()
        {
            if (!AtShelter || Boarded || !motor.TryRestorePose(BusSeat.position, BusSeat.rotation)) return false;
            Boarded = true;
            if (siren != null) siren.Stop();
            if (droneAudio != null) droneAudio.Stop();
            if (rainAudio != null) rainAudio.volume = 0.09f; // Muffled inside the bus cabin
            return true;
        }

        public bool TryRestoreCheckpoint()
        {
            if (motor == null) return false;
            if (Boarded) return motor.TryRestorePose(BusSeat.position, BusSeat.rotation);
            if (!AtShelter) return Encounter.TryRestoreCheckpoint();
            Vector3 entrance = Shelter.transform.TransformPoint(new Vector3(0, -1.01f, -.92f));
            return motor.TryRestorePose(entrance, Shelter.transform.rotation);
        }

        public void Leave()
        {
            motor = null;
            player = null;
            Segment = 0;
            AtShelter = Boarded = false;
            sirenPaused = false;
            if (siren != null) siren.Stop();
            if (rainAudio != null) rainAudio.Stop();
            if (droneAudio != null) droneAudio.Stop();
            if (Encounter != null) { Encounter.enabled = false; Destroy(Encounter); Encounter = null; }
            RestoreAtmosphere();
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            RestoreAtmosphere();
            if (droneClip != null) Destroy(droneClip);
            if (particleMaterial != null) Destroy(particleMaterial);
        }
    }
}
