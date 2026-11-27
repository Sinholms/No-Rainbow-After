using UnityEngine;

namespace NoRainbowAfter.Interaction
{
    /// <summary>
    /// Sintesis suara ketikan keyboard, bunyi berhasil kirim kalimat, dan bunyi typo/error
    /// untuk LaptopTypingMinigame secara prosedural.
    /// </summary>
    public sealed partial class LaptopTypingMinigame
    {
        private void PrepareTypingSound()
        {
            if (typingClip == null)
            {
                const int sampleRate = 22050;
                var samples = new float[2205];
                var noise = new System.Random(34);
                for (int i = 0; i < samples.Length; i++)
                {
                    float seconds = i / (float)sampleRate;
                    float click = (float)(noise.NextDouble() * 2 - 1) * Mathf.Exp(-seconds * 128f);
                    float resonance = Mathf.Sin(seconds * 2f * Mathf.PI * 620f) * Mathf.Exp(-seconds * 68f);
                    samples[i] = (click * .5f + resonance * .18f) * Mathf.Clamp01(i / 8f);
                }
                typingClip = AudioClip.Create("Report keystroke", samples.Length, 1, sampleRate, false);
                typingClip.SetData(samples, 0);
            }

            if (submitClip == null)
            {
                const int sampleRate = 22050;
                var samples = new float[(int)(sampleRate * 0.14f)];
                for (int i = 0; i < samples.Length; i++)
                {
                    float s = i / (float)sampleRate;
                    float env = Mathf.Exp(-s * 18f) * Mathf.Clamp01(i / 24f);
                    samples[i] = (Mathf.Sin(2f * Mathf.PI * 660f * s) * 0.45f + Mathf.Sin(2f * Mathf.PI * 880f * s) * 0.35f) * env * 0.4f;
                }
                submitClip = AudioClip.Create("Laptop submit", samples.Length, 1, sampleRate, false);
                submitClip.SetData(samples, 0);
            }

            if (errorClip == null)
            {
                const int sampleRate = 22050;
                var samples = new float[(int)(sampleRate * 0.10f)];
                for (int i = 0; i < samples.Length; i++)
                {
                    float s = i / (float)sampleRate;
                    float env = Mathf.Exp(-s * 28f) * Mathf.Clamp01(i / 16f);
                    samples[i] = Mathf.Sin(2f * Mathf.PI * 155f * s) * env * 0.35f;
                }
                errorClip = AudioClip.Create("Laptop error", samples.Length, 1, sampleRate, false);
                errorClip.SetData(samples, 0);
            }

            if (typingSource == null)
            {
                typingSource = gameObject.AddComponent<AudioSource>();
                typingSource.playOnAwake = false;
                typingSource.volume = .25f;
                typingSource.clip = typingClip;
            }
        }

        private void PlayTypingSound()
        {
            if (typingSource != null && typingClip != null)
            {
                typingSource.PlayOneShot(typingClip, 1f);
            }
        }

        private void PlaySubmitSound()
        {
            if (typingSource != null && submitClip != null)
            {
                typingSource.PlayOneShot(submitClip, 0.85f);
            }
        }

        private void PlayErrorSound()
        {
            if (typingSource != null && errorClip != null)
            {
                typingSource.PlayOneShot(errorClip, 0.75f);
            }
        }
    }
}
