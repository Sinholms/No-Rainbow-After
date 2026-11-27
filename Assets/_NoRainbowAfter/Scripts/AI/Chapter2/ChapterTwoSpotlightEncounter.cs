using System.Collections.Generic;
using NoRainbowAfter.Player;
using UnityEngine;

namespace NoRainbowAfter.AI
{
    /// <summary>
    /// Pengendali utama encounter lampu sorot polisi di Chapter 2 (file inti / siklus hidup).
    ///
    /// [GRAFIKA KOMPUTER]
    /// - Menyinkronkan jam simulasi deteksi dengan visual cahaya URP Spot Light, kerucut volumetrik
    ///   (SpotlightBeam), dan proyeksi lantai (SpotlightFootprint) secara real-time.
    /// - Terpisah dari kualitas bayangan Unity sehingga gameplay tetap konsisten di Low/Medium/High.
    ///
    /// [TEKNIS KODING]
    /// - Dipecah menjadi 4 file partial class agar mudah dibaca:
    ///   1. ChapterTwoSpotlightEncounter.cs (state & loop utama)
    ///   2. ChapterTwoSpotlightEncounter.Detection.cs (sapuan rotasi, cek kerucut, raycast LOS)
    ///   3. ChapterTwoSpotlightEncounter.MeshVisuals.cs (mesh prosedural kerucut & binary search lantai)
    ///   4. ChapterTwoSpotlightEncounter.AudioAndHud.cs (sintesis audio PCM & HUD IMGUI)
    /// </summary>
    public sealed partial class ChapterTwoSpotlightEncounter : MonoBehaviour
    {
        // =========================================================================
        // Konfigurasi Publik & Properti Status
        // =========================================================================

        public bool AutoTick = true;
        public bool RenderFootprints = true;
        public bool ShowFloorFootprint = false;

        // Paparan diukur dalam detik. Dengan pengaturan awal, dua detik tersorot memicu penangkapan.
        // Saat Raka berlindung, DecayPerSecond menentukan seberapa cepat paparan yang terkumpul berkurang.
        public float CatchSeconds = 2f;
        public float DecayPerSecond = 2f;
        public float Range = 12f;
        public float ConeAngle = 52f;

        public float Exposure { get; private set; }
        public int Segment { get; private set; }
        public bool IsIlluminated { get; private set; }
        public int CatchCount { get; private set; }

        public event System.Action<int> Caught;

        // =========================================================================
        // Konstanta Simulasi, Resolusi Mesh, & Intensitas Cahaya
        // =========================================================================

        private const int FootprintSectors = 44;
        private const int FootprintRings = 22;
        private const int ConeSegments = 36;
        private const float FootprintStartRadius = 0.42f;
        private const float MaxWalkableFloorRise = 0.38f;
        private const float BaseLightIntensity = 62f;
        private const float AlertLightIntensity = 94f;

        private static readonly Color CalmBeamColor = new Color(0.84f, 0.93f, 1.0f);
        private static readonly Color AlertBeamColor = new Color(1.0f, 0.18f, 0.14f);

        // =========================================================================
        // Referensi Scene, Sumber Audio, State Waktu, & Aset HUD
        // =========================================================================

        private FirstPersonMotor motor;
        private CharacterController controller;
        private FirstPersonPlayer player;
        private Transform[] checkpoints;
        private Beam[] beams;

        private AudioSource warningSource;
        private AudioSource tensionSource;
        private AudioClip alertClip;
        private AudioClip tensionClip;
        private AudioClip catchClip;

        private float clock;
        private float visualClock;
        private float catchFlash;
        private bool warned;
        private bool wasIlluminated;

        private Texture2D hudTexture;
        private Texture2D vignetteTexture;
        private GUIStyle warningLabelStyle;

        // Struktur data per lampu polisi: menyimpan transform emitter, komponen Light URP,
        // buffer raycast (tanpa alokasi GC tiap frame), dan mesh prosedural kerucut/lantai.
        private sealed class Beam
        {
            public Transform emitter;
            public Transform gaze;
            public Transform post;
            public Quaternion restingGaze;
            public Vector3 gazeAxis;
            public float range;
            public float angle;
            public Quaternion baseRotation;
            public float yaw;
            public float period;
            public float phaseOffset;
            public bool seesPlayer;
            public float alertBlend;

            public readonly RaycastHit[] hits = new RaycastHit[64];

            public Mesh mesh;
            public readonly List<Vector3> vertices = new List<Vector3>(2048);
            public readonly List<Vector2> uvs = new List<Vector2>(2048);
            public readonly List<int> triangles = new List<int>(6144);
            public readonly List<Color> colors = new List<Color>(2048);
            public readonly Vector3[] points = new Vector3[(FootprintSectors + 1) * (FootprintRings + 1)];
            public readonly bool[] visible = new bool[(FootprintSectors + 1) * (FootprintRings + 1)];
            public Material material;
            public MeshFilter filter;

            public Mesh coneMesh;
            public Material coneMaterial;
            public MeshFilter coneFilter;
            public readonly List<Vector3> coneVertices = new List<Vector3>(512);
            public readonly List<Vector3> coneNormals = new List<Vector3>(512);
            public readonly List<Vector2> coneUvs = new List<Vector2>(512);
            public readonly List<Color> coneColors = new List<Color>(512);
            public readonly List<int> coneTriangles = new List<int>(1024);

            public Light light;
        }

        // =========================================================================
        // Inisialisasi & Konfigurasi Parameter Sorot
        // =========================================================================

        /// <summary>
        /// Menyiapkan seluruh sumber sorot polisi, komponen URP Spot Light,
        /// GameObject mesh kerucut volumetrik/lantai, serta efek suara prosedural.
        /// </summary>
        public void Configure(FirstPersonMotor playerMotor, Transform[] emitters, Transform[] segmentCheckpoints, Transform[] gazePivots = null)
        {
            motor = playerMotor;
            controller = motor != null ? motor.GetComponent<CharacterController>() : null;
            player = motor != null ? motor.GetComponent<FirstPersonPlayer>() : null;
            checkpoints = segmentCheckpoints;
            beams = new Beam[emitters.Length];

            Shader footprintShader = null;
            if (ShowFloorFootprint)
            {
                footprintShader = Resources.Load<Shader>("SpotlightFootprint");
                if (footprintShader == null)
                {
                    footprintShader = Shader.Find("Universal Render Pipeline/Unlit");
                }
            }
            var beamShader = Resources.Load<Shader>("SpotlightBeam");

            for (int i = 0; i < beams.Length; i++)
            {
                var beam = new Beam { emitter = emitters[i] };
                beams[i] = beam;
                beam.range = Range;
                beam.angle = ConeAngle;
                if (beam.emitter == null)
                {
                    continue;
                }

                beam.post = beam.emitter.parent;
                beam.baseRotation = beam.emitter.localRotation;

                // Simpan orientasi awal leher/kepala polisi (ACT_TKPSentry) dan sumbu atas lokalnya
                // agar kepala polisi ikut menoleh mengikuti sapuan senter.
                if (gazePivots != null && i < gazePivots.Length && gazePivots[i] != null)
                {
                    beam.gaze = gazePivots[i];
                    beam.restingGaze = beam.gaze.localRotation;
                    beam.gazeAxis = beam.gaze.parent != null
                        ? beam.gaze.parent.InverseTransformDirection(Vector3.up)
                        : Vector3.up;
                }

                ConfigureBeamSpotLight(beam);
                CreateBeamVisualObjects(beam, footprintShader, beamShader);
            }

            CreateAudio();
            ResetEncounter();
        }

        private void ConfigureBeamSpotLight(Beam beam)
        {
            // [GRAFIKA KOMPUTER] Spot Light real-time URP dengan Soft Shadows untuk bayangan peti & tiang di aspal.
            beam.light = beam.emitter.GetComponent<Light>();
            if (beam.light == null)
            {
                beam.light = beam.emitter.gameObject.AddComponent<Light>();
            }
            beam.light.enabled = true;
            beam.light.type = LightType.Spot;
            beam.light.color = CalmBeamColor;
            beam.light.range = Range + 2.5f;
            beam.light.spotAngle = Mathf.Min(170f, ConeAngle + 3f);
            beam.light.innerSpotAngle = ConeAngle * 0.62f;
            beam.light.intensity = BaseLightIntensity;
            beam.light.shadows = LightShadows.Soft;
            beam.light.shadowStrength = 0.94f;
            beam.light.shadowBias = 0.025f;
            beam.light.shadowNormalBias = 0.14f;
            beam.light.shadowNearPlane = 0.25f;
        }

        private void CreateBeamVisualObjects(Beam beam, Shader footprintShader, Shader beamShader)
        {
            if (ShowFloorFootprint)
            {
                var footprint = new GameObject("Spotlight floor footprint");
                footprint.transform.SetParent(transform, false);
                beam.filter = footprint.AddComponent<MeshFilter>();
                beam.mesh = new Mesh { name = "Spotlight footprint" };
                // Tandai mesh sebagai dinamis karena vertex diperbarui berulang setiap frame/interval.
                beam.mesh.MarkDynamic();
                beam.filter.sharedMesh = beam.mesh;

                var renderer = footprint.AddComponent<MeshRenderer>();
                beam.material = new Material(footprintShader);
                renderer.sharedMaterial = beam.material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            if (beamShader != null)
            {
                var coneGo = new GameObject("Spotlight volumetric cone");
                coneGo.transform.SetParent(transform, false);
                beam.coneFilter = coneGo.AddComponent<MeshFilter>();
                beam.coneMesh = new Mesh { name = "Spotlight volumetric cone" };
                beam.coneMesh.MarkDynamic();
                beam.coneFilter.sharedMesh = beam.coneMesh;

                var coneRenderer = coneGo.AddComponent<MeshRenderer>();
                beam.coneMaterial = new Material(beamShader);
                coneRenderer.sharedMaterial = beam.coneMaterial;
                coneRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                coneRenderer.receiveShadows = false;
            }
        }

        public void ConfigureBeam(int index, float fullAngleDegrees, float range)
        {
            if (beams == null || index < 0 || index >= beams.Length)
            {
                return;
            }
            var beam = beams[index];
            beam.angle = fullAngleDegrees;
            beam.range = range;
            if (beam.light != null)
            {
                beam.light.spotAngle = Mathf.Min(170f, fullAngleDegrees + 3f);
                beam.light.innerSpotAngle = fullAngleDegrees * 0.62f;
                beam.light.range = range + 2.5f;
            }
        }

        public void ConfigureSweep(int index, float yawDegrees, float periodSeconds, float phaseOffset = 0f)
        {
            if (beams == null || index < 0 || index >= beams.Length)
            {
                return;
            }
            var beam = beams[index];
            beam.yaw = yawDegrees;
            beam.period = Mathf.Max(.01f, periodSeconds);
            beam.phaseOffset = phaseOffset;
            AimFlashlights();
        }

        // Segmen baru memperbarui tujuan checkpoint. Paparan cahaya yang sedang berlangsung tetap dihitung.
        public void SetSegment(int index)
        {
            if (checkpoints == null || checkpoints.Length == 0)
            {
                return;
            }
            Segment = Mathf.Clamp(index, 0, checkpoints.Length - 1);
        }

        public void ResetEncounter()
        {
            clock = 0f;
            visualClock = .1f;
            Segment = 0;
            Exposure = 0f;
            IsIlluminated = false;
            warned = false;
            wasIlluminated = false;
            catchFlash = 0f;

            if (beams != null)
            {
                foreach (var beam in beams)
                {
                    beam.seesPlayer = false;
                    beam.alertBlend = 0f;
                }
                AimFlashlights();
            }

            if (tensionSource != null && tensionSource.isPlaying)
            {
                tensionSource.Stop();
            }
        }

        // =========================================================================
        // Siklus Update, Simulasi Paparan, & Reset Checkpoint
        // =========================================================================

        private void Update()
        {
            bool paused = player != null && (player.IsTransitionLocked || player.IsPaused);
            float ratio = CatchSeconds > 0f ? Mathf.Clamp01(Exposure / CatchSeconds) : 0f;

            if (tensionSource != null)
            {
                if (paused && tensionSource.isPlaying)
                {
                    tensionSource.Pause();
                }
                else if (!paused && ratio > 0.01f && !tensionSource.isPlaying)
                {
                    tensionSource.UnPause();
                }
            }

            if (catchFlash > 0f && !paused)
            {
                catchFlash = Mathf.Max(0f, catchFlash - Time.deltaTime * 1.4f);
            }

            // Jeda dan transisi membekukan posisi sorot sekaligus penambahan paparan.
            if (AutoTick && !paused)
            {
                ManualTick(Time.deltaTime);
            }
        }

        // Reset sapuan lampu dan paparan setelah posisi pemain berhasil dipulihkan.
        public bool TryRestoreCheckpoint()
        {
            if (motor == null || checkpoints == null || Segment >= checkpoints.Length || checkpoints[Segment] == null)
            {
                return false;
            }
            if (!motor.TryRestorePose(checkpoints[Segment].position, checkpoints[Segment].rotation))
            {
                return false;
            }

            clock = 0f;
            Exposure = 0f;
            IsIlluminated = false;
            warned = false;
            wasIlluminated = false;

            if (beams != null)
            {
                foreach (var beam in beams)
                {
                    beam.seesPlayer = false;
                    beam.alertBlend = 0f;
                }
            }

            AimFlashlights();
            Physics.SyncTransforms();
            UpdateBeamLights(1f);
            if (RenderFootprints)
            {
                RebuildFootprints();
            }

            visualClock = 0f;
            if (tensionSource != null && tensionSource.isPlaying)
            {
                tensionSource.Stop();
            }
            return true;
        }

        // Setiap lampu diuji terpisah. Paparan bertambah sekali per frame jika ada lampu yang melihat pemain.
        public void ManualTick(float dt)
        {
            if (motor == null || beams == null || dt <= 0f)
            {
                return;
            }

            clock += dt;
            AimFlashlights();
            Physics.SyncTransforms();

            IsIlluminated = false;
            foreach (var beam in beams)
            {
                if (beam.emitter == null)
                {
                    continue;
                }
                beam.seesPlayer = SeesPlayer(beam);
                if (beam.seesPlayer)
                {
                    IsIlluminated = true;
                }
            }

            if (IsIlluminated)
            {
                Exposure += dt;
                if (!warned)
                {
                    if (warningSource != null && alertClip != null)
                    {
                        warningSource.PlayOneShot(alertClip, 0.85f);
                    }
                    warned = true;
                }

                if (Exposure >= CatchSeconds)
                {
                    Exposure = 0f;
                    IsIlluminated = false;
                    warned = false;
                    catchFlash = 0.85f;
                    if (warningSource != null && catchClip != null)
                    {
                        warningSource.PlayOneShot(catchClip, 1f);
                    }

                    // Coba pindahkan pemain ke checkpoint sebelum memberi tahu ChapterTwoStreet untuk menambah stres.
                    TryRestoreCheckpoint();
                    CatchCount++;
                    Caught?.Invoke(Segment);
                }
            }
            else
            {
                // Paparan turun perlahan di luar sorot. Peringatan bisa berbunyi lagi setelah paparan habis.
                Exposure = Mathf.Max(0f, Exposure - DecayPerSecond * dt);
                if (Exposure <= 0f)
                {
                    warned = false;
                }
            }

            UpdateTensionAudio();
            UpdateBeamLights(dt);

            bool illuminationChanged = wasIlluminated != IsIlluminated;
            wasIlluminated = IsIlluminated;
            visualClock += dt;

            // Perbarui mesh langsung saat status terdeteksi berubah, atau sekitar 30 kali per detik (hemat CPU).
            if (RenderFootprints && (illuminationChanged || visualClock >= .033f))
            {
                visualClock = 0f;
                RebuildFootprints();
            }
        }

        // =========================================================================
        // Enable / Disable & Pembersihan Memori GPU/Audio
        // =========================================================================

        private void OnDisable()
        {
            if (beams != null)
            {
                foreach (var beam in beams)
                {
                    if (beam.filter != null) beam.filter.gameObject.SetActive(false);
                    if (beam.coneFilter != null) beam.coneFilter.gameObject.SetActive(false);
                    if (beam.light != null) beam.light.enabled = false;
                }
            }
            if (warningSource != null) warningSource.Stop();
            if (tensionSource != null) tensionSource.Stop();
        }

        private void OnEnable()
        {
            if (beams != null)
            {
                foreach (var beam in beams)
                {
                    if (beam.filter != null) beam.filter.gameObject.SetActive(true);
                    if (beam.coneFilter != null) beam.coneFilter.gameObject.SetActive(true);
                    if (beam.light != null) beam.light.enabled = true;
                }
            }
        }

        private void OnDestroy()
        {
            if (beams != null)
            {
                foreach (var beam in beams)
                {
                    if (beam.filter != null) Destroy(beam.filter.gameObject);
                    if (beam.mesh != null) Destroy(beam.mesh);
                    if (beam.material != null) Destroy(beam.material);
                    if (beam.coneFilter != null) Destroy(beam.coneFilter.gameObject);
                    if (beam.coneMesh != null) Destroy(beam.coneMesh);
                    if (beam.coneMaterial != null) Destroy(beam.coneMaterial);
                }
            }
            if (alertClip != null) Destroy(alertClip);
            if (tensionClip != null) Destroy(tensionClip);
            if (catchClip != null) Destroy(catchClip);
            if (hudTexture != null) Destroy(hudTexture);
            if (vignetteTexture != null) Destroy(vignetteTexture);
        }
    }
}
