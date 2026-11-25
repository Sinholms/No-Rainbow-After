using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NoRainbowAfter.UI
{
    /// <summary>
    /// Smooth unscaled-time focus and hover treatment for editorial menu buttons:
    /// animates an amber focus needle, soft horizontal highlight glow, caption glide, and tactile horror UI audio.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class MenuItemFocus : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IPointerClickHandler, ISubmitHandler
    {
        [SerializeField] private Graphic marker;
        [SerializeField] private Graphic highlightFill;
        [SerializeField] private RectTransform captionRect;
        [SerializeField] private float hoverShiftX = 11f;
        [SerializeField] private float markerExpandedWidth = 22f;
        [SerializeField] private float fillTargetAlpha = 0.85f;
        [SerializeField] private bool playAudio = true;

        private Button button;
        private bool hovered;
        private bool selected;
        private float focusBlend;
        private float baseCaptionLeft;
        private float baseCaptionRight;
        private bool hasBaseCaptionOffsets;
        private float enabledAtUnscaledTime;

        private void Awake()
        {
            button = GetComponent<Button>();
            CacheCaptionOffsets();
        }

        private void OnEnable()
        {
            enabledAtUnscaledTime = Time.unscaledTime;
            CacheCaptionOffsets();
            bool active = IsFocused();
            focusBlend = active ? 1f : 0f;
            ApplyVisuals(focusBlend, active);
        }

        private void OnDisable()
        {
            hovered = selected = false;
            focusBlend = 0f;
            ApplyVisuals(0f, false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            bool wasActive = IsFocused();
            hovered = true;
            if (!wasActive && IsFocused()) TryPlayHover();
        }

        public void OnPointerExit(PointerEventData eventData) => hovered = false;

        public void OnSelect(BaseEventData eventData)
        {
            bool wasActive = IsFocused();
            selected = true;
            if (!wasActive && IsFocused() && Time.unscaledTime - enabledAtUnscaledTime > 0.12f)
                TryPlayHover();
        }

        public void OnDeselect(BaseEventData eventData) => selected = false;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (playAudio && button != null && button.IsInteractable()) MenuUiAudio.PlayConfirm();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (playAudio && button != null && button.IsInteractable()) MenuUiAudio.PlayConfirm();
        }

        private void LateUpdate()
        {
            if (button == null || !button.IsInteractable())
                hovered = selected = false;
            bool active = IsFocused();
            float target = active ? 1f : 0f;
            float speed = active ? 11f : 8f;
            focusBlend = Mathf.MoveTowards(focusBlend, target, Time.unscaledDeltaTime * speed);
            ApplyVisuals(focusBlend, active);
        }

        private bool IsFocused() => button != null && button.IsInteractable() && (hovered || selected);

        private void TryPlayHover()
        {
            if (playAudio) MenuUiAudio.PlayHover();
        }

        private void CacheCaptionOffsets()
        {
            if (hasBaseCaptionOffsets) return;
            if (captionRect == null)
            {
                Transform cap = transform.Find("Caption");
                if (cap != null) captionRect = cap as RectTransform;
            }
            if (captionRect != null)
            {
                baseCaptionLeft = captionRect.offsetMin.x;
                baseCaptionRight = captionRect.offsetMax.x;
                hasBaseCaptionOffsets = true;
            }
        }

        private void ApplyVisuals(float t, bool active)
        {
            float eased = 1f - (1f - t) * (1f - t);
            if (marker != null)
            {
                marker.enabled = active || eased > 0.01f;
                Color c = marker.color;
                c.a = eased;
                marker.color = c;
                var mr = marker.rectTransform;
                if (mr != null)
                    mr.sizeDelta = new Vector2(Mathf.Lerp(4f, markerExpandedWidth, eased), mr.sizeDelta.y);
            }

            if (highlightFill != null)
            {
                highlightFill.enabled = eased > 0.005f;
                Color fc = highlightFill.color;
                fc.a = fillTargetAlpha * eased;
                highlightFill.color = fc;
            }

            if (captionRect != null && hasBaseCaptionOffsets)
            {
                float shift = hoverShiftX * eased;
                captionRect.offsetMin = new Vector2(baseCaptionLeft + shift, captionRect.offsetMin.y);
                captionRect.offsetMax = new Vector2(baseCaptionRight + shift, captionRect.offsetMax.y);
            }
        }
    }
}
