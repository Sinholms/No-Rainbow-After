using System.Collections;
using NoRainbowAfter.AI;
using NoRainbowAfter.Narrative;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NoRainbowAfter.Core
{
    // A chapter entry loads the same authored gameplay as the normal run.
    public sealed class ChapterSceneEntry : MonoBehaviour
    {
        [SerializeField, Range(1, 2)] private int chapter = 1;

        private static string GameplayPath => SceneFlow.PathFor(GameScene.MasterGameplay);
        private ChapterDirector director;

        private IEnumerator Start()
        {
            if (SceneManager.GetActiveScene() != gameObject.scene) yield break;
            ChapterOneCorridorShift.ShowCrashMemoryOnStart = false;

            var gameplay = SceneManager.GetSceneByPath(GameplayPath);
            if (gameplay.isLoaded)
            {
                Debug.LogError("[NRA] Open the chapter scene on its own before pressing Play.", this);
                yield break;
            }

            SceneManager.sceneLoaded += PrepareGameplay;
            var load = SceneManager.LoadSceneAsync(GameplayPath, LoadSceneMode.Additive);
            yield return load;
            SceneManager.sceneLoaded -= PrepareGameplay;

            // Both the director and the chapter flow initialize in Start.
            yield return null;
            if (director == null || director.State == null || load == null)
            {
                Debug.LogError("[NRA] The chapter entry could not load MasterGameplay.", this);
                yield break;
            }

            if (chapter == 1) yield break;
            var flow = director.GetComponent<ChapterTwoFlow>();
            if (flow == null)
            {
                Debug.LogError("[NRA] The loaded gameplay has no chapter flow.", this);
                yield break;
            }
            yield return flow.StartChapterTwoPreview();
        }

        private void PrepareGameplay(Scene scene, LoadSceneMode mode)
        {
            if (scene.path != GameplayPath) return;
            // Make the shared scene active so its lighting and runtime objects are used.
            SceneManager.SetActiveScene(scene);
            foreach (var root in scene.GetRootGameObjects())
            {
                director = root.GetComponentInChildren<ChapterDirector>();
                if (director != null) break;
            }
            if (director != null) director.ConfigureChapterPreview(chapter);
        }

        private void OnDestroy() => SceneManager.sceneLoaded -= PrepareGameplay;
    }
}
