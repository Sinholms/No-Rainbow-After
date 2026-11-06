using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NoRainbowAfter.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class MenuItemFocus : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private Graphic marker;
        private Button button;
        private bool hovered;
        private bool selected;

        private void Awake() => button = GetComponent<Button>();
        private void OnEnable() => Refresh();
        private void OnDisable()
        {
            hovered = selected = false;
            if (marker != null) marker.enabled = false;
        }

        public void OnPointerEnter(PointerEventData eventData) { hovered = true; Refresh(); }
        public void OnPointerExit(PointerEventData eventData) { hovered = false; Refresh(); }
        public void OnSelect(BaseEventData eventData) { selected = true; Refresh(); }
        public void OnDeselect(BaseEventData eventData) { selected = false; Refresh(); }

        private void LateUpdate()
        {
            // A modal can disable this Button without changing the current selected object.
            Refresh();
        }

        private void Refresh()
        {
            if (marker != null)
                marker.enabled = button != null && button.IsInteractable() && (hovered || selected);
        }
    }
}
