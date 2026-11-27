using UnityEngine;

namespace NoRainbowAfter.AI
{
    /// <summary>
    /// Audio prosedural dan antarmuka peringatan paparan (IMGUI) untuk ChapterTwoSpotlightEncounter.
    ///
    /// [TEKNIS KODING]
    /// - AudioClip dibuat langsung di memori menggunakan rumus gelombang sinus + envelope eksponensial
    ///   tanpa bergantung pada file WAV eksternal (alert, detak tegang loop 1 detik, efek tertangkap).
    /// - Tekstur vignet merah 64x64 dihasilkan secara prosedural di CPU memakai jarak Euclidean radial
    ///   dan kurva SmoothStep.
    /// </summary>
    public sealed partial class ChapterTwoSpotlightEncounter
    {
        // =========================================================================
        // Sintesis Audio Prosedural (PCM Sample Generation)
        // =========================================================================

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

            // 1. Bunyi peringatan pertama kali tersorot: menggabungkan dua nada disonan dengan dering metalik.
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

            // 2. Detak jantung ganda (lub-dub) + dengung rendah yang berulang mulus setiap 1 detik.
            int tensionSamples = rate;
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

            // 3. Bunyi tertangkap: sweep frekuensi rendah (145 -> 44 Hz), nada tinggi, dan derau radio deterministik.
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

        private void UpdateTensionAudio()
        {
            if (tensionSource == null || tensionClip == null)
            {
                return;
            }
            float ratio = CatchSeconds > 0f ? Mathf.Clamp01(Exposure / CatchSeconds) : 0f;
            if (ratio > 0.01f)
            {
                tensionSource.volume = Mathf.Lerp(0.08f, 0.55f, ratio);
                tensionSource.pitch = Mathf.Lerp(0.95f, 1.48f, ratio);
                if (!tensionSource.isPlaying)
                {
                    tensionSource.Play();
                }
            }
            else if (tensionSource.isPlaying)
            {
                tensionSource.Stop();
            }
        }

        // =========================================================================
        // Tekstur Prosedural Vignet & Indikator Paparan (IMGUI HUD)
        // =========================================================================

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

                // [GRAFIKA KOMPUTER] Bangkitkan masker alfa radial untuk efek vignet di tepi layar.
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
            if (motor == null)
            {
                return;
            }
            if (player != null && (player.IsPaused || player.IsTransitionLocked))
            {
                return;
            }

            float ratio = CatchSeconds > 0f ? Mathf.Clamp01(Exposure / CatchSeconds) : 0f;
            if (ratio <= 0.001f && catchFlash <= 0.001f)
            {
                return;
            }

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
                warningLabelStyle.normal.textColor = new Color(
                    1f,
                    Mathf.Lerp(0.82f, 0.28f, ratio),
                    Mathf.Lerp(0.65f, 0.24f, ratio),
                    Mathf.Clamp01(ratio * 3f));
                GUI.color = Color.white;
                GUI.Label(
                    new Rect(x - 40f, y - 28f, barWidth + 80f, 24f),
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
    }
}
