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
            if (objective != null) objective.text = "Bersiap berangkat";
            if (hint != null) hint.text = interactor != null && interactor.Flags.Contains("objective.book")
                ? "Buku sudah diambil. Jelajahi kamar dan lorong."
                : "Ambil buku di meja. Arahkan pandangan ke buku dan tekan E.";
        }
    }
}
