using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NoRainbowAfter.Narrative;
using NoRainbowAfter.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace NoRainbowAfter.Tests
{
    public sealed class MenuAndPauseUiTests
    {
        private const string MainMenuPath = "Assets/_NoRainbowAfter/Scenes/MainMenu.unity";
        private const string MasterGameplayPath = "Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity";
        private const string MasterVolumePrefKey = "NRA.MasterVolume";

        private float initialTimeScale;
        private float initialAudioVolume;
        private bool hadMasterVolumePref;
        private float initialMasterVolumePref;
        private string previousClueOverride;
        private string tempFolder;

        [SetUp]
        public void SetUpIsolation()
        {
            initialTimeScale = Time.timeScale;
            Time.timeScale = 1f;

            initialAudioVolume = AudioListener.volume;
            hadMasterVolumePref = PlayerPrefs.HasKey(MasterVolumePrefKey);
            initialMasterVolumePref = PlayerPrefs.GetFloat(MasterVolumePrefKey, 0.8f);

            previousClueOverride = ChapterDirector.StoragePathOverride;
            tempFolder = Path.Combine(Path.GetTempPath(), "NRA-UI-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempFolder);
            ChapterDirector.StoragePathOverride = Path.Combine(tempFolder, "clues.json");
        }

        [TearDown]
        public void TearDownIsolation()
        {
            Time.timeScale = initialTimeScale;
            AudioListener.volume = initialAudioVolume;

            if (hadMasterVolumePref)
                PlayerPrefs.SetFloat(MasterVolumePrefKey, initialMasterVolumePref);
            else
                PlayerPrefs.DeleteKey(MasterVolumePrefKey);
            PlayerPrefs.Save();

            ChapterDirector.StoragePathOverride = previousClueOverride;
            if (!string.IsNullOrEmpty(tempFolder) && Directory.Exists(tempFolder))
            {
                foreach (string file in Directory.GetFiles(tempFolder))
                    File.Delete(file);
                Directory.Delete(tempFolder, true);
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        [UnityTest]
        public IEnumerator MainMenuLoadsAtmosphereTaglineAndSettingsVolumeFlow()
        {
            yield return SceneManager.LoadSceneAsync(MainMenuPath, LoadSceneMode.Single);
            yield return null;

            GameObject uiRoot = GameObject.Find("UI_Root");
            Assert.That(uiRoot, Is.Not.Null, "UI_Root must exist in MainMenu.");

            Type menuAtmosphereType = Type.GetType("NoRainbowAfter.UI.MenuAtmosphere, Assembly-CSharp");
            Assert.That(menuAtmosphereType, Is.Not.Null, "MenuAtmosphere type must resolve from Assembly-CSharp.");
            Transform atmosphereTr = uiRoot.transform.Find("MenuAtmosphere");
            Assert.That(atmosphereTr, Is.Not.Null, "MenuAtmosphere object must exist under UI_Root.");
            Assert.That(atmosphereTr.gameObject.activeInHierarchy, Is.True);
            Assert.That(atmosphereTr.GetComponent(menuAtmosphereType), Is.Not.Null);

            Transform screenContent = uiRoot.transform.Find("ScreenContent");
            Assert.That(screenContent, Is.Not.Null, "ScreenContent must exist under UI_Root.");
            CanvasGroup screenContentGroup = screenContent.GetComponent<CanvasGroup>();
            Assert.That(screenContentGroup, Is.Not.Null, "ScreenContent must have a CanvasGroup.");
            Assert.That(screenContentGroup.interactable, Is.True);
            Assert.That(screenContentGroup.blocksRaycasts, Is.True);

            Transform taglineTr = screenContent.Find("TitleTagline");
            Assert.That(taglineTr, Is.Not.Null, "TitleTagline must exist under ScreenContent.");
            Text taglineText = taglineTr.GetComponent<Text>();
            Assert.That(taglineText, Is.Not.Null);
            Assert.That(string.IsNullOrWhiteSpace(taglineText.text), Is.False);

            Transform settingsPanel = uiRoot.transform.Find("SettingsPanel");
            Assert.That(settingsPanel, Is.Not.Null, "SettingsPanel must exist under UI_Root.");
            Assert.That(settingsPanel.gameObject.activeSelf, Is.False, "SettingsPanel must be initially hidden.");

            Button settingsButton = screenContent.Find("SettingsButton").GetComponent<Button>();
            settingsButton.onClick.Invoke();
            yield return null;

            Assert.That(settingsPanel.gameObject.activeSelf, Is.True);
            Assert.That(screenContentGroup.interactable, Is.False, "Opening SettingsPanel must disable ScreenContent CanvasGroup interaction.");
            Assert.That(screenContentGroup.blocksRaycasts, Is.False);

            Slider masterVolumeSlider = settingsPanel.Find("MasterVolumeSlider").GetComponent<Slider>();
            Text volumeValue = settingsPanel.Find("VolumeValue").GetComponent<Text>();
            Assert.That(masterVolumeSlider, Is.Not.Null);
            Assert.That(volumeValue, Is.Not.Null);

            masterVolumeSlider.value = 0.42f;
            Assert.That(AudioListener.volume, Is.EqualTo(0.42f).Within(0.001f));
            Assert.That(volumeValue.text, Is.EqualTo("42%"));
            Assert.That(PlayerPrefs.GetFloat(MasterVolumePrefKey), Is.EqualTo(0.42f).Within(0.001f));

            Button backButton = settingsPanel.Find("SettingsBackButton").GetComponent<Button>();
            backButton.onClick.Invoke();
            yield return null;

            Assert.That(settingsPanel.gameObject.activeSelf, Is.False);
            Assert.That(screenContentGroup.interactable, Is.True, "Closing SettingsPanel must restore ScreenContent interaction.");
            Assert.That(screenContentGroup.blocksRaycasts, Is.True);
        }

        [UnityTest]
        public IEnumerator MasterGameplayPausePanelExposesContextUpdatesStressAndVolumeAndRestoresTimeScale()
        {
            yield return SceneManager.LoadSceneAsync(MasterGameplayPath, LoadSceneMode.Single);
            yield return null;

            GameObject ui = GameObject.Find("UI_FirstPerson");
            Assert.That(ui, Is.Not.Null, "UI_FirstPerson must exist in MasterGameplay.");

            Type sessionType = Type.GetType("NoRainbowAfter.UI.FirstPersonSession, Assembly-CSharp");
            Assert.That(sessionType, Is.Not.Null, "FirstPersonSession type must resolve from Assembly-CSharp.");
            object session = ui.GetComponent(sessionType);
            Assert.That(session, Is.Not.Null);

            MethodInfo setPaused = sessionType.GetMethod("SetPaused", new[] { typeof(bool) });
            Assert.That(setPaused, Is.Not.Null);

            Transform pausePanel = ui.transform.Find("PausePanel");
            Assert.That(pausePanel, Is.Not.Null);
            Assert.That(pausePanel.gameObject.activeSelf, Is.False);

            setPaused.Invoke(session, new object[] { true });
            yield return null;

            Assert.That(pausePanel.gameObject.activeSelf, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f).Within(0.001f));

            Transform scrim = pausePanel.Find("PauseScrimBackdrop");
            Assert.That(scrim, Is.Not.Null, "PauseScrimBackdrop must exist under PausePanel.");
            Assert.That(scrim.gameObject.activeInHierarchy, Is.True);
            Assert.That(scrim.GetComponent<RawImage>(), Is.Not.Null);

            Slider masterVolumeSlider = pausePanel.Find("MasterVolumeSlider").GetComponent<Slider>();
            Assert.That(masterVolumeSlider, Is.Not.Null, "MasterVolumeSlider must exist under PausePanel.");

            Transform contextCard = pausePanel.Find("PauseContextCard");
            Assert.That(contextCard, Is.Not.Null, "PauseContextCard must exist under PausePanel.");
            Text chapterStatusLabel = contextCard.Find("ChapterStatusLabel").GetComponent<Text>();
            Text stressStatusLabel = contextCard.Find("StressStatusLabel").GetComponent<Text>();
            Assert.That(chapterStatusLabel, Is.Not.Null);
            Assert.That(stressStatusLabel, Is.Not.Null);
            Assert.That(string.IsNullOrWhiteSpace(chapterStatusLabel.text), Is.False);

            StressBarometer stressBarometer = Object.FindAnyObjectByType<StressBarometer>();
            Assert.That(stressBarometer, Is.Not.Null, "StressBarometer must exist in MasterGameplay.");
            stressBarometer.SetStress(85f);
            yield return null;

            Assert.That(stressStatusLabel.text, Does.Contain("Tertekan Berat"));
            Assert.That(stressStatusLabel.text, Does.Contain("85%"));

            masterVolumeSlider.value = 0.35f;
            Assert.That(AudioListener.volume, Is.EqualTo(0.35f).Within(0.001f));
            Assert.That(PlayerPrefs.GetFloat(MasterVolumePrefKey), Is.EqualTo(0.35f).Within(0.001f));

            setPaused.Invoke(session, new object[] { false });
            yield return null;

            Assert.That(pausePanel.gameObject.activeSelf, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f).Within(0.001f));
        }
    }
}
