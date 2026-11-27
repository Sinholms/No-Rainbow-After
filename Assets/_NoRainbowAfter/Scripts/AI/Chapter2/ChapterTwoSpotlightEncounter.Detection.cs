using UnityEngine;

namespace NoRainbowAfter.AI
{
    /// <summary>
    /// Bagian deteksi & kinematika lampu sorot polisi (ChapterTwoSpotlightEncounter).
    ///
    /// [GRAFIKA KOMPUTER & MATEMATIKA 3D]
    /// 1. Animasi Sapuan Harmonik: PingPong + SmoothStep memperlambat rotasi senter saat
    ///    mencapai titik balik kiri/kanan.
    /// 2. Komposisi Quaternion: Mengalikan Quaternion.Euler(0, yaw, 0) * baseRotation
    ///    menjaga lampu tetap miring ke bawah sambil memutar arah pandang terhadap sumbu Y.
    /// 3. Uji Kerucut Horizontal (Planar Cone Test): Vector3.ProjectOnPlane memproyeksikan
    ///    vektor arah & delta ke bidang aspal (XZ), lalu Vector3.Angle memeriksa apakah target
    ///    berada dalam setengah sudut buka kerucut lampu.
    /// 4. Line-of-Sight Raycast: Physics.RaycastNonAlloc memeriksa apakah peti/beton menutup
    ///    jalur cahaya antara lensa senter dan titik torso/kepala pemain.
    /// 5. Pencahayaan Reaktif: Color.Lerp & Mathf.MoveTowards mengubah warna Spot Light dari
    ///    putih dingin ke merah siaga saat pemain tersorot.
    /// </summary>
    public sealed partial class ChapterTwoSpotlightEncounter
    {
        // =========================================================================
        // Sapuan Arah Senter & Pembaruan Warna Cahaya URP
        // =========================================================================

        private void AimFlashlights()
        {
            foreach (var beam in beams)
            {
                if (beam.emitter == null)
                {
                    continue;
                }

                // PingPong bergerak dari 0 ke 1 lalu kembali. Offset fase membedakan posisi tiap lampu.
                float rawPhase = beam.period > 0f
                    ? Mathf.PingPong((clock + beam.phaseOffset) / beam.period * 2f, 1f)
                    : .5f;

                // SmoothStep memberi percepatan/perlambatan alami di kedua ujung sebelum berbalik arah.
                float phase = Mathf.SmoothStep(0f, 1f, rawPhase);
                float yaw = (phase * 2f - 1f) * beam.yaw;

                // Kalikan rotasi sapuan di kiri rotasi dasar agar lampu berputar di sumbu vertikal tiang,
                // tetap mempertahankan kemiringan ke bawah (local pitch) pada baseRotation.
                beam.emitter.localRotation = Quaternion.Euler(0f, yaw, 0f) * beam.baseRotation;
                if (beam.gaze != null)
                {
                    // Kepala polisi ikut menoleh sebesar 82% sudut senter agar terlihat mengawasi.
                    beam.gaze.localRotation = Quaternion.AngleAxis(yaw * .82f, beam.gazeAxis) * beam.restingGaze;
                }
            }
        }

        private void UpdateBeamLights(float dt)
        {
            if (beams == null)
            {
                return;
            }
            float exposureRatio = CatchSeconds > 0f ? Mathf.Clamp01(Exposure / CatchSeconds) : 0f;
            foreach (var beam in beams)
            {
                if (beam.light == null)
                {
                    continue;
                }
                float target = beam.seesPlayer ? Mathf.Max(0.35f, exposureRatio) : 0f;
                beam.alertBlend = Mathf.MoveTowards(beam.alertBlend, target, dt * 6f);
                beam.light.color = Color.Lerp(CalmBeamColor, AlertBeamColor, beam.alertBlend);
                beam.light.intensity = Mathf.Lerp(BaseLightIntensity, AlertLightIntensity, beam.alertBlend);
            }
        }

        // =========================================================================
        // Deteksi Garis Pandang Pemain & Evaluasi Titik Lantai
        // =========================================================================

        public bool IsFloorPointVisible(int beamIndex, Vector3 worldPoint)
        {
            if (beams == null || beamIndex < 0 || beamIndex >= beams.Length || beams[beamIndex].emitter == null)
            {
                return false;
            }
            return ProjectFloor(beams[beamIndex], ref worldPoint) && Visible(beams[beamIndex], worldPoint);
        }

        private bool SeesPlayer(Beam beam)
        {
            if (controller == null)
            {
                controller = motor.GetComponent<CharacterController>();
            }
            if (controller == null)
            {
                return false;
            }

            Vector3 bottom = motor.transform.TransformPoint(controller.center - Vector3.up * controller.height * .5f);
            float crouchBlend = motor.CrouchBlend;

            // [TEKNIS KODING] Jongkok (CrouchBlend 0 -> 1) menurunkan titik uji kepala & dada
            // sehingga pemain dapat bersembunyi secara konsisten di balik pembatas beton 1.04m.
            // Satu titik saja yang terlihat sudah cukup untuk mendeteksi pemain.
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
            if (distance < .001f)
            {
                return false;
            }

            Vector3 forward = Vector3.ProjectOnPlane(beam.emitter.forward, Vector3.up);
            if (forward.sqrMagnitude < .0001f)
            {
                return false;
            }
            forward.Normalize();

            // Uji batas jangkauan radial & sudut setengah kerucut pada bidang horizontal (XZ).
            Vector3 planar = Vector3.ProjectOnPlane(delta, Vector3.up);
            float planarDistance = planar.magnitude;
            if (planarDistance > beam.range + .01f || planarDistance < .001f ||
                Vector3.Angle(forward, planar) > beam.angle * .5f + .05f)
            {
                return false;
            }

            // Uji ruang antara lensa dan titik sasaran memakai RaycastNonAlloc (zero GC allocation).
            // Collider trigger (seperti Zona Aman halte atau volume post-processing) diabaikan.
            int hitCount = Physics.RaycastNonAlloc(origin, delta / distance, beam.hits, distance - 0.02f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                var hit = beam.hits[i];
                if (IsIgnoredObstacle(beam, hit))
                {
                    continue;
                }
                return false;
            }

            // Jika buffer penuh, anggap titik tertutup karena mungkin ada penghalang yang tidak tercatat.
            return hitCount < beam.hits.Length;
        }

        private bool IsIgnoredObstacle(Beam beam, RaycastHit hit)
        {
            Transform hitTransform = hit.collider.transform;
            if (motor != null && hitTransform.IsChildOf(motor.transform))
            {
                return true;
            }
            if (hitTransform.IsChildOf(beam.emitter))
            {
                return true;
            }
            // Abaikan badan penjaga yang berada di dekat lensa agar lampunya tidak langsung terhalang.
            if (beam.post != null && hitTransform == beam.post && hit.distance < 0.75f)
            {
                return true;
            }
            return false;
        }

        // Memproyeksikan titik uji ke permukaan lantai tertinggi yang layak jalan (normal.y >= 0.7).
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
                if (IsIgnoredObstacle(beam, hit))
                {
                    continue;
                }
                if (hit.point.y > bestY)
                {
                    bestY = hit.point.y;
                    best = hit;
                    found = true;
                }
            }

            if (!found || best.normal.y < .7f || best.point.y > streetY + MaxWalkableFloorRise)
            {
                return false;
            }

            // Sedikit diangkat (+2.2 cm) untuk mencegah z-fighting terhadap permukaan aspal/trotoar.
            point.y = best.point.y + .022f;
            return true;
        }
    }
}
