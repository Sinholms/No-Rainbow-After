using System;
using NoRainbowAfter.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace NoRainbowAfter.Narrative
{
    public sealed class ObjectiveTracker : MonoBehaviour
    {
        [SerializeField] private ChapterDirector director;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private Text objective, hint;
        private static readonly string[] KnownFlags = { "objective.laptop", "objective.book", "objective.overtime", "objective.doorChecked" };
        public event Action<string> ObjectiveCompleted;
        private string chapterTitle, chapterHint;
        private Text bulletLabel;
        private bool layoutApplied;
        private Vector2 lastObjectivePos;

        public void SetChapterGuidance(string title, string detail) { chapterTitle=title; chapterHint=detail; }
        public void Configure(ChapterDirector source, PlayerInteractor interaction, Text title, Text detail)
        { director = source; interactor = interaction; objective = title; hint = detail; layoutApplied = false; }
        public bool IsCompleted(string flag) => director != null && director.State != null && director.State.HasObjective(flag);

        private void Awake() => EnsureTopLeftObjectiveLayout();

        private void Update()
        {
            EnsureTopLeftObjectiveLayout();
            if (director == null || interactor == null || director.State == null) return;
            // Flag interaksi diubah menjadi catatan progres satu kali agar objective tidak tercatat ulang.
            foreach (string flag in KnownFlags)
            {
                if (flag == "objective.overtime" && !director.HasReadOvertimeSlip) continue;
                if (interactor.Flags.Contains(flag) && director.State.CompleteObjective(flag)) ObjectiveCompleted?.Invoke(flag);
            }
            if (director.State.ActiveChapter == "Chapter3")
            {
                ShowGuidance("???", "");
                return;
            }
            if (director.State.ActiveChapter == "Chapter2")
            {
                ShowChapterTwoGuidance(director.State.Chapter2Beat);
                return;
            }
            if (director.ChapterOneFinished)
            {
                ShowGuidance("", "");
                return;
            }
            if (!string.IsNullOrEmpty(chapterTitle))
            {
                if (objective != null) objective.text=chapterTitle;
                if (hint != null) hint.text=chapterHint;
                return;
            }
            if (objective != null) objective.text = "Mengerjakan tugas";
            if (hint == null) return;
            bool taskDone = interactor.Flags.Contains("objective.laptop") || interactor.Flags.Contains("objective.book");
            if (director.BeatIndex >= 6) hint.text = "";
            else if (!taskDone) hint.text = "Kerjakan tugas di laptop meja belajar. Arahkan pandangan ke laptop dan tekan E.";
            else if (director.LoopIndex == 0) hint.text = "Telusuri lorong, lalu kembali ke kamar.";
            else if (!director.HasReadOvertimeSlip) hint.text = "Periksa slip lembur di kamar.";
            else if (director.BeatIndex < 4) hint.text = "Buka pintu di ujung lorong, lalu melangkah keluar.";
            else hint.text = "Hadapi atau hindari sosok di lorong.";
            UpdateBulletVisibility();
        }

        private void ShowChapterTwoGuidance(ChapterTwoBeat beat)
        {
            // Petunjuk mengikuti beat aktif, jadi perubahan beat menjadi sumber progres UI bab 2.
            switch (beat)
            {
                case ChapterTwoBeat.WokeInDorm:
                    ShowGuidance("Kerjakan esai di laptop", "Tekan E di depan laptop untuk mengerjakan esai kuliah.");
                    break;
                case ChapterTwoBeat.StayedInBed:
                    ShowGuidance("", "");
                    break;
                case ChapterTwoBeat.OvertimeRequested:
                    ShowGuidance("Berangkat lembur", "Buka pintu di ujung lorong, lalu melangkah keluar.");
                    break;
                case ChapterTwoBeat.StreetEntered:
                    ShowGuidance("Capai halte dan naik bus", "Jongkok di balik peti dan tunggu sorot senter lewat.");
                    break;
                case ChapterTwoBeat.ShelterReached:
                    ShowGuidance("Naik bus", "Dekati pintu bus dan tekan E.");
                    break;
            }
        }

        private void ShowGuidance(string title, string detail)
        {
            if (objective != null) objective.text = title;
            if (hint != null) hint.text = detail;
            UpdateBulletVisibility();
        }

        private void EnsureTopLeftObjectiveLayout()
        {
            if (objective == null || hint == null) return;
            RectTransform objRect = objective.rectTransform;
            if (layoutApplied && objRect.anchoredPosition == lastObjectivePos) return;
            layoutApplied = true;
            lastObjectivePos = objRect.anchoredPosition;

            RectTransform hintRect = hint.rectTransform;
            if (hintRect.parent != objRect.parent)
                hintRect.SetParent(objRect.parent, false);

            hintRect.anchorMin = objRect.anchorMin;
            hintRect.anchorMax = objRect.anchorMax;
            hintRect.pivot = new Vector2(0f, 1f);

            float objLeftX = objRect.anchoredPosition.x - objRect.pivot.x * objRect.sizeDelta.x;
            float objBottomY = objRect.anchoredPosition.y - objRect.pivot.y * objRect.sizeDelta.y;

            hintRect.anchoredPosition = new Vector2(objLeftX + 22f, objBottomY - 2f);
            hintRect.sizeDelta = new Vector2(560f, 84f);

            hint.alignment = TextAnchor.UpperLeft;
            hint.horizontalOverflow = HorizontalWrapMode.Wrap;
            hint.verticalOverflow = VerticalWrapMode.Overflow;
            hint.fontSize = Mathf.Max(16, objective.fontSize - 8);
            hint.lineSpacing = 1.15f;
            hint.color = new Color(0.84f, 0.89f, 0.88f, 0.90f);

            if (bulletLabel == null)
            {
                Transform existing = objRect.parent != null ? objRect.parent.Find("ObjectiveBullet") : null;
                if (existing != null) bulletLabel = existing.GetComponent<Text>();
            }
            if (bulletLabel == null)
            {
                var bulletGo = new GameObject("ObjectiveBullet", typeof(RectTransform), typeof(Text));
                bulletGo.transform.SetParent(objRect.parent, false);
                bulletLabel = bulletGo.GetComponent<Text>();
            }

            RectTransform bulletRect = bulletLabel.rectTransform;
            bulletRect.anchorMin = objRect.anchorMin;
            bulletRect.anchorMax = objRect.anchorMax;
            bulletRect.pivot = new Vector2(0f, 1f);
            bulletRect.anchoredPosition = new Vector2(objLeftX + 4f, objBottomY - 2f);
            bulletRect.sizeDelta = new Vector2(20f, 32f);

            bulletLabel.font = hint.font;
            bulletLabel.fontSize = hint.fontSize;
            bulletLabel.alignment = TextAnchor.UpperLeft;
            bulletLabel.color = hint.color;
            bulletLabel.text = "•";
            bulletLabel.raycastTarget = false;
            UpdateBulletVisibility();
        }

        private void UpdateBulletVisibility()
        {
            if (bulletLabel != null)
                bulletLabel.enabled = hint != null && !string.IsNullOrWhiteSpace(hint.text);
        }
    }
}
