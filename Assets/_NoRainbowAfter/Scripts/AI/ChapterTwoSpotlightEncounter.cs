using System.Collections.Generic;
using NoRainbowAfter.Player;
using UnityEngine;

namespace NoRainbowAfter.AI
{
    // Encounter clock owns both sensing and its visualisation, independent of light/shadow quality settings.
    public sealed class ChapterTwoSpotlightEncounter : MonoBehaviour
    {
        public bool AutoTick = true;
        public bool RenderFootprints = true;
        public float CatchSeconds = 2f;
        public float DecayPerSecond = 2f;
        public float Range = 12f;
        public float ConeAngle = 52f;
        public float Exposure { get; private set; }
        public int Segment { get; private set; }
        public bool IsIlluminated { get; private set; }
        public int CatchCount { get; private set; }
        public event System.Action<int> Caught;

        private FirstPersonMotor motor;
        private Transform[] checkpoints;
        private Beam[] beams;
        private AudioSource warning;
        private AudioClip warningClip;
        private float clock, visualClock;
        private bool warned, wasIlluminated;
        private Texture2D hudTexture;

        private sealed class Beam
        {
            public Transform emitter;
            public float range, angle;
            public Quaternion baseRotation;
            public float yaw, period, phaseOffset;
            public readonly RaycastHit[] hits = new RaycastHit[32];
            public Mesh mesh;
            public readonly List<Vector3> vertices = new List<Vector3>();
            public readonly List<int> triangles = new List<int>();
            public readonly List<Color> colors = new List<Color>();
            public readonly Vector3[] points = new Vector3[33 * 19];
            public readonly bool[] visible = new bool[33 * 19];
            public Material material;
            public MeshFilter filter;
            public Light light;
        }

        public void Configure(FirstPersonMotor player, Transform[] emitters, Transform[] segmentCheckpoints)
        {
            motor = player;
            checkpoints = segmentCheckpoints;
            beams = new Beam[emitters.Length];
            for (int i = 0; i < beams.Length; i++)
            {
                var beam = new Beam { emitter = emitters[i] };
                beams[i] = beam; beam.range = Range; beam.angle = ConeAngle;
                if (beam.emitter == null) continue;
                beam.baseRotation = beam.emitter.localRotation;
                beam.light = beam.emitter.GetComponent<Light>();
                if (beam.light == null) beam.light = beam.emitter.gameObject.AddComponent<Light>();
                beam.light.enabled = true; beam.light.type = LightType.Spot; beam.light.color = Color.white;
                beam.light.range = Range; beam.light.spotAngle = ConeAngle; beam.light.intensity = 2f;
                beam.light.shadows = LightShadows.Soft;
                var footprint = new GameObject("Spotlight floor footprint");
                footprint.transform.SetParent(transform, false);
                beam.filter = footprint.AddComponent<MeshFilter>();
                beam.mesh = new Mesh { name = "Spotlight footprint" };
                beam.mesh.MarkDynamic(); beam.filter.sharedMesh = beam.mesh;
                var renderer = footprint.AddComponent<MeshRenderer>();
                var shader = Resources.Load<Shader>("SpotlightFootprint");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                beam.material = new Material(shader);

                renderer.sharedMaterial = beam.material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            warning = gameObject.AddComponent<AudioSource>();
            warning.playOnAwake = false; warning.spatialBlend = 0f; warning.volume = .25f;
            const int samples = 4410;
            var data = new float[samples];
            for (int i = 0; i < samples; i++) data[i] = Mathf.Sin(i * 2f * Mathf.PI * 660f / 44100f) * (1f - (float)i / samples);
            warningClip = AudioClip.Create("Exposure warning", samples, 1, 44100, false);
            warningClip.SetData(data, 0); warning.clip = warningClip;
            ResetEncounter();
        }

        public void ConfigureBeam(int index, float fullAngleDegrees, float range)
        {
            var beam = beams[index]; beam.angle = fullAngleDegrees; beam.range = range;
            beam.light.spotAngle = fullAngleDegrees; beam.light.range = range;
        }

        public void ConfigureSweep(int index, float yawDegrees, float periodSeconds, float phaseOffset = 0f)
        {
            var beam = beams[index]; beam.yaw = yawDegrees; beam.period = Mathf.Max(.01f, periodSeconds); beam.phaseOffset = phaseOffset;
        }

        public void SetSegment(int index) { Segment = Mathf.Clamp(index, 0, checkpoints.Length - 1); Exposure = 0; warned = false; }
        public void ResetEncounter() { clock = 0f; visualClock = .1f; Segment = 0; Exposure = 0f; IsIlluminated = false; warned = false; wasIlluminated = false; }
        public bool IsFloorPointVisible(int beamIndex, Vector3 worldPoint)
        {
            if (beams == null || beamIndex < 0 || beamIndex >= beams.Length || beams[beamIndex].emitter == null) return false;
            return ProjectFloor(beams[beamIndex], ref worldPoint) && Visible(beams[beamIndex], worldPoint);
        }
        public void RebuildFootprints()
        {
            if (beams == null) return;
            foreach (var beam in beams) if (beam.emitter != null) RebuildFootprint(beam);
        }
        private void Update()
        {
            var player = motor != null ? motor.GetComponent<FirstPersonPlayer>() : null;
            if (AutoTick && (player == null || (!player.IsTransitionLocked && !player.IsPaused))) ManualTick(Time.deltaTime);
        }

        public bool TryRestoreCheckpoint()
        {
            if (motor == null || checkpoints == null || Segment >= checkpoints.Length || checkpoints[Segment] == null) return false;
            if (!motor.TryRestorePose(checkpoints[Segment].position, checkpoints[Segment].rotation)) return false;
            clock = 0f; visualClock = .1f; Exposure = 0f; IsIlluminated = false; warned = false; wasIlluminated = false;
            AimFlashlights();
            Physics.SyncTransforms();
            if (RenderFootprints) RebuildFootprints();
            if (warning != null) warning.Stop();
            return true;
        }

        public void ManualTick(float dt)
        {
            if (motor == null || beams == null || dt <= 0f) return;
            clock += dt;
            AimFlashlights();
            Physics.SyncTransforms();
            IsIlluminated = false;
            foreach (var beam in beams) if (beam.emitter != null && SeesPlayer(beam)) { IsIlluminated = true; break; }
            if (IsIlluminated)
            {
                Exposure += dt;
                if (!warned) { warning.Play(); warned = true; }
                if (Exposure >= CatchSeconds)
                {
                    Exposure = 0f; IsIlluminated = false; warned = false;
                    TryRestoreCheckpoint();
                    CatchCount++; Caught?.Invoke(Segment);
                }
            }
            else { Exposure = Mathf.Max(0f, Exposure - DecayPerSecond * dt); if (Exposure <= 0f) warned = false; }
            bool illuminationChanged = wasIlluminated != IsIlluminated;
            wasIlluminated = IsIlluminated;
            visualClock += dt;
            if (RenderFootprints && (illuminationChanged || visualClock >= .04f))
            {
                visualClock = 0f;
                RebuildFootprints();
            }
        }

        private void AimFlashlights()
        {
            foreach (var beam in beams)
            {
                if (beam.emitter == null) continue;
                float phase = beam.period > 0f
                    ? Mathf.PingPong((clock + beam.phaseOffset) / beam.period * 2f, 1f)
                    : .5f;
                beam.emitter.localRotation = beam.baseRotation * Quaternion.Euler(0f, (phase * 2f - 1f) * beam.yaw, 0f);
            }
        }

        private bool SeesPlayer(Beam beam)
        {
            var controller = motor.GetComponent<CharacterController>();
            Vector3 bottom = motor.transform.TransformPoint(controller.center - Vector3.up * controller.height * .5f);
            // Torso and head distinguish crouching behind low cover from standing above it.
            return Visible(beam, bottom + Vector3.up * (controller.height * .55f)) ||
                   Visible(beam, bottom + Vector3.up * (controller.height * .9f));
        }

        private bool Visible(Beam beam, Vector3 point)
        {
            Vector3 delta = point - beam.emitter.position;
            float distance = delta.magnitude;
            if (distance < .001f) return false;
            Vector3 forward = Vector3.ProjectOnPlane(beam.emitter.forward, Vector3.up).normalized;
            Vector3 planar = Vector3.ProjectOnPlane(delta, Vector3.up);
            float planarDistance = planar.magnitude;
            if (planarDistance > beam.range + .01f || planarDistance < .001f ||
                Vector3.Angle(forward, planar) > beam.angle * .5f + .05f) return false;
            int hitCount = Physics.RaycastNonAlloc(beam.emitter.position, delta / distance, beam.hits, distance, ~0, QueryTriggerInteraction.Ignore);
            if (hitCount >= beam.hits.Length) return false;
            for (int i = 0; i < hitCount; i++)
            {
                var hit = beam.hits[i];
                if (hit.collider.transform.IsChildOf(motor.transform) || hit.collider.transform.IsChildOf(beam.emitter)) continue;
                return false;
            }
            return true;
        }

        private void RebuildFootprint(Beam beam)
        {
            // Polar strips align the mesh edges with the actual cone rather than world axes.
            const int sectors = 32, rings = 18;
            var verts = beam.vertices; var triangles = beam.triangles; var colors = beam.colors; verts.Clear(); triangles.Clear(); colors.Clear();
            Vector3 origin = beam.emitter.position;
            Vector3 forward = Vector3.ProjectOnPlane(beam.emitter.forward, Vector3.up).normalized;
            float floorY = motor.transform.position.y + .025f;
            for (int x = 0; x <= sectors; x++)
            {
                float angle = -beam.angle * .5f + beam.angle * x / sectors;
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
                for (int z = 0; z <= rings; z++)
                {
                    int i = x * (rings + 1) + z;
                    float radius = Mathf.Lerp(.35f, beam.range, (float)z / rings);
                    Vector3 point = origin + direction * radius; point.y = floorY;
                    beam.visible[i] = ProjectFloor(beam, ref point) && Visible(beam, point);
                    beam.points[i] = point;
                }
            }
            for (int x = 0; x < sectors; x++) for (int z = 0; z < rings; z++)
            {
                int ia = x * (rings + 1) + z, ib = (x + 1) * (rings + 1) + z;
                int ic = ia + 1, id = ib + 1;
                if (!beam.visible[ia] || !beam.visible[ib] || !beam.visible[ic] || !beam.visible[id]) continue;
                Vector3 a = beam.points[ia], b = beam.points[ib], c = beam.points[ic], d = beam.points[id];
                float minY = Mathf.Min(Mathf.Min(a.y, b.y), Mathf.Min(c.y, d.y));
                float maxY = Mathf.Max(Mathf.Max(a.y, b.y), Mathf.Max(c.y, d.y));
                if (maxY - minY > .06f) continue;
                int n = verts.Count;
                verts.Add(transform.InverseTransformPoint(a)); verts.Add(transform.InverseTransformPoint(b)); verts.Add(transform.InverseTransformPoint(c)); verts.Add(transform.InverseTransformPoint(d));
                colors.Add(FootprintColor(a, x, z, sectors, rings)); colors.Add(FootprintColor(b, x + 1, z, sectors, rings));
                colors.Add(FootprintColor(c, x, z + 1, sectors, rings)); colors.Add(FootprintColor(d, x + 1, z + 1, sectors, rings));
                triangles.Add(n); triangles.Add(n + 2); triangles.Add(n + 1); triangles.Add(n + 1); triangles.Add(n + 2); triangles.Add(n + 3);
            }
            beam.mesh.Clear(); beam.mesh.SetVertices(verts); beam.mesh.SetTriangles(triangles, 0); beam.mesh.SetColors(colors); beam.mesh.RecalculateBounds();
        }

        private Color FootprintColor(Vector3 point, int sector, int ring, int sectors, int rings)
        {
            float across = (float)sector / sectors, along = (float)ring / rings;
            const float fadeWidth = .12f, minFade = .45f;
            float edge = Mathf.SmoothStep(minFade, 1f, Mathf.Clamp01(Mathf.Min(across, 1f - across) / fadeWidth));
            float end = Mathf.SmoothStep(minFade, 1f, Mathf.Clamp01((1f - along) / fadeWidth));
            bool nearPlayer = IsIlluminated && Vector3.Distance(point, motor.transform.position) < 1.8f;
            Color color = nearPlayer ? new Color(1f, .14f, .14f) : Color.white;
            color.a = (nearPlayer ? .32f : .22f) * edge * end;
            return color;
        }

        private bool ProjectFloor(Beam beam, ref Vector3 point)
        {
            int hitCount = Physics.RaycastNonAlloc(point + Vector3.up * .45f, Vector3.down, beam.hits, 1.5f, ~0, QueryTriggerInteraction.Ignore);
            float bestY = float.NegativeInfinity;
            RaycastHit best = default;
            bool found = false;
            for (int i = 0; i < hitCount; i++)
            {
                var hit = beam.hits[i];
                if (hit.collider.transform.IsChildOf(motor.transform) || hit.collider.transform.IsChildOf(beam.emitter)) continue;
                if (hit.point.y > bestY)
                {
                    bestY = hit.point.y;
                    best = hit;
                    found = true;
                }
            }
            if (!found || best.normal.y < .7f || best.point.y > transform.position.y + .05f) return false;
            point.y = best.point.y + .025f;
            return true;
        }

        private void OnDisable()
        {
            if (beams != null) foreach (var beam in beams) { if (beam.filter != null) beam.filter.gameObject.SetActive(false); if (beam.light != null) beam.light.enabled = false; }
            if (warning != null) warning.Stop();
        }
        private void OnEnable()
        {
            if (beams != null) foreach (var beam in beams) { if (beam.filter != null) beam.filter.gameObject.SetActive(true); if (beam.light != null) beam.light.enabled = true; }
        }
        private void OnGUI()
        {
            if (motor == null || Exposure <= 0f) return;
            if (hudTexture == null) { hudTexture = new Texture2D(1, 1); hudTexture.SetPixel(0, 0, Color.white); hudTexture.Apply(); }
            var rect = new Rect(Screen.width * .4f, Screen.height * .84f, Screen.width * .2f, 12f);
            GUI.color = new Color(.12f, .12f, .12f, .85f); GUI.DrawTexture(rect, hudTexture);
            GUI.color = new Color(1f, .2f, .2f, .95f); rect.width *= Mathf.Clamp01(Exposure / CatchSeconds); GUI.DrawTexture(rect, hudTexture); GUI.color = Color.white;
        }

        private void OnDestroy()
        {
            if (beams != null) foreach (var beam in beams) { if (beam.filter != null) Destroy(beam.filter.gameObject); if (beam.mesh != null) Destroy(beam.mesh); if (beam.material != null) Destroy(beam.material); }
            if (warningClip != null) Destroy(warningClip); if (hudTexture != null) Destroy(hudTexture);
        }
    }
}
