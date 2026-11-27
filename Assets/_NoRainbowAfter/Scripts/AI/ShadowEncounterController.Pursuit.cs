using NoRainbowAfter.Level;
using NoRainbowAfter.Narrative;
using UnityEngine;

namespace NoRainbowAfter.AI
{
    /// <summary>
    /// Logika pengejaran, persembunyian di cover/lemari, dan pergerakan lantai pada ShadowEncounterController.
    ///
    /// [GRAFIKA KOMPUTER & TEKNIS KODING]
    /// - Navigasi NavMesh melalui ChapterOneCorridorShift (pada lorong horor ekstensi)
    ///   dan waypoint cerdas melewati celah pintu / ceruk lorong kamar kos.
    /// - ShadowVisibility.EvaluateCover memeriksa posisi jongkok, garis pandang mata/dada,
    ///   serta sudut pandang monster untuk menentukan persembunyian yang adil.
    /// - Physics.OverlapCapsule memilih titik spawn yang bebas tabrakan dengan furnitur.
    /// </summary>
    public sealed partial class ShadowEncounterController
    {
        // =========================================================================
        // Fase Spawn & Pursue (Pengejaran / Pencarian)
        // =========================================================================

        private void TickSpawn(float dt)
        {
            FaceHorizontallyToward(motor.transform.position, dt);
            placeholderVisual?.TickVisual(ShadowEncounterState.Spawn, dt, 0f, stateTimer / spawnHoldDuration, motor.transform);
            stateTimer += dt;
            if (stateTimer >= spawnHoldDuration)
            {
                stateTimer = 0f;
                CueRequested?.Invoke(new NarrativeCue("", "", 0f, "AUD_C1_SHADOW_PURSUE"));
                SetState(ShadowEncounterState.Pursue);
            }
        }

        private void TickPursue(float dt)
        {
            if (postCaughtGraceTimer > 0f)
            {
                currentBlackoutAlpha = Mathf.MoveTowards(currentBlackoutAlpha, 0f, dt);
                placeholderVisual?.AnimateRecovery(1f - postCaughtGraceTimer / EscapeRecoveryDuration);
                return;
            }

            var wardrobe = HidingWardrobe.Occupied;
            if (wardrobe != null && wardrobe.Occupant == motor)
            {
                TickWardrobePursuit(wardrobe, dt);
                return;
            }

            wardrobeHideTime = 0f;
            Vector3 eyePos = GetMonsterEyePosition();
            CoverEvaluation cover = ShadowVisibility.EvaluateCover(
                motor,
                coverZones,
                eyePos,
                transform.forward,
                transform,
                crouchedGazeHalfAngle);

            UpdateHideProgress(cover, dt);
            if (State != ShadowEncounterState.Pursue)
            {
                return;
            }

            bool concealed = cover.IsConcealedFromMonster && !spottedEnteringCover;
            Vector3 destination = shiftedWorld != null && shiftedWorld.OwnsPlayer
                ? shiftedWorld.Navigate(transform.position, motor.transform.position)
                : concealed ? SelectSearchPassWaypoint(dt) : SelectPursuitWaypoint(motor.transform.position);
            float moveSpeed = concealed ? searchPassSpeed : pursueSpeed;

            float actualHorizontalSpeed = MoveAlongFloor(destination, moveSpeed, dt);
            placeholderVisual?.TickVisual(
                ShadowEncounterState.Pursue,
                dt,
                Mathf.Clamp01(actualHorizontalSpeed / Mathf.Max(0.1f, searchPassSpeed)),
                0f,
                motor.transform);

            if (!concealed && postCaughtGraceTimer <= 0f && ShouldTriggerCaught())
            {
                EnterCaught();
            }
        }

        private void TickWardrobePursuit(HidingWardrobe wardrobe, float dt)
        {
            // Shadow mengingat apakah ia melihat Raka masuk. Setelah 2 detik ia menangkap
            // Raka yang ketahuan; jika tidak terlihat, bertahan 6 detik menyelesaikan persembunyian.
            wardrobeHideTime += dt;
            if (wardrobe.Spotted)
            {
                placeholderVisual?.TickVisual(ShadowEncounterState.Pursue, dt, 0f, 0f, motor.transform, true);
                if (wardrobeHideTime > 2f)
                {
                    wardrobe.TryExit();
                    wardrobe.Release();
                    EnterCaught();
                    wardrobeHideTime = 0f;
                }
                return;
            }

            Vector3 search = shiftedWorld != null ? shiftedWorld.MonsterSpawn + Vector3.left * 6f : new Vector3(0, 0, 17);
            Vector3 next = shiftedWorld != null ? shiftedWorld.Navigate(transform.position, search) : search;
            float searchSpeed = MoveAlongFloor(next, searchPassSpeed, dt);
            placeholderVisual?.TickVisual(ShadowEncounterState.Pursue, dt, searchSpeed / searchPassSpeed, 0f, motor.transform, true);

            if (wardrobeHideTime >= 6f)
            {
                stateTimer = 0f;
                CueRequested?.Invoke(new NarrativeCue("Raka", "Langkahnya menjauh...", 2.5f, "AUD_C1_HIDE"));
                SetState(ShadowEncounterState.HideResolved);
            }
        }

        // =========================================================================
        // Evaluasi Progres Sembunyi di Zona Cover
        // =========================================================================

        private void UpdateHideProgress(CoverEvaluation cover, float dt)
        {
            // Cover biasa baru mulai menghitung bila pemain jongkok di zona. Pindah zona atau berdiri
            // menghapus timer; masuk dari dekat, terlihat, dan tanpa penghalang menandai pemain spotted.
            if (!cover.IsCrouchedInCover || cover.ActiveZone == null)
            {
                ResetHideTracking();
                return;
            }

            if (!ReferenceEquals(trackedCoverZone, cover.ActiveZone))
            {
                trackedCoverZone = cover.ActiveZone;
                sawMonsterNorthOfCover = false;
                sawMonsterSouthOfCover = false;
                hideTimer = 0f;

                float horizontalDist = HorizontalDistance(transform.position, motor.transform.position);
                // Bayangan mengingat pemain yang mulai jongkok dari dekat di dalam sudut pandang tanpa penghalang.
                spottedEnteringCover = !cover.IsPhysicallyOccluded &&
                    cover.AngleFromMonsterGaze <= crouchedGazeHalfAngle &&
                    horizontalDist <= 3.1f;
            }

            if (spottedEnteringCover || !cover.IsConcealedFromMonster)
            {
                hideTimer = 0f;
                return;
            }

            float coverZ = trackedCoverZone.transform.position.z;
            float deltaZ = transform.position.z - coverZ;
            if (deltaZ >= 0.6f) sawMonsterNorthOfCover = true;
            if (deltaZ <= -0.6f) sawMonsterSouthOfCover = true;

            hideTimer += dt;

            // Tunggu hingga bayangan melewati zona, atau menjauh cukup lama saat pemain tetap tertutup.
            bool passedSouth = sawMonsterNorthOfCover && deltaZ <= -coverPassClearanceZ;
            bool passedNorth = sawMonsterSouthOfCover && deltaZ >= coverPassClearanceZ;
            bool farAndOccluded = Mathf.Abs(deltaZ) >= 3.6f && cover.IsPhysicallyOccluded && hideTimer >= requiredHideDuration + 1.0f;

            if (hideTimer >= requiredHideDuration && cover.IsPhysicallyOccluded && (passedSouth || passedNorth || farAndOccluded))
            {
                stateTimer = 0f;
                CueRequested?.Invoke(new NarrativeCue("", "", 0f, "AUD_C1_HIDE"));
                SetState(ShadowEncounterState.HideResolved);
            }
        }

        private void ResetHideTracking()
        {
            hideTimer = 0f;
            trackedCoverZone = null;
            spottedEnteringCover = false;
            sawMonsterNorthOfCover = false;
            sawMonsterSouthOfCover = false;
        }

        // =========================================================================
        // Pemilihan Waypoint, Pergerakan Lantai, & Spawning Aman
        // =========================================================================

        private Vector3 SelectSearchPassWaypoint(float dt)
        {
            // Keluar lewat pintu kamar sebelum mulai menyusuri lorong untuk mencari pemain.
            if (transform.position.z < 5.45f)
            {
                if (Mathf.Abs(transform.position.x - 0.16f) > 0.16f && transform.position.z < 4.70f)
                {
                    return new Vector3(0.16f, 0f, 4.55f);
                }
                return new Vector3(0.16f, 0f, 6.15f);
            }

            float targetZ = searchDirection < 0 ? 6.05f : 17.65f;
            if (Mathf.Abs(transform.position.z - targetZ) <= 0.25f)
            {
                searchTurnPauseTimer += dt;
                if (searchTurnPauseTimer >= 0.35f)
                {
                    searchTurnPauseTimer = 0f;
                    searchDirection = -searchDirection;
                    targetZ = searchDirection < 0 ? 6.05f : 17.65f;
                }
            }
            return new Vector3(0f, 0f, targetZ);
        }

        private Vector3 SelectPursuitWaypoint(Vector3 playerPos)
        {
            Vector3 pos = transform.position;
            const float doorThresholdZ = 5.45f;
            const float doorLaneX = 0.16f;

            // Sejajarkan bayangan dengan celah pintu di x = 0.16 sebelum masuk ke kamar.
            if (pos.z > doorThresholdZ && playerPos.z < 5.05f)
            {
                if (pos.z > 6.10f || Mathf.Abs(pos.x - doorLaneX) > 0.14f)
                {
                    return new Vector3(doorLaneX, 0f, 6.05f);
                }
                return new Vector3(doorLaneX, 0f, 4.50f);
            }

            // Keluar dari kamar melalui celah pintu yang sama.
            if (pos.z <= doorThresholdZ && playerPos.z >= doorThresholdZ)
            {
                if (pos.z < 4.45f || Mathf.Abs(pos.x - doorLaneX) > 0.14f)
                {
                    return new Vector3(doorLaneX, 0f, 4.50f);
                }
                return new Vector3(doorLaneX, 0f, 6.10f);
            }

            // Bila pemain berada di ceruk lorong, dekati bukaan ceruk sebelum masuk.
            if (pos.z > doorThresholdZ)
            {
                if (Mathf.Abs(playerPos.x) > 0.92f)
                {
                    float nicheCenterZ = playerPos.x < 0f ? 9.0f : 13.0f;
                    if (Mathf.Abs(pos.z - nicheCenterZ) > 0.42f)
                    {
                        return new Vector3(0f, 0f, nicheCenterZ);
                    }
                    return new Vector3(
                        Mathf.Clamp(playerPos.x, -1.38f, 1.38f),
                        0f,
                        Mathf.Clamp(playerPos.z, nicheCenterZ - 0.35f, nicheCenterZ + 0.35f));
                }
                return new Vector3(Mathf.Clamp(playerPos.x, -0.56f, 0.56f), 0f, Mathf.Clamp(playerPos.z, 5.75f, 18.4f));
            }

            // Gunakan jalur tengah kamar jika furnitur menghalangi lintasan langsung setinggi pinggang.
            Vector3 waistOrigin = new Vector3(pos.x, 0.55f, pos.z);
            Vector3 waistTarget = new Vector3(playerPos.x, 0.55f, playerPos.z);
            if (ShadowVisibility.IsLineOfSightClear(waistOrigin, waistTarget, transform, motor.transform))
            {
                return new Vector3(Mathf.Clamp(playerPos.x, -1.65f, 1.65f), 0f, Mathf.Clamp(playerPos.z, 0.35f, 4.65f));
            }

            const float roomSpineX = 0.05f;
            if (Mathf.Abs(pos.x - roomSpineX) > 0.22f)
            {
                return new Vector3(roomSpineX, 0f, Mathf.Clamp(pos.z, 0.55f, 4.55f));
            }
            return new Vector3(roomSpineX, 0f, Mathf.Clamp(playerPos.z, 0.55f, 4.55f));
        }

        private float MoveAlongFloor(Vector3 target, float speed, float dt)
        {
            if (monsterController == null || !monsterController.enabled)
            {
                return 0f;
            }

            Vector3 before = transform.position;
            Vector3 planarDelta = new Vector3(target.x - before.x, 0f, target.z - before.z);
            Vector3 horizVel = Vector3.zero;
            if (planarDelta.sqrMagnitude > 0.0004f)
            {
                Vector3 dir = planarDelta.normalized;
                horizVel = dir * Mathf.Min(speed, planarDelta.magnitude / dt);
                FaceDirection(dir, dt);
            }

            if (monsterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }
            verticalVelocity += gravity * dt;

            CollisionFlags flags = monsterController.Move((horizVel + Vector3.up * verticalVelocity) * dt);
            if ((flags & CollisionFlags.Below) != 0 && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            Vector3 moved = transform.position - before;
            moved.y = 0f;
            return dt > 0f ? moved.magnitude / dt : 0f;
        }

        private void FaceHorizontallyToward(Vector3 worldPoint, float dt)
        {
            Vector3 delta = worldPoint - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude > 0.0004f)
            {
                FaceDirection(delta.normalized, dt);
            }
        }

        private void FaceDirection(Vector3 planarDir, float dt)
        {
            Quaternion desired = Quaternion.LookRotation(planarDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, turnSpeedDegrees * dt);
        }

        private Vector3 SelectSafeCorridorSpawn()
        {
            if (shiftedWorld != null && shiftedWorld.OwnsPlayer)
            {
                return shiftedWorld.MonsterSpawn;
            }
            float playerZ = motor != null ? motor.transform.position.z : 6.0f;
            // Uji kandidat posisi di ujung lorong yang lebih jauh dari pemain, lalu pilih posisi yang bebas collider.
            float[] candidates = playerZ <= 11.2f
                ? new[] { Mathf.Clamp(playerZ + 10.6f, 16.2f, 17.6f), 17.4f, 16.4f, 15.2f }
                : new[] { Mathf.Clamp(playerZ - 9.2f, 6.8f, 8.4f), 7.2f, 8.2f, 17.6f };

            for (int i = 0; i < candidates.Length; i++)
            {
                var pos = new Vector3(0f, 0.02f, candidates[i]);
                if (motor != null && HorizontalDistance(pos, motor.transform.position) < 6.0f)
                {
                    continue;
                }
                if (IsCapsuleClearAt(pos))
                {
                    return pos;
                }
            }
            return new Vector3(0f, 0.02f, playerZ <= 11.2f ? 17.4f : 7.2f);
        }

        private bool IsCapsuleClearAt(Vector3 feetPosition)
        {
            float radius = monsterController != null ? Mathf.Max(0.08f, monsterController.radius - 0.02f) : 0.21f;
            float height = monsterController != null ? monsterController.height : 1.80f;
            Vector3 p0 = feetPosition + Vector3.up * (radius + 0.06f);
            Vector3 p1 = feetPosition + Vector3.up * (height - radius);
            Collider[] cols = Physics.OverlapCapsule(p0, p1, radius, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] != null && !cols[i].transform.IsChildOf(transform))
                {
                    return false;
                }
            }
            return true;
        }

        private void SetMonsterActiveInWorld(bool active, Vector3 position, Quaternion rotation)
        {
            if (monsterController == null)
            {
                monsterController = GetComponent<CharacterController>();
            }
            if (monsterController != null)
            {
                monsterController.enabled = false;
            }
            transform.SetPositionAndRotation(position, rotation);
            if (placeholderVisual != null)
            {
                placeholderVisual.SetVisible(active);
                if (active)
                {
                    placeholderVisual.ResetPostureImmediate(motor != null ? motor.transform : null);
                }
            }
            if (monsterController != null)
            {
                monsterController.enabled = active;
            }
            Physics.SyncTransforms();
        }

        private Vector3 GetMonsterEyePosition()
        {
            float h = placeholderVisual != null ? placeholderVisual.CurrentVisualHeight : 2.15f;
            return transform.position + Vector3.up * Mathf.Clamp(h - 0.34f, 1.45f, 1.92f);
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
