using System.Collections.Generic;
using NoRainbowAfter.Player;
using UnityEngine;
using UnityEngine.UI;

namespace NoRainbowAfter.Narrative
{
    /// <summary>Displays queued narrative cues as subtitles without requiring voice audio.</summary>
    public sealed class SubtitlePlaceholder : MonoBehaviour
    {
        [SerializeField] private ChapterDirector director;
        [SerializeField] private FirstPersonPlayer player;

        private readonly Queue<NarrativeCue> pending = new Queue<NarrativeCue>();
        private Canvas canvas;
        private GameObject subtitleRoot;
        private Text label;
        private NarrativeCue currentCue;
        private float remaining;
        private bool hasCurrentCue;

        public string CurrentText => label != null ? label.text : string.Empty;
        public int PendingCount => pending.Count + (hasCurrentCue ? 1 : 0);

        private void Awake() => BuildUi();

        private void OnEnable()
        {
            AttachDirector();
            if (canvas != null) canvas.enabled = true;
            RefreshVisibility();
        }

        private void OnDisable()
        {
            DetachDirector();
            if (canvas != null) canvas.enabled = false;
        }

        private void OnDestroy()
        {
            DetachDirector();
            if (canvas != null) Destroy(canvas.gameObject);
        }

        /// <summary>Assigns runtime dependencies and safely moves the cue subscription.</summary>
        public void Configure(ChapterDirector source, FirstPersonPlayer firstPersonPlayer)
        {
            DetachDirector();
            director = source;
            player = firstPersonPlayer;
            if (isActiveAndEnabled) AttachDirector();
            RefreshVisibility();
        }

        private void AttachDirector()
        {
            if (director != null) director.CueRequested += EnqueueCue;
        }

        private void DetachDirector()
        {
            if (director != null) director.CueRequested -= EnqueueCue;
        }

        private void EnqueueCue(NarrativeCue cue)
        {
            if (cue == null) return;
            pending.Enqueue(cue);
            StartNextCueIfReady();
            RefreshVisibility();
        }

        private void Update()
        {
            bool suspended = player != null && (player.IsPaused || player.IsInspecting);
            if (suspended)
            {
                RefreshVisibility();
                return;
            }

            if (!hasCurrentCue) StartNextCueIfReady();
            if (hasCurrentCue)
            {
                remaining -= Time.deltaTime;
                if (remaining <= 0f)
                {
                    hasCurrentCue = false;
                    currentCue = null;
                    label.text = string.Empty;
                    StartNextCueIfReady();
                }
            }
            RefreshVisibility();
        }

        private void StartNextCueIfReady()
        {
            if (hasCurrentCue || pending.Count == 0) return;
            currentCue = pending.Dequeue();
            hasCurrentCue = true;
            remaining = Mathf.Max(0f, currentCue.Duration);
            string speaker = string.IsNullOrWhiteSpace(currentCue.Speaker) ? string.Empty : currentCue.Speaker.Trim() + "\n";
            label.text = speaker + (currentCue.Text ?? string.Empty);
        }

        private void RefreshVisibility()
        {
            if (subtitleRoot == null) return;
            bool suspended = player != null && (player.IsPaused || player.IsInspecting);
            subtitleRoot.SetActive(hasCurrentCue && !suspended);
        }

        private void BuildUi()
        {
            var root = new GameObject("Narrative Subtitles", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;

            subtitleRoot = new GameObject("Subtitle", typeof(RectTransform), typeof(Image));
            subtitleRoot.transform.SetParent(root.transform, false);
            var panelRect = (RectTransform)subtitleRoot.transform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(.5f, 0f);
            panelRect.pivot = new Vector2(.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 290f);
            panelRect.sizeDelta = new Vector2(1420f, 140f);
            var panel = subtitleRoot.GetComponent<Image>();
            panel.color = new Color(.015f, .018f, .02f, .78f);
            panel.raycastTarget = false;

            var labelObject = new GameObject("Subtitle Text", typeof(RectTransform), typeof(Text), typeof(Outline));
            labelObject.transform.SetParent(subtitleRoot.transform, false);
            var textRect = (RectTransform)labelObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(28f, 12f);
            textRect.offsetMax = new Vector2(-28f, -12f);
            label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 30;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.color = new Color(.97f, .96f, .92f);
            label.raycastTarget = false;
            var outline = labelObject.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, .95f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            subtitleRoot.SetActive(false);
        }
    }
}
