using System;
using System.Collections.Generic;
using NoRainbowAfter.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NoRainbowAfter.Interaction
{
    /// <summary>
    /// Minigame mengetik gaya Monkeytype pada laptop di meja belajar Raka (INT_LAPTOP).
    /// Dipakai di Chapter 1 ("Kerjakan Tugas" 5 kalimat) maupun Chapter 2 ("Kerjakan esai" 3 bagian).
    ///
    /// [TEKNIS KODING]
    /// Dipecah menjadi 3 file partial class:
    /// 1. LaptopTypingMinigame.cs (logika input ketikan, pengatur waktu kalimat, penilaian tugas/esai)
    /// 2. LaptopTypingMinigame.Ui.cs (kanvas UI, Rich Text pewarnaan per karakter, bilah timer)
    /// 3. LaptopTypingMinigame.Audio.cs (sintesis klik keyboard mekanikal & suara kirim/salah)
    /// </summary>
    public sealed partial class LaptopTypingMinigame : MonoBehaviour, IInteractable
    {
        // =========================================================================
        // Konstanta Aturan & Bank Kalimat Kuliah
        // =========================================================================

        public const float SentenceTimeLimit = 60f;
        public const float MinSentenceTimeLimit = 30f;
        public const int RequiredSentencesForObjective = 5;
        public const float TypoStressPenalty = 2f;
        public const float IncompleteSubmitStressPenalty = 10f;
        public const float TimeoutStressPenalty = 15f;

        private const float EssaySectionSeconds = 45f;

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

        // =========================================================================
        // Referensi Scene, Komponen UI, Audio, & State Pengetikan
        // =========================================================================

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
        private string[] essaySections;

        private AudioSource typingSource;
        private AudioClip typingClip;
        private AudioClip submitClip;
        private AudioClip errorClip;

        public string StableId => "INT_LAPTOP";
        public string DisplayName => "Laptop Raka";
        public string Prompt => IsEssayComplete ? null : IsEssay ? "Kerjakan esai" : "Kerjakan Tugas";

        public bool IsActive { get; private set; }
        public string TargetSentence { get; private set; } = string.Empty;
        public string CurrentInput { get; private set; } = string.Empty;
        public float CurrentSentenceTimeLimit { get; private set; } = SentenceTimeLimit;
        public float RemainingTime { get; private set; } = SentenceTimeLimit;
        public int CompletedSentences { get; private set; }
        public int SubmittedAttempts { get; private set; }
        public int JudgedEssaySections { get; private set; }
        public bool IsEssayComplete => IsEssay && JudgedEssaySections == essaySections.Length;

        private bool IsEssay => essaySections != null;
        private int RequiredSentences => essaySections?.Length ?? RequiredSentencesForObjective;

        public event Action<bool> SessionStateChanged;
        public event Action<bool> SentenceSubmitted;
        public event Action<int> EssayFinished;

        // =========================================================================
        // Konfigurasi Mode Esai (Chapter 2) & Siklus Hidup
        // =========================================================================

        public void ConfigureEssay(IReadOnlyList<string> sections)
        {
            if (IsActive || sections == null || sections.Count == 0)
            {
                return;
            }
            essaySections = new string[sections.Count];
            for (int i = 0; i < sections.Count; i++)
            {
                essaySections[i] = sections[i];
            }
            ResetEssay();
        }

        public void ResetEssay()
        {
            if (!IsEssay || IsActive)
            {
                return;
            }
            JudgedEssaySections = 0;
            CompletedSentences = 0;
            SubmittedAttempts = 0;
            LoadNextSentence();
        }

        public void SkipEssay()
        {
            if (!IsEssay || IsEssayComplete)
            {
                return;
            }
            if (IsActive)
            {
                CloseLaptop();
            }
            CompletedSentences = Mathf.Max(1, CompletedSentences);
            JudgedEssaySections = essaySections.Length;
        }

        private void Awake() => BuildUi();

        private void OnDisable()
        {
            UnhookTextInput();
            if (IsActive)
            {
                CloseLaptop();
            }
        }

        private void OnDestroy()
        {
            UnhookTextInput();
            if (overlayCanvas != null) Destroy(overlayCanvas.gameObject);
            if (typingClip != null) Destroy(typingClip);
            if (submitClip != null) Destroy(submitClip);
            if (errorClip != null) Destroy(errorClip);
        }

        // =========================================================================
        // Interaksi, Buka/Tutup Sesi Laptop, & Loop Input Karakter
        // =========================================================================

        public void SetFocused(bool focused) { }

        public void Interact(PlayerInteractor interactor)
        {
            if (interactor == null || IsActive || IsEssayComplete)
            {
                return;
            }
            activeInteractor = interactor;
            player = interactor.GetComponent<FirstPersonPlayer>();
            stressBarometer = interactor.GetComponent<StressBarometer>();
            if (stressBarometer == null && player != null)
            {
                stressBarometer = player.GetComponent<StressBarometer>();
            }
            OpenLaptop();
        }

        // Membuka sesi mengetik dan mengalihkan kendali pemain ke laptop.
        public void OpenLaptop()
        {
            if (IsActive || IsEssayComplete)
            {
                return;
            }
            BuildUi();
            if (!IsEssay && CompletedSentences >= RequiredSentencesForObjective)
            {
                CompletedSentences = 0;
            }

            IsActive = true;
            openedFrame = Time.frameCount;
            if (player != null)
            {
                player.SetUsingLaptop(true);
                player.BreatheRequested -= OnTabCloseRequested;
                player.BreatheRequested += OnTabCloseRequested;
            }

            PrepareTypingSound();
            if (!IsEssay)
            {
                LoadNextSentence();
            }
            HookTextInput();

            if (feedbackLabel != null)
            {
                feedbackLabel.text = IsEssay
                    ? "Ketik bagian esai. Tekan [ENTER] untuk kirim."
                    : "Ketik bagian tugas. Tekan [ENTER] untuk kirim.";
            }
            RefreshUi();
            SessionStateChanged?.Invoke(true);
        }

        public void CloseLaptop()
        {
            if (!IsActive)
            {
                return;
            }
            IsActive = false;
            if (player != null)
            {
                player.BreatheRequested -= OnTabCloseRequested;
            }
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
            {
                CloseLaptop();
            }
        }

        private void Update()
        {
            if (!IsActive)
            {
                return;
            }
            if (player != null && player.IsPaused)
            {
                if (overlayCanvas != null) overlayCanvas.enabled = false;
                if (typingSource != null && typingSource.isPlaying) typingSource.Stop();
                return;
            }
            if (overlayCanvas != null && !overlayCanvas.enabled)
            {
                overlayCanvas.enabled = true;
            }

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

        public void Tick(float deltaTime, bool enterPressed, bool tabPressed = false)
        {
            if (!IsActive)
            {
                return;
            }
            if (tabPressed)
            {
                CloseLaptop();
                return;
            }

            float dt = Mathf.Max(0f, deltaTime);
            RemainingTime -= dt;
            if (RemainingTime <= 0f)
            {
                SubmitInput(fromTimeout: true);
                return;
            }

            if (enterPressed)
            {
                SubmitInput(fromTimeout: false);
                if (!IsActive)
                {
                    return;
                }
            }

            RefreshUi();
        }

        // Karakter dibandingkan dengan posisi yang sama pada kalimat target.
        public void ProcessTypedCharacter(char c)
        {
            if (!IsActive || Time.frameCount == openedFrame)
            {
                return;
            }

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

            if (char.IsControl(c) || CurrentInput.Length >= TargetSentence.Length + 12)
            {
                return;
            }

            int idx = CurrentInput.Length;
            bool isCorrect = idx < TargetSentence.Length &&
                             char.ToLowerInvariant(c) == char.ToLowerInvariant(TargetSentence[idx]);
            CurrentInput += c;
            PlayTypingSound();

            if (!isCorrect)
            {
                stressBarometer?.AddStress(TypoStressPenalty);
                PlayErrorSound();
                if (feedbackLabel != null)
                {
                    feedbackLabel.text = "<color=#FF5555>Periksa huruf yang merah.</color>";
                }
            }
            else if (feedbackLabel != null && CurrentInput.Length == TargetSentence.Length)
            {
                feedbackLabel.text = "<color=#9EE6A0>Kalimat lengkap! Tekan [ENTER] untuk mengirim.</color>";
            }

            RefreshUi();
        }

        // =========================================================================
        // Evaluasi Pengiriman Kalimat & Transisi Kalimat Berikutnya
        // =========================================================================

        // Nilai ketikan saat Enter atau waktu habis, lalu lanjutkan sesuai mode (Tugas / Esai).
        public void SubmitInput(bool fromTimeout = false)
        {
            if (!IsActive)
            {
                return;
            }
            SubmittedAttempts++;

            bool exactMatch = string.Equals(CurrentInput, TargetSentence, StringComparison.OrdinalIgnoreCase);
            // Pada tugas biasa, kalimat tepat tetap dihitung saat waktu habis. Esai harus dikirim sebelum itu.
            bool passed = exactMatch && (!IsEssay || !fromTimeout);
            float penalty = fromTimeout ? TimeoutStressPenalty : IncompleteSubmitStressPenalty;
            if (!passed)
            {
                stressBarometer?.AddStress(penalty);
            }

            if (IsEssay)
            {
                SubmitEssaySection(passed, fromTimeout);
                return;
            }

            if (exactMatch)
            {
                CompletedSentences++;
                MarkObjectiveProgress();
                PlaySubmitSound();
                SentenceSubmitted?.Invoke(true);

                if (CompletedSentences >= RequiredSentences)
                {
                    CloseLaptop();
                    return;
                }

                if (feedbackLabel != null)
                {
                    feedbackLabel.text = "<color=#8BE78B>Bagian terkirim.</color>";
                }
            }
            else
            {
                PlayErrorSound();
                if (feedbackLabel != null)
                {
                    feedbackLabel.text = fromTimeout
                        ? "<color=#FF5555>Waktu habis. Lanjut ke bagian berikutnya.</color>"
                        : "<color=#FF5555>Bagian belum tepat. Perbaiki sebelum waktu habis.</color>";
                }
                SentenceSubmitted?.Invoke(false);
            }

            LoadNextSentence();
            RefreshUi();
        }

        private void SubmitEssaySection(bool passed, bool fromTimeout)
        {
            if (!passed && !fromTimeout)
            {
                PlayErrorSound();
                feedbackLabel.text = "<color=#FF5555>Bagian belum tepat. Perbaiki sebelum waktu habis.</color>";
                SentenceSubmitted?.Invoke(false);
                return;
            }

            if (passed)
            {
                CompletedSentences++;
                PlaySubmitSound();
            }
            else
            {
                PlayErrorSound();
            }

            JudgedEssaySections++;
            if (IsEssayComplete)
            {
                CloseLaptop();
                SentenceSubmitted?.Invoke(passed);
                EssayFinished?.Invoke(CompletedSentences);
                return;
            }

            feedbackLabel.text = passed
                ? "<color=#8BE78B>Bagian terkirim.</color>"
                : "<color=#FF5555>Waktu habis. Lanjut ke bagian berikutnya.</color>";
            LoadNextSentence();
            RefreshUi();
            SentenceSubmitted?.Invoke(passed);
        }

        // Setelah jumlah kalimat objektif tercapai, simpan flag dan kirim pemberitahuan.
        private void MarkObjectiveProgress()
        {
            if (activeInteractor == null)
            {
                return;
            }
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
            if (IsEssay)
            {
                TargetSentence = essaySections[JudgedEssaySections];
                CurrentSentenceTimeLimit = RemainingTime = EssaySectionSeconds;
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

        private void OnTextInput(char ch)
        {
            if (!IsActive || (player != null && player.IsPaused))
            {
                return;
            }
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
            if (kb != null)
            {
                kb.onTextInput -= OnTextInput;
            }
        }
    }
}
