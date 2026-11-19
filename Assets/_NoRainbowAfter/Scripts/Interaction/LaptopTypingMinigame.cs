using System;
using System.Collections.Generic;
using System.Text;
using NoRainbowAfter.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NoRainbowAfter.Interaction
{
    /// <summary>
    /// Monkeytype-style typing minigame on Raka's study desk laptop (INT_LAPTOP).
    /// - Objective: "Mengerjakan tugas" with generated Indonesian college-themed sentences (5 sentences).
    /// - Wrong keystrokes, incomplete/incorrect Enter submission, or timer expiration raise StressBarometer.
    /// - Press [Enter] once to submit the current sentence; auto-exits once all 5 sentences are finished.
    /// - Press [Tab] once to exit the laptop back to first-person exploration at any time.
    /// </summary>
    public sealed class LaptopTypingMinigame : MonoBehaviour, IInteractable
    {
        public const float SentenceTimeLimit = 60f;
        public const float MinSentenceTimeLimit = 30f;
        public const int RequiredSentencesForObjective = 5;
        public const float TypoStressPenalty = 2f;
        public const float IncompleteSubmitStressPenalty = 10f;
        public const float TimeoutStressPenalty = 15f;

        public static float GetTimeLimitForSentence(int completedCount) => completedCount switch
        {
            <= 0 => 60f,
            1 => 52.5f,
            2 => 45f,
            3 => 37.5f,
            _ => MinSentenceTimeLimit
        };

        private static readonly string[] CollegeSentenceBank =
        {
            "laporan praktikum grafika komputer harus dikumpulkan malam ini",
            "revisi bab metodologi penelitian masih kurang referensi jurnal",
            "jadwal kuliah semester lima bentrok dengan lembur kerja lagi",
            "implementasi shader kamera dan pencahayaan ruang harus diuji",
            "analisis struktur data dan algoritma membutuhkan evaluasi ulang",
            "batas waktu pengumpulan tugas besar kuliah tidak diperpanjang"
        };

        private static readonly string[] RandomTopicSuffixes =
        {
            "sebelum pukul dua belas",
            "untuk evaluasi dosen pembimbing",
            "agar nilai semester tidak turun",
            "meski waktu istirahat hampir habis"
        };

        private PlayerInteractor activeInteractor;
        private FirstPersonPlayer player;
        private StressBarometer stressBarometer;

        private Canvas overlayCanvas;
        private GameObject panelRoot;
        private Text headerLabel;
        private Text timerLabel;
        private RectTransform timerFill;
        private Text targetDisplay;
        private Text typedDisplay;
        private Text feedbackLabel;
        private Text exitHintLabel;

        private int sentenceIndex;
        private int openedFrame = -1;
        private string[] reportSections;
        private bool retryReportTransition;
        private GameObject timerTrack;
        private AudioSource typingSource;
        private AudioClip typingClip;

        public string StableId => "INT_LAPTOP";
        public string DisplayName => "Laptop Raka";
        public string Prompt => IsReportComplete ? (retryReportTransition ? "Lanjutkan" : null)
            : IsReport ? "Salin laporan kecelakaan" : "Kerjakan Tugas";

        public bool IsActive { get; private set; }
        public string TargetSentence { get; private set; } = string.Empty;
        public string CurrentInput { get; private set; } = string.Empty;
        public float CurrentSentenceTimeLimit { get; private set; } = SentenceTimeLimit;
        public float RemainingTime { get; private set; } = SentenceTimeLimit;
        public int CompletedSentences { get; private set; }
        public int SubmittedAttempts { get; private set; }
        public bool IsReportComplete => IsReport && CompletedSentences >= reportSections.Length;
        private bool IsReport => reportSections != null;
        private int RequiredSentences => reportSections?.Length ?? RequiredSentencesForObjective;

        public event Action<bool> SessionStateChanged;
        public event Action<bool> SentenceSubmitted;
        public event Action ReportContinueRequested;

        public void ConfigureReport(IReadOnlyList<string> sections)
        {
            if (IsReport || sections == null || sections.Count == 0 || IsActive) return;
            reportSections = new string[sections.Count];
            for (int i = 0; i < sections.Count; i++) reportSections[i] = sections[i];
            CompletedSentences = 0;
            SubmittedAttempts = 0;
        }

        public void SetReportTransitionRetry(bool available) => retryReportTransition = available;

        private void Awake()
        {
            BuildUi();
        }

        private void OnDisable()
        {
            UnhookTextInput();
            if (IsActive) CloseLaptop();
        }

        private void OnDestroy()
        {
            UnhookTextInput();
            if (overlayCanvas != null) Destroy(overlayCanvas.gameObject);
            if (typingClip != null) Destroy(typingClip);
        }

        public void SetFocused(bool focused) { }

        public void Interact(PlayerInteractor interactor)
        {
            if (interactor == null || IsActive) return;
            if (IsReportComplete)
            {
                if (retryReportTransition) ReportContinueRequested?.Invoke();
                return;
            }
            activeInteractor = interactor;
            player = interactor.GetComponent<FirstPersonPlayer>();
            stressBarometer = interactor.GetComponent<StressBarometer>();
            if (stressBarometer == null && player != null)
                stressBarometer = player.GetComponent<StressBarometer>();

            OpenLaptop();
        }

        public void OpenLaptop()
        {
            if (IsActive || IsReportComplete) return;
            BuildUi();
            if (!IsReport && CompletedSentences >= RequiredSentencesForObjective)
                CompletedSentences = 0;
            IsActive = true;
            openedFrame = Time.frameCount;

            if (player != null)
            {
                player.SetUsingLaptop(true);
                player.BreatheRequested -= OnTabCloseRequested;
                player.BreatheRequested += OnTabCloseRequested;
            }
            LoadNextSentence();
            if (IsReport) PrepareTypingSound();
            HookTextInput();
            if (feedbackLabel != null)
                feedbackLabel.text = IsReport ? "Salin bagian berita yang tampil."
                    : "Ketik 5 kalimat tugas dengan tepat. Tekan [ENTER] untuk kirim, tekan [TAB] untuk keluar.";
            RefreshUi();
            SessionStateChanged?.Invoke(true);
        }

        public void CloseLaptop()
        {
            if (!IsActive) return;
            IsActive = false;
            if (player != null)
                player.BreatheRequested -= OnTabCloseRequested;
            UnhookTextInput();
            if (typingSource != null) typingSource.Stop();
            if (panelRoot != null) panelRoot.SetActive(false);
            if (overlayCanvas != null) overlayCanvas.enabled = false;
            player?.SetUsingLaptop(false);
            SessionStateChanged?.Invoke(false);
        }

        private void OnTabCloseRequested()
        {
            if (IsActive && Time.frameCount != openedFrame)
                CloseLaptop();
        }

        private void Update()
        {
            if (!IsActive) return;
            if (player != null && player.IsPaused)
            {
                if (overlayCanvas != null) overlayCanvas.enabled = false;
                if (typingSource != null && typingSource.isPlaying) typingSource.Stop();
                return;
            }
            if (overlayCanvas != null && !overlayCanvas.enabled) overlayCanvas.enabled = true;

            bool enterPressed = false;
            bool tabPressed = false;
            var kb = Keyboard.current;
            if (kb != null && Time.frameCount != openedFrame)
            {
                enterPressed = kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame;
                tabPressed = kb.tabKey.wasPressedThisFrame;
            }

            Tick(Time.unscaledDeltaTime, enterPressed, tabPressed);
        }

        /// <summary>Deterministic update step for runtime typing timer, [Enter] submission, and 1x [Tab] exit.</summary>
        public void Tick(float deltaTime, bool enterPressed, bool tabPressed = false)
        {
            if (!IsActive) return;
            if (tabPressed)
            {
                CloseLaptop();
                return;
            }

            float dt = Mathf.Max(0f, deltaTime);
            if (!IsReport) RemainingTime -= dt;
            if (!IsReport && RemainingTime <= 0f)
            {
                SubmitInput(fromTimeout: true);
                if (!IsActive) return;
            }

            if (enterPressed)
            {
                SubmitInput(fromTimeout: false);
                if (!IsActive) return;
            }

            RefreshUi();
        }

        public void ProcessTypedCharacter(char c)
        {
            if (!IsActive) return;
            if (Time.frameCount == openedFrame) return;

            if (c == '\b')
            {
                if (CurrentInput.Length > 0)
                {
                    CurrentInput = CurrentInput.Substring(0, CurrentInput.Length - 1);
                    PlayTypingSound();
                    RefreshUi();
                }
                return;
            }

            if (char.IsControl(c)) return;
            if (CurrentInput.Length >= TargetSentence.Length + 12) return;

            int idx = CurrentInput.Length;
            bool isCorrect = idx < TargetSentence.Length &&
                             char.ToLowerInvariant(c) == char.ToLowerInvariant(TargetSentence[idx]);
            CurrentInput += c;
            PlayTypingSound();

            if (!isCorrect)
            {
                stressBarometer?.AddStress(TypoStressPenalty);
                if (feedbackLabel != null)
                    feedbackLabel.text = "<color=#FF5555>Salah ketik! Stres meningkat (+2%).</color>";
            }
            else if (feedbackLabel != null && CurrentInput.Length == TargetSentence.Length)
            {
                feedbackLabel.text = "<color=#9EE6A0>Kalimat lengkap! Tekan [ENTER] untuk mengirim.</color>";
            }

            RefreshUi();
        }

        public void SubmitInput(bool fromTimeout = false)
        {
            if (!IsActive) return;
            SubmittedAttempts++;

            bool exactMatch = string.Equals(CurrentInput, TargetSentence, StringComparison.OrdinalIgnoreCase);
            if (exactMatch)
            {
                CompletedSentences++;
                if (IsReportComplete)
                {
                    stressBarometer?.SetStress(StressBarometer.MaxStress);
                    CloseLaptop();
                    ReportContinueRequested?.Invoke();
                    SentenceSubmitted?.Invoke(true);
                    return;
                }
                if (!IsReport) MarkObjectiveProgress();
                SentenceSubmitted?.Invoke(true);

                if (CompletedSentences >= RequiredSentences)
                {
                    CloseLaptop();
                    return;
                }

                if (feedbackLabel != null)
                    feedbackLabel.text = IsReport ? "<color=#8BE78B>Bagian tersalin.</color>"
                        : $"<color=#8BE78B>Kalimat terkirim tepat waktu! ({CompletedSentences}/{RequiredSentences})</color>";
            }
            else
            {
                float penalty = fromTimeout ? TimeoutStressPenalty : IncompleteSubmitStressPenalty;
                stressBarometer?.AddStress(penalty);
                if (feedbackLabel != null)
                {
                    feedbackLabel.text = fromTimeout
                        ? $"<color=#FF4D4D>Waktu habis! Input terkirim otomatis belum selesai — Stres +{Mathf.RoundToInt(penalty)}%.</color>"
                        : $"<color=#FF4D4D>Ketikan belum selesai atau masih salah! Stres +{Mathf.RoundToInt(penalty)}%.</color>";
                }
                SentenceSubmitted?.Invoke(false);
            }

            LoadNextSentence();
            RefreshUi();
        }

        private void MarkObjectiveProgress()
        {
            if (activeInteractor == null) return;
            if (CompletedSentences >= RequiredSentencesForObjective)
            {
                activeInteractor.Flags.Set("objective.laptop");
                activeInteractor.Flags.Set("objective.book");
                activeInteractor.NotifyInteraction(StableId);
            }
        }

        private void LoadNextSentence()
        {
            CurrentInput = string.Empty;
            if (IsReport)
            {
                TargetSentence = reportSections[CompletedSentences];
                CurrentSentenceTimeLimit = RemainingTime = 0f;
                return;
            }
            string baseSentence = CollegeSentenceBank[sentenceIndex % CollegeSentenceBank.Length];
            TargetSentence = baseSentence;
            if (sentenceIndex >= CollegeSentenceBank.Length)
            {
                string extra = RandomTopicSuffixes[(sentenceIndex / CollegeSentenceBank.Length) % RandomTopicSuffixes.Length];
                TargetSentence += " " + extra;
            }
            sentenceIndex++;
            CurrentSentenceTimeLimit = GetTimeLimitForSentence(CompletedSentences);
            RemainingTime = CurrentSentenceTimeLimit;
        }

        private void PrepareTypingSound()
        {
            if (typingClip != null) return;
            const int sampleRate = 22050;
            var samples = new float[2205];
            var noise = new System.Random(34);
            for (int i = 0; i < samples.Length; i++)
            {
                float seconds = i / (float)sampleRate;
                float click = (float)(noise.NextDouble() * 2 - 1) * Mathf.Exp(-seconds * 128f);
                float resonance = Mathf.Sin(seconds * 2f * Mathf.PI * 620f) * Mathf.Exp(-seconds * 68f);
                samples[i] = (click * .5f + resonance * .18f) * Mathf.Clamp01(i / 8f);
            }
            typingClip = AudioClip.Create("Report keystroke", samples.Length, 1, sampleRate, false);
            typingClip.SetData(samples, 0);
            typingSource = gameObject.AddComponent<AudioSource>();
            typingSource.playOnAwake = false;
            typingSource.volume = .25f;
            typingSource.clip = typingClip;
        }

        private void PlayTypingSound()
        {
            if (IsReport && typingSource != null) typingSource.Play();
        }

        private void OnTextInput(char ch)
        {
            if (!IsActive) return;
            if (player != null && player.IsPaused) return;
            ProcessTypedCharacter(ch);
        }

        private void HookTextInput()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                kb.onTextInput -= OnTextInput;
                kb.onTextInput += OnTextInput;
            }
        }

        private void UnhookTextInput()
        {
            var kb = Keyboard.current;
            if (kb != null) kb.onTextInput -= OnTextInput;
        }

        private void RefreshUi()
        {
            if (panelRoot != null) panelRoot.SetActive(IsActive);
            if (overlayCanvas != null) overlayCanvas.enabled = IsActive && (player == null || !player.IsPaused);
            if (!IsActive) return;

            if (headerLabel != null)
            {
                string title = IsReport ? "BERITA KECELAKAAN" : "TUGAS KULIAH — MENGETIK LAPORAN";
                headerLabel.text = $"{title}  ({CompletedSentences}/{RequiredSentences})";
            }

            timerLabel.enabled = !IsReport;
            if (timerTrack.activeSelf == IsReport) timerTrack.SetActive(!IsReport);
            if (timerLabel != null)
                timerLabel.text = $"Sisa waktu: {Mathf.Max(0f, RemainingTime):F1} dtk";
            if (timerFill != null)
                timerFill.anchorMax = new Vector2(Mathf.Clamp01(RemainingTime / Mathf.Max(1f, CurrentSentenceTimeLimit)), 1f);

            if (targetDisplay != null)
                targetDisplay.text = BuildMonkeytypeRichText(TargetSentence, CurrentInput);

            if (typedDisplay != null)
                typedDisplay.text = string.IsNullOrEmpty(CurrentInput) ? "<color=#5A6870>Mulai mengetik di keyboard...</color>" : CurrentInput + "_";

            if (exitHintLabel != null)
                exitHintLabel.text = "Tekan [ENTER] untuk kirim kalimat  •  Tekan [TAB] untuk keluar laptop";
        }

        public static string BuildMonkeytypeRichText(string target, string typed)
        {
            var sb = new StringBuilder(target.Length * 22);
            int maxLen = target.Length;
            for (int i = 0; i < maxLen; i++)
            {
                char expected = target[i];
                if (i < typed.Length)
                {
                    bool match = char.ToLowerInvariant(typed[i]) == char.ToLowerInvariant(expected);
                    if (match)
                        sb.Append("<color=#FFD54F>").Append(expected).Append("</color>");
                    else
                        sb.Append("<color=#FF3B3B>").Append(expected == ' ' ? '·' : expected).Append("</color>");
                }
                else if (i == typed.Length)
                {
                    sb.Append("<color=#FFF176>").Append(expected).Append("</color>");
                }
                else
                {
                    sb.Append("<color=#67757D>").Append(expected).Append("</color>");
                }
            }
            return sb.ToString();
        }

        private void BuildUi()
        {
            if (overlayCanvas != null) return;

            var canvasGo = new GameObject("LaptopTypingUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            overlayCanvas = canvasGo.GetComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 15;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            panelRoot = new GameObject("MonkeytypePanel", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(canvasGo.transform, false);
            var bgRect = (RectTransform)panelRoot.transform;
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
            var bg = panelRoot.GetComponent<Image>();
            bg.color = new Color(0.055f, 0.068f, 0.078f, 0.96f);
            bg.raycastTarget = false;

            // Header
            headerLabel = CreateText("Header", panelRoot.transform, $"TUGAS KULIAH — MENGETIK LAPORAN (0/{RequiredSentencesForObjective})", 28, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Color(0.84f, 0.89f, 0.90f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 320f), new Vector2(1200f, 50f));

            // Timer bar + text
            timerLabel = CreateText("TimerText", panelRoot.transform, "Sisa waktu: 60.0 dtk", 24, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Color(0.96f, 0.72f, 0.28f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 250f), new Vector2(600f, 36f));
            var tTrack = CreateRect("TimerTrack", panelRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 215f), new Vector2(960f, 10f));
            timerTrack = tTrack.gameObject;
            tTrack.gameObject.AddComponent<Image>().color = new Color(0.14f, 0.17f, 0.19f, 1f);
            timerFill = CreateRect("TimerFill", tTrack, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            timerFill.gameObject.AddComponent<Image>().color = new Color(0.95f, 0.69f, 0.25f, 1f);

            // Monkeytype center target block
            targetDisplay = CreateText("TargetDisplay", panelRoot.transform, "", 38, FontStyle.Normal, TextAnchor.MiddleLeft,
                Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 65f), new Vector2(1180f, 190f));
            targetDisplay.supportRichText = true;
            targetDisplay.horizontalOverflow = HorizontalWrapMode.Wrap;
            targetDisplay.verticalOverflow = VerticalWrapMode.Overflow;

            // Live typed input box
            var inputBox = CreateRect("InputBox", panelRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -105f), new Vector2(1180f, 96f));
            inputBox.gameObject.AddComponent<Image>().color = new Color(0.09f, 0.115f, 0.135f, 0.98f);
            typedDisplay = CreateText("TypedDisplay", inputBox, "", 28, FontStyle.Normal, TextAnchor.MiddleLeft,
                new Color(0.92f, 0.95f, 0.96f, 1f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(-40f, -16f));

            // Status / penalty feedback
            feedbackLabel = CreateText("Feedback", panelRoot.transform, "", 24, FontStyle.Normal, TextAnchor.MiddleCenter,
                new Color(0.84f, 0.88f, 0.86f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -205f), new Vector2(1180f, 52f));
            feedbackLabel.supportRichText = true;

            // Single [TAB] exit hint
            exitHintLabel = CreateText("ExitHintLabel", panelRoot.transform, "Tekan [ENTER] untuk kirim kalimat  •  Tekan [TAB] untuk keluar laptop", 22, FontStyle.Normal, TextAnchor.MiddleCenter,
                new Color(0.82f, 0.84f, 0.82f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -335f), new Vector2(960f, 36f));

            panelRoot.SetActive(false);
            overlayCanvas.enabled = false;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            return rect;
        }

        private static Text CreateText(string name, Transform parent, string value, int fontSize, FontStyle style,
            TextAnchor align, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var rect = CreateRect(name, parent, anchorMin, anchorMax, pivot, pos, size);
            var txt = rect.gameObject.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = align;
            txt.color = color;
            txt.text = value;
            txt.raycastTarget = false;
            return txt;
        }
    }
}
