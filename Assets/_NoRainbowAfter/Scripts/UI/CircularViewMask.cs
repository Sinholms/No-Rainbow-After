using UnityEngine;
using UnityEngine.UI;

namespace NoRainbowAfter.UI
{
    /// <summary>
    /// Mengendalikan RawImage ber-shader masker pandang melingkar (NoRainbowAfter/UI/CircularViewMask),
    /// memperbarui rasio aspek layar, radius, dan kelembutan tepi secara real-time di LateUpdate.
    /// </summary>
    public sealed class CircularViewMask : MonoBehaviour
    {
        [SerializeField] private RawImage target;
        [SerializeField] private Shader maskShader;
        [SerializeField, Range(0.45f, 0.80f)] private float radius = 0.50f;
        [SerializeField, Min(0.001f)] private float softness = 0.11f;

        private Material runtimeMaterial;
        private Material originalMaterial;
        private bool effectEnabled = true;

        public float Radius => radius;
        public bool EffectEnabled => effectEnabled;

        private void Awake()
        {
            if (target == null)
            {
                return;
            }
            target.raycastTarget = false;
            originalMaterial = target.material;
            Shader shader = maskShader != null ? maskShader : Shader.Find("NoRainbowAfter/UI/CircularViewMask");
            if (shader == null)
            {
                Debug.LogError("Circular view mask shader is missing.", this);
                target.enabled = false;
                return;
            }

            runtimeMaterial = new Material(shader) { name = "Circular View Mask (runtime)" };
            target.material = runtimeMaterial;
            Refresh();
        }

        private void LateUpdate() => Refresh();

        public void ApplySettings(bool enabled, float newRadius)
        {
            effectEnabled = enabled;
            radius = Mathf.Clamp(newRadius, 0.45f, 0.80f);
            Refresh();
        }

        private void Refresh()
        {
            if (target == null)
            {
                return;
            }
            target.enabled = effectEnabled && runtimeMaterial != null;
            if (runtimeMaterial == null)
            {
                return;
            }

            Rect rect = target.rectTransform.rect;
            Vector3 scale = target.rectTransform.lossyScale;
            float width = Mathf.Abs(rect.width * scale.x);
            float height = Mathf.Abs(rect.height * scale.y);

            runtimeMaterial.SetFloat("_Aspect", height > 0 ? width / height : 1f);
            runtimeMaterial.SetFloat("_Radius", radius);
            runtimeMaterial.SetFloat("_Softness", Mathf.Max(0.001f, softness));
        }

        private void OnDestroy()
        {
            if (target != null && target.material == runtimeMaterial)
            {
                target.material = originalMaterial;
            }
            if (runtimeMaterial != null)
            {
                Destroy(runtimeMaterial);
            }
        }
    }
}
