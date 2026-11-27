using UnityEngine;

namespace NoRainbowAfter.AI
{
    /// <summary>
    /// Generator mesh prosedural untuk kerucut cahaya volumetrik dan jejak cahaya di aspal.
    ///
    /// [GRAFIKA KOMPUTER — INTI MATERI UAS CHAPTER 2]
    /// 1. Volumetric Cone Mesh Ganda (Dual-Shell Additive Mesh):
    ///    - AppendConeShell menembakkan 36 sinar melingkar (ConeSegments) dari posisi lensa senter.
    ///    - Tiap sinar berhenti di penghalang terdekat (peti/tembok) melalui Physics.RaycastNonAlloc,
    ///      sehingga kerucut cahaya otomatis terpotong realistis saat menabrak rintangan!
    ///    - Dibangun dua lapis: kulit luar yang lebar + inti dalam yang lebih terang.
    ///    - Normal radial (radial) dipakai shader untuk menghitung soft edge (dot(N, V)).
    /// 2. Floor Footprint Grid Polar (44 sektor x 22 cincin):
    ///    - Membangun quad lantai secara dinamis dalam koordinat lokal objek jalan (InverseTransformPoint).
    ///    - Menggunakan Binary Search 5 iterasi (RefineFloorEdge) pada tepi bayangan agar
    ///      siluet bayangan di atas aspal mengikuti tepi peti kayu dan pembatas beton dengan tajam.
    ///    - Vertex Colors menyimpan alpha falloff (ujung kiri/kanan, dekat lensa, dan ekor cahaya)
    ///      agar shader cukup mengalikan warna vertex dengan tekstur/noise prosedural.
    /// </summary>
    public sealed partial class ChapterTwoSpotlightEncounter
    {
        public void RebuildFootprints()
        {
            if (beams == null)
            {
                return;
            }
            foreach (var beam in beams)
            {
                if (beam.emitter == null)
                {
                    continue;
                }
                if (ShowFloorFootprint && beam.mesh != null)
                {
                    RebuildFootprint(beam);
                }
                RebuildVolumetricCone(beam);
            }
        }

        // =========================================================================
        // 1. Proyeksi Jejak Cahaya di Lantai (Polar Grid + Binary Search Tepi)
        // =========================================================================

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
            if (forward.sqrMagnitude < .0001f)
            {
                return;
            }
            forward.Normalize();

            float streetY = transform.position.y;
            const int stride = FootprintRings + 1;

            // Tahap A: Hitung seluruh simpul grid polar (sudut x jarak radial) dan uji visibilitasnya.
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

            // Tahap B: Rangkai quad segitiga untuk sel yang terlihat, perhalus tepi bayangan dengan binary search.
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

                    bool va = beam.visible[ia];
                    bool vb = beam.visible[ib];
                    bool vc = beam.visible[ic];
                    bool vd = beam.visible[id];
                    if (!va || !vb)
                    {
                        continue;
                    }

                    Vector3 a = beam.points[ia];
                    Vector3 b = beam.points[ib];
                    Vector3 c = beam.points[ic];
                    Vector3 d = beam.points[id];
                    float alongFar = (float)(z + 1) / FootprintRings;

                    if (!vc || !vd)
                    {
                        // Gunakan pencarian biner (Binary Search) di sepanjang sinar untuk memperkirakan awal bayangan.
                        float rNear = Mathf.Lerp(FootprintStartRadius, beam.range, (float)z / FootprintRings);
                        float rFar = Mathf.Lerp(FootprintStartRadius, beam.range, alongFar);
                        if (!vc)
                        {
                            float rHit = RefineFloorEdge(beam, origin, dirA, streetY, rNear, rFar, out c);
                            if (rHit <= rNear + 0.01f)
                            {
                                continue;
                            }
                        }
                        if (!vd)
                        {
                            float rHit = RefineFloorEdge(beam, origin, dirB, streetY, rNear, rFar, out d);
                            if (rHit <= rNear + 0.01f)
                            {
                                continue;
                            }
                        }
                    }

                    float minY = Mathf.Min(Mathf.Min(a.y, b.y), Mathf.Min(c.y, d.y));
                    float maxY = Mathf.Max(Mathf.Max(a.y, b.y), Mathf.Max(c.y, d.y));
                    // Tolak quad yang melompati undakan tinggi (>16 cm) agar mesh tidak merentang miring antar trotoar.
                    if (maxY - minY > .16f)
                    {
                        continue;
                    }

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

                    // Susunan indeks dua segitiga per quad (a-c-b dan b-c-d).
                    triangles.Add(n);
                    triangles.Add(n + 2);
                    triangles.Add(n + 1);
                    triangles.Add(n + 1);
                    triangles.Add(n + 2);
                    triangles.Add(n + 3);
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

        private Color FootprintColor(Beam beam, Vector3 point, float across, float along)
        {
            const float fadeWidth = .22f;
            const float minFade = .05f;
            float edge = Mathf.SmoothStep(minFade, 1f, Mathf.Clamp01(Mathf.Min(across, 1f - across) / fadeWidth));
            float end = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - along) / .24f));
            float nearFade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(along / .10f));

            bool alertSpot = beam.seesPlayer && motor != null && Vector3.Distance(point, motor.transform.position) < 2.8f;
            Color color = Color.Lerp(CalmBeamColor, AlertBeamColor, alertSpot ? 1f : beam.alertBlend * 0.90f);
            float baseAlpha = Mathf.Lerp(.24f, .44f, alertSpot ? 1f : beam.alertBlend);
            color.a = baseAlpha * edge * end * nearFade;
            return color;
        }

        // =========================================================================
        // 2. Kerucut Cahaya Volumetrik Prosedural (Raycast-Clipped Dual Cone)
        // =========================================================================

        private void RebuildVolumetricCone(Beam beam)
        {
            if (beam.coneMesh == null || beam.emitter == null)
            {
                return;
            }
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

            // Bangun dua lapis kerucut: kulit luar yang menyebar dan inti dalam yang lebih terang.
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
                    if (IsIgnoredObstacle(beam, hit))
                    {
                        continue;
                    }
                    if (hit.distance < hitDist)
                    {
                        hitDist = hit.distance;
                    }
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
                tris.Add(a);
                tris.Add(b);
                tris.Add(c);
                tris.Add(c);
                tris.Add(b);
                tris.Add(d);
            }
        }
    }
}
