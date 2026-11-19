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
        public void SetChapterGuidance(string title, string detail) { chapterTitle=title; chapterHint=detail; }
        public void Configure(ChapterDirector source, PlayerInteractor interaction, Text title, Text detail)
        { director = source; interactor = interaction; objective = title; hint = detail; }
        public bool IsCompleted(string flag) => director != null && director.State != null && director.State.HasObjective(flag);
        private void Update()
        {
            if (director == null || interactor == null || director.State == null) return;
            foreach (string flag in KnownFlags)
            {
                if (flag == "objective.overtime" && !director.HasReadOvertimeSlip) continue;
                if (interactor.Flags.Contains(flag) && director.State.CompleteObjective(flag)) ObjectiveCompleted?.Invoke(flag);
            }
            if (director.State.ActiveChapter == "Chapter3")
            {
                ShowGuidance("Temui Bayu di kampus", "Bayu menunggu di depan lab grafkom besok setelah kelas.");
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
            else if (director.BeatIndex < 4) hint.text = "Kembali ke lorong.";
            else hint.text = "Hadapi atau hindari sosok di lorong.";
        }

        private void ShowChapterTwoGuidance(ChapterTwoBeat beat)
        {
            switch (beat)
            {
                case ChapterTwoBeat.WokeInDorm:
                    ShowGuidance("Kerjakan tugas di laptop", "Tekan E di depan laptop untuk menyalin berita.");
                    break;
                case ChapterTwoBeat.ReportSubmitted:
                    ShowGuidance("Laporan selesai", "");
                    break;
                case ChapterTwoBeat.StreetEntered:
                    ShowGuidance("Capai halte di seberang garis polisi", "Berlindung di balik peti. Tekan Ctrl untuk jongkok dan tunggu sorotan berlalu.");
                    break;
                case ChapterTwoBeat.ShelterReached:
                    ShowGuidance("Periksa tas di halte", "Arahkan pandangan ke tas dan tekan E.");
                    break;
                case ChapterTwoBeat.BagInspected:
                    ShowGuidance("", "");
                    break;
                case ChapterTwoBeat.ReturnedToDorm:
                    ShowGuidance("Balas pesan Bayu", "Ponsel ada di samping laptop. Arahkan pandangan ke ponsel dan tekan E.");
                    break;
            }
        }

        private void ShowGuidance(string title, string detail)
        {
            if (objective != null) objective.text = title;
            if (hint != null) hint.text = detail;
        }
    }
}
