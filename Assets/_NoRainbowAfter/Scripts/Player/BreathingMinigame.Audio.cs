using UnityEngine;

namespace NoRainbowAfter.Player
{
    /// <summary>
    /// Audio napas, detak jantung tenang, peredaman audio ambien (low-pass ducking),
    /// serta sintesis AudioClip darurat pada BreathingMinigame.
    /// </summary>
    public sealed partial class BreathingMinigame
    {
        private void UpdateBreathAudio(bool holdInhale)
        {
            if (audioSource == null)
            {
                return;
            }
            if (holdInhale == wasInhalingAudio && audioSource.isPlaying)
            {
                return;
            }

            if (holdInhale && !wasInhalingAudio)
            {
                PlayClip(inhaleClip ?? GetFallbackClip(isInhale: true));
                wasInhalingAudio = true;
            }
            else if (!holdInhale && wasInhalingAudio)
            {
                PlayClip(exhaleClip ?? GetFallbackClip(isInhale: false));
                wasInhalingAudio = false;
            }
        }

        private void UpdateCalmHeartbeat(float dt)
        {
            if (calmHeartbeatSource == null)
            {
                return;
            }
            if (player != null && player.IsPaused)
            {
                if (calmHeartbeatSource.isPlaying) calmHeartbeatSource.Pause();
                return;
            }

            float stressNorm = stressBarometer != null ? stressBarometer.NormalizedStress : 0.5f;
            float targetPitch = IsInGreenZone
                ? Mathf.Lerp(0.72f, 0.98f, stressNorm)
                : Mathf.Lerp(0.92f, 1.25f, stressNorm);
            float targetVol = Mathf.Lerp(0.14f, 0.36f, stressNorm);

            float smooth = dt > 0f ? 1f - Mathf.Exp(-2.5f * dt) : 1f;
            calmHeartbeatSource.pitch = Mathf.Lerp(calmHeartbeatSource.pitch, targetPitch, smooth);
            calmHeartbeatSource.volume = Mathf.Lerp(calmHeartbeatSource.volume, targetVol, smooth);
            if (!calmHeartbeatSource.isPlaying)
            {
                calmHeartbeatSource.Play();
            }
        }

        private void BeginAudioDucking()
        {
            RestoreDuckedAudio();
            var sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude);
            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource src = sources[i];
                if (src == null || src == audioSource || src == calmHeartbeatSource)
                {
                    continue;
                }
                if (src.gameObject == gameObject && src.clip != null && src.clip.name == "RakaStressHeartbeat")
                {
                    continue;
                }

                var filter = src.GetComponent<AudioLowPassFilter>();
                bool added = false;
                float origCutoff = NormalLowPassCutoff;
                if (filter == null)
                {
                    filter = src.gameObject.AddComponent<AudioLowPassFilter>();
                    filter.cutoffFrequency = NormalLowPassCutoff;
                    added = true;
                }
                else
                {
                    origCutoff = filter.cutoffFrequency;
                }

                duckedAudioSources.Add(new DuckedAudioEntry
                {
                    Source = src,
                    OriginalVolume = src.volume,
                    Filter = filter,
                    AddedFilter = added,
                    OriginalCutoff = origCutoff
                });
            }
        }

        private void UpdateAudioDucking(float dt)
        {
            float smooth = dt > 0f ? 1f - Mathf.Exp(-5f * dt) : 1f;
            for (int i = 0; i < duckedAudioSources.Count; i++)
            {
                var entry = duckedAudioSources[i];
                if (entry.Source != null)
                {
                    entry.Source.volume = Mathf.Lerp(entry.Source.volume, entry.OriginalVolume * AmbientDuckFactor, smooth);
                }
                if (entry.Filter != null)
                {
                    entry.Filter.cutoffFrequency = Mathf.Lerp(entry.Filter.cutoffFrequency, AmbientLowPassCutoff, smooth);
                }
            }
        }

        private void RestoreDuckedAudio()
        {
            for (int i = 0; i < duckedAudioSources.Count; i++)
            {
                var entry = duckedAudioSources[i];
                if (entry.Source != null)
                {
                    entry.Source.volume = entry.OriginalVolume;
                }
                if (entry.Filter != null)
                {
                    if (entry.AddedFilter) Destroy(entry.Filter);
                    else entry.Filter.cutoffFrequency = entry.OriginalCutoff;
                }
            }
            duckedAudioSources.Clear();
        }

        private void PlayClip(AudioClip clip)
        {
            if (audioSource == null || clip == null)
            {
                return;
            }
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.volume = 0.58f;
            audioSource.Play();
        }

        private AudioClip GetFallbackClip(bool isInhale)
        {
            ref AudioClip cache = ref (isInhale ? ref runtimeInhaleFallback : ref runtimeExhaleFallback);
            if (cache != null)
            {
                return cache;
            }

            const int sampleRate = 22050;
            const int sampleCount = (int)(sampleRate * 2.2f);
            float[] samples = new float[sampleCount];
            var rng = new System.Random(isInhale ? 71 : 93);
            float lp = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float progress = (float)i / sampleCount;
                float env = Mathf.Sin(Mathf.PI * progress);
                float cutoff = isInhale ? (0.07f + 0.08f * progress) : (0.13f - 0.08f * progress);
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += cutoff * (white - lp);
                samples[i] = Mathf.Clamp(lp * env * 0.45f, -0.9f, 0.9f);
            }

            cache = AudioClip.Create(isInhale ? "RakaBreathInhale_Runtime" : "RakaBreathExhale_Runtime", sampleCount, 1, sampleRate, false);
            cache.SetData(samples, 0);
            return cache;
        }

        private AudioClip GetHeartbeatClip()
        {
            if (runtimeHeartbeatClip != null)
            {
                return runtimeHeartbeatClip;
            }
            const int rate = 22050;
            const float duration = 1.1f;
            int samples = Mathf.RoundToInt(rate * duration);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / rate;
                float lub = Mathf.Exp(-Mathf.Pow((t - 0.14f) / 0.045f, 2f)) * Mathf.Sin(2f * Mathf.PI * 48f * t);
                float dub = Mathf.Exp(-Mathf.Pow((t - 0.34f) / 0.055f, 2f)) * Mathf.Sin(2f * Mathf.PI * 42f * t) * 0.72f;
                data[i] = Mathf.Clamp((lub + dub) * 0.85f, -1f, 1f);
            }
            runtimeHeartbeatClip = AudioClip.Create("RakaCalmHeartbeat_Runtime", samples, 1, rate, false);
            runtimeHeartbeatClip.SetData(data, 0);
            return runtimeHeartbeatClip;
        }
    }
}
