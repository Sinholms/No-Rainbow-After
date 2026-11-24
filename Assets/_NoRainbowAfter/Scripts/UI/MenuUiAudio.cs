using UnityEngine;

namespace NoRainbowAfter.UI
{
    /// <summary>
    /// Shared lightweight procedural audio feedback for Main Menu and Pause Menu.
    /// Produces restrained, atmospheric psychological-horror tactile clicks and transitions
    /// without external asset dependencies, working reliably while Time.timeScale == 0.
    /// </summary>
    public static class MenuUiAudio
    {
        private static AudioSource sharedSource;
        private static AudioClip hoverClip;
        private static AudioClip clickClip;
        private static AudioClip transitionClip;
        private static float lastHoverTime = -10f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            sharedSource = null;
            hoverClip = null;
            clickClip = null;
            transitionClip = null;
            lastHoverTime = -10f;
        }

        private sealed class AudioHostLifecycle : MonoBehaviour
        {
            private void OnDestroy()
            {
                if (hoverClip != null) Object.Destroy(hoverClip);
                if (clickClip != null) Object.Destroy(clickClip);
                if (transitionClip != null) Object.Destroy(transitionClip);
                ResetStatics();
            }
        }

        public static void PlayHover()
        {
            if (Time.unscaledTime - lastHoverTime < 0.055f) return;
            lastHoverTime = Time.unscaledTime;
            PlayOneShot(GetHoverClip(), 0.22f);
        }

        public static void PlayConfirm()
        {
            PlayOneShot(GetClickClip(), 0.32f);
        }

        public static void PlayPanelTransition()
        {
            PlayOneShot(GetTransitionClip(), 0.26f);
        }

        private static void PlayOneShot(AudioClip clip, float volume)
        {
            if (clip == null || !Application.isPlaying) return;
            AudioSource source = EnsureSource();
            if (source == null) return;
            source.PlayOneShot(clip, volume);
        }

        private static AudioSource EnsureSource()
        {
            if (sharedSource != null) return sharedSource;
            var host = new GameObject("NRA_MenuUiAudio");
            Object.DontDestroyOnLoad(host);
            host.hideFlags = HideFlags.HideInHierarchy;
            host.AddComponent<AudioHostLifecycle>();
            sharedSource = host.AddComponent<AudioSource>();
            sharedSource.playOnAwake = false;
            sharedSource.loop = false;
            sharedSource.spatialBlend = 0f;
            sharedSource.ignoreListenerPause = true;
            return sharedSource;
        }

        private static AudioClip GetHoverClip()
        {
            if (hoverClip != null) return hoverClip;
            const int sampleRate = 22050;
            const float duration = 0.065f;
            int count = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[count];
            float prevNoise = 0f;
            uint seed = 0x51A921u;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 68f) * Mathf.Clamp01(t / 0.004f);
                seed = seed * 1664525u + 1013904223u;
                float white = ((seed >> 9) / 8388608f) - 1f;
                prevNoise = Mathf.Lerp(prevNoise, white, 0.22f);
                float tone = Mathf.Sin(2f * Mathf.PI * (340f - t * 900f) * t) * 0.45f;
                data[i] = (tone + prevNoise * 0.35f) * env * 0.55f;
            }
            hoverClip = AudioClip.Create("NRA_UiHover", count, 1, sampleRate, false);
            hoverClip.SetData(data, 0);
            return hoverClip;
        }

        private static AudioClip GetClickClip()
        {
            if (clickClip != null) return clickClip;
            const int sampleRate = 22050;
            const float duration = 0.18f;
            int count = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[count];
            float prevNoise = 0f;
            uint seed = 0x73C219u;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float attack = Mathf.Clamp01(t / 0.003f);
                float bodyEnv = Mathf.Exp(-t * 17f) * attack;
                float knockEnv = Mathf.Exp(-t * 58f) * attack;
                seed = seed * 1664525u + 1013904223u;
                float white = ((seed >> 9) / 8388608f) - 1f;
                prevNoise = Mathf.Lerp(prevNoise, white, 0.18f);
                float low = Mathf.Sin(2f * Mathf.PI * (128f - t * 42f) * t) * 0.62f;
                float harmonic = Mathf.Sin(2f * Mathf.PI * 256f * t) * 0.22f;
                data[i] = ((low + harmonic) * bodyEnv + prevNoise * 0.38f * knockEnv) * 0.72f;
            }
            clickClip = AudioClip.Create("NRA_UiConfirm", count, 1, sampleRate, false);
            clickClip.SetData(data, 0);
            return clickClip;
        }

        private static AudioClip GetTransitionClip()
        {
            if (transitionClip != null) return transitionClip;
            const int sampleRate = 22050;
            const float duration = 0.24f;
            int count = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[count];
            float lp = 0f;
            uint seed = 0x19B8A3u;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float u = t / duration;
                float env = Mathf.Sin(u * Mathf.PI) * Mathf.Exp(-u * 1.4f);
                seed = seed * 1664525u + 1013904223u;
                float white = ((seed >> 9) / 8388608f) - 1f;
                lp = Mathf.Lerp(lp, white, 0.09f);
                float hum = Mathf.Sin(2f * Mathf.PI * (96f + u * 28f) * t) * 0.28f;
                data[i] = (lp * 0.65f + hum) * env * 0.55f;
            }
            transitionClip = AudioClip.Create("NRA_UiTransition", count, 1, sampleRate, false);
            transitionClip.SetData(data, 0);
            return transitionClip;
        }
    }
}
