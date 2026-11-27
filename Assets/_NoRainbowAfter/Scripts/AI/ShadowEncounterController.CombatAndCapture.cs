using System;
using NoRainbowAfter.Narrative;
using NoRainbowAfter.Player;
using UnityEngine;

namespace NoRainbowAfter.AI
{
    /// <summary>
    /// Resolusi perlawanan (Fight 3x), sembunyi berhasil, cengkeraman pertama (Grabbed),
    /// dan tertangkap dengan pemulihan checkpoint pada ShadowEncounterController.
    /// </summary>
    public sealed partial class ShadowEncounterController
    {
        // =========================================================================
        // Aksi Melawan (Fight) & Penyelesaian Jalur Berhasil
        // =========================================================================

        private void OnFightRequested()
        {
            if (!isActiveAndEnabled || permanentlyResolved || player == null || motor == null)
            {
                return;
            }
            if (player.IsPaused || player.IsInspecting || player.IsUsingLaptop || player.IsTransitionLocked)
            {
                return;
            }
            if (State != ShadowEncounterState.Spawn && State != ShadowEncounterState.Pursue)
            {
                return;
            }
            if (!CanFightRightNow())
            {
                return;
            }

            // Setiap pukulan memicu animasi mundur. Selama animasi itu, klik berikutnya tidak dihitung.
            successfulHits++;
            placeholderVisual?.SetHitVariant(successfulHits >= 3);
            stateTimer = 0f;
            currentBlackoutAlpha = 0.22f;
            CueRequested?.Invoke(new NarrativeCue("", "", 0f, "AUD_C1_FIGHT"));
            SetState(ShadowEncounterState.FightResolved);
            RefreshUi();
        }

        public bool CanFightRightNow()
        {
            if (player == null || motor == null || motor.ViewCamera == null)
            {
                return false;
            }
            if (player.IsHiding)
            {
                return false;
            }
            if (player.IsPaused || player.IsInspecting || player.IsUsingLaptop || player.IsTransitionLocked)
            {
                return false;
            }
            if (State != ShadowEncounterState.Spawn && State != ShadowEncounterState.Pursue)
            {
                return false;
            }

            float visualHeight = placeholderVisual != null ? placeholderVisual.CurrentVisualHeight : 2.15f;
            return ShadowVisibility.CanPlayerFightMonster(
                motor.ViewCamera,
                motor.transform,
                transform,
                visualHeight,
                fightRange,
                fightAimHalfAngle);
        }

        private void TickFightResolved(float dt)
        {
            stateTimer += dt;
            float duration = Mathf.Max(
                fightFeedbackDuration,
                placeholderVisual != null ? placeholderVisual.HitDuration(successfulHits >= 3) : .52f);
            float t = Mathf.Clamp01(stateTimer / duration);
            currentBlackoutAlpha = Mathf.Lerp(0.22f, 0f, t);
            placeholderVisual?.TickVisual(ShadowEncounterState.FightResolved, dt, 0f, t, motor.transform);

            if (stateTimer >= duration)
            {
                if (successfulHits >= 3)
                {
                    CompleteSuccessResolution(EncounterResponse.Fight);
                }
                else
                {
                    stateTimer = 0f;
                    SetState(ShadowEncounterState.Pursue);
                }
            }
        }

        private void TickHideResolved(float dt)
        {
            stateTimer += dt;
            float t = Mathf.Clamp01(stateTimer / hideResolveDuration);
            placeholderVisual?.TickVisual(ShadowEncounterState.HideResolved, dt, 0f, t, motor.transform);

            // Sembunyi baru mengirim hasil setelah animasi pendek selesai, seperti jalur Fight.
            if (stateTimer >= hideResolveDuration)
            {
                CompleteSuccessResolution(EncounterResponse.Hide);
            }
        }

        private void CompleteSuccessResolution(EncounterResponse response)
        {
            captureFeedback?.ResetFeedback();
            permanentlyResolved = true;
            currentBlackoutAlpha = 0f;
            SetMonsterActiveInWorld(false, dormantPosition, Quaternion.Euler(0f, 180f, 0f));

            Action<EncounterResponse> callback = activeResolve;
            activeResolve = null;
            RefreshUi();

            // Hanya di sini hasil Fight/Hide diteruskan ke callback ChapterDirector.
            callback?.Invoke(response);
        }

        // =========================================================================
        // Cengkeram Pertama (Grabbed) & Tertangkap (Caught -> Checkpoint)
        // =========================================================================

        // Bayangan perlu berada dalam batas jarak horizontal dan vertikal serta punya garis pandang ke pemain.
        private bool ShouldTriggerCaught()
        {
            if (motor == null)
            {
                return false;
            }
            Vector3 mPos = transform.position;
            Vector3 pPos = motor.transform.position;
            if (Mathf.Abs(mPos.y - pPos.y) > 1.35f)
            {
                return false;
            }
            if (HorizontalDistance(mPos, pPos) > catchDistance)
            {
                return false;
            }
            return ShadowVisibility.HasAnyClearSightlineToPlayer(GetMonsterEyePosition(), motor, transform);
        }

        private void EnterCaught()
        {
            if (State == ShadowEncounterState.Grabbed || State == ShadowEncounterState.Caught || permanentlyResolved)
            {
                return;
            }
            if (allowFirstEscape)
            {
                grabWillEscape = !escapeUsed;
                if (grabWillEscape)
                {
                    escapeUsed = true;
                }
                stateTimer = 0f;
                ResetHideTracking();
                player?.SetTransitionLocked(true);
                captureFeedback?.BeginGrab(transform);
                SetState(ShadowEncounterState.Grabbed);
                CueRequested?.Invoke(new NarrativeCue("Raka", "Lepas!", 1.15f, "AUD_C1_GRAB"));
                return;
            }
            BeginCheckpointCaught();
        }

        private void TickGrabbed(float dt)
        {
            stateTimer += dt;
            placeholderVisual?.AnimateGrab(stateTimer / GrabDuration);
            if (stateTimer < GrabDuration)
            {
                return;
            }

            captureFeedback?.FinishGrab();
            placeholderVisual?.ResetPostureImmediate(motor.transform);
            if (grabWillEscape)
            {
                postCaughtGraceTimer = EscapeRecoveryDuration;
                currentBlackoutAlpha = 0f;
                player?.SetTransitionLocked(false);
                SetState(ShadowEncounterState.Pursue);
                CueRequested?.Invoke(new NarrativeCue("Raka", "Aku harus cari tempat sembunyi...", 3f, "AUD_C1_ESCAPE"));
            }
            else
            {
                BeginCheckpointCaught();
            }
        }

        private void BeginCheckpointCaught()
        {
            CaughtCount++;
            successfulHits = 0;
            placeholderVisual?.SetHitVariant(false);
            stateTimer = 0f;
            caughtRestoredPose = false;
            ResetHideTracking();

            // Tertangkap membatalkan callback aktif agar Director tidak mencatat respons/beat berhasil;
            // setelah fade, pemain dipulihkan ke checkpoint dan encounter dapat dimulai ulang.
            activeResolve = null;
            director?.ResetActiveEncounter();
            player?.SetTransitionLocked(true);
            SetState(ShadowEncounterState.Caught);
        }

        private void TickCaught(float dt)
        {
            stateTimer += dt;
            float totalDuration = caughtFadeOutDuration + caughtFadeInDuration;

            if (!caughtRestoredPose && stateTimer >= caughtFadeOutDuration)
            {
                caughtRestoredPose = true;
                currentBlackoutAlpha = 1f;

                // Pindahkan dan nonaktifkan collider bayangan agar tidak menghalangi titik pemulihan pemain.
                SetMonsterActiveInWorld(false, dormantPosition, Quaternion.Euler(0f, 180f, 0f));
                captureFeedback?.ResetFeedback();
                RestoreSafeCheckpointPose();
                string caughtLine = shiftedWorld != null && shiftedWorld.Expanded
                    ? "Stresku memuncak..."
                    : "Napas dulu... aku harus menenangkan diri.";
                CueRequested?.Invoke(new NarrativeCue("Raka", caughtLine, 2.8f, "AUD_C1_CAUGHT"));
            }

            if (!caughtRestoredPose)
            {
                currentBlackoutAlpha = Mathf.Clamp01(stateTimer / caughtFadeOutDuration);
            }
            else
            {
                float fadeInProgress = (stateTimer - caughtFadeOutDuration) / caughtFadeInDuration;
                currentBlackoutAlpha = 1f - Mathf.Clamp01(fadeInProgress);
            }

            if (stateTimer >= totalDuration)
            {
                currentBlackoutAlpha = 0f;
                postCaughtGraceTimer = 0.75f;
                player?.SetTransitionLocked(false);
                SetState(ShadowEncounterState.Dormant);
                if (IsReady)
                {
                    Ready?.Invoke();
                }
            }
        }

        private void RestoreSafeCheckpointPose()
        {
            internalCheckpointRestore = true;
            try
            {
                if (shiftedWorld != null && shiftedWorld.OwnsPlayer)
                {
                    // Tertangkap di lorong horor langsung memaksimalkan stres dan memicu pemuatan ulang bab.
                    player?.GetComponent<StressBarometer>()?.SetStress(StressBarometer.MaxStress);
                    return;
                }

                bool restored = checkpoint != null && checkpoint.TryRestore();
                if (!restored && motor != null)
                {
                    Quaternion fallbackRot = Quaternion.Euler(0f, 10f, 0f);
                    for (int i = 0; i < DormFallbackSpawns.Length; i++)
                    {
                        if (motor.TryRestorePose(DormFallbackSpawns[i], fallbackRot))
                        {
                            break;
                        }
                    }
                }

                player?.GetComponent<StressBarometer>()?.SetStress(StressBarometer.MaxStress);

                // Reset status lintasan agar encounter menunggu pemain melewati batas pemicu lagi.
                director?.ResetCrossing();
            }
            finally
            {
                internalCheckpointRestore = false;
            }
        }

        private void OnPlayerPoseRestored()
        {
            if (internalCheckpointRestore || permanentlyResolved)
            {
                return;
            }
            if (State == ShadowEncounterState.Spawn ||
                State == ShadowEncounterState.Pursue ||
                State == ShadowEncounterState.Grabbed ||
                State == ShadowEncounterState.Caught)
            {
                AbortToDormant(notifyDirectorReset: true);
            }
        }
    }
}
