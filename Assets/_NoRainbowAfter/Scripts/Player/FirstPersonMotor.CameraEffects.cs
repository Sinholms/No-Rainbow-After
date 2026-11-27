using UnityEngine;

namespace NoRainbowAfter.Player
{
    /// <summary>
    /// Efek kamera akibat stres (ayunan asimetris langkah lelah, tunnel vision FOV) serta
    /// sinkronisasi pitch/FOV/tinggi mata dari latihan napas pada FirstPersonMotor.
    /// </summary>
    public sealed partial class FirstPersonMotor
    {
        public void SetViewSettings(float sensitivity, float fov)
        {
            mouseSensitivity = Mathf.Clamp(sensitivity, 0.01f, 0.3f);
            fieldOfView = Mathf.Clamp(fov, 65f, 95f);
            if (viewCamera != null)
            {
                viewCamera.fieldOfView = fieldOfView;
            }
        }

        private float ReadCurrentStress()
        {
            if (stressBarometer == null)
            {
                stressBarometer = GetComponent<StressBarometer>();
            }
            return stressBarometer != null ? stressBarometer.CurrentStress : 0f;
        }

        private void UpdateStressCamera(float stress, float actualSpeed, float dt)
        {
            if (StressGaitIntensity <= 0.0001f)
            {
                gaitBlend = 0f;
                StressRollOffset = 0f;
                StressPitchOffset = 0f;
                StressEyeOffset = Vector3.zero;
                ApplyFieldOfView(0f, 1f);
                return;
            }

            float refSpeed = Mathf.Max(0.4f, walkSpeed * MovementScale * 0.85f);
            float moveActivity = Mathf.Clamp01(actualSpeed / refSpeed);
            gaitBlend = Mathf.MoveTowards(gaitBlend, moveActivity, (moveActivity > gaitBlend ? 4.5f : 3.2f) * dt);

            if (moveActivity > 0.05f)
            {
                float cadence = IsSprinting ? 8.8f : (IsCrouched ? 4.6f : 5.6f);
                gaitPhase += dt * cadence * Mathf.Lerp(1f, 0.84f, StressGaitIntensity);
            }
            idleSwayClock += dt;

            float idleIntensity = stress > StressLimpOnset
                ? Mathf.InverseLerp(StressLimpOnset, StressBarometer.MaxStress, stress)
                : 0f;
            float idleRoll = Mathf.Sin(idleSwayClock * 0.95f) * 0.85f * idleIntensity;
            float idlePitch = Mathf.Sin(idleSwayClock * 0.65f + 0.8f) * 0.55f * idleIntensity;

            float walkWeight = StressGaitIntensity * gaitBlend * (IsCrouched ? 0.58f : 1f);
            float asymWave = Mathf.Sin(gaitPhase) + 0.32f * Mathf.Sin(2f * gaitPhase + 0.4f);
            float stepDip = Mathf.Abs(Mathf.Sin(gaitPhase));

            float targetRoll = asymWave * 3.1f * walkWeight + idleRoll;
            float targetPitch = 1.25f * StressGaitIntensity * gaitBlend + stepDip * 1.85f * walkWeight + idlePitch;
            Vector3 targetEyeOffset = new Vector3(
                Mathf.Sin(gaitPhase) * 0.032f * walkWeight,
                -stepDip * 0.044f * walkWeight - 0.014f * StressGaitIntensity * gaitBlend,
                0f);

            float smooth = 1f - Mathf.Exp(-10f * dt);
            StressRollOffset = Mathf.Lerp(StressRollOffset, targetRoll, smooth);
            StressPitchOffset = Mathf.Lerp(StressPitchOffset, targetPitch, smooth);
            StressEyeOffset = Vector3.Lerp(StressEyeOffset, targetEyeOffset, smooth);

            if (viewCamera != null)
            {
                float tunnel = stress > StressTunnelOnset
                    ? Mathf.InverseLerp(StressTunnelOnset, StressBarometer.MaxStress, stress)
                    : 0f;
                float tunnelOffset = tunnel > 0.001f
                    ? -4.2f * tunnel + Mathf.Sin(idleSwayClock * 1.4f) * 0.65f * tunnel
                    : 0f;
                ApplyFieldOfView(tunnelOffset, smooth);
            }
        }

        private void ClearStressCameraOffsets()
        {
            gaitBlend = 0f;
            StressRollOffset = 0f;
            StressPitchOffset = 0f;
            StressEyeOffset = Vector3.zero;
            if (viewCamera != null) viewCamera.fieldOfView = fieldOfView;
            ApplyViewRotation();
            if (controller != null && viewCamera != null) UpdateEyeHeight();
        }

        public void SetBreathingPitchOffset(float pitchOffsetDegrees)
        {
            breathingPitchOffset = Mathf.Clamp(pitchOffsetDegrees, -15f, 15f);
            ApplyViewRotation();
        }

        public void SetBreathingCameraEffects(float pitchOffsetDegrees, float fovOffsetDegrees, float eyeHeightOffsetMeters, Vector2 shakeDegrees)
        {
            breathingPitchOffset = Mathf.Clamp(pitchOffsetDegrees, -15f, 15f);
            breathingFovOffset = Mathf.Clamp(fovOffsetDegrees, -5f, 6f);
            breathingEyeOffsetY = Mathf.Clamp(eyeHeightOffsetMeters, -0.08f, 0.08f);
            breathingShakeOffset = new Vector2(
                Mathf.Clamp(shakeDegrees.x, -3f, 3f),
                Mathf.Clamp(shakeDegrees.y, -3f, 3f));
            ApplyViewRotation();
            ApplyFieldOfView(0f, 1f);
            if (controller != null && viewCamera != null)
            {
                UpdateEyeHeight();
            }
        }

        private void ApplyFieldOfView(float stressTunnelOffset, float blend)
        {
            if (viewCamera == null)
            {
                return;
            }
            float targetFov = Mathf.Clamp(fieldOfView + stressTunnelOffset + breathingFovOffset, 60f, 95f);
            viewCamera.fieldOfView = blend >= 0.999f
                ? targetFov
                : Mathf.Lerp(viewCamera.fieldOfView, targetFov, blend);
        }

        private void ApplyViewRotation()
        {
            if (viewCamera != null)
            {
                float combinedPitch = Mathf.Clamp(pitch + breathingPitchOffset + StressPitchOffset + breathingShakeOffset.x, -85f, 85f);
                float combinedYaw = breathingShakeOffset.y;
                viewCamera.transform.localRotation = Quaternion.Euler(combinedPitch, combinedYaw, StressRollOffset);
            }
        }

        private void UpdateEyeHeight()
        {
            float blend = Mathf.InverseLerp(crouchingHeight, standingHeight, controller.height);
            viewCamera.transform.localPosition = Vector3.up * (Mathf.Lerp(crouchingEyeHeight, standingEyeHeight, blend) + breathingEyeOffsetY) + StressEyeOffset;
        }
    }
}
