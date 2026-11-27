using System;
using NoRainbowAfter.Level;
using NoRainbowAfter.Narrative;
using NoRainbowAfter.Player;
using UnityEngine;
using UnityEngine.UI;

namespace NoRainbowAfter.AI
{
    public enum ShadowEncounterState
    {
        Dormant,
        Spawn,
        Pursue,
        FightResolved,
        HideResolved,
        Caught,
        Grabbed
    }

    /// <summary>
    /// Mengatur mesin keadaan (state machine) pertemuan dengan monster bayangan di Chapter 1.
    /// Dari Dormant, bayangan muncul (Spawn) lalu mengejar (Pursue), hingga pemain
    /// berhasil melawan (3 pukulan), bersembunyi di balik cover/lemari, atau tertangkap.
    ///
    /// [TEKNIS KODING]
    /// Dipecah menjadi 4 file partial class:
    /// 1. ShadowEncounterController.cs (field SerializeField, siklus hidup, & mesin keadaan utama)
    /// 2. ShadowEncounterController.Pursuit.cs (navigasi lorong, raycast cover, gerak lantai)
    /// 3. ShadowEncounterController.CombatAndCapture.cs (lawan 3x, cengkeram pertama, tertangkap)
    /// 4. ShadowEncounterController.Ui.cs (HUD prompt "[Klik Kiri] Lawan" & layar blackout)
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed partial class ShadowEncounterController : MonoBehaviour, IShadowEncounter, IAudioCueSource
    {
        // =========================================================================
        // Konstanta & Konfigurasi Inspector (Scene Bindings + Rules)
        // =========================================================================

        private static readonly Vector3[] DormFallbackSpawns =
        {
            new Vector3(0.10f, 0.04f, 2.20f),
            new Vector3(-0.15f, 0.04f, 3.20f),
            new Vector3(-0.20f, 0.04f, 0.65f),
            new Vector3(0.25f, 0.04f, 4.10f)
        };

        private const float GrabDuration = 1.15f;
        private const float EscapeRecoveryDuration = 3f;

        [Header("Scene bindings")]
        [SerializeField] private ChapterDirector director;
        [SerializeField] private FirstPersonPlayer player;
        [SerializeField] private FirstPersonMotor motor;
        [SerializeField] private DormCheckpoint checkpoint;
        [SerializeField] private CoverZone[] coverZones;
        [SerializeField] private ShadowPlaceholderVisual placeholderVisual;

        [Header("Staging & movement (metres, seconds)")]
        [SerializeField] private Vector3 dormantPosition = new Vector3(90f, 0.12f, -1.65f);
        [SerializeField, Min(0.2f)] private float spawnHoldDuration = 1.15f;
        [SerializeField, Min(0.5f)] private float pursueSpeed = 2.15f;
        [SerializeField, Min(0.5f)] private float searchPassSpeed = 2.35f;
        [SerializeField, Min(0.5f)] private float turnSpeedDegrees = 320f;
        [SerializeField] private float gravity = -20f;

        [Header("Fight / Hide / Caught rules")]
        [SerializeField, Min(1.1f)] private float fightRange = 2.75f;
        [SerializeField, Range(15f, 60f)] private float fightAimHalfAngle = 34f;
        [SerializeField, Min(0.15f)] private float fightFeedbackDuration = 0.42f;
        [SerializeField, Min(0.5f)] private float requiredHideDuration = 2.0f;
        [SerializeField, Min(1.0f)] private float coverPassClearanceZ = 2.15f;
        [SerializeField, Range(15f, 60f)] private float crouchedGazeHalfAngle = 35f;
        [SerializeField, Min(0.15f)] private float hideResolveDuration = 0.38f;
        [SerializeField, Min(0.55f)] private float catchDistance = 1.05f;
        [SerializeField, Min(0.15f)] private float caughtFadeOutDuration = 0.32f;
        [SerializeField, Min(0.15f)] private float caughtFadeInDuration = 0.38f;
        [SerializeField] private bool allowFirstEscape;

        // =========================================================================
        // State Runtime, UI, Event, & Properti Publik
        // =========================================================================

        private bool escapeUsed;
        private ChapterOneCorridorShift shiftedWorld;
        private float wardrobeHideTime;
        private FirstPersonCaptureFeedback captureFeedback;
        private bool grabWillEscape;

        private CharacterController monsterController;
        private Action<EncounterResponse> activeResolve;
        private Canvas uiCanvas;
        private Text fightPromptLabel;
        private int successfulHits;
        private Image blackoutImage;

        private float stateTimer;
        private float hideTimer;
        private float verticalVelocity;
        private float postCaughtGraceTimer;
        private float searchTurnPauseTimer;
        private int searchDirection = -1;
        private CoverZone trackedCoverZone;
        private bool spottedEnteringCover;
        private bool sawMonsterNorthOfCover;
        private bool sawMonsterSouthOfCover;
        private bool caughtRestoredPose;
        private bool internalCheckpointRestore;
        private bool permanentlyResolved;
        private bool bound;
        private float currentBlackoutAlpha;

        public event Action Ready;
        public event Action<NarrativeCue> CueRequested;
        public event Action<ShadowEncounterState> StateChanged;

        public ShadowEncounterState State { get; private set; } = ShadowEncounterState.Dormant;
        public float DamageBorderAlpha => captureFeedback != null ? captureFeedback.DamageAlpha : 0f;
        public bool EscapeUsed => escapeUsed;
        public int SuccessfulHits => successfulHits;
        public float HideElapsedSeconds => hideTimer;
        public float BlackoutAlpha => currentBlackoutAlpha;
        public int CaughtCount { get; private set; }
        public bool IsFightPromptVisible => fightPromptLabel != null && fightPromptLabel.enabled;

        public bool IsReady =>
            isActiveAndEnabled &&
            !permanentlyResolved &&
            State == ShadowEncounterState.Dormant &&
            director != null &&
            player != null &&
            motor != null &&
            (shiftedWorld == null || shiftedWorld.CanBegin);

        public void EnableFirstEscape() => allowFirstEscape = true;

        // =========================================================================
        // Siklus Hidup Komponen & Pengikatan ke ChapterDirector
        // =========================================================================

        public void Configure(
            ChapterDirector chapterDirector,
            FirstPersonPlayer firstPersonPlayer,
            FirstPersonMotor firstPersonMotor,
            DormCheckpoint dormCheckpoint,
            CoverZone[] zones,
            ShadowPlaceholderVisual visual)
        {
            UnbindEvents();
            director = chapterDirector;
            player = firstPersonPlayer;
            motor = firstPersonMotor;
            checkpoint = dormCheckpoint;
            coverZones = zones;
            placeholderVisual = visual;
            captureFeedback?.Configure(motor);
            shiftedWorld = director != null ? director.GetComponent<ChapterOneCorridorShift>() : null;

            if (isActiveAndEnabled)
            {
                BindEvents();
                RegisterWithDirector();
            }
        }

        private void Awake()
        {
            shiftedWorld = director != null ? director.GetComponent<ChapterOneCorridorShift>() : null;
            monsterController = GetComponent<CharacterController>();
            if (placeholderVisual == null)
            {
                placeholderVisual = GetComponent<ShadowPlaceholderVisual>();
            }

            BuildRuntimeUi();
            captureFeedback = gameObject.AddComponent<FirstPersonCaptureFeedback>();
            captureFeedback.Configure(motor);
            SetMonsterActiveInWorld(false, dormantPosition, Quaternion.Euler(0f, 180f, 0f));
        }

        private void OnEnable()
        {
            BindEvents();
            if (uiCanvas != null)
            {
                uiCanvas.enabled = true;
            }
            RegisterWithDirector();
            RefreshUi();
        }

        private void Start()
        {
            RegisterWithDirector();
            if (IsReady)
            {
                Ready?.Invoke();
            }
        }

        private void OnDisable()
        {
            captureFeedback?.ResetFeedback();
            UnbindEvents();
            if (player != null && player.IsTransitionLocked &&
                (State == ShadowEncounterState.Grabbed || State == ShadowEncounterState.Caught))
            {
                player.SetTransitionLocked(false);
            }

            activeResolve = null;
            currentBlackoutAlpha = 0f;
            postCaughtGraceTimer = 0f;
            SetMonsterActiveInWorld(false, dormantPosition, Quaternion.Euler(0f, 180f, 0f));
            if (!permanentlyResolved)
            {
                SetState(ShadowEncounterState.Dormant);
            }

            if (director != null && director.IsCurrentEncounter(this))
            {
                director.SetEncounter(null);
            }

            if (uiCanvas != null)
            {
                uiCanvas.enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (uiCanvas != null)
            {
                Destroy(uiCanvas.gameObject);
            }
        }

        private void BindEvents()
        {
            if (bound)
            {
                return;
            }
            if (player != null)
            {
                player.FightRequested += OnFightRequested;
            }
            if (motor != null)
            {
                motor.PoseRestored += OnPlayerPoseRestored;
            }
            bound = true;
        }

        private void UnbindEvents()
        {
            if (!bound)
            {
                return;
            }
            if (player != null)
            {
                player.FightRequested -= OnFightRequested;
            }
            if (motor != null)
            {
                motor.PoseRestored -= OnPlayerPoseRestored;
            }
            bound = false;
        }

        private void RegisterWithDirector()
        {
            if (director != null && !permanentlyResolved && !director.IsCurrentEncounter(this))
            {
                director.SetEncounter(this);
            }
        }

        // =========================================================================
        // Mulai Percobaan & Loop Mesin Keadaan (FSM Tick)
        // =========================================================================

        // Simpan callback penyelesaian saat memulai percobaan. Cerita maju jika pemain berhasil melawan atau bersembunyi.
        public void Begin(Action<EncounterResponse> resolve)
        {
            if (!isActiveAndEnabled || permanentlyResolved || resolve == null)
            {
                return;
            }

            // Director memberikan callback ini saat encounter dimulai; hanya jalur berhasil yang memanggilnya.
            // Percobaan baru mereset pukulan, hitungan sembunyi, dan jeda setelah tertangkap.
            activeResolve = resolve;
            postCaughtGraceTimer = 0f;
            successfulHits = 0;
            stateTimer = 0f;
            ResetHideTracking();
            verticalVelocity = 0f;
            searchDirection = -1;
            searchTurnPauseTimer = 0f;
            currentBlackoutAlpha = 0f;

            Vector3 spawnPos = SelectSafeCorridorSpawn();
            escapeUsed = false;
            captureFeedback?.ResetFeedback();

            Vector3 toPlayer = motor != null ? (motor.transform.position - spawnPos) : Vector3.back;
            toPlayer.y = 0f;
            Quaternion spawnRot = toPlayer.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(toPlayer.normalized, Vector3.up)
                : Quaternion.Euler(0f, 180f, 0f);

            SetMonsterActiveInWorld(true, spawnPos, spawnRot);
            SetState(ShadowEncounterState.Spawn);
            RefreshUi();
        }

        private void Update() => TickEncounter(Time.deltaTime);

        public void TickEncounter(float deltaTime)
        {
            // Hentikan encounter lokal jika ChapterDirector sudah menggantinya dengan pengendali lain.
            if (director != null && State != ShadowEncounterState.Dormant && !permanentlyResolved && !director.IsCurrentEncounter(this))
            {
                AbortToDormant(notifyDirectorReset: false);
                return;
            }

            bool suspended = player != null && (player.IsPaused || player.IsInspecting || player.IsUsingLaptop);
            if (suspended)
            {
                captureFeedback?.Tick(0f, true, 0f);
                RefreshUi();
                return;
            }

            float dt = Mathf.Clamp(deltaTime, 0f, 0.1f);
            captureFeedback?.Tick(dt, false, State == ShadowEncounterState.Grabbed ? stateTimer / GrabDuration : 0f);
            if (postCaughtGraceTimer > 0f)
            {
                postCaughtGraceTimer = Mathf.Max(0f, postCaughtGraceTimer - dt);
            }

            if (dt <= 0f)
            {
                RefreshUi();
                return;
            }

            switch (State)
            {
                // Spawn memberi jeda sebelum Pursue; setelah itu tiap frame menjalankan aturan fase aktifnya.
                case ShadowEncounterState.Spawn:
                    TickSpawn(dt);
                    break;
                case ShadowEncounterState.Pursue:
                    TickPursue(dt);
                    break;
                case ShadowEncounterState.FightResolved:
                    TickFightResolved(dt);
                    break;
                case ShadowEncounterState.HideResolved:
                    TickHideResolved(dt);
                    break;
                case ShadowEncounterState.Caught:
                    TickCaught(dt);
                    break;
                case ShadowEncounterState.Grabbed:
                    TickGrabbed(dt);
                    break;
            }

            placeholderVisual?.TickSecondaryMotion(dt);
            RefreshUi();
        }

        private void SetState(ShadowEncounterState next)
        {
            if (State == next)
            {
                return;
            }
            State = next;
            StateChanged?.Invoke(next);
        }

        public void AbortToDormant(bool notifyDirectorReset = true)
        {
            captureFeedback?.ResetFeedback();
            if (permanentlyResolved)
            {
                return;
            }

            activeResolve = null;
            postCaughtGraceTimer = 0f;
            stateTimer = 0f;
            currentBlackoutAlpha = 0f;
            ResetHideTracking();
            if (player != null && player.IsTransitionLocked)
            {
                player.SetTransitionLocked(false);
            }
            SetMonsterActiveInWorld(false, dormantPosition, Quaternion.Euler(0f, 180f, 0f));
            SetState(ShadowEncounterState.Dormant);
            if (notifyDirectorReset)
            {
                director?.ResetActiveEncounter();
            }
            RefreshUi();
            if (IsReady)
            {
                Ready?.Invoke();
            }
        }
    }
}
