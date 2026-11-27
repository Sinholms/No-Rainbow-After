using NoRainbowAfter.Level;
using NoRainbowAfter.Player;
using UnityEngine;

namespace NoRainbowAfter.AI
{
    public struct CoverEvaluation
    {
        public CoverZone ActiveZone;
        public bool PlayerInsideVolume;
        public bool IsCrouchedInCover;
        public bool IsPhysicallyOccluded;
        public float AngleFromMonsterGaze;
        public bool IsConcealedFromMonster;
    }

    /// <summary>
    /// Memeriksa penghalang fisik, persembunyian, dan syarat serangan terhadap bayangan.
    /// </summary>
    public static class ShadowVisibility
    {
        private static readonly RaycastHit[] HitBuffer = new RaycastHit[32];

        public static bool IsLineOfSightClear(Vector3 origin, Vector3 target, Transform ignoreA, Transform ignoreB)
        {
            Vector3 delta = target - origin;
            float distance = delta.magnitude;
            if (distance <= 0.02f) return true;

            int count = Physics.RaycastNonAlloc(
                origin,
                delta / distance,
                HitBuffer,
                distance - 0.015f,
                ~0,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Collider col = HitBuffer[i].collider;
                if (col == null) continue;
                Transform t = col.transform;
                if (ignoreA != null && t.IsChildOf(ignoreA)) continue;
                if (ignoreB != null && t.IsChildOf(ignoreB)) continue;
                return false;
            }
            return true;
        }

        public static bool HasAnyClearSightlineToPlayer(Vector3 monsterEye, FirstPersonMotor motor, Transform monsterRoot)
        {
            if (motor == null) return false;
            Transform playerRoot = motor.transform;
            Vector3 eyePoint = motor.ViewCamera != null
                ? motor.ViewCamera.transform.position
                : playerRoot.position + Vector3.up * (motor.IsCrouched ? 0.95f : 1.65f);

            var cc = motor.GetComponent<CharacterController>();
            float bodyHeight = cc != null ? cc.height : (motor.IsCrouched ? 1.1f : 1.8f);
            Vector3 chestPoint = playerRoot.position + Vector3.up * (bodyHeight * 0.56f);

            return IsLineOfSightClear(monsterEye, eyePoint, monsterRoot, playerRoot) ||
                   IsLineOfSightClear(monsterEye, chestPoint, monsterRoot, playerRoot);
        }

        public static CoverEvaluation EvaluateCover(
            FirstPersonMotor motor,
            CoverZone[] coverZones,
            Vector3 monsterEye,
            Vector3 monsterForward,
            Transform monsterRoot,
            float crouchedGazeHalfAngle = 35f)
        {
            // Cover biasa bukan interaksi lemari: pemain harus jongkok di CoverZone, lalu terlindung
            // oleh dinding atau berada di luar sudut pandang. Controller menunggu bayangan lewat atau menjauh cukup lama.
            var result = new CoverEvaluation();
            if (motor == null) return result;

            if (coverZones != null)
            {
                for (int i = 0; i < coverZones.Length; i++)
                {
                    CoverZone zone = coverZones[i];
                    if (zone != null && zone.PlayerInside)
                    {
                        result.ActiveZone = zone;
                        result.PlayerInsideVolume = true;
                        break;
                    }
                }
            }

            var cc = motor.GetComponent<CharacterController>();
            float currentHeight = cc != null ? cc.height : (motor.IsCrouched ? 1.1f : 1.8f);
            // Pemain perlu berada di zona dan sudah cukup rendah untuk dinilai sedang bersembunyi.
            result.IsCrouchedInCover = result.PlayerInsideVolume && motor.IsCrouched && currentHeight <= 1.28f;
            result.IsPhysicallyOccluded = !HasAnyClearSightlineToPlayer(monsterEye, motor, monsterRoot);

            Vector3 playerEye = motor.ViewCamera != null
                ? motor.ViewCamera.transform.position
                : motor.transform.position + Vector3.up * (motor.IsCrouched ? 0.95f : 1.65f);
            Vector3 toPlayer = playerEye - monsterEye;
            Vector3 fwd = monsterForward.sqrMagnitude > 0.0001f ? monsterForward.normalized : Vector3.back;
            result.AngleFromMonsterGaze = toPlayer.sqrMagnitude > 0.0001f
                ? Vector3.Angle(fwd, toPlayer.normalized)
                : 0f;

            // Saat jongkok di zona, dinding atau posisi di luar sudut pandang bayangan dapat menyembunyikan pemain.
            result.IsConcealedFromMonster = result.IsCrouchedInCover &&
                (result.IsPhysicallyOccluded || result.AngleFromMonsterGaze > crouchedGazeHalfAngle);

            return result;
        }

        // Uji jarak ke permukaan badan, lalu cek apakah kamera membidik salah satu titik pada tubuh bayangan.
        public static bool CanPlayerFightMonster(
            Camera playerCamera,
            Transform playerRoot,
            Transform monsterRoot,
            float currentMonsterHeight,
            float maxRange = 2.75f,
            float maxAimAngle = 34f)
        {
            if (playerCamera == null || playerRoot == null || monsterRoot == null) return false;

            Vector3 camPos = playerCamera.transform.position;
            Vector3 camFwd = playerCamera.transform.forward;
            float height = Mathf.Max(1.4f, currentMonsterHeight);

            var bodyBounds = new Bounds(
                monsterRoot.position + Vector3.up * (height * 0.5f),
                new Vector3(0.58f, height, 0.58f));

            Vector3 closest = bodyBounds.ClosestPoint(camPos);
            if (Vector3.Distance(camPos, closest) > maxRange) return false;

            float topSampleY = Mathf.Min(1.75f, height - 0.14f);
            float[] sampleHeights = { 0.92f, 1.32f, topSampleY };
            for (int i = 0; i < sampleHeights.Length; i++)
            {
                Vector3 sample = monsterRoot.position + Vector3.up * sampleHeights[i];
                Vector3 toSample = sample - camPos;
                float dist = toSample.magnitude;
                if (dist > maxRange + 0.35f || dist <= 0.02f) continue;
                if (Vector3.Angle(camFwd, toSample / dist) > maxAimAngle) continue;
                if (IsLineOfSightClear(camPos, sample, playerRoot, monsterRoot)) return true;
            }
            return false;
        }
    }
}
