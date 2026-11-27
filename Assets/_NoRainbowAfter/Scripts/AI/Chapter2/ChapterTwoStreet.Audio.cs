using UnityEngine;

namespace NoRainbowAfter.AI
{
    /// <summary>
    /// Pengelola audio lingkungan, sirene patroli, dan sintesis drone horor pada ChapterTwoStreet.
    ///
    /// [TEKNIS KODING]
    /// - Memastikan seluruh AudioSource jalanan (sirene, hujan, drone rendah) terjeda rapi
    ///   ketika menu pause dibuka lalu dilanjutkan saat unpause.
    /// - CreateHorrorDroneClip() mensintesis sub-bass + LFO (Low Frequency Oscillator) secara prosedural.
    /// </summary>
    public sealed partial class ChapterTwoStreet
    {
        public void PlaySiren(float volume)
        {
            Stage();
            if (siren == null)
            {
                siren = gameObject.AddComponent<AudioSource>();
                siren.playOnAwake = false;
                siren.loop = true;
                siren.clip = Resources.Load<AudioClip>("ChapterTwoAudio/TKPSiren");
            }
            siren.volume = volume;
            sirenPaused = false;
            if (siren.clip != null && !siren.isPlaying)
            {
                siren.Play();
            }
        }

        private void StartAmbienceAudio()
        {
            if (RainAmbienceClip != null)
            {
                if (rainAudio == null)
                {
                    rainAudio = gameObject.AddComponent<AudioSource>();
                    rainAudio.playOnAwake = false;
                    rainAudio.loop = true;
                    rainAudio.spatialBlend = 0f;
                    rainAudio.clip = RainAmbienceClip;
                }
                rainAudio.volume = 0.24f;
                if (!rainAudio.isPlaying)
                {
                    rainAudio.Play();
                }
            }

            if (droneAudio == null)
            {
                droneAudio = gameObject.AddComponent<AudioSource>();
                droneAudio.playOnAwake = false;
                droneAudio.loop = true;
                droneAudio.spatialBlend = 0f;
                droneAudio.volume = 0.16f;
                droneClip = CreateHorrorDroneClip();
                droneAudio.clip = droneClip;
            }
            if (!droneAudio.isPlaying)
            {
                droneAudio.Play();
            }

            if (ElectricTickClip != null && crackleAudio == null)
            {
                crackleAudio = gameObject.AddComponent<AudioSource>();
                crackleAudio.playOnAwake = false;
                crackleAudio.spatialBlend = 0f;
                crackleAudio.volume = 0.18f;
            }
        }

        private static AudioClip CreateHorrorDroneClip()
        {
            const int rate = 22050;
            const int seconds = 4;
            int count = rate * seconds;
            var data = new float[count];

            // Gabungan frekuensi berdekatan (41.2 Hz & 43.0 Hz) menghasilkan efek beating lambat 1.8 Hz.
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate;
                float lfo = 0.72f + 0.28f * Mathf.Sin(2f * Mathf.PI * 0.25f * t);
                float wave = Mathf.Sin(2f * Mathf.PI * 41.2f * t) * 0.55f
                           + Mathf.Sin(2f * Mathf.PI * 43.0f * t) * 0.35f
                           + Mathf.Sin(2f * Mathf.PI * 82.4f * t) * 0.15f;
                data[i] = wave * lfo * 0.65f;
            }

            var clip = AudioClip.Create("Market dread drone", count, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void SyncAudioPause(bool paused)
        {
            if (siren != null)
            {
                if (paused && siren.isPlaying)
                {
                    siren.Pause();
                    sirenPaused = true;
                }
                else if (!paused && sirenPaused && !Boarded)
                {
                    siren.UnPause();
                    sirenPaused = false;
                }
            }

            if (rainAudio != null)
            {
                if (paused && rainAudio.isPlaying)
                {
                    rainAudio.Pause();
                }
                else if (!paused && !rainAudio.isPlaying && motor != null)
                {
                    rainAudio.UnPause();
                }
            }

            if (droneAudio != null)
            {
                if (paused && droneAudio.isPlaying)
                {
                    droneAudio.Pause();
                }
                else if (!paused && !Boarded && !droneAudio.isPlaying && motor != null)
                {
                    droneAudio.UnPause();
                }
            }
        }
    }
}
