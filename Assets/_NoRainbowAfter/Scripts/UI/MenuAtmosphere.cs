using UnityEngine;
using UnityEngine.UI;

namespace NoRainbowAfter.UI
{
    /// <summary>Fits the artwork without distortion and animates a restrained menu atmosphere.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class MenuAtmosphere : MonoBehaviour
    {
        [SerializeField] private RawImage artwork;
        [SerializeField] private RawImage weather;
        [SerializeField, Range(0f, 1f)] private float motionAmount = 0.35f;
        private Material weatherInstance;
        private RectTransform viewport;
        private Rect baseUv;

        private void Awake()
        {
            viewport = (RectTransform)transform;
            if (weather != null && weather.material != null)
            {
                weatherInstance = new Material(weather.material);
                weather.material = weatherInstance;
            }
            FitArtwork();
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
            width /= 1.018f;
            height /= 1.018f;
            baseUv = new Rect((1f - width) * 0.52f, (1f - height) * 0.5f, width, height);
            artwork.uvRect = baseUv;
        }

        private void Update()
        {
            float time = Time.unscaledTime;
            if (artwork != null)
            {
                Rect uv = baseUv;
                uv.x += Mathf.Sin(time * 0.075f) * 0.004f * motionAmount;
                uv.y += Mathf.Sin(time * 0.061f + 1.2f) * 0.0025f * motionAmount;
                artwork.uvRect = uv;
            }
            if (weatherInstance != null)
            {
                weatherInstance.SetFloat("_WeatherTime", time);
                weatherInstance.SetFloat("_Aspect", viewport.rect.width / Mathf.Max(1f, viewport.rect.height));
            }
        }

        private void OnDestroy()
        {
            if (weatherInstance != null) Destroy(weatherInstance);
        }
    }
}
