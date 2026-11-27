using NoRainbowAfter.Narrative;
using NoRainbowAfter.Player;
using NoRainbowAfter.Quality;
using UnityEngine;
using UnityEngine.UI;

namespace NoRainbowAfter.UI
{
    /// <summary>
    /// Pembaruan panel pengaturan, status kondisi mental/bab di menu jeda, pilihan kualitas grafis URP,
    /// serta masker bidang pandang melingkar pada FirstPersonSession.
    /// </summary>
    public sealed partial class FirstPersonSession
    {
        private void OnStressChanged(float _)
        {
            if (IsPaused)
            {
                RefreshPauseContext();
            }
        }

        private void RefreshPauseContext()
        {
            if (chapterStatusLabel != null)
            {
                chapterStatusLabel.text = ResolveChapterLabel();
            }
            if (stressBarometer == null && player != null)
            {
                stressBarometer = player.GetComponent<StressBarometer>();
            }
            if (stressStatusLabel == null)
            {
                return;
            }

            if (stressBarometer != null)
            {
                float stress = stressBarometer.CurrentStress;
                float norm = stressBarometer.NormalizedStress;
                string mood = stress >= 80f ? "Tertekan Berat"
                    : stress >= 65f ? "Tegang"
                    : stress >= 45f ? "Gelisah"
                    : "Tenang";
                stressStatusLabel.text = "Kondisi Mental  ·  " + mood + " (" + Mathf.RoundToInt(stress) + "%)";
                if (stressFillBar != null)
                {
                    stressFillBar.anchorMax = new Vector2(Mathf.Clamp01(norm), 1f);
                }
                if (stressFillImage != null)
                {
                    stressFillImage.color = stress >= 70f
                        ? new Color(0.76f, 0.24f, 0.22f, 0.95f)
                        : stress >= 45f
                            ? new Color(0.84f, 0.65f, 0.39f, 0.92f)
                            : new Color(0.42f, 0.66f, 0.72f, 0.90f);
                }
            }
            else
            {
                stressStatusLabel.text = "Mode Latihan Kontrol";
                if (stressFillBar != null)
                {
                    stressFillBar.anchorMax = new Vector2(0.25f, 1f);
                }
            }
        }

        private string ResolveChapterLabel()
        {
            if (chapterDirector == null)
            {
                chapterDirector = FindAnyObjectByType<ChapterDirector>();
            }
            if (chapterDirector == null || chapterDirector.State == null)
            {
                return "UJI KONTROL  ·  ORANG PERTAMA";
            }

            switch (chapterDirector.State.ActiveChapter)
            {
                case "Chapter2":
                    return "BAB II  ·  PASAR MALAM";
                case "Chapter3":
                    return "BAB III  ·  PERJALANAN";
                default:
                    return "BAB I  ·  KOS RAKA";
            }
        }

        private void ApplySettings()
        {
            motor.SetViewSettings(sensitivity.value, fov.value);
            PlayerPrefs.SetFloat("NRA.MouseSensitivity", motor.MouseSensitivity);
            PlayerPrefs.SetFloat("NRA.FieldOfView", motor.FieldOfView);
            UpdateLabels();
        }

        private void OnMasterVolumeChanged(float value)
        {
            AudioListener.volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("NRA.MasterVolume", AudioListener.volume);
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            sensitivityLabel.text = "Sensitivitas mouse  " + motor.MouseSensitivity.ToString("F2");
            fovLabel.text = "Bidang pandang  " + Mathf.RoundToInt(motor.FieldOfView) + "°";
            if (viewRadiusLabel != null)
                viewRadiusLabel.text = "Lebar pandang  " + Mathf.RoundToInt(circularViewRadius * 100f) + "%";
            if (masterVolumeLabel != null)
                masterVolumeLabel.text = "Volume utama  " + Mathf.RoundToInt(AudioListener.volume * 100f) + "%";
            if (qualityPresetLabel != null)
                qualityPresetLabel.text = "Kualitas grafis  —  " + GraphicsQualityService.GetDisplayLabel(GraphicsQualityService.CurrentPreset);
        }

        public void SelectQualityPreset(QualityPreset preset)
        {
            GraphicsQualityService.ApplyPreset(preset, savePreference: true);
            MenuUiAudio.PlayConfirm();
        }

        private void OnQualityPresetChanged(QualityPreset preset)
        {
            UpdateLabels();
            UpdateQualityButtons(preset);
        }

        private void UpdateQualityButtons(QualityPreset activePreset)
        {
            StyleQualityButton(qualityLowButton, activePreset == QualityPreset.Low);
            StyleQualityButton(qualityMediumButton, activePreset == QualityPreset.Medium);
            StyleQualityButton(qualityHighButton, activePreset == QualityPreset.High);
        }

        private static void StyleQualityButton(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }
            var bg = button.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = selected ? QualitySelectedBg : QualityIdleBg;
            }

            Transform activeBar = button.transform.Find("ActiveBar");
            if (activeBar != null) activeBar.gameObject.SetActive(selected);

            Transform outline = button.transform.Find("Border");
            if (outline != null) outline.gameObject.SetActive(false);

            var caption = button.GetComponentInChildren<Text>(true);
            if (caption != null)
            {
                caption.color = selected ? QualitySelectedText : QualityIdleText;
                caption.fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        private void OnCircularViewChanged(bool enabled)
        {
            circularViewEnabled = enabled;
            PlayerPrefs.SetInt("NRA.CircularViewEnabled", enabled ? 1 : 0);
            MenuUiAudio.PlayHover();
            ApplyCircularView();
        }

        private void OnViewRadiusChanged(float radius)
        {
            circularViewRadius = Mathf.Clamp(radius, 0.45f, 0.80f);
            PlayerPrefs.SetFloat("NRA.CircularViewRadius", circularViewRadius);
            UpdateLabels();
            ApplyCircularView();
        }

        private void ApplyCircularView()
        {
            if (viewMask != null)
            {
                viewMask.ApplySettings(circularViewEnabled && !IsPaused, circularViewRadius);
            }
        }
    }
}
