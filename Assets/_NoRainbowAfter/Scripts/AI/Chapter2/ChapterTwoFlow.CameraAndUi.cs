using System;
using System.Collections;
using NoRainbowAfter.Narrative;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NoRainbowAfter.AI
{
    /// <summary>
    /// Animasi kamera bangun tidur (Lerp + Quaternion.Slerp) serta UI transisi & pengulangan
    /// saat Raka memilih rebahan pada ChapterTwoFlow.
    ///
    /// [GRAFIKA KOMPUTER]
    /// GetOutOfBed() menginterpolasi posisi kamera dengan Vector3.Lerp dan orientasi kamera dengan
    /// Quaternion.Slerp memakai kurva Mathf.SmoothStep agar gerakan bangun dari bantal terasa halus.
    /// </summary>
    public sealed partial class ChapterTwoFlow
    {
        // Interpolasi posisi dan rotasi kamera selama fade memberi kesan Raka bangkit dari kasur.
        private IEnumerator GetOutOfBed()
        {
            var camera = motor.ViewCamera.transform;
            var standing = new Pose(camera.position, camera.rotation);
            var lying = BedPose();

            camera.SetPositionAndRotation(lying.position, lying.rotation);
            yield return Fade(1f, 0f, 1.05f, progress =>
            {
                float rise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.27f, 1f, progress));
                camera.SetPositionAndRotation(
                    Vector3.Lerp(lying.position, standing.position, rise),
                    Quaternion.Slerp(lying.rotation, standing.rotation, rise));
            });

            director.AdvanceChapterTwo(ChapterTwoBeat.WokeInDorm);
            SetTransition(false);
        }

        private static Pose BedPose()
        {
            var pillow = GameObject.Find("SM_PROP_SoftPillow");
            var renderer = pillow != null ? pillow.GetComponent<Renderer>() : null;
            Vector3 center = renderer != null ? renderer.bounds.center : new Vector3(-1.27f, .67f, 2.47f);
            return new Pose(center + Vector3.up * .18f, Quaternion.Euler(-58f, 110f, 30f));
        }

        private void ShowRetry()
        {
            if (retryButton == null)
            {
                var panel = new GameObject("Akhir hari", typeof(RectTransform), typeof(Image));
                var rect = panel.GetComponent<RectTransform>();
                rect.SetParent(transitionCanvas.transform, false);
                rect.sizeDelta = new Vector2(700f, 242f);

                var background = panel.GetComponent<Image>();
                background.color = new Color(.043f, .057f, .077f, .97f);
                Label(rect, "Raka rebahan sampai malam.", 52f, 32);

                var action = new GameObject("Ulangi Chapter 2", typeof(RectTransform), typeof(Image), typeof(Button));
                var actionRect = action.GetComponent<RectTransform>();
                actionRect.SetParent(rect, false);
                actionRect.anchoredPosition = new Vector2(0, -50f);
                actionRect.sizeDelta = new Vector2(300f, 58f);
                action.GetComponent<Image>().color = new Color(.20f, .32f, .36f);

                retryButton = action.GetComponent<Button>();
                retryButton.targetGraphic = action.GetComponent<Image>();
                Label(actionRect, "Ulangi Chapter 2", 0f, 26);
                retryButton.onClick.AddListener(RetryFromBed);
            }

            retryButton.transform.parent.gameObject.SetActive(true);
            retryVisible = true;
            player.SetInspecting(true);
            player.SetTransitionLocked(false);
            EventSystem.current?.SetSelectedGameObject(retryButton.gameObject);
        }

        private static void Label(RectTransform parent, string value, float y, int fontSize)
        {
            var text = new GameObject(value, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.up * y;
            text.rectTransform.offsetMax = Vector2.up * y;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = value;
        }

        private void SetTransition(bool active)
        {
            if (transitionCanvas == null)
            {
                var root = new GameObject("Chapter Transition", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                root.transform.SetParent(transform, false);
                transitionCanvas = root.GetComponent<Canvas>();
                transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                transitionCanvas.sortingOrder = 50;

                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                var panel = new GameObject("Blackout", typeof(RectTransform), typeof(Image));
                panel.transform.SetParent(root.transform, false);
                var rect = (RectTransform)panel.transform;
                rect.anchorMax = Vector2.one;
                rect.anchorMin = rect.offsetMin = rect.offsetMax = Vector2.zero;

                fadeImage = panel.GetComponent<Image>();
                fadeImage.raycastTarget = false;
                fadeImage.color = Color.clear;
            }

            transitioning = active;
            player.SetTransitionLocked(active);
            transitionCanvas.enabled = active && !player.IsPaused;
        }

        // Fade dan animasi yang mengikutinya berhenti selama menu jeda terbuka.
        private IEnumerator Fade(float from, float to, float duration, Action<float> animate = null)
        {
            float elapsed = 0f;
            fadeImage.color = new Color(0f, 0f, 0f, from);
            while (elapsed < duration)
            {
                yield return null;
                transitionCanvas.enabled = !player.IsPaused;
                if (player.IsPaused)
                {
                    continue;
                }
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                fadeImage.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, progress)));
                animate?.Invoke(progress);
            }
        }
    }
}
