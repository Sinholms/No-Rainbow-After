using NoRainbowAfter.Narrative;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NoRainbowAfter.Core
{
    /// <summary>
    /// Komponen kompatibilitas bila scene pembuka bab terpisah masih memuat script ini.
    /// Karena Chapter1, Chapter2, dan Chapter3 kini berdiri sendiri tanpa MasterGameplay,
    /// komponen ini mengarahkan scene ke bab yang dipilih bila diperlukan.
    /// </summary>
    public sealed class ChapterSceneEntry : MonoBehaviour
    {
        [SerializeField, Range(1, 3)] private int chapter = 1;

        private void Start()
        {
            if (SceneManager.GetActiveScene() != gameObject.scene)
            {
                return;
            }
            // Jika scene sudah berisi ChapterDirector mandiri, biarkan scene berjalan langsung.
            if (FindAnyObjectByType<ChapterDirector>() != null)
            {
                return;
            }

            GameScene target = chapter switch
            {
                2 => GameScene.Chapter2,
                3 => GameScene.Chapter3,
                _ => GameScene.Chapter1
            };
            SceneFlow.EnsureInstance().Load(target);
        }
    }
}
