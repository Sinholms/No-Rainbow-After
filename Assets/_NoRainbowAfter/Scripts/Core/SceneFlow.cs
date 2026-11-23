using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NoRainbowAfter.Core
{
    public enum GameScene { Boot, MainMenu, MasterGameplay, Credits, FirstPersonTest, Chapter1, Chapter2, Chapter3 }

    /// <summary>One persistent owner of guarded, asynchronous scene transitions.</summary>
    public sealed class SceneFlow : MonoBehaviour
    {
        private static SceneFlow instance;
        public bool IsLoading { get; private set; }
        public string Status { get; private set; } = string.Empty;
        public event Action StateChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        public static SceneFlow EnsureInstance()
        {
            if (instance == null)
                instance = new GameObject("SceneFlow").AddComponent<SceneFlow>();
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            AudioListener.volume = PlayerPrefs.GetFloat("NRA.MasterVolume", 0.8f);
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        public static string PathFor(GameScene scene)
        {
            if (!Enum.IsDefined(typeof(GameScene), scene))
                throw new ArgumentOutOfRangeException(nameof(scene));
            return "Assets/_NoRainbowAfter/Scenes/" + scene + ".unity";
        }

        public bool Load(GameScene target)
        {
            if (IsLoading) return false;
            if (!Enum.IsDefined(typeof(GameScene), target))
            {
                SetStatus("Tujuan scene tidak dikenal.");
                return false;
            }
            string path = PathFor(target);
            if (!Application.CanStreamedLevelBeLoaded(path))
            {
                SetStatus("Scene belum tersedia: " + target);
                Debug.LogError("[NRA] Scene is absent from enabled build settings: " + path);
                return false;
            }
            if (SceneManager.GetActiveScene().path == path) return false;
            StartCoroutine(LoadRoutine(path));
            return true;
        }

        private IEnumerator LoadRoutine(string path)
        {
            IsLoading = true;
            SetStatus("Memuat...");
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            AsyncOperation operation;
            try
            {
                operation = SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
            }
            catch (Exception exception)
            {
                IsLoading = false;
                SetStatus("Scene gagal dimuat. Silakan coba lagi.");
                Debug.LogException(exception);
                yield break;
            }
            if (operation == null)
            {
                IsLoading = false;
                SetStatus("Scene gagal dimuat. Silakan coba lagi.");
                yield break;
            }
            while (!operation.isDone) yield return null;
            IsLoading = false;
            SetStatus(string.Empty);
        }

        private void SetStatus(string message)
        {
            Status = message;
            StateChanged?.Invoke();
        }
    }
}
