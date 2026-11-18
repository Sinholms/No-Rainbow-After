using NoRainbowAfter.Player;
using UnityEngine;
using UnityEngine.UI;

namespace NoRainbowAfter.Interaction
{
    [RequireComponent(typeof(FirstPersonPlayer))]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        public const float Reach = 2.3f;
        [SerializeField] private Camera viewCamera;
        private FirstPersonPlayer player;
        private IInteractable focused;
        private Canvas canvas;
        private Text reticle, prompt, heading, body;
        private GameObject overlay;
        public IObjectiveFlags Flags { get; set; } = new SessionObjectiveFlags();
        public event System.Action<string> InteractionCompleted;
        public void NotifyInteraction(string stableId) => InteractionCompleted?.Invoke(stableId);
        public bool IsInspecting => overlay != null && overlay.activeSelf;
        public IInteractable Focused => focused;

        private void Awake()
        {
            player = GetComponent<FirstPersonPlayer>();
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
            BuildUi();
        }
        private void OnEnable()
        {
            if (player != null)
            {
                player.InteractRequested += Interact;
                player.CloseInspectRequested += OnCloseInspectRequested;
            }
            if (canvas != null) canvas.enabled = true;
        }
        private void OnDisable()
        {
            if (player != null)
            {
                player.InteractRequested -= Interact;
                player.CloseInspectRequested -= OnCloseInspectRequested;
                CloseInspection();
            }
            SetFocus(null);
            if (canvas != null) canvas.enabled = false;
        }
        private void OnCloseInspectRequested()
        {
            if (player != null && !player.IsPaused && !player.IsUsingLaptop && !player.IsBreathing) CloseInspection();
        }
        private void Update()
        {
            if (canvas != null) canvas.enabled = !player.IsPaused;
            bool showWorldUi = !player.IsPaused && !player.IsInspecting && !player.IsUsingLaptop && !player.IsBreathing;
            reticle.enabled = showWorldUi;
            prompt.enabled = showWorldUi;
            if (!showWorldUi || viewCamera == null) { SetFocus(null); return; }
            if (player.IsHiding) { SetFocus(null); prompt.text="[E] Keluar lemari"; return; }
            SetFocus(FindTarget(new Ray(viewCamera.transform.position, viewCamera.transform.forward), transform));
        }
        // The first non-trigger solid on any layer wins, even if it has no interactable.
        public static IInteractable FindTarget(Ray ray, Transform ignoredRoot = null)
        {
            var hits = Physics.RaycastAll(ray, Reach, ~0, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            Collider closest = null;
            foreach (var hit in hits)
            {
                if (ignoredRoot != null && hit.collider.transform.IsChildOf(ignoredRoot)) continue;
                if (hit.distance < nearest) { nearest = hit.distance; closest = hit.collider; }
            }
            return closest != null ? closest.GetComponentInParent<IInteractable>() : null;
        }
        private void SetFocus(IInteractable next)
        {
            if (next != null && string.IsNullOrEmpty(next.Prompt)) next = null;
            if (!ReferenceEquals(focused, next)) { focused?.SetFocused(false); focused = next; focused?.SetFocused(true); }
            if (reticle != null) reticle.color = focused == null ? new Color(1, 1, 1, .65f) : Color.white;
            if (prompt != null) prompt.text = focused == null ? "" : "[E] " + focused.Prompt;
        }
        public void Interact()
        {
            if (player.IsPaused || player.IsInspecting || player.IsUsingLaptop || player.IsBreathing || player.IsHiding || player.IsTransitionLocked || viewCamera == null) return;
            SetFocus(FindTarget(new Ray(viewCamera.transform.position, viewCamera.transform.forward), transform));
            focused?.Interact(this);
        }
        public void ShowInspection(string title, string text)
        {
            SetFocus(null); reticle.enabled = false; prompt.enabled = false; heading.text = title; body.text = text; overlay.SetActive(true); player.SetInspecting(true);
        }
        public bool CloseInspection()
        {
            if (!IsInspecting) return false;
            overlay.SetActive(false); player.SetInspecting(false); return true;
        }
        private void BuildUi()
        {
            var root = new GameObject("Interaction UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false); canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            reticle = Label(root.transform, "Reticle", "·", 28, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(40, 40));
            prompt = Label(root.transform, "Prompt", "", 25, TextAnchor.MiddleCenter, new Vector2(0, -55), new Vector2(600, 50));
            var promptOutline = prompt.gameObject.AddComponent<Outline>();
            promptOutline.effectColor = new Color(0f, 0f, 0f, .9f);
            promptOutline.effectDistance = new Vector2(1.5f, -1.5f);
            overlay = new GameObject("Inspect", typeof(RectTransform), typeof(Image)); overlay.transform.SetParent(root.transform, false);
            var rect = (RectTransform)overlay.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            overlay.GetComponent<Image>().color = new Color(.015f, .018f, .02f, .95f); overlay.GetComponent<Image>().raycastTarget = false;
            heading = Label(overlay.transform, "Title", "", 38, TextAnchor.MiddleLeft, new Vector2(0, 210), new Vector2(900, 80));
            body = Label(overlay.transform, "Body", "", 30, TextAnchor.UpperLeft, new Vector2(0, -10), new Vector2(900, 340));
            Label(overlay.transform, "Close", "[F]  Tutup", 23, TextAnchor.MiddleCenter, new Vector2(0, -330), new Vector2(500, 50));
            overlay.SetActive(false);
        }
        private static Text Label(Transform parent, string name, string value, int size, TextAnchor align, Vector2 position, Vector2 dimensions)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = dimensions;
            var text = go.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = size; text.alignment = align; text.color = new Color(.94f, .94f, .90f); text.text = value; text.raycastTarget = false; return text;
        }
    }
}
