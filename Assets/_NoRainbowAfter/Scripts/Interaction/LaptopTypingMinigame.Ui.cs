using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace NoRainbowAfter.Interaction
{
    /// <summary>
    /// Pembangun dan pembaru antarmuka mengetik LaptopTypingMinigame (gaya Monkeytype).
    /// Mewarnai huruf target per karakter menggunakan tag Rich Text (emas, merah, kuning, abu-abu).
    /// </summary>
    public sealed partial class LaptopTypingMinigame
    {
        private void RefreshUi()
        {
            if (panelRoot != null) panelRoot.SetActive(IsActive);
            if (overlayCanvas != null) overlayCanvas.enabled = IsActive && (player == null || !player.IsPaused);
            if (!IsActive)
            {
                return;
            }

            if (headerLabel != null)
            {
                headerLabel.text = IsEssay
                    ? $"ESAI KULIAH  ·  Bagian {JudgedEssaySections + 1}/{RequiredSentences}  ·  Terkirim {CompletedSentences}"
                    : $"TUGAS KULIAH  ·  Bagian {Mathf.Min(CompletedSentences + 1, RequiredSentences)}/{RequiredSentences}  ·  Terkirim {CompletedSentences}";
            }
            if (timerLabel != null)
            {
                timerLabel.text = $"Sisa waktu: {Mathf.Max(0f, RemainingTime):F1} dtk";
            }
            if (timerFill != null)
            {
                timerFill.anchorMax = new Vector2(Mathf.Clamp01(RemainingTime / Mathf.Max(1f, CurrentSentenceTimeLimit)), 1f);
            }
            if (targetDisplay != null)
            {
                targetDisplay.text = BuildMonkeytypeRichText(TargetSentence, CurrentInput);
            }
            if (typedDisplay != null)
            {
                typedDisplay.text = string.IsNullOrEmpty(CurrentInput)
                    ? "<color=#5A6870>Mulai mengetik di keyboard...</color>"
                    : CurrentInput + "_";
            }
            if (exitHintLabel != null)
            {
                exitHintLabel.text = "Tekan [ENTER] untuk kirim kalimat  •  Tekan [TAB] untuk keluar laptop";
            }
        }

        public static string BuildMonkeytypeRichText(string target, string typed)
        {
            var sb = new StringBuilder(target.Length * 22);
            int maxLen = target.Length;
            for (int i = 0; i < maxLen; i++)
            {
                char expected = target[i];
                if (i < typed.Length)
                {
                    bool match = char.ToLowerInvariant(typed[i]) == char.ToLowerInvariant(expected);
                    if (match)
                    {
                        sb.Append("<color=#FFD54F>").Append(expected).Append("</color>");
                    }
                    else
                    {
                        sb.Append("<color=#FF3B3B>").Append(expected == ' ' ? '·' : expected).Append("</color>");
                    }
                }
                else if (i == typed.Length)
                {
                    sb.Append("<color=#FFF176>").Append(expected).Append("</color>");
                }
                else
                {
                    sb.Append("<color=#67757D>").Append(expected).Append("</color>");
                }
            }
            return sb.ToString();
        }

        private void BuildUi()
        {
            if (overlayCanvas != null)
            {
                return;
            }

            var canvasGo = new GameObject("LaptopTypingUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            overlayCanvas = canvasGo.GetComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 15;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            panelRoot = new GameObject("MonkeytypePanel", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(canvasGo.transform, false);
            var bgRect = (RectTransform)panelRoot.transform;
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
            var bg = panelRoot.GetComponent<Image>();
            bg.color = new Color(0.055f, 0.068f, 0.078f, 0.96f);
            bg.raycastTarget = false;

            // Header
            headerLabel = CreateText(
                "Header",
                panelRoot.transform,
                $"TUGAS KULIAH — MENGETIK LAPORAN (0/{RequiredSentencesForObjective})",
                28,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                new Color(0.84f, 0.89f, 0.90f, 1f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 320f),
                new Vector2(1200f, 50f));

            // Teks & bilah sisa waktu
            timerLabel = CreateText(
                "TimerText",
                panelRoot.transform,
                "Sisa waktu: 60.0 dtk",
                24,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                new Color(0.96f, 0.72f, 0.28f, 1f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 250f),
                new Vector2(600f, 36f));

            var tTrack = CreateRect(
                "TimerTrack",
                panelRoot.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 215f),
                new Vector2(960f, 10f));
            tTrack.gameObject.AddComponent<Image>().color = new Color(0.14f, 0.17f, 0.19f, 1f);
            timerFill = CreateRect("TimerFill", tTrack, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            timerFill.gameObject.AddComponent<Image>().color = new Color(0.95f, 0.69f, 0.25f, 1f);

            // Blok target kalimat gaya Monkeytype
            targetDisplay = CreateText(
                "TargetDisplay",
                panelRoot.transform,
                "",
                38,
                FontStyle.Normal,
                TextAnchor.MiddleLeft,
                Color.white,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 65f),
                new Vector2(1180f, 190f));
            targetDisplay.supportRichText = true;
            targetDisplay.horizontalOverflow = HorizontalWrapMode.Wrap;
            targetDisplay.verticalOverflow = VerticalWrapMode.Overflow;

            // Kotak ketikan langsung
            var inputBox = CreateRect(
                "InputBox",
                panelRoot.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -105f),
                new Vector2(1180f, 96f));
            inputBox.gameObject.AddComponent<Image>().color = new Color(0.09f, 0.115f, 0.135f, 0.98f);
            typedDisplay = CreateText(
                "TypedDisplay",
                inputBox,
                "",
                28,
                FontStyle.Normal,
                TextAnchor.MiddleLeft,
                new Color(0.92f, 0.95f, 0.96f, 1f),
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(-40f, -16f));

            // Status / umpan balik kesalahan ketik
            feedbackLabel = CreateText(
                "Feedback",
                panelRoot.transform,
                "",
                24,
                FontStyle.Normal,
                TextAnchor.MiddleCenter,
                new Color(0.84f, 0.88f, 0.86f, 1f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -205f),
                new Vector2(1180f, 52f));
            feedbackLabel.supportRichText = true;

            // Petunjuk keluar laptop ([TAB] 1x)
            exitHintLabel = CreateText(
                "ExitHintLabel",
                panelRoot.transform,
                "Tekan [ENTER] untuk kirim kalimat  •  Tekan [TAB] untuk keluar laptop",
                22,
                FontStyle.Normal,
                TextAnchor.MiddleCenter,
                new Color(0.82f, 0.84f, 0.82f, 1f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -335f),
                new Vector2(960f, 36f));

            panelRoot.SetActive(false);
            overlayCanvas.enabled = false;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            return rect;
        }

        private static Text CreateText(string name, Transform parent, string value, int fontSize, FontStyle style,
            TextAnchor align, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var rect = CreateRect(name, parent, anchorMin, anchorMax, pivot, pos, size);
            var txt = rect.gameObject.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = align;
            txt.color = color;
            txt.text = value;
            txt.raycastTarget = false;
            return txt;
        }
    }
}
