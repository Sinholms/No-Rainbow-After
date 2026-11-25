using UnityEngine;
using UnityEngine.UI;

namespace NoRainbowAfter.UI
{
    /// <summary>Fits the artwork without distortion, animates a restrained menu atmosphere, and fades in rain ambience.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class MenuAtmosphere : MonoBehaviour
    {
        [SerializeField] private RawImage artwork;
        [SerializeField] private RawImage weather;
        [SerializeField, Range(0f, 1f)] private float motionAmount = 0.42f;
        [SerializeField] private AudioClip rainAmbienceClip;
        [SerializeField, Range(0f, 1f)] private float rainTargetVolume = 0.24f;
        private Material weatherInstance;
        private RectTransform viewport;
        private Rect baseUv = new Rect(0f, 0f, 1f, 1f);
        private AudioSource rainSource;
        private float fadeElapsed;

        private void Awake()
        {
            viewport = (RectTransform)transform;
            if (weather != null && weather.material != null)
            {
                weatherInstance = new Material(weather.material);
                weather.material = weatherInstance;
            }
            FitArtwork();
            StartRainAmbience();
        }

        private void StartRainAmbience()
        {
            if (rainAmbienceClip == null || !Application.isPlaying) return;
            rainSource = GetComponent<AudioSource>();
            if (rainSource == null) rainSource = gameObject.AddComponent<AudioSource>();
            rainSource.clip = rainAmbienceClip;
            rainSource.loop = true;
            rainSource.playOnAwake = false;
            rainSource.spatialBlend = 0f;
            rainSource.pitch = 0.94f;
            rainSource.volume = 0f;
            rainSource.Play();
            fadeElapsed = 0f;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (viewport != null) FitArtwork();
        }

        private void FitArtwork()
        {
            if (artwork == null || artwork.texture == null || viewport == null) return;
            float screenAspect = viewport.rect.width / Mathf.Max(1f, viewport.rect.height);
            float imageAspect = (float)artwork.texture.width / artwork.texture.height;
            float width = screenAspect < imageAspect ? screenAspect / imageAspect : 1f;
            float height = screenAspect > imageAspect ? imageAspect / screenAspect : 1f;
            // Small overscan keeps the edges covered during the slow camera drift.
            width /= 1.024f;
            height /= 1.024f;
            baseUv = new Rect((1f - width) * 0.56f, (1f - height) * 0.5f, width, height);
            artwork.uvRect = baseUv;
        }

        private void Update()
        {
            float time = Time.unscaledTime;
            if (artwork != null && artwork.texture != null)
            {
                Rect uv = baseUv;
                uv.x += Mathf.Sin(time * 0.072f) * 0.005f * motionAmount;
                uv.y += Mathf.Sin(time * 0.055f + 1.2f) * 0.003f * motionAmount;
                artwork.uvRect = uv;
            }
            if (weatherInstance != null && viewport != null)
            {
                weatherInstance.SetFloat("_WeatherTime", time);
                weatherInstance.SetFloat("_Aspect", viewport.rect.width / Mathf.Max(1f, viewport.rect.height));
            }
            if (rainSource != null && rainSource.isPlaying && fadeElapsed < 1.6f)
            {
                fadeElapsed += Time.unscaledDeltaTime;
                rainSource.volume = Mathf.Lerp(0f, rainTargetVolume, Mathf.Clamp01(fadeElapsed / 1.6f));
            }
        }

        private void OnDestroy()
        {
            if (weatherInstance != null) Destroy(weatherInstance);
        }
    }
}
