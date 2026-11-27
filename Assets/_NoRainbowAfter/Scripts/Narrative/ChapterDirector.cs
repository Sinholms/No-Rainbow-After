using System;
using System.IO;
using NoRainbowAfter.Interaction;
using NoRainbowAfter.Player;
using UnityEngine;

namespace NoRainbowAfter.Narrative
{
    public sealed class NarrativeCue
    {
        public string Speaker, Text, AudioCueId;
        public float Duration;
        public NarrativeCue(string speaker, string text, float duration, string audioCueId)
        { Speaker = speaker; Text = text; Duration = duration; AudioCueId = audioCueId; }
    }

    // AudioDirector may subscribe to this same cue stream; no audio playback is implied here.
    public interface IAudioCueSource { event Action<NarrativeCue> CueRequested; }
    public interface IShadowEncounter
    {
        bool IsReady { get; }
        event Action Ready;
        void Begin(Action<EncounterResponse> resolve);
    }

    public sealed class ChapterDirector : MonoBehaviour, IAudioCueSource
    {
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private FirstPersonPlayer player;
        [SerializeField] private FirstPersonMotor motor;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private float thresholdZ = 5f;
        public event Action<NarrativeCue> CueRequested;
        public event Action<int> LoopChanged;
        public event Action<string> BeatCompleted;
        public event Action ChapterTransitionRequested;
        public event Action ChapterThreeTransitionRequested;
        public GameState State { get; private set; }
        public int LoopIndex { get; private set; }
        public int BeatIndex { get; private set; }
        public int PreviewChapter { get; private set; }
        public bool HasReadOvertimeSlip => BeatIndex >= 3;
        public bool AwaitingEncounter => (BeatIndex == 3 || BeatIndex == 4) && !encounterBegun && !resolved;
        public bool IsCurrentEncounter(IShadowEncounter candidate) => ReferenceEquals(encounter, candidate);
        // Tests set this before scene load and restore it after the test run.
        public static string StoragePathOverride { get; set; }
        // Environmental monologues use the existing cue stream without advancing story beats.
        public void RequestNarrativeCue(NarrativeCue cue) { if(cue != null) CueRequested?.Invoke(cue); }

        private bool outside, armed, encounterBegun, resolved, bound, pathConfigured;
        private SceneInteractable overtimeSlip;
        private int generation;
        private IShadowEncounter encounter;
        private string storagePath;
        private static (int chapter, GameState state) pendingRestart;

        public void Configure(PlayerInteractor source, FirstPersonPlayer input, FirstPersonMotor movement, Transform actor, float doorwayZ)
        {
            Unbind(); interactor = source; player = input; motor = movement; playerTransform = actor; thresholdZ = doorwayZ;
            if (isActiveAndEnabled) Bind();
        }
        public void SetStoragePath(string path)
        {
            if (State != null) throw new InvalidOperationException("State already initialized");
            storagePath = path; pathConfigured = true;
        }
        public void ConfigureChapterPreview(int chapter)
        {
            if (State != null) throw new InvalidOperationException("State already initialized");
            if (chapter < 1 || chapter > 2) throw new ArgumentOutOfRangeException(nameof(chapter));
            PreviewChapter = chapter;
        }

        public void PrepareChapterRestart(int chapter) => pendingRestart = (chapter, State);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void CancelChapterRestart() => pendingRestart = default;

        private void OnEnable() => Bind();
        private void Start()
        {
            // Setiap beat menandai satu tahap cerita. Setelah tugas kamar selesai, pemain bisa
            // keluar dan kembali untuk menemukan slip lembur. Membacanya membuka pertemuan dengan Shadow.
            var mainStorage = pathConfigured ? storagePath : StoragePathOverride ??
                Path.Combine(Application.persistentDataPath, "clues-v1.json");
            if (pendingRestart.state != null && pendingRestart.chapter == PreviewChapter)
            {
                // Pakai lagi GameState agar petunjuk yang terkumpul tetap dimiliki saat bab diulang.
                State = pendingRestart.state;
                CancelChapterRestart();
                State.ResetChapterForRestart(PreviewChapter);
                BeatIndex = State.Chapter1Beat;
                LoopIndex = State.LoopIndex;
                if (interactor != null)
                    foreach (string objective in State.CompletedObjectives) interactor.Flags.Set(objective);
            }
            else
                State = new GameState(PreviewChapter == 0 ? mainStorage : null);
            SetStressFloor(PreviewChapter == 2 ? 2 : 1);
            foreach (var target in FindObjectsByType<SceneInteractable>(FindObjectsInactive.Include))
                if (target.gameObject.scene == gameObject.scene && target.StableId == "INT_OVERTIME")
                {
                    overtimeSlip = target;
                    overtimeSlip.SetAvailable(false);
                    break;
                }
            if (PreviewChapter < 2)
            {
                Complete("C1-01", "Raka", "Hari ini aku harus berangkat.", "c1_wake");
                return;
            }

            ChapterOneFinished = true;
            State.ActiveChapter = "Chapter2";
        }
        private void OnDisable() { Unbind(); generation++; SetEncounter(null); }
        private void Bind()
        {
            if (bound) return;
            if (interactor != null) interactor.InteractionCompleted += OnInteraction;
            if (motor != null) motor.PoseRestored += ResetCrossing;
            bound = true; ResetCrossing();
        }
        private void Unbind()
        {
            if (!bound) return;
            if (interactor != null) interactor.InteractionCompleted -= OnInteraction;
            if (motor != null) motor.PoseRestored -= ResetCrossing;
            bound = false;
        }
        public void SetEncounter(IShadowEncounter provider)
        {
            if (!ReferenceEquals(encounter, provider))
            {
                generation++;
                if (encounterBegun && !resolved) encounterBegun = false;
            }
            if (encounter != null) encounter.Ready -= TryBeginEncounter;
            encounter = provider;
            if (encounter != null) encounter.Ready += TryBeginEncounter;
            TryBeginEncounter();
        }
        // Invalidates an active unresolved attempt (e.g. Caught or checkpoint restore) without repeating C1-04.
        public void ResetActiveEncounter()
        {
            if (!encounterBegun || resolved) return;
            generation++;
            encounterBegun = false;
            TryBeginEncounter();
        }
        private void OnInteraction(string id) { if (id == "INT_OVERTIME") RegisterSlip(); }
        private void RegisterSlip()
        {
            // Slip hanya valid setelah loop pertama (beat 2); clue kedua dicatat sebelum encounter dimulai.
            if (State == null || BeatIndex != 2 || LoopIndex < 1) return;
            State.AddClue("CLUE_02");
            Complete("C1-03", "Raka", "Lembur lagi. Aku hampir lupa.", "c1_slip");
            TryBeginEncounter();
        }
        private void Update()
        {
            if (State == null || ChapterOneFinished || playerTransform == null || player == null || player.IsPaused || player.IsInspecting || player.IsUsingLaptop) return;
            if (Mathf.Abs(playerTransform.position.x) > 1.3f) ResetCrossing();
            else SamplePosition(playerTransform.position.z);
            TryBeginEncounter();
        }
        public void SamplePosition(float z)
        {
            if (State == null || ChapterOneFinished || BeatIndex > 4 || player == null || player.IsPaused || player.IsInspecting || player.IsUsingLaptop) return;
            bool taskReady = interactor != null && (interactor.Flags.Contains("objective.laptop") || interactor.Flags.Contains("objective.book"));
            // Batas keluar dan masuk berbeda agar langkah kecil di ambang pintu tidak terhitung sebagai satu putaran.
            if (!outside && z >= thresholdZ + 1f && taskReady)
            {
                outside = true; armed = true; TryBeginEncounter();
            }
            else if (outside && z <= thresholdZ - .5f)
            {
                outside = false;
                if (!armed) return;
                armed = false;
                if (BeatIndex < 4 && LoopIndex < 3) { LoopIndex++; State.LoopIndex = LoopIndex; LoopChanged?.Invoke(LoopIndex); }
                if (BeatIndex == 1)
                {
                    overtimeSlip?.SetAvailable(true);
                    Complete("C1-02", "Raka", "Ada yang berubah di kamar ini.", "c1_loop");
                }
            }
        }
        public void ResetCrossing() { outside = false; armed = false; }
        private void TryBeginEncounter()
        {
            // Encounter hanya berjalan di beat 3/4 ketika pemain berada di lorong dan provider siap.
            if (!isActiveAndEnabled || player == null || player.IsPaused || player.IsInspecting || player.IsUsingLaptop ||
                (BeatIndex != 3 && BeatIndex != 4) || !outside || encounterBegun || encounter == null || !encounter.IsReady) return;
            encounterBegun = true;
            int token = ++generation;
            var provider = encounter;
            if (BeatIndex == 3) Complete("C1-04", "Raka", "Siapa di sana?", "AUD_C1_SHADOW");
            if (isActiveAndEnabled && token == generation && ReferenceEquals(provider, encounter))
                provider.Begin(response => Resolve(token, response));
        }
        private void Resolve(int token, EncounterResponse response)
        {
            // Abaikan callback lama / respons lain; hanya Fight atau Hide pada beat respons yang sah.
            if (!isActiveAndEnabled || !encounterBegun || resolved || token != generation || BeatIndex != 4 ||
                (response != EncounterResponse.Fight && response != EncounterResponse.Hide)) return;
            resolved = true; State.Chapter1Response = response;
            Complete("C1-05", "Raka", response == EncounterResponse.Fight ? "Bayangan itu..." : "Sudah pergi?", "c1_resolve");
            if (!isActiveAndEnabled || token != generation) return;
            Complete("C1-06", "", "", "c1_transition");
        }
        public bool ChapterOneFinished { get; private set; }
        public bool FinishChapterOne()
        {
            // Setelah tahap keenam, kirim sinyal akhir bab satu kali agar pendengar bisa memulai bab 2.
            if (State == null || BeatIndex < 6 || ChapterOneFinished) return false;
            ChapterOneFinished = true;
            SetStressFloor(2);
            ChapterTransitionRequested?.Invoke();
            return true;
        }

        private void SetStressFloor(int chapter)
        {
            var stress = GetComponent<StressBarometer>();
            if (stress != null)
                stress.SetMinimumStress(chapter == 2 ? StressBarometer.ChapterTwoMinimumStress : StressBarometer.ChapterOneMinimumStress);
        }
        public bool RecordEllaMessage()
        {
            if (State == null || State.EllaMessageSent ||
                (State.Chapter2Beat != ChapterTwoBeat.WokeInDorm && State.Chapter2Beat != ChapterTwoBeat.OvertimeRequested))
                return false;
            State.EllaMessageSent = true;
            return true;
        }

        public void EnterChapterThree()
        {
            if (State != null) State.ActiveChapter = "Chapter3";
        }

        public bool AdvanceChapterTwo(ChapterTwoBeat next)
        {
            if (!ChapterOneFinished || State == null) return false;
            // Daftar ini adalah state machine bab 2: hanya perpindahan di bawah yang sah.
            bool allowed = (State.Chapter2Beat, next) switch
            {
                (ChapterTwoBeat.None, ChapterTwoBeat.WokeInDorm) => true,
                (ChapterTwoBeat.WokeInDorm, ChapterTwoBeat.StayedInBed) => true,
                (ChapterTwoBeat.WokeInDorm, ChapterTwoBeat.OvertimeRequested) => true,
                (ChapterTwoBeat.StayedInBed, ChapterTwoBeat.WokeInDorm) => true,
                (ChapterTwoBeat.OvertimeRequested, ChapterTwoBeat.StreetEntered) => true,
                (ChapterTwoBeat.StreetEntered, ChapterTwoBeat.ShelterReached) => true,
                (ChapterTwoBeat.ShelterReached, ChapterTwoBeat.BoardedBus) => true,
                _ => false
            };
            if (!allowed) return false;
            if (next == ChapterTwoBeat.WokeInDorm) State.EllaMessageSent = false;
            State.Chapter2Beat = next;
            State.ActiveChapter = next == ChapterTwoBeat.BoardedBus ? "Chapter3" : "Chapter2";
            BeatCompleted?.Invoke($"C2-{(int)next:00}");
            if (next == ChapterTwoBeat.BoardedBus) ChapterThreeTransitionRequested?.Invoke();
            return true;
        }
        private void Complete(string id, string speaker, string text, string audio)
        {
            // Beat bab 1 maju berurutan; cue subtitle/audio mengikuti event yang sama.
            BeatIndex++; State.Chapter1Beat = BeatIndex;
            BeatCompleted?.Invoke(id);
            CueRequested?.Invoke(new NarrativeCue(speaker, text, 4f, audio));
        }
    }
}
