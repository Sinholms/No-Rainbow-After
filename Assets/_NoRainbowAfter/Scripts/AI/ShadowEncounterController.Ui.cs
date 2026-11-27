using UnityEngine;
using UnityEngine.UI;

namespace NoRainbowAfter.AI
{
    /// <summary>
    /// Pembuatan dan pembaruan UI overlay encounter (prompt "[Klik Kiri] Lawan"
    /// serta lapisan blackout saat pemain tertangkap).
    /// </summary>
    public sealed partial class ShadowEncounterController
    {
        private void BuildRuntimeUi()
        {
            var root = new GameObject("Encounter UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false);
            uiCanvas = root.GetComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            uiCanvas.sortingOrder = 15;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var promptObj = new GameObject("FightPrompt", typeof(RectTransform), typeof(Text), typeof(Outline));
            promptObj.transform.SetParent(root.transform, false);
            var promptRect = (RectTransform)promptObj.transform;
            promptRect.anchorMin = promptRect.anchorMax = new Vector2(0.5f, 0.5f);
            promptRect.anchoredPosition = new Vector2(0f, -102f);
            promptRect.sizeDelta = new Vector2(640f, 48f);

            fightPromptLabel = promptObj.GetComponent<Text>();
            fightPromptLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            fightPromptLabel.fontSize = 24;
            fightPromptLabel.alignment = TextAnchor.MiddleCenter;
            fightPromptLabel.color = new Color(0.95f, 0.92f, 0.88f, 0.96f);
            fightPromptLabel.text = "[Klik Kiri] Lawan";
            fightPromptLabel.raycastTarget = false;

            var outline = promptObj.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.92f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            fightPromptLabel.enabled = false;

            var fadeObj = new GameObject("CaughtBlackout", typeof(RectTransform), typeof(Image));
            fadeObj.transform.SetParent(root.transform, false);
            var fadeRect = (RectTransform)fadeObj.transform;
            fadeRect.anchorMin = Vector2.zero;
            fadeRect.anchorMax = Vector2.one;
            fadeRect.offsetMin = fadeRect.offsetMax = Vector2.zero;

            blackoutImage = fadeObj.GetComponent<Image>();
            blackoutImage.color = new Color(0.01f, 0.012f, 0.016f, 0f);
            blackoutImage.raycastTarget = false;
            blackoutImage.enabled = false;
        }

        private void RefreshUi()
        {
            bool paused = player != null && player.IsPaused;
            bool inspecting = player != null && player.IsInspecting;

            if (fightPromptLabel != null)
            {
                fightPromptLabel.enabled = !paused && !inspecting && CanFightRightNow();
                fightPromptLabel.text = "[Klik Kiri] Lawan (" + successfulHits + "/3)";
            }

            if (blackoutImage != null)
            {
                bool showFade = !paused && currentBlackoutAlpha > 0.001f;
                blackoutImage.enabled = showFade;
                if (showFade)
                {
                    blackoutImage.color = new Color(0.01f, 0.012f, 0.016f, Mathf.Clamp01(currentBlackoutAlpha));
                }
            }
        }
    }
}
