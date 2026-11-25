using System.Collections.Generic;
using NoRainbowAfter.Player;
using UnityEngine;

namespace NoRainbowAfter.AI
{
    // Deteksi dan visual sorot memakai waktu simulasi yang sama. Keduanya terpisah dari pengaturan kualitas bayangan Unity.
    public sealed class ChapterTwoSpotlightEncounter : MonoBehaviour
    {
        public bool AutoTick = true;
        public bool RenderFootprints = true;
        public bool ShowFloorFootprint = false;
        public float CatchSeconds = 2f;
        public float DecayPerSecond = 2f;
        public float Range = 12f;
        public float ConeAngle = 52f;
        public float Exposure { get; private set; }
        public int Segment { get; private set; }
        public bool IsIlluminated { get; private set; }
        public int CatchCount { get; private set; }
        public event System.Action<int> Caught;

        private const int FootprintSectors = 44;
        private const int FootprintRings = 22;
        private const int ConeSegments = 36;
        private const float FootprintStartRadius = 0.42f;
        private const float MaxWalkableFloorRise = 0.38f;
        private const float BaseLightIntensity = 62f;
        private const float AlertLightIntensity = 94f;

        private static readonly Color CalmBeamColor = new Color(0.84f, 0.93f, 1.0f);
        private static readonly Color AlertBeamColor = new Color(1.0f, 0.18f, 0.14f);

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
        private float clock, visualClock;
        private float catchFlash;
        private bool warned, wasIlluminated;
        private Texture2D hudTexture;
        private Texture2D vignetteTexture;
        private GUIStyle warningLabelStyle;

        private sealed class Beam
        {
            public Transform emitter, gaze, post;
            public Quaternion restingGaze;
            public Vector3 gazeAxis;
            public float range, angle;
            public Quaternion baseRotation;
            public float yaw, period, phaseOffset;
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
                if (footprintShader == null) footprintShader = Shader.Find("Universal Render Pipeline/Unlit");
            }
            var beamShader = Resources.Load<Shader>("SpotlightBeam");

            for (int i = 0; i < beams.Length; i++)
            {
                var beam = new Beam { emitter = emitters[i] };
                beams[i] = beam;
                beam.range = Range;
                beam.angle = ConeAngle;
                if (beam.emitter == null) continue;

                beam.post = beam.emitter.parent;
                beam.baseRotation = beam.emitter.localRotation;
                if (gazePivots != null && i < gazePivots.Length && gazePivots[i] != null)
                {
                    beam.gaze = gazePivots[i];
                    beam.restingGaze = beam.gaze.localRotation;
                    beam.gazeAxis = beam.gaze.parent != null
                        ? beam.gaze.parent.InverseTransformDirection(Vector3.up)
                        : Vector3.up;
                }

                beam.light = beam.emitter.GetComponent<Light>();
                if (beam.light == null) beam.light = beam.emitter.gameObject.AddComponent<Light>();
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

                if (ShowFloorFootprint)
                {
                    var footprint = new GameObject("Spotlight floor footprint");
                    footprint.transform.SetParent(transform, false);
                    beam.filter = footprint.AddComponent<MeshFilter>();
                    beam.mesh = new Mesh { name = "Spotlight footprint" };
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

            CreateAudio();
            ResetEncounter();
        }

        private void CreateAudio()
        {
            warningSource = gameObject.AddComponent<AudioSource>();
            warningSource.playOnAwake = false;
            warningSource.spatialBlend = 0f;
            warningSource.volume = 0.36f;

            tensionSource = gameObject.AddComponent<AudioSource>();
            tensionSource.playOnAwake = false;
            tensionSource.loop = true;
            tensionSource.spatialBlend = 0f;
            tensionSource.volume = 0f;

            const int rate = 44100;
            // Bunyi peringatan menggabungkan dua nada berdekatan dengan bunyi metalik singkat.
            int alertSamples = (int)(rate * 0.32f);
            var alertData = new float[alertSamples];
            for (int i = 0; i < alertSamples; i++)
            {
                float t = (float)i / rate;
                float env = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.015f)) * Mathf.Exp(-t * 9.5f);
                float tone = Mathf.Sin(2f * Mathf.PI * 520f * t) * 0.55f
                           + Mathf.Sin(2f * Mathf.PI * 554.37f * t) * 0.45f
                           + Mathf.Sin(2f * Mathf.PI * 1040f * t) * 0.18f;
                alertData[i] = tone * env;
            }
            alertClip = AudioClip.Create("Exposure alert", alertSamples, 1, rate, false);
            alertClip.SetData(alertData, 0);

            // Detak jantung dan dengung rendah berulang selama pemain terpapar.
            int tensionSamples = rate; // 1.0s seamless loop
            var tensionData = new float[tensionSamples];
            for (int i = 0; i < tensionSamples; i++)
            {
                float t = (float)i / rate;
                float beat1 = Mathf.Exp(-Mathf.Pow((t - 0.12f) / 0.045f, 2f)) * Mathf.Sin(2f * Mathf.PI * 62f * t);
                float beat2 = Mathf.Exp(-Mathf.Pow((t - 0.30f) / 0.055f, 2f)) * Mathf.Sin(2f * Mathf.PI * 54f * t) * 0.75f;
                float hum = Mathf.Sin(2f * Mathf.PI * 92f * t) * 0.22f + Mathf.Sin(2f * Mathf.PI * 139f * t) * 0.12f;
                tensionData[i] = Mathf.Clamp((beat1 + beat2) * 0.8f + hum, -1f, 1f);
            }
            tensionClip = AudioClip.Create("Exposure tension", tensionSamples, 1, rate, false);
            tensionClip.SetData(tensionData, 0);
            tensionSource.clip = tensionClip;

            // Bunyi tertangkap menggabungkan hentakan rendah dan derau seperti radio.
            int catchSamples = (int)(rate * 0.55f);
            var catchData = new float[catchSamples];
            var rng = new System.Random(2026);
            for (int i = 0; i < catchSamples; i++)
            {
                float t = (float)i / rate;
                float env = Mathf.Exp(-t * 5.2f);
                float sub = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(145f, 44f, Mathf.Clamp01(t / 0.28f)) * t) * 0.85f;
                float screech = Mathf.Sin(2f * Mathf.PI * 880f * t) * Mathf.Exp(-t * 14f) * 0.25f;
                float grit = ((float)rng.NextDouble() * 2f - 1f) * 0.22f * Mathf.Exp(-t * 8f);
                catchData[i] = Mathf.Clamp((sub + screech + grit) * env, -1f, 1f);
            }
            catchClip = AudioClip.Create("Exposure caught", catchSamples, 1, rate, false);
            catchClip.SetData(catchData, 0);
        }

        public void ConfigureBeam(int index, float fullAngleDegrees, float range)
        {
            if (beams == null || index < 0 || index >= beams.Length) return;
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
            if (beams == null || index < 0 || index >= beams.Length) return;
            var beam = beams[index];
            beam.yaw = yawDegrees;
            beam.period = Mathf.Max(.01f, periodSeconds);
            beam.phaseOffset = phaseOffset;
            AimFlashlights();
        }

        // Segmen baru memperbarui tujuan checkpoint. Paparan cahaya yang sedang berlangsung tetap dihitung.
        public void SetSegment(int index)
        {
            if (checkpoints == null || checkpoints.Length == 0) return;
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
            if (tensionSource != null && tensionSource.isPlaying) tensionSource.Stop();
        }

        public bool IsFloorPointVisible(int beamIndex, Vector3 worldPoint)
        {
            if (beams == null || beamIndex < 0 || beamIndex >= beams.Length || beams[beamIndex].emitter == null) return false;
            return ProjectFloor(beams[beamIndex], ref worldPoint) && Visible(beams[beamIndex], worldPoint);
        }

        public void RebuildFootprints()
        {
            if (beams == null) return;
            foreach (var beam in beams)
            {
                if (beam.emitter == null) continue;
                if (ShowFloorFootprint && beam.mesh != null) RebuildFootprint(beam);
                RebuildVolumetricCone(beam);
            }
        }

        private void Update()
        {
            bool paused = player != null && (player.IsTransitionLocked || player.IsPaused);
            float ratio = CatchSeconds > 0f ? Mathf.Clamp01(Exposure / CatchSeconds) : 0f;
            if (tensionSource != null)
            {
                if (paused && tensionSource.isPlaying) tensionSource.Pause();
                else if (!paused && ratio > 0.01f && !tensionSource.isPlaying) tensionSource.UnPause();
            }
            if (catchFlash > 0f && !paused) catchFlash = Mathf.Max(0f, catchFlash - Time.deltaTime * 1.4f);
            if (AutoTick && !paused) ManualTick(Time.deltaTime);
        }

        // Reset sapuan lampu dan paparan setelah posisi pemain berhasil dipulihkan.
        public bool TryRestoreCheckpoint()
        {
            if (motor == null || checkpoints == null || Segment >= checkpoints.Length || checkpoints[Segment] == null) return false;
            if (!motor.TryRestorePose(checkpoints[Segment].position, checkpoints[Segment].rotation)) return false;
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
            if (RenderFootprints) RebuildFootprints();
            visualClock = 0f;
            if (tensionSource != null && tensionSource.isPlaying) tensionSource.Stop();
            return true;
        }

        // Setiap lampu diuji terpisah. Paparan bertambah sekali per frame jika ada lampu yang melihat pemain.
        public void ManualTick(float dt)
        {
            if (motor == null || beams == null || dt <= 0f) return;
            clock += dt;
            AimFlashlights();
            Physics.SyncTransforms();

            IsIlluminated = false;
            foreach (var beam in beams)
            {
                if (beam.emitter == null) continue;
                beam.seesPlayer = SeesPlayer(beam);
                if (beam.seesPlayer) IsIlluminated = true;
            }

            if (IsIlluminated)
            {
                Exposure += dt;
                if (!warned)
                {
                    if (warningSource != null && alertClip != null) warningSource.PlayOneShot(alertClip, 0.85f);
                    warned = true;
                }
                if (Exposure >= CatchSeconds)
                {
                    Exposure = 0f;
                    IsIlluminated = false;
                    warned = false;
                    catchFlash = 0.85f;
                    if (warningSource != null && catchClip != null) warningSource.PlayOneShot(catchClip, 1f);
                    TryRestoreCheckpoint();
                    CatchCount++;
                    Caught?.Invoke(Segment);
                }
            }
            else
            {
                // Paparan turun perlahan di luar sorot. Peringatan bisa berbunyi lagi setelah paparan habis.
                Exposure = Mathf.Max(0f, Exposure - DecayPerSecond * dt);
                if (Exposure <= 0f) warned = false;
            }

            UpdateTensionAudio();
            UpdateBeamLights(dt);

            bool illuminationChanged = wasIlluminated != IsIlluminated;
            wasIlluminated = IsIlluminated;
            visualClock += dt;
            if (RenderFootprints && (illuminationChanged || visualClock >= .033f))
            {
                visualClock = 0f;
                RebuildFootprints();
            }
        }

        private void UpdateTensionAudio()
        {
            if (tensionSource == null || tensionClip == null) return;
            float ratio = CatchSeconds > 0f ? Mathf.Clamp01(Exposure / CatchSeconds) : 0f;
            if (ratio > 0.01f)
            {
                tensionSource.volume = Mathf.Lerp(0.08f, 0.55f, ratio);
                tensionSource.pitch = Mathf.Lerp(0.95f, 1.48f, ratio);
                if (!tensionSource.isPlaying) tensionSource.Play();
            }
            else if (tensionSource.isPlaying)
            {
                tensionSource.Stop();
            }
        }

        private void UpdateBeamLights(float dt)
        {
            if (beams == null) return;
            float exposureRatio = CatchSeconds > 0f ? Mathf.Clamp01(Exposure / CatchSeconds) : 0f;
            foreach (var beam in beams)
            {
                if (beam.light == null) continue;
                float target = beam.seesPlayer ? Mathf.Max(0.35f, exposureRatio) : 0f;
                beam.alertBlend = Mathf.MoveTowards(beam.alertBlend, target, dt * 6f);
                beam.light.color = Color.Lerp(CalmBeamColor, AlertBeamColor, beam.alertBlend);
                beam.light.intensity = Mathf.Lerp(BaseLightIntensity, AlertLightIntensity, beam.alertBlend);
            }
        }

        private void AimFlashlights()
        {
            foreach (var beam in beams)
            {
                if (beam.emitter == null) continue;
                float rawPhase = beam.period > 0f
                    ? Mathf.PingPong((clock + beam.phaseOffset) / beam.period * 2f, 1f)
                    : .5f;
                // SmoothStep memperlambat sapuan di kedua ujung sebelum lampu berbalik arah.
                float phase = Mathf.SmoothStep(0f, 1f, rawPhase);
                float yaw = (phase * 2f - 1f) * beam.yaw;
                // Kalikan rotasi sapuan pada rotasi dasar agar lampu berputar di sumbu vertikal tiang.
                beam.emitter.localRotation = Quaternion.Euler(0f, yaw, 0f) * beam.baseRotation;
                if (beam.gaze != null)
                    beam.gaze.localRotation = Quaternion.AngleAxis(yaw * .82f, beam.gazeAxis) * beam.restingGaze;
            }
        }

        private bool SeesPlayer(Beam beam)
        {
            if (controller == null) controller = motor.GetComponent<CharacterController>();
            if (controller == null) return false;
            Vector3 bottom = motor.transform.TransformPoint(controller.center - Vector3.up * controller.height * .5f);
            float crouchBlend = motor.CrouchBlend;
            // Jongkok menurunkan titik uji kepala dan dada. Satu titik yang terlihat sudah cukup untuk mendeteksi pemain.
            float torsoHeight = controller.height * Mathf.Lerp(.55f, .46f, crouchBlend);
            float headHeight = controller.height * Mathf.Lerp(.90f, .74f, crouchBlend);
            return Visible(beam, bottom + Vector3.up * torsoHeight) ||
                   Visible(beam, bottom + Vector3.up * headHeight);
        }

        // Titik harus berada dalam sudut dan jarak horizontal lampu serta memiliki garis pandang tanpa penghalang.
        private bool Visible(Beam beam, Vector3 point)
        {
            Vector3 origin = beam.emitter.position;
            Vector3 delta = point - origin;
            float distance = delta.magnitude;
            if (distance < .001f) return false;
            Vector3 forward = Vector3.ProjectOnPlane(beam.emitter.forward, Vector3.up);
            if (forward.sqrMagnitude < .0001f) return false;
            forward.Normalize();
            Vector3 planar = Vector3.ProjectOnPlane(delta, Vector3.up);
            float planarDistance = planar.magnitude;
            if (planarDistance > beam.range + .01f || planarDistance < .001f ||
                Vector3.Angle(forward, planar) > beam.angle * .5f + .05f) return false;

            int hitCount = Physics.RaycastNonAlloc(origin, delta / distance, beam.hits, distance - 0.02f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                var hit = beam.hits[i];
                if (IsIgnoredObstacle(beam, hit)) continue;
                return false;
            }
            return hitCount < beam.hits.Length;
        }

        private bool IsIgnoredObstacle(Beam beam, RaycastHit hit)
        {
            Transform hitTransform = hit.collider.transform;
            if (motor != null && hitTransform.IsChildOf(motor.transform)) return true;
            if (hitTransform.IsChildOf(beam.emitter)) return true;
            // Abaikan badan penjaga yang berada di dekat lensa agar lampunya tidak langsung terhalang.
            if (beam.post != null && hitTransform == beam.post && hit.distance < 0.75f) return true;
            return false;
        }

        private void RebuildFootprint(Beam beam)
        {
            var verts = beam.vertices;
            var uvs = beam.uvs;
            var triangles = beam.triangles;
            var colors = beam.colors;
            verts.Clear();
            uvs.Clear();
            triangles.Clear();
            colors.Clear();

            Vector3 origin = beam.emitter.position;
            Vector3 forward = Vector3.ProjectOnPlane(beam.emitter.forward, Vector3.up);
            if (forward.sqrMagnitude < .0001f) return;
            forward.Normalize();

            float streetY = transform.position.y;
            const int stride = FootprintRings + 1;

            for (int x = 0; x <= FootprintSectors; x++)
            {
                float angle = -beam.angle * .5f + beam.angle * x / FootprintSectors;
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
                for (int z = 0; z <= FootprintRings; z++)
                {
                    int i = x * stride + z;
                    float radius = Mathf.Lerp(FootprintStartRadius, beam.range, (float)z / FootprintRings);
                    Vector3 point = origin + direction * radius;
                    point.y = streetY + .025f;
                    beam.visible[i] = ProjectFloor(beam, ref point) && Visible(beam, point);
                    beam.points[i] = point;
                }
            }

            // Perinci batas bayangan agar jejak cahaya mengikuti tepi peti dan trotoar.
            for (int x = 0; x < FootprintSectors; x++)
            {
                float angleA = -beam.angle * .5f + beam.angle * x / FootprintSectors;
                float angleB = -beam.angle * .5f + beam.angle * (x + 1) / FootprintSectors;
                Vector3 dirA = Quaternion.AngleAxis(angleA, Vector3.up) * forward;
                Vector3 dirB = Quaternion.AngleAxis(angleB, Vector3.up) * forward;

                for (int z = 0; z < FootprintRings; z++)
                {
                    int ia = x * stride + z;
                    int ib = (x + 1) * stride + z;
                    int ic = ia + 1;
                    int id = ib + 1;

                    bool va = beam.visible[ia], vb = beam.visible[ib], vc = beam.visible[ic], vd = beam.visible[id];
                    if (!va || !vb) continue;

                    Vector3 a = beam.points[ia];
                    Vector3 b = beam.points[ib];
                    Vector3 c = beam.points[ic];
                    Vector3 d = beam.points[id];
                    float alongFar = (float)(z + 1) / FootprintRings;

                    if (!vc || !vd)
                    {
                        // Gunakan pencarian biner di sepanjang sinar untuk memperkirakan awal bayangan.
                        float rNear = Mathf.Lerp(FootprintStartRadius, beam.range, (float)z / FootprintRings);
                        float rFar = Mathf.Lerp(FootprintStartRadius, beam.range, alongFar);
                        if (!vc)
                        {
                            float rHit = RefineFloorEdge(beam, origin, dirA, streetY, rNear, rFar, out c);
                            if (rHit <= rNear + 0.01f) continue;
                        }
                        if (!vd)
                        {
                            float rHit = RefineFloorEdge(beam, origin, dirB, streetY, rNear, rFar, out d);
                            if (rHit <= rNear + 0.01f) continue;
                        }
                    }

                    float minY = Mathf.Min(Mathf.Min(a.y, b.y), Mathf.Min(c.y, d.y));
                    float maxY = Mathf.Max(Mathf.Max(a.y, b.y), Mathf.Max(c.y, d.y));
                    if (maxY - minY > .16f) continue;

                    float acrossA = (float)x / FootprintSectors;
                    float acrossB = (float)(x + 1) / FootprintSectors;
                    float alongNear = (float)z / FootprintRings;

                    int n = verts.Count;
                    verts.Add(transform.InverseTransformPoint(a));
                    verts.Add(transform.InverseTransformPoint(b));
                    verts.Add(transform.InverseTransformPoint(c));
                    verts.Add(transform.InverseTransformPoint(d));

                    uvs.Add(new Vector2(acrossA, alongNear));
                    uvs.Add(new Vector2(acrossB, alongNear));
                    uvs.Add(new Vector2(acrossA, alongFar));
                    uvs.Add(new Vector2(acrossB, alongFar));

                    colors.Add(FootprintColor(beam, a, acrossA, alongNear));
                    colors.Add(FootprintColor(beam, b, acrossB, alongNear));
                    colors.Add(FootprintColor(beam, c, acrossA, alongFar));
                    colors.Add(FootprintColor(beam, d, acrossB, alongFar));

                    triangles.Add(n); triangles.Add(n + 2); triangles.Add(n + 1);
                    triangles.Add(n + 1); triangles.Add(n + 2); triangles.Add(n + 3);
                }
            }

            beam.mesh.Clear();
            beam.mesh.SetVertices(verts);
            beam.mesh.SetUVs(0, uvs);
            beam.mesh.SetColors(colors);
            beam.mesh.SetTriangles(triangles, 0);
            beam.mesh.RecalculateBounds();
        }

        private float RefineFloorEdge(Beam beam, Vector3 origin, Vector3 direction, float streetY, float rMin, float rMax, out Vector3 edgePoint)
        {
            float lo = rMin;
            float hi = rMax;
            edgePoint = origin + direction * rMin;
            edgePoint.y = streetY + .025f;
            ProjectFloor(beam, ref edgePoint);

            for (int iter = 0; iter < 5; iter++)
            {
                float mid = (lo + hi) * 0.5f;
                Vector3 candidate = origin + direction * mid;
                candidate.y = streetY + .025f;
                if (ProjectFloor(beam, ref candidate) && Visible(beam, candidate))
                {
                    lo = mid;
                    edgePoint = candidate;
                }
                else
                {
                    hi = mid;
                }
            }
            return lo;
        }

        private void RebuildVolumetricCone(Beam beam)
        {
            if (beam.coneMesh == null || beam.emitter == null) return;
            var verts = beam.coneVertices;
            var norms = beam.coneNormals;
            var uvs = beam.coneUvs;
            var cols = beam.coneColors;
            var tris = beam.coneTriangles;
            verts.Clear();
            norms.Clear();
            uvs.Clear();
            cols.Clear();
            tris.Clear();

            Vector3 origin = beam.emitter.position;
            Vector3 fwd = beam.emitter.forward;
            Vector3 right = beam.emitter.right;
            Vector3 up = beam.emitter.up;

            // Pangkas kerucut cahaya luar dan inti yang lebih terang pada peti dan dinding.
            AppendConeShell(beam, origin, fwd, right, up, beam.angle * 0.49f, 0.055f, 0.18f);
            AppendConeShell(beam, origin, fwd, right, up, beam.angle * 0.27f, 0.032f, 0.23f);

            beam.coneMesh.Clear();
            beam.coneMesh.SetVertices(verts);
            beam.coneMesh.SetNormals(norms);
            beam.coneMesh.SetUVs(0, uvs);
            beam.coneMesh.SetColors(cols);
            beam.coneMesh.SetTriangles(tris, 0);
            beam.coneMesh.RecalculateBounds();
        }

        private void AppendConeShell(Beam beam, Vector3 origin, Vector3 fwd, Vector3 right, Vector3 up,
            float halfAngleDeg, float startRadius, float baseAlpha)
        {
            var verts = beam.coneVertices;
            var norms = beam.coneNormals;
            var uvs = beam.coneUvs;
            var cols = beam.coneColors;
            var tris = beam.coneTriangles;

            float tanHalf = Mathf.Tan(halfAngleDeg * Mathf.Deg2Rad);
            Color beamTint = Color.Lerp(CalmBeamColor, AlertBeamColor, beam.alertBlend);
            float alphaBoost = Mathf.Lerp(1f, 1.65f, beam.alertBlend);
            int baseIndex = verts.Count;

            for (int i = 0; i <= ConeSegments; i++)
            {
                float u = (float)i / ConeSegments;
                float theta = u * Mathf.PI * 2f;
                Vector3 radial = Mathf.Cos(theta) * right + Mathf.Sin(theta) * up;
                Vector3 rayDir = (fwd + radial * tanHalf).normalized;

                float hitDist = beam.range;
                int hitCount = Physics.RaycastNonAlloc(origin, rayDir, beam.hits, beam.range, ~0, QueryTriggerInteraction.Ignore);
                for (int h = 0; h < hitCount; h++)
                {
                    var hit = beam.hits[h];
                    if (IsIgnoredObstacle(beam, hit)) continue;
                    if (hit.distance < hitDist) hitDist = hit.distance;
                }
                hitDist = Mathf.Max(0.25f, hitDist);

                Vector3 pStart = origin + fwd * 0.05f + radial * startRadius;
                Vector3 pEnd = origin + rayDir * hitDist;
                Vector3 localNormal = transform.InverseTransformDirection(radial);

                float endRatio = Mathf.Clamp01(hitDist / Mathf.Max(0.01f, beam.range));
                Color cStart = beamTint;
                cStart.a = baseAlpha * alphaBoost;
                Color cEnd = beamTint;
                cEnd.a = baseAlpha * alphaBoost * Mathf.Lerp(0.45f, 0.0f, endRatio);

                verts.Add(transform.InverseTransformPoint(pStart));
                norms.Add(localNormal);
                uvs.Add(new Vector2(u, 0f));
                cols.Add(cStart);

                verts.Add(transform.InverseTransformPoint(pEnd));
                norms.Add(localNormal);
                uvs.Add(new Vector2(u, endRatio));
                cols.Add(cEnd);
            }

            for (int i = 0; i < ConeSegments; i++)
            {
                int a = baseIndex + i * 2;
                int b = a + 1;
                int c = a + 2;
                int d = a + 3;
                tris.Add(a); tris.Add(b); tris.Add(c);
                tris.Add(c); tris.Add(b); tris.Add(d);
            }
        }

        private Color FootprintColor(Beam beam, Vector3 point, float across, float along)
        {
            const float fadeWidth = .22f, minFade = .05f;
            float edge = Mathf.SmoothStep(minFade, 1f, Mathf.Clamp01(Mathf.Min(across, 1f - across) / fadeWidth));
            float end = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - along) / .24f));
            float nearFade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(along / .10f));
            bool alertSpot = beam.seesPlayer && motor != null && Vector3.Distance(point, motor.transform.position) < 2.8f;
            Color color = Color.Lerp(CalmBeamColor, AlertBeamColor, alertSpot ? 1f : beam.alertBlend * 0.90f);
            float baseAlpha = Mathf.Lerp(.24f, .44f, alertSpot ? 1f : beam.alertBlend);
            color.a = baseAlpha * edge * end * nearFade;
            return color;
        }

        private bool ProjectFloor(Beam beam, ref Vector3 point)
        {
            float streetY = transform.position.y;
            float probeTop = Mathf.Max(point.y + 0.45f, streetY + 0.65f);
            float probeDistance = Mathf.Max(1.6f, probeTop - (streetY - 0.8f));
            Vector3 probeStart = new Vector3(point.x, probeTop, point.z);
            int hitCount = Physics.RaycastNonAlloc(probeStart, Vector3.down, beam.hits, probeDistance, ~0, QueryTriggerInteraction.Ignore);
            float bestY = float.NegativeInfinity;
            RaycastHit best = default;
            bool found = false;
            for (int i = 0; i < hitCount; i++)
            {
                var hit = beam.hits[i];
                if (IsIgnoredObstacle(beam, hit)) continue;
                if (hit.point.y > bestY)
                {
                    bestY = hit.point.y;
                    best = hit;
                    found = true;
                }
            }
            if (!found || best.normal.y < .7f || best.point.y > streetY + MaxWalkableFloorRise) return false;
            point.y = best.point.y + .022f;
            return true;
        }

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

        private void EnsureHudAssets()
        {
            if (hudTexture == null)
            {
                hudTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                hudTexture.SetPixel(0, 0, Color.white);
                hudTexture.Apply();
            }
            if (vignetteTexture == null)
            {
                const int size = 64;
                vignetteTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                vignetteTexture.wrapMode = TextureWrapMode.Clamp;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float nx = (x + 0.5f) / size * 2f - 1f;
                        float ny = (y + 0.5f) / size * 2f - 1f;
                        float d = Mathf.Sqrt(nx * nx + ny * ny);
                        float a = Mathf.SmoothStep(0.45f, 1.32f, d);
                        vignetteTexture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                }
                vignetteTexture.Apply();
            }
            if (warningLabelStyle == null)
            {
                warningLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 15,
                    fontStyle = FontStyle.Bold
                };
            }
        }

        private void OnGUI()
        {
            if (motor == null) return;
            if (player != null && (player.IsPaused || player.IsTransitionLocked)) return;
            float ratio = CatchSeconds > 0f ? Mathf.Clamp01(Exposure / CatchSeconds) : 0f;
            if (ratio <= 0.001f && catchFlash <= 0.001f) return;

            EnsureHudAssets();

            // Efek merah menguat saat paparan naik dan berkedip saat pemain tertangkap.
            float pulse = 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * Mathf.Lerp(6f, 18f, ratio));
            float edgeAlpha = Mathf.Clamp01(ratio * 0.62f * pulse + catchFlash * 0.72f);
            if (edgeAlpha > 0.01f)
            {
                GUI.color = new Color(0.55f, 0.03f, 0.04f, edgeAlpha);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), vignetteTexture);
            }

            if (catchFlash > 0.05f)
            {
                GUI.color = new Color(0.05f, 0.0f, 0.0f, catchFlash * 0.55f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), hudTexture);
            }

            if (ratio > 0.001f)
            {
                float barWidth = Mathf.Clamp(Screen.width * 0.24f, 240f, 420f);
                float barHeight = 14f;
                float x = (Screen.width - barWidth) * 0.5f;
                float y = Screen.height * 0.84f;

                // Judul indikator paparan.
                warningLabelStyle.normal.textColor = new Color(1f, Mathf.Lerp(0.82f, 0.28f, ratio), Mathf.Lerp(0.65f, 0.24f, ratio), Mathf.Clamp01(ratio * 3f));
                GUI.color = Color.white;
                GUI.Label(new Rect(x - 40f, y - 28f, barWidth + 80f, 24f),
                    ratio > 0.65f ? "TERDETEKSI — CARI PERLINDUNGAN!" : "TERSOROT SENTER",
                    warningLabelStyle);

                // Bingkai dan latar indikator.
                GUI.color = new Color(0.04f, 0.05f, 0.07f, 0.88f);
                GUI.DrawTexture(new Rect(x - 3f, y - 3f, barWidth + 6f, barHeight + 6f), hudTexture);
                GUI.color = new Color(0.32f, 0.10f, 0.10f, 0.65f);
                GUI.DrawTexture(new Rect(x - 1f, y - 1f, barWidth + 2f, barHeight + 2f), hudTexture);
                GUI.color = new Color(0.08f, 0.08f, 0.10f, 0.92f);
                GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), hudTexture);

                // Isi indikator mengikuti rasio paparan.
                GUI.color = Color.Lerp(new Color(0.95f, 0.52f, 0.14f, 0.95f), new Color(1f, 0.12f, 0.10f, 0.98f), ratio) * pulse;
                GUI.DrawTexture(new Rect(x, y, barWidth * ratio, barHeight), hudTexture);
            }
            GUI.color = Color.white;
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
