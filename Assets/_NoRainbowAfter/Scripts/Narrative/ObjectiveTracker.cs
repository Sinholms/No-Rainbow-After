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
                if (interactor.Flags.Contains(flag) && director.State.CompleteObjective(flag)) ObjectiveCompleted?.Invoke(flag);
            if (director.ChapterOneFinished)
            {
                if (objective != null) objective.text = "Chapter 2 sedang dikembangkan";
                if (hint != null) hint.text = "Chapter 1 selesai. Tekan [TAB] untuk atur napas atau [ESC] untuk jeda.";
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
            else if (!interactor.Flags.Contains("objective.overtime")) hint.text = "Periksa slip lembur di kamar (tekan F untuk menutup).";
            else if (director.BeatIndex < 4) hint.text = "Kembali ke lorong.";
            else hint.text = "Hadapi atau hindari sosok di lorong.";
        }
    }
}
