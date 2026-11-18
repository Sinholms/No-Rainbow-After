using NoRainbowAfter.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace NoRainbowAfter.Level
{
    public sealed class ChapterOneObjective : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private Text objective;
        [SerializeField] private Text hint;
        public void Configure(PlayerInteractor source, Text objectiveText, Text hintText)
        { interactor = source; objective = objectiveText; hint = hintText; }
        private void Update()
        {
            if (objective != null) objective.text = "Mengerjakan tugas";
            bool taskDone = interactor != null && (interactor.Flags.Contains("objective.laptop") || interactor.Flags.Contains("objective.book"));
            if (hint != null) hint.text = taskDone
                ? "Tugas sudah dikerjakan. Jelajahi kamar dan lorong."
                : "Kerjakan tugas di laptop meja belajar. Arahkan pandangan ke laptop dan tekan E.";
        }
    }
}
