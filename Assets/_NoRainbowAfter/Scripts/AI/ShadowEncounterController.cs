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
    /// Mengatur fase pertemuan dengan bayangan pada bab pertama.
    /// Dari Dormant, bayangan muncul (Spawn) lalu mengejar (Pursue).
    /// Percobaan berakhir saat pemain berhasil melawan, bersembunyi, atau tertangkap.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class ShadowEncounterController : MonoBehaviour, IShadowEncounter, IAudioCueSource
    {
        private static readonly Vector3[] DormFallbackSpawns =
        {
            new Vector3(0.10f, 0.04f, 2.20f),
            new Vector3(-0.15f, 0.04f, 3.20f),
            new Vector3(-0.20f, 0.04f, 0.65f),
            new Vector3(0.25f, 0.04f, 4.10f)
        };

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
        private bool escapeUsed;
        private ChapterOneCorridorShift shiftedWorld;
        private float wardrobeHideTime;
        private FirstPersonCaptureFeedback captureFeedback;
        private bool grabWillEscape;
        private const float GrabDuration=1.15f;
        private const float EscapeRecoveryDuration = 3f;
        public float DamageBorderAlpha => captureFeedback != null ? captureFeedback.DamageAlpha : 0f;
        public bool EscapeUsed => escapeUsed;
        public void EnableFirstEscape() => allowFirstEscape = true;

        private CharacterController monsterController;
        private Action<EncounterResponse> activeResolve;
        private Canvas uiCanvas;
        private Text fightPromptLabel;
        private int successfulHits;
        public int SuccessfulHits => successfulHits;
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
            motor != null && (shiftedWorld == null || shiftedWorld.CanBegin);

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
                placeholderVisual = GetComponent<ShadowPlaceholderVisual>();
            BuildRuntimeUi();
            captureFeedback=gameObject.AddComponent<FirstPersonCaptureFeedback>();
            captureFeedback.Configure(motor);
            SetMonsterActiveInWorld(false, dormantPosition, Quaternion.Euler(0f, 180f, 0f));
        }

        private void OnEnable()
        {
            BindEvents();
            if (uiCanvas != null) uiCanvas.enabled = true;
            RegisterWithDirector();
            RefreshUi();
        }

        private void Start()
        {
            RegisterWithDirector();
            if (IsReady) Ready?.Invoke();
        }

        private void OnDisable()
        {
            captureFeedback?.ResetFeedback();
            UnbindEvents();
            if (player != null && player.IsTransitionLocked &&
                (State == ShadowEncounterState.Grabbed || State == ShadowEncounterState.Caught))
                player.SetTransitionLocked(false);

            activeResolve = null;
            currentBlackoutAlpha = 0f;
            postCaughtGraceTimer = 0f;
            SetMonsterActiveInWorld(false, dormantPosition, Quaternion.Euler(0f, 180f, 0f));
            if (!permanentlyResolved)
                SetState(ShadowEncounterState.Dormant);

            if (director != null && director.IsCurrentEncounter(this))
                director.SetEncounter(null);

            if (uiCanvas != null) uiCanvas.enabled = false;
        }

        private void OnDestroy()
        {
            if (uiCanvas != null) Destroy(uiCanvas.gameObject);
        }

        private void BindEvents()
        {
            if (bound) return;
            if (player != null) player.FightRequested += OnFightRequested;
            if (motor != null) motor.PoseRestored += OnPlayerPoseRestored;
            bound = true;
        }

        private void UnbindEvents()
        {
            if (!bound) return;
            if (player != null) player.FightRequested -= OnFightRequested;
            if (motor != null) motor.PoseRestored -= OnPlayerPoseRestored;
            bound = false;
        }

        private void RegisterWithDirector()
        {
            if (director != null && !permanentlyResolved && !director.IsCurrentEncounter(this))
                director.SetEncounter(this);
        }

        // Simpan callback penyelesaian saat memulai percobaan. Cerita maju jika pemain berhasil melawan atau bersembunyi.
        public void Begin(Action<EncounterResponse> resolve)
        {
            if (!isActiveAndEnabled || permanentlyResolved || resolve == null) return;

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
                captureFeedback?.Tick(0f,true,0f);
                RefreshUi();
                return;
            }

            float dt = Mathf.Clamp(deltaTime, 0f, 0.1f);
            captureFeedback?.Tick(dt,false,State==ShadowEncounterState.Grabbed ? stateTimer/GrabDuration : 0f);
            if (postCaughtGraceTimer > 0f)
                postCaughtGraceTimer = Mathf.Max(0f, postCaughtGraceTimer - dt);

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

        private void TickSpawn(float dt)
        {
            FaceHorizontallyToward(motor.transform.position, dt);
            placeholderVisual?.TickVisual(ShadowEncounterState.Spawn, dt, 0f, stateTimer / spawnHoldDuration, motor.transform);
            stateTimer += dt;
            if (stateTimer >= spawnHoldDuration)
            {
                stateTimer = 0f;
                CueRequested?.Invoke(new NarrativeCue("", "", 0f, "AUD_C1_SHADOW_PURSUE"));
                SetState(ShadowEncounterState.Pursue);
            }
        }

        private void TickPursue(float dt)
        {
            if (postCaughtGraceTimer > 0f)
            {
                currentBlackoutAlpha = Mathf.MoveTowards(currentBlackoutAlpha, 0f, dt);
                placeholderVisual?.AnimateRecovery(1f - postCaughtGraceTimer / EscapeRecoveryDuration);
                return;
            }
            var wardrobe=HidingWardrobe.Occupied;
            if(wardrobe != null && wardrobe.Occupant == motor)
            {
                // Shadow mengingat apakah ia melihat Raka masuk. Setelah dua detik ia mencoba
                // menangkap Raka yang ketahuan. Jika tidak terlihat, bertahan enam detik menyelesaikan persembunyian.
                wardrobeHideTime += dt;
                if(wardrobe.Spotted)
                {
                    placeholderVisual?.TickVisual(ShadowEncounterState.Pursue, dt, 0f, 0f, motor.transform, true);
                    if(wardrobeHideTime > 2f) { wardrobe.TryExit(); wardrobe.Release(); EnterCaught(); wardrobeHideTime=0f; }
                    return;
                }
                Vector3 search=shiftedWorld != null ? shiftedWorld.MonsterSpawn+Vector3.left*6f : new Vector3(0,0,17);
                Vector3 next=shiftedWorld != null ? shiftedWorld.Navigate(transform.position,search) : search;
                float searchSpeed = MoveAlongFloor(next,searchPassSpeed,dt);
                placeholderVisual?.TickVisual(ShadowEncounterState.Pursue,dt,searchSpeed / searchPassSpeed,0f,motor.transform,true);
                if(wardrobeHideTime >= 6f)
                {
                    stateTimer=0f;
                    CueRequested?.Invoke(new NarrativeCue("Raka","Langkahnya menjauh...",2.5f,"AUD_C1_HIDE"));
                    SetState(ShadowEncounterState.HideResolved);
                }
                return;
            }
            wardrobeHideTime=0f;
            Vector3 eyePos = GetMonsterEyePosition();
            CoverEvaluation cover = ShadowVisibility.EvaluateCover(
                motor,
                coverZones,
                eyePos,
                transform.forward,
                transform,
                crouchedGazeHalfAngle);

            UpdateHideProgress(cover, dt);
            if (State != ShadowEncounterState.Pursue) return;

            bool concealed = cover.IsConcealedFromMonster && !spottedEnteringCover;
            Vector3 destination = shiftedWorld != null && shiftedWorld.OwnsPlayer
                ? shiftedWorld.Navigate(transform.position,motor.transform.position)
                : concealed ? SelectSearchPassWaypoint(dt) : SelectPursuitWaypoint(motor.transform.position);
            float moveSpeed = concealed ? searchPassSpeed : pursueSpeed;

            float actualHorizontalSpeed = MoveAlongFloor(destination, moveSpeed, dt);
            placeholderVisual?.TickVisual(
                ShadowEncounterState.Pursue,
                dt,
                Mathf.Clamp01(actualHorizontalSpeed / Mathf.Max(0.1f, searchPassSpeed)),
                0f,
                motor.transform);

            if (!concealed && postCaughtGraceTimer <= 0f && ShouldTriggerCaught())
                EnterCaught();
        }

        private void UpdateHideProgress(CoverEvaluation cover, float dt)
        {
            // Cover biasa baru mulai menghitung bila pemain jongkok di zona. Pindah zona atau berdiri
            // menghapus timer; masuk dari dekat, terlihat, dan tanpa penghalang menandai pemain spotted.
            if (!cover.IsCrouchedInCover || cover.ActiveZone == null)
            {
                ResetHideTracking();
                return;
            }

            if (!ReferenceEquals(trackedCoverZone, cover.ActiveZone))
            {
                trackedCoverZone = cover.ActiveZone;
                sawMonsterNorthOfCover = false;
                sawMonsterSouthOfCover = false;
                hideTimer = 0f;

                float horizontalDist = HorizontalDistance(transform.position, motor.transform.position);
                // Bayangan mengingat pemain yang mulai jongkok dari dekat di dalam sudut pandang tanpa penghalang.
                spottedEnteringCover = !cover.IsPhysicallyOccluded &&
                    cover.AngleFromMonsterGaze <= crouchedGazeHalfAngle &&
                    horizontalDist <= 3.1f;
            }

            if (spottedEnteringCover || !cover.IsConcealedFromMonster)
            {
                hideTimer = 0f;
                return;
            }

            float coverZ = trackedCoverZone.transform.position.z;
            float deltaZ = transform.position.z - coverZ;
            if (deltaZ >= 0.6f) sawMonsterNorthOfCover = true;
            if (deltaZ <= -0.6f) sawMonsterSouthOfCover = true;

            hideTimer += dt;

            // Tunggu hingga bayangan melewati zona, atau menjauh cukup lama saat pemain tetap tertutup.
            bool passedSouth = sawMonsterNorthOfCover && deltaZ <= -coverPassClearanceZ;
            bool passedNorth = sawMonsterSouthOfCover && deltaZ >= coverPassClearanceZ;
            bool farAndOccluded = Mathf.Abs(deltaZ) >= 3.6f && cover.IsPhysicallyOccluded && hideTimer >= requiredHideDuration + 1.0f;

            if (hideTimer >= requiredHideDuration && cover.IsPhysicallyOccluded && (passedSouth || passedNorth || farAndOccluded))
            {
                stateTimer = 0f;
                CueRequested?.Invoke(new NarrativeCue("", "", 0f, "AUD_C1_HIDE"));
                SetState(ShadowEncounterState.HideResolved);
            }
        }

        private void ResetHideTracking()
        {
            hideTimer = 0f;
            trackedCoverZone = null;
            spottedEnteringCover = false;
            sawMonsterNorthOfCover = false;
            sawMonsterSouthOfCover = false;
        }

        private Vector3 SelectSearchPassWaypoint(float dt)
        {
            // Keluar lewat pintu kamar sebelum mulai menyusuri lorong untuk mencari pemain.
            if (transform.position.z < 5.45f)
            {
                if (Mathf.Abs(transform.position.x - 0.16f) > 0.16f && transform.position.z < 4.70f)
                    return new Vector3(0.16f, 0f, 4.55f);
                return new Vector3(0.16f, 0f, 6.15f);
            }

            float targetZ = searchDirection < 0 ? 6.05f : 17.65f;
            if (Mathf.Abs(transform.position.z - targetZ) <= 0.25f)
            {
                searchTurnPauseTimer += dt;
                if (searchTurnPauseTimer >= 0.35f)
                {
                    searchTurnPauseTimer = 0f;
                    searchDirection = -searchDirection;
                    targetZ = searchDirection < 0 ? 6.05f : 17.65f;
                }
            }
            return new Vector3(0f, 0f, targetZ);
        }

        private Vector3 SelectPursuitWaypoint(Vector3 playerPos)
        {
            Vector3 pos = transform.position;
            const float doorThresholdZ = 5.45f;
            const float doorLaneX = 0.16f;

            // Sejajarkan bayangan dengan celah pintu di x = 0,16 sebelum masuk ke kamar.
            if (pos.z > doorThresholdZ && playerPos.z < 5.05f)
            {
                if (pos.z > 6.10f || Mathf.Abs(pos.x - doorLaneX) > 0.14f)
                    return new Vector3(doorLaneX, 0f, 6.05f);
                return new Vector3(doorLaneX, 0f, 4.50f);
            }

            // Keluar dari kamar melalui celah pintu yang sama.
            if (pos.z <= doorThresholdZ && playerPos.z >= doorThresholdZ)
            {
                if (pos.z < 4.45f || Mathf.Abs(pos.x - doorLaneX) > 0.14f)
                    return new Vector3(doorLaneX, 0f, 4.50f);
                return new Vector3(doorLaneX, 0f, 6.10f);
            }

            // Bila pemain berada di ceruk lorong, dekati bukaan ceruk sebelum masuk.
            if (pos.z > doorThresholdZ)
            {
                if (Mathf.Abs(playerPos.x) > 0.92f)
                {
                    float nicheCenterZ = playerPos.x < 0f ? 9.0f : 13.0f;
                    if (Mathf.Abs(pos.z - nicheCenterZ) > 0.42f)
                        return new Vector3(0f, 0f, nicheCenterZ);
                    return new Vector3(Mathf.Clamp(playerPos.x, -1.38f, 1.38f), 0f, Mathf.Clamp(playerPos.z, nicheCenterZ - 0.35f, nicheCenterZ + 0.35f));
                }
                return new Vector3(Mathf.Clamp(playerPos.x, -0.56f, 0.56f), 0f, Mathf.Clamp(playerPos.z, 5.75f, 18.4f));
            }

            // Gunakan jalur tengah kamar jika furnitur menghalangi lintasan langsung setinggi pinggang.
            Vector3 waistOrigin = new Vector3(pos.x, 0.55f, pos.z);
            Vector3 waistTarget = new Vector3(playerPos.x, 0.55f, playerPos.z);
            if (ShadowVisibility.IsLineOfSightClear(waistOrigin, waistTarget, transform, motor.transform))
                return new Vector3(Mathf.Clamp(playerPos.x, -1.65f, 1.65f), 0f, Mathf.Clamp(playerPos.z, 0.35f, 4.65f));

            const float roomSpineX = 0.05f;
            if (Mathf.Abs(pos.x - roomSpineX) > 0.22f)
                return new Vector3(roomSpineX, 0f, Mathf.Clamp(pos.z, 0.55f, 4.55f));
            return new Vector3(roomSpineX, 0f, Mathf.Clamp(playerPos.z, 0.55f, 4.55f));
        }

        private float MoveAlongFloor(Vector3 target, float speed, float dt)
        {
            if (monsterController == null || !monsterController.enabled) return 0f;

            Vector3 before = transform.position;
            Vector3 planarDelta = new Vector3(target.x - before.x, 0f, target.z - before.z);
            Vector3 horizVel = Vector3.zero;
            if (planarDelta.sqrMagnitude > 0.0004f)
            {
                Vector3 dir = planarDelta.normalized;
                horizVel = dir * Mathf.Min(speed, planarDelta.magnitude / dt);
                FaceDirection(dir, dt);
            }

            if (monsterController.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += gravity * dt;

            CollisionFlags flags = monsterController.Move((horizVel + Vector3.up * verticalVelocity) * dt);
            if ((flags & CollisionFlags.Below) != 0 && verticalVelocity < 0f) verticalVelocity = -2f;

            Vector3 moved = transform.position - before;
            moved.y = 0f;
            return dt > 0f ? moved.magnitude / dt : 0f;
        }

        private void FaceHorizontallyToward(Vector3 worldPoint, float dt)
        {
            Vector3 delta = worldPoint - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude > 0.0004f)
                FaceDirection(delta.normalized, dt);
        }

        private void FaceDirection(Vector3 planarDir, float dt)
        {
            Quaternion desired = Quaternion.LookRotation(planarDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, turnSpeedDegrees * dt);
        }

        // Bayangan perlu berada dalam batas jarak horizontal dan vertikal serta punya garis pandang ke pemain.
        private bool ShouldTriggerCaught()
        {
            if (motor == null) return false;
            Vector3 mPos = transform.position;
            Vector3 pPos = motor.transform.position;
            if (Mathf.Abs(mPos.y - pPos.y) > 1.35f) return false;
            if (HorizontalDistance(mPos, pPos) > catchDistance) return false;
            return ShadowVisibility.HasAnyClearSightlineToPlayer(GetMonsterEyePosition(), motor, transform);
        }

        private void OnFightRequested()
        {
            if (!isActiveAndEnabled || permanentlyResolved || player == null || motor == null) return;
            if (player.IsPaused || player.IsInspecting || player.IsUsingLaptop || player.IsTransitionLocked) return;
            if (State != ShadowEncounterState.Spawn && State != ShadowEncounterState.Pursue) return;
            if (!CanFightRightNow()) return;

            // Setiap pukulan memicu animasi mundur. Selama animasi itu, klik berikutnya tidak dihitung.
            successfulHits++;
            placeholderVisual?.SetHitVariant(successfulHits >= 3);
            stateTimer = 0f;
            currentBlackoutAlpha = 0.22f;
            CueRequested?.Invoke(new NarrativeCue("", "", 0f, "AUD_C1_FIGHT"));
            SetState(ShadowEncounterState.FightResolved);
            RefreshUi();
        }

        public bool CanFightRightNow()
        {
            if (player == null || motor == null || motor.ViewCamera == null) return false;
            if (player.IsHiding) return false;
            if (player.IsPaused || player.IsInspecting || player.IsUsingLaptop || player.IsTransitionLocked) return false;
            if (State != ShadowEncounterState.Spawn && State != ShadowEncounterState.Pursue) return false;
            float visualHeight = placeholderVisual != null ? placeholderVisual.CurrentVisualHeight : 2.15f;
            return ShadowVisibility.CanPlayerFightMonster(
                motor.ViewCamera,
                motor.transform,
                transform,
                visualHeight,
                fightRange,
                fightAimHalfAngle);
        }

        private void TickFightResolved(float dt)
        {
            stateTimer += dt;
            float duration = Mathf.Max(fightFeedbackDuration, placeholderVisual != null ? placeholderVisual.HitDuration(successfulHits >= 3) : .52f);
            float t = Mathf.Clamp01(stateTimer / duration);
            currentBlackoutAlpha = Mathf.Lerp(0.22f, 0f, t);
            placeholderVisual?.TickVisual(ShadowEncounterState.FightResolved, dt, 0f, t, motor.transform);

            if (stateTimer >= duration)
            {
                if (successfulHits >= 3) CompleteSuccessResolution(EncounterResponse.Fight);
                else { stateTimer = 0f; SetState(ShadowEncounterState.Pursue); }
            }
        }

        private void TickHideResolved(float dt)
        {
            stateTimer += dt;
            float t = Mathf.Clamp01(stateTimer / hideResolveDuration);
            placeholderVisual?.TickVisual(ShadowEncounterState.HideResolved, dt, 0f, t, motor.transform);

            // Sembunyi baru mengirim hasil setelah animasi pendek selesai, seperti jalur Fight.
            if (stateTimer >= hideResolveDuration)
                CompleteSuccessResolution(EncounterResponse.Hide);
        }

        private void CompleteSuccessResolution(EncounterResponse response)
        {
            captureFeedback?.ResetFeedback();
            permanentlyResolved = true;
            currentBlackoutAlpha = 0f;
            SetMonsterActiveInWorld(false, dormantPosition, Quaternion.Euler(0f, 180f, 0f));

            Action<EncounterResponse> callback = activeResolve;
            activeResolve = null;
            RefreshUi();
            // Hanya di sini hasil Fight/Hide diteruskan ke callback ChapterDirector.
            callback?.Invoke(response);
        }

        private void EnterCaught()
        {
            if(State==ShadowEncounterState.Grabbed || State==ShadowEncounterState.Caught || permanentlyResolved)return;
            if (allowFirstEscape)
            {
                grabWillEscape=!escapeUsed;
                if(grabWillEscape)escapeUsed=true;
                stateTimer=0f;
                ResetHideTracking();
                player?.SetTransitionLocked(true);
                captureFeedback?.BeginGrab(transform);
                SetState(ShadowEncounterState.Grabbed);
                CueRequested?.Invoke(new NarrativeCue("Raka", "Lepas!", 1.15f, "AUD_C1_GRAB"));
                return;
            }
            BeginCheckpointCaught();
        }
        private void TickGrabbed(float dt)
        {
            stateTimer+=dt;
            placeholderVisual?.AnimateGrab(stateTimer/GrabDuration);
            if(stateTimer<GrabDuration)return;
            captureFeedback?.FinishGrab();
            placeholderVisual?.ResetPostureImmediate(motor.transform);
            if(grabWillEscape)
            {
                postCaughtGraceTimer=EscapeRecoveryDuration;
                currentBlackoutAlpha=0f;
                player?.SetTransitionLocked(false);
                SetState(ShadowEncounterState.Pursue);
                CueRequested?.Invoke(new NarrativeCue("Raka","Aku harus cari tempat sembunyi...",3f,"AUD_C1_ESCAPE"));
            }
            else BeginCheckpointCaught();
        }
        private void BeginCheckpointCaught()
        {
            CaughtCount++;
            successfulHits = 0;
            placeholderVisual?.SetHitVariant(false);
            stateTimer = 0f;
            caughtRestoredPose = false;
            ResetHideTracking();

            // Tertangkap membatalkan callback aktif agar Director tidak mencatat respons/beat berhasil;
            // setelah fade, pemain dipulihkan ke checkpoint dan encounter dapat dimulai ulang.
            activeResolve = null;
            director?.ResetActiveEncounter();
            player?.SetTransitionLocked(true);
            SetState(ShadowEncounterState.Caught);
        }

        private void TickCaught(float dt)
        {
            stateTimer += dt;
            float totalDuration = caughtFadeOutDuration + caughtFadeInDuration;

            if (!caughtRestoredPose && stateTimer >= caughtFadeOutDuration)
            {
                caughtRestoredPose = true;
                currentBlackoutAlpha = 1f;

                // Pindahkan dan nonaktifkan collider bayangan agar tidak menghalangi titik pemulihan pemain.
                SetMonsterActiveInWorld(false, dormantPosition, Quaternion.Euler(0f, 180f, 0f));
                captureFeedback?.ResetFeedback();
                RestoreSafeCheckpointPose();
                string caughtLine = shiftedWorld != null && shiftedWorld.Expanded
                    ? "Stresku memuncak..."
                    : "Napas dulu... aku harus menenangkan diri.";
                CueRequested?.Invoke(new NarrativeCue("Raka", caughtLine, 2.8f, "AUD_C1_CAUGHT"));
            }

            if (!caughtRestoredPose)
            {
                currentBlackoutAlpha = Mathf.Clamp01(stateTimer / caughtFadeOutDuration);
            }
            else
            {
                float fadeInProgress = (stateTimer - caughtFadeOutDuration) / caughtFadeInDuration;
                currentBlackoutAlpha = 1f - Mathf.Clamp01(fadeInProgress);
            }

            if (stateTimer >= totalDuration)
            {
                currentBlackoutAlpha = 0f;
                postCaughtGraceTimer = 0.75f;
                player?.SetTransitionLocked(false);
                SetState(ShadowEncounterState.Dormant);
                if (IsReady) Ready?.Invoke();
            }
        }

        private void RestoreSafeCheckpointPose()
        {
            internalCheckpointRestore = true;
            try
            {
                if (shiftedWorld != null && shiftedWorld.OwnsPlayer)
                {
                    // Tertangkap di lorong horor langsung memaksimalkan stres dan memicu pemuatan ulang bab.
                    player?.GetComponent<StressBarometer>()?.SetStress(StressBarometer.MaxStress);
                    return;
                }

                bool restored = checkpoint != null && checkpoint.TryRestore();
                if (!restored && motor != null)
                {
                    Quaternion fallbackRot = Quaternion.Euler(0f, 10f, 0f);
                    for (int i = 0; i < DormFallbackSpawns.Length; i++)
                    {
                        if (motor.TryRestorePose(DormFallbackSpawns[i], fallbackRot))
                        {
                            break;
                        }
                    }
                }

                player?.GetComponent<StressBarometer>()?.SetStress(StressBarometer.MaxStress);

                // Reset status lintasan agar encounter menunggu pemain melewati batas pemicu lagi.
                director?.ResetCrossing();
            }
            finally
            {
                internalCheckpointRestore = false;
            }
        }

        private void OnPlayerPoseRestored()
        {
            if (internalCheckpointRestore || permanentlyResolved) return;
            if (State == ShadowEncounterState.Spawn ||
                State == ShadowEncounterState.Pursue ||
                State == ShadowEncounterState.Grabbed ||
                State == ShadowEncounterState.Caught)
            {
                AbortToDormant(notifyDirectorReset: true);
            }
        }

        public void AbortToDormant(bool notifyDirectorReset = true)
        {
            captureFeedback?.ResetFeedback();
            if (permanentlyResolved) return;
            activeResolve = null;
            postCaughtGraceTimer = 0f;
            stateTimer = 0f;
            currentBlackoutAlpha = 0f;
            ResetHideTracking();
            if (player != null && player.IsTransitionLocked)
                player.SetTransitionLocked(false);
            SetMonsterActiveInWorld(false, dormantPosition, Quaternion.Euler(0f, 180f, 0f));
            SetState(ShadowEncounterState.Dormant);
            if (notifyDirectorReset)
                director?.ResetActiveEncounter();
            RefreshUi();
            if (IsReady) Ready?.Invoke();
        }

        private Vector3 SelectSafeCorridorSpawn()
        {
            if(shiftedWorld != null && shiftedWorld.OwnsPlayer) return shiftedWorld.MonsterSpawn;
            float playerZ = motor != null ? motor.transform.position.z : 6.0f;
            // Uji kandidat posisi di ujung lorong yang lebih jauh dari pemain, lalu pilih posisi yang bebas collider.
            float[] candidates = playerZ <= 11.2f
                ? new[] { Mathf.Clamp(playerZ + 10.6f, 16.2f, 17.6f), 17.4f, 16.4f, 15.2f }
                : new[] { Mathf.Clamp(playerZ - 9.2f, 6.8f, 8.4f), 7.2f, 8.2f, 17.6f };

            for (int i = 0; i < candidates.Length; i++)
            {
                var pos = new Vector3(0f, 0.02f, candidates[i]);
                if (motor != null && HorizontalDistance(pos, motor.transform.position) < 6.0f) continue;
                if (IsCapsuleClearAt(pos)) return pos;
            }
            return new Vector3(0f, 0.02f, playerZ <= 11.2f ? 17.4f : 7.2f);
        }

        private bool IsCapsuleClearAt(Vector3 feetPosition)
        {
            float radius = monsterController != null ? Mathf.Max(0.08f, monsterController.radius - 0.02f) : 0.21f;
            float height = monsterController != null ? monsterController.height : 1.80f;
            Vector3 p0 = feetPosition + Vector3.up * (radius + 0.06f);
            Vector3 p1 = feetPosition + Vector3.up * (height - radius);
            Collider[] cols = Physics.OverlapCapsule(p0, p1, radius, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] != null && !cols[i].transform.IsChildOf(transform))
                    return false;
            }
            return true;
        }

        private void SetMonsterActiveInWorld(bool active, Vector3 position, Quaternion rotation)
        {
            if (monsterController == null) monsterController = GetComponent<CharacterController>();
            if (monsterController != null) monsterController.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            if (placeholderVisual != null)
            {
                placeholderVisual.SetVisible(active);
                if (active) placeholderVisual.ResetPostureImmediate(motor != null ? motor.transform : null);
            }
            if (monsterController != null) monsterController.enabled = active;
            Physics.SyncTransforms();
        }

        private Vector3 GetMonsterEyePosition()
        {
            float h = placeholderVisual != null ? placeholderVisual.CurrentVisualHeight : 2.15f;
            return transform.position + Vector3.up * Mathf.Clamp(h - 0.34f, 1.45f, 1.92f);
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private void SetState(ShadowEncounterState next)
        {
            if (State == next) return;
            State = next;
            StateChanged?.Invoke(next);
        }

        private void BuildRuntimeUi()
        {
            var root = new GameObject("Encounter UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false);
            uiCanvas = root.GetComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            uiCanvas.sortingOrder = 15;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var promptObj = new GameObject("FightPrompt", typeof(RectTransform), typeof(Text), typeof(Outline));
            promptObj.transform.SetParent(root.transform, false);
            var promptRect = (RectTransform)promptObj.transform;
            promptRect.anchorMin = promptRect.anchorMax = new Vector2(0.5f, 0.5f);
            promptRect.anchoredPosition = new Vector2(0f, -102f);
            promptRect.sizeDelta = new Vector2(640f, 48f);
            fightPromptLabel = promptObj.GetComponent<Text>();
            fightPromptLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            fightPromptLabel.fontSize = 24;
            fightPromptLabel.alignment = TextAnchor.MiddleCenter;
            fightPromptLabel.color = new Color(0.95f, 0.92f, 0.88f, 0.96f);
            fightPromptLabel.text = "[Klik Kiri] Lawan";
            fightPromptLabel.raycastTarget = false;
            var outline = promptObj.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.92f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            fightPromptLabel.enabled = false;

            var fadeObj = new GameObject("CaughtBlackout", typeof(RectTransform), typeof(Image));
            fadeObj.transform.SetParent(root.transform, false);
            var fadeRect = (RectTransform)fadeObj.transform;
            fadeRect.anchorMin = Vector2.zero;
            fadeRect.anchorMax = Vector2.one;
            fadeRect.offsetMin = fadeRect.offsetMax = Vector2.zero;
            blackoutImage = fadeObj.GetComponent<Image>();
            blackoutImage.color = new Color(0.01f, 0.012f, 0.016f, 0f);
            blackoutImage.raycastTarget = false;
            blackoutImage.enabled = false;
        }

        private void RefreshUi()
        {
            bool paused = player != null && player.IsPaused;
            bool inspecting = player != null && player.IsInspecting;

            if (fightPromptLabel != null)
                fightPromptLabel.enabled = !paused && !inspecting && CanFightRightNow();
            if (fightPromptLabel != null) fightPromptLabel.text = "[Klik Kiri] Lawan (" + successfulHits + "/3)";

            if (blackoutImage != null)
            {
                bool showFade = !paused && currentBlackoutAlpha > 0.001f;
                blackoutImage.enabled = showFade;
                if (showFade)
                    blackoutImage.color = new Color(0.01f, 0.012f, 0.016f, Mathf.Clamp01(currentBlackoutAlpha));
            }
        }
    }
}
