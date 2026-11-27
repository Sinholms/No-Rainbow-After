using System;
using System.Collections;
using NoRainbowAfter.Narrative;
using NoRainbowAfter.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NoRainbowAfter.AI
{
    /// <summary>
    /// Full-screen epilogue presentation for Good (Reality), Bad, and Hidden endings
    /// with transitions to Credits or MainMenu.
    /// </summary>
    public sealed class EndingPresentationController : MonoBehaviour
    {
        private FirstPersonPlayer player;
        private Canvas canvas;
        private Image backdrop;
        private Text headerText;
        private Text bodyText;
        private Button creditsButton;
        private Button menuButton;
        private CanvasGroup buttonGroup;
        private Coroutine activeRoutine;

        public bool IsShowing { get; private set; }
        public StoryEnding ShownEnding { get; private set; } = StoryEnding.None;

        public void Show(StoryEnding ending)
        {
            if (IsShowing || ending == StoryEnding.None) return;
            player = GetComponent<FirstPersonPlayer>();
            ShownEnding = ending;
            IsShowing = true;

            if (player != null)
            {
                player.SetPaused(false);
                player.SetInspecting(true);
                player.SetTransitionLocked(true);
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            BuildUi();
            if (activeRoutine != null) StopCoroutine(activeRoutine);
            activeRoutine = StartCoroutine(PresentSequence(ending));
        }

        private void OnDestroy()
        {
            if (canvas != null) Destroy(canvas.gameObject);
        }

        private IEnumerator PresentSequence(StoryEnding ending)
        {
            canvas.enabled = true;
            buttonGroup.alpha = 0f;
            buttonGroup.interactable = false;
            buttonGroup.blocksRaycasts = false;

            Color targetBg = ending == StoryEnding.Good
                ? new Color(0.05f, 0.07f, 0.08f, 0.98f)
                : new Color(0.015f, 0.018f, 0.024f, 0.99f);

            headerText.text = ending switch
            {
                StoryEnding.Good => "GOOD ENDING  —  REALITY",
                StoryEnding.Hidden => "HIDDEN ENDING  —  SEBUAH BALASAN",
                _ => "BAD ENDING"
            };
            headerText.color = ending == StoryEnding.Good
                ? new Color(0.62f, 0.85f, 0.80f, 0f)
                : ending == StoryEnding.Hidden
                    ? new Color(0.78f, 0.86f, 0.92f, 0f)
                    : new Color(0.76f, 0.42f, 0.40f, 0f);

            bodyText.text = ending switch
            {
                StoryEnding.Good =>
                    "Raka berhenti berlari dan menatap bayangan itu tanpa bersembunyi lagi.\n\n" +
                    "Rasa bersalah yang ia pendam perlahan menemukan jalan pulang. Bulan demi bulan berlalu, Raka kembali menghadiri kelas, menyelesaikan kuliahnya hingga hari wisuda, dan di antara tumpukan ucapan selamat ia menemukan sepucuk surat tanpa nama dari Ella.",
                StoryEnding.Hidden =>
                    "Dua menit penuh rasa takut berakhir di balik kegelapan.\n\n" +
                    "Raka tidak sanggup menghadapi bayangan itu, tetapi pesan yang sempat ia kirim ke ponsel Ella di malam sebelum lembur ternyata bergetar dengan balasan baru. Di ujung pesan itu terungkap bahwa Ella ternyata masih hidup dan baru saja sadar.",
                _ =>
                    "Raka terus bersembunyi hingga suara langkah itu lenyap bersama waktu.\n\n" +
                    "Ponsel di kamar kos tetap sepi, hanya menyisakan satu pesan terakhir dari Bayu yang tak pernah sempat terjawab. Kabur dari trauma hanya meninggalkan keheningan yang semakin dalam."
            };
            bodyText.color = new Color(0.86f, 0.90f, 0.90f, 0f);

            float t = 0f;
            while (t < 1.1f)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(t / 1.1f);
                backdrop.color = new Color(targetBg.r, targetBg.g, targetBg.b, a * targetBg.a);
                yield return null;
            }

            t = 0f;
            while (t < 0.9f)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(t / 0.9f);
                var hc = headerText.color; hc.a = a; headerText.color = hc;
                var bc = bodyText.color; bc.a = a; bodyText.color = bc;
                yield return null;
            }

            buttonGroup.alpha = 1f;
            buttonGroup.interactable = true;
            buttonGroup.blocksRaycasts = true;
            if (EventSystem.current != null && creditsButton != null)
                EventSystem.current.SetSelectedGameObject(creditsButton.gameObject);
        }

        private void BuildUi()
        {
            if (canvas != null) return;

            var root = new GameObject("EndingPresentationUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var bgGo = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(root.transform, false);
            var bgRect = (RectTransform)bgGo.transform;
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
            backdrop = bgGo.GetComponent<Image>();
            backdrop.color = Color.clear;

            headerText = CreateText("EndingTitle", root.transform, "", 42, FontStyle.Bold,
                new Vector2(0f, 190f), new Vector2(1200f, 70f));
            bodyText = CreateText("EndingBody", root.transform, "", 26, FontStyle.Normal,
                new Vector2(0f, 10f), new Vector2(1060f, 280f));
            bodyText.lineSpacing = 1.35f;

            var btnRow = new GameObject("EndingActions", typeof(RectTransform), typeof(CanvasGroup));
            btnRow.transform.SetParent(root.transform, false);
            var rowRect = (RectTransform)btnRow.transform;
            rowRect.anchoredPosition = new Vector2(0f, -240f);
            rowRect.sizeDelta = new Vector2(680f, 64f);
            buttonGroup = btnRow.GetComponent<CanvasGroup>();

            creditsButton = CreateButton(btnRow.transform, "CreditsButton", "Lanjut ke Credits", new Vector2(-165f, 0f),
                () => LoadScene("Assets/_NoRainbowAfter/Scenes/Credits.unity"));
            menuButton = CreateButton(btnRow.transform, "MainMenuButton", "Kembali ke Menu", new Vector2(165f, 0f),
                () => LoadScene("Assets/_NoRainbowAfter/Scenes/MainMenu.unity"));

            canvas.enabled = false;
        }

        private void LoadScene(string path)
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (Application.CanStreamedLevelBeLoaded(path))
                SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
        }

        private static Text CreateText(string name, Transform parent, string text, int size, FontStyle style, Vector2 pos, Vector2 bounds)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = pos;
            rect.sizeDelta = bounds;
            var label = go.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            label.text = text;
            return label;
        }

        private static Button CreateButton(Transform parent, string name, string caption, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(280f, 54f);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.16f, 0.25f, 0.27f, 0.94f);
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);
            var txt = CreateText("Label", go.transform, caption, 22, FontStyle.Normal, Vector2.zero, new Vector2(260f, 44f));
            txt.color = new Color(0.90f, 0.94f, 0.93f, 1f);
            return btn;
        }
    }
}
