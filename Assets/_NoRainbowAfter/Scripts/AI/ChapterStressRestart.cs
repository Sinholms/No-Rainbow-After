using NoRainbowAfter.Narrative;
using NoRainbowAfter.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NoRainbowAfter.AI
{
    // Memuat ulang scene awal bab untuk membangun kembali dunia dan progres cerita bab itu.
    public sealed class ChapterStressRestart : MonoBehaviour
    {
        private ChapterDirector director;
        private ChapterOneCorridorShift corridor;
        private StressBarometer stress;
        private bool restarting;

        private void Awake()
        {
            director = GetComponent<ChapterDirector>();
            corridor = GetComponent<ChapterOneCorridorShift>();
            stress = GetComponent<StressBarometer>();
            if (stress != null) stress.StressChanged += OnStressChanged;
        }

        private void OnDestroy()
        {
            if (stress != null) stress.StressChanged -= OnStressChanged;
        }

        private void Update()
        {
            // Periksa juga pemain yang memasuki area berbahaya saat stresnya sudah maksimum.
            if (stress != null && stress.CurrentStress >= StressBarometer.MaxStress)
                OnStressChanged(stress.CurrentStress);
        }

        private void OnStressChanged(float value)
        {
            if (restarting || value < StressBarometer.MaxStress || director == null || director.State == null) return;
            int chapter = !director.ChapterOneFinished && corridor != null && corridor.OwnsPlayer ? 1 :
                director.State.Chapter2Beat >= ChapterTwoBeat.StreetEntered &&
                director.State.Chapter2Beat < ChapterTwoBeat.BoardedBus ? 2 : 0;
            if (chapter == 0) return;

            string path = "Assets/_NoRainbowAfter/Scenes/Chapter" + chapter + ".unity";
            if (!Application.CanStreamedLevelBeLoaded(path))
            {
                Debug.LogError("[NRA] Cannot restart chapter: scene is missing from build settings: " + path, this);
                return;
            }
            restarting = true;
            director.PrepareChapterRestart(chapter);
            Time.timeScale = 1f;
            try
            {
                if (SceneManager.LoadSceneAsync(path, LoadSceneMode.Single) != null) return;
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, this);
            }
            ChapterDirector.CancelChapterRestart();
            restarting = false;
        }
    }
}
