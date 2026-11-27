using System;
using NoRainbowAfter.Interaction;
using NoRainbowAfter.Player;
using UnityEngine;
using UnityEngine.Rendering;

namespace NoRainbowAfter.AI
{
    /// <summary>
    /// Pengelola area jalan pasar malam pada Chapter 2 (file inti / alur progres).
    ///
    /// [GRAFIKA KOMPUTER]
    /// Mengorkestrasi atmosfer jalan malam (kabut eksponensial kuadratik, pencahayaan
    /// Trilight, cuaca hujan & kabut partikel, lampu rotator TKP, serta sorot polisi).
    ///
    /// [TEKNIS KODING]
    /// Dipecah menjadi 3 file partial class:
    /// 1. ChapterTwoStreet.cs (siklus hidup, segmen checkpoint, zona aman halte, naik bus)
    /// 2. ChapterTwoStreet.Atmosphere.cs (fog, ambient, strobe, flicker MaterialPropertyBlock, partikel)
    /// 3. ChapterTwoStreet.Audio.cs (sirene, hujan, sintesis drone horor LFO, sinkron pause audio)
    /// </summary>
    public sealed partial class ChapterTwoStreet : MonoBehaviour
    {
        // =========================================================================
        // Referensi Prefab & Properti Status
        // =========================================================================

        public const float StressPerCapture = 10f;

        public Transform[] Lamps;
        public Transform[] Checkpoints;
        public Transform[] GazePivots;
        public Transform PatrolCar;
        public Transform BusSeat;
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

        // =========================================================================
        // State Internal Simulasi, Audio, Atmosfer, & Visual
        // =========================================================================

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
        private Color savedAmbientSky;
        private Color savedAmbientEquator;
        private Color savedAmbientGround;

        // =========================================================================
        // Siklus Aktif Jalan, Masuk Pemain, & Pengaturan Encounter
        // =========================================================================

        /// <summary>
        /// Mengaktifkan prefab jalan di scene. Jika masih di dekat titik asal (0,0,0),
        /// pindahkan ke (240, 0, 180) agar terisolasi; posisi yang sudah ditempatkan di scene
        /// (seperti Chapter2.unity di x = 80) dipertahankan.
        /// </summary>
        public void Stage()
        {
            if (transform.position.sqrMagnitude < 1600f)
            {
                transform.position = new Vector3(240f, 0f, 180f);
            }
            gameObject.SetActive(true);
            beacons ??= PatrolCar != null ? PatrolCar.GetComponentsInChildren<Light>() : Array.Empty<Light>();
            InitFlickerState();
            InitSentryVisuals();
            EnsureWeatherParticles();
            Physics.SyncTransforms();
        }

        public bool Enter(FirstPersonMotor target)
        {
            if (motor != null)
            {
                return true;
            }
            Stage();
            if (!target.TryRestorePose(Checkpoints[0].position, Checkpoints[0].rotation))
            {
                return false;
            }

            motor = target;
            player = target.GetComponent<FirstPersonPlayer>();
            Segment = 0;
            AtShelter = false;
            Boarded = false;
            Bag.SetAvailable(false);
            BusDoor.Bind(this);

            ApplyHorrorAtmosphere();
            StartAmbienceAudio();

            // Pasang dan konfigurasikan encounter 2 lampu sorot polisi.
            Encounter = gameObject.AddComponent<ChapterTwoSpotlightEncounter>();
            Encounter.Configure(target, Lamps, Checkpoints, GazePivots);
            Encounter.Caught += OnCaught;
            Encounter.ConfigureBeam(0, 42f, 14f);
            Encounter.ConfigureBeam(1, 42f, 14f);

            // Atur sudut, durasi siklus, dan offset fase supaya kedua sorot menyapu jalan dengan ritme berbeda.
            Encounter.ConfigureSweep(0, 27f, 7.6f);
            Encounter.ConfigureSweep(1, 25f, 8.6f, 1.3f);

            Physics.SyncTransforms();
            if (Encounter.RenderFootprints)
            {
                Encounter.RebuildFootprints();
            }
            return true;
        }

        // Encounter menangani pemulihan posisi. Jalan menambah stres setiap kali pemain tertangkap.
        private void OnCaught(int segment)
        {
            if (player != null)
            {
                player.GetComponent<StressBarometer>()?.AddStress(StressPerCapture);
            }
        }

        // =========================================================================
        // Update Harian, Progres Segmen, & Naik Bus Akhir Bab
        // =========================================================================

        private void Update()
        {
            bool paused = player != null && player.IsPaused;
            SyncAudioPause(paused);
            if (paused)
            {
                return;
            }

            UpdatePatrolBeacons();
            UpdateFlickerLights(Time.deltaTime);
            UpdateSentryBreathing();

            if (player == null || !player.IsTransitionLocked)
            {
                CheckProgress();
            }
        }

        public void CheckProgress()
        {
            if (motor == null || AtShelter)
            {
                return;
            }
            Vector3 feet = motor.transform.position;

            // Ukur kemajuan pada sumbu Z lokal jalan. Segmen baru menjadi tujuan checkpoint berikutnya.
            if (Checkpoints != null && Segment + 1 < Checkpoints.Length && Checkpoints[Segment + 1] != null &&
                transform.InverseTransformPoint(feet).z >= transform.InverseTransformPoint(Checkpoints[Segment + 1].position).z)
            {
                Encounter.SetSegment(++Segment);
            }

            if (Shelter == null || !Shelter.bounds.Contains(feet + Vector3.up * .5f))
            {
                return;
            }

            // Pemeriksaan volume halte menandai akhir area patroli dan mematikan deteksi sorot.
            AtShelter = true;
            if (Encounter != null)
            {
                Encounter.enabled = false;
            }
            if (Bag != null)
            {
                Bag.SetAvailable(true);
            }
            SafeZoneReached?.Invoke();
        }

        internal void RequestBoard()
        {
            if (AtShelter && !Boarded)
            {
                BoardRequested?.Invoke();
            }
        }

        public bool TryBoard()
        {
            if (!AtShelter || Boarded || !motor.TryRestorePose(BusSeat.position, BusSeat.rotation))
            {
                return false;
            }
            Boarded = true;
            if (siren != null) siren.Stop();
            if (droneAudio != null) droneAudio.Stop();
            if (rainAudio != null) rainAudio.volume = 0.09f; // Redam suara hujan saat berada di dalam kabin bus.
            return true;
        }

        public bool TryRestoreCheckpoint()
        {
            if (motor == null)
            {
                return false;
            }
            if (Boarded)
            {
                return motor.TryRestorePose(BusSeat.position, BusSeat.rotation);
            }
            if (!AtShelter)
            {
                return Encounter.TryRestoreCheckpoint();
            }
            Vector3 entrance = Shelter.transform.TransformPoint(new Vector3(0, -1.01f, -.92f));
            return motor.TryRestorePose(entrance, Shelter.transform.rotation);
        }

        public void Leave()
        {
            motor = null;
            player = null;
            Segment = 0;
            AtShelter = false;
            Boarded = false;
            sirenPaused = false;
            if (siren != null) siren.Stop();
            if (rainAudio != null) rainAudio.Stop();
            if (droneAudio != null) droneAudio.Stop();
            if (Encounter != null)
            {
                Encounter.enabled = false;
                Destroy(Encounter);
                Encounter = null;
            }
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
