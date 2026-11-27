using System;
using System.Collections;
using NoRainbowAfter.Narrative;
using NoRainbowAfter.Player;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace NoRainbowAfter.AI
{
    /// <summary>
    /// Mengatur alur Chapter 3: arena parkir basemen bawah tanah terbuka, objektif "???",
    /// penghitung waktu mundur samar 2 menit, pengejaran monster bayangan di atas NavMesh,
    /// serta penyelesaian Good (Approach), Bad (Hide tanpa pesan Ella), atau Hidden (Hide dengan pesan Ella).
    ///
    /// [TEKNIS KODING]
    /// Dipecah menjadi 2 file partial class:
    /// 1. ChapterThreeFlow.cs (siklus hidup, autostart scene Chapter 3, loop kejaran, NavMesh pathfinding, resolusi ending)
    /// 2. ChapterThreeFlow.Setup.cs (spawn monster bayangan &amp; visual rig, audio detak kejaran, UI timer samar)
    /// </summary>
    public sealed partial class ChapterThreeFlow : MonoBehaviour
    {
        // =========================================================================
        // Konstanta Kecepatan & Konfigurasi SerializeField
        // =========================================================================

        public const float SurvivalDurationSeconds = 120f;
        public const float ApproachTriggerDistance = 1.80f;
        public const float ChaseSpeed = 2.45f;
        public const float PatrolSpeed = 2.15f;

        [SerializeField] private GameObject monsterVisualPrefab;

        // =========================================================================
        // Referensi Komponen, State Monster, Audio, & UI Timer
        // =========================================================================

        private FirstPersonPlayer player;
        private FirstPersonMotor motor;
        private ChapterDirector director;
        private EndingPresentationController endingPresenter;

        private ChapterThreeArena arena;
        private GameObject monsterObject;
        private CharacterController monsterController;
        private ShadowPlaceholderVisual monsterVisual;
        private float monsterVerticalVelocity;
        private int patrolIndex;
        private float patrolWaitTimer;

        private Canvas timerCanvas;
        private Text faintTimerLabel;
        private AudioSource chaseAudio;
        private AudioClip chaseClip;
        private bool transitioning;

        public bool IsActive { get; private set; }
        public bool AutoTick { get; set; } = true;
        public float RemainingSeconds { get; private set; } = SurvivalDurationSeconds;
        public StoryEnding ResolvedEnding { get; private set; } = StoryEnding.None;
        public FinalChoice ResolvedChoice { get; private set; } = FinalChoice.None;
        public ChapterThreeArena Arena => arena;
        public Transform MonsterTransform => monsterObject != null ? monsterObject.transform : null;
        public Text FaintTimerLabel => faintTimerLabel;

        public event Action<StoryEnding> EndingResolved;

        // =========================================================================
        // Siklus Hidup, Autostart Scene Chapter 3, & Transisi Masuk Arena
        // =========================================================================

        private void Awake()
        {
            player = GetComponent<FirstPersonPlayer>();
            motor = GetComponent<FirstPersonMotor>();
            director = GetComponent<ChapterDirector>();
            endingPresenter = GetComponent<EndingPresentationController>() ?? gameObject.AddComponent<EndingPresentationController>();

            // Temukan ChapterThreeArena yang sudah diletakkan di scene agar dapat dipakai ulang.
            foreach (var existing in FindObjectsByType<ChapterThreeArena>(FindObjectsInactive.Include))
            {
                if (existing.gameObject.scene != gameObject.scene)
                {
                    continue;
                }
                arena = existing;
                arena.gameObject.SetActive(false);
                break;
            }
        }

        private void Start() => StartCoroutine(AutoStartIfChapterThreeScene());

        private IEnumerator AutoStartIfChapterThreeScene()
        {
            // Tunggu satu frame agar ChapterDirector.Start() selesai memulihkan state & clue lintas scene.
            yield return null;
            if (director != null && director.SceneChapter == 3 && !IsActive)
            {
                BeginChapterThree();
            }
        }

        private void OnEnable()
        {
            if (director == null) director = GetComponent<ChapterDirector>();
            if (director != null)
            {
                director.ChapterThreeTransitionRequested -= OnChapterThreeRequested;
                director.ChapterThreeTransitionRequested += OnChapterThreeRequested;
            }
        }

        private void OnDisable()
        {
            if (director != null)
            {
                director.ChapterThreeTransitionRequested -= OnChapterThreeRequested;
            }
        }

        private void OnDestroy()
        {
            if (timerCanvas != null) Destroy(timerCanvas.gameObject);
            if (monsterObject != null) Destroy(monsterObject);
            if (arena != null) Destroy(arena.gameObject);
            if (chaseClip != null) Destroy(chaseClip);
        }

        private void OnChapterThreeRequested()
        {
            if (!IsActive && !transitioning)
            {
                StartCoroutine(EnterChapterThreeRoutine());
            }
        }

        private IEnumerator EnterChapterThreeRoutine()
        {
            transitioning = true;
            while (player != null && player.IsTransitionLocked)
            {
                yield return null;
            }

            player?.SetTransitionLocked(true);

            var street = FindAnyObjectByType<ChapterTwoStreet>();
            if (street != null)
            {
                street.Leave();
            }

            yield return null;

            bool entered = BeginChapterThree();
            player?.SetTransitionLocked(false);
            transitioning = false;
            if (!entered)
            {
                Debug.LogError("[NRA] Failed to enter Chapter 3 arena.", this);
            }
        }

        public bool BeginChapterThree()
        {
            if (IsActive)
            {
                return true;
            }
            if (motor == null) motor = GetComponent<FirstPersonMotor>();
            if (player == null) player = GetComponent<FirstPersonPlayer>();
            if (director == null) director = GetComponent<ChapterDirector>();

            if (arena == null)
            {
                var prefab = Resources.Load<GameObject>("ChapterThreeArena");
                if (prefab == null)
                {
                    Debug.LogError("[NRA] Missing Resources/ChapterThreeArena.prefab.", this);
                    return false;
                }
                arena = Instantiate(prefab).GetComponent<ChapterThreeArena>();
            }

            if (arena == null || !arena.Enter(motor))
            {
                return false;
            }

            director?.EnterChapterThree();

            motor.MovementScale = 1.0f;
            RemainingSeconds = SurvivalDurationSeconds;
            ResolvedEnding = StoryEnding.None;
            ResolvedChoice = FinalChoice.None;
            patrolIndex = 0;
            patrolWaitTimer = 0f;

            SpawnMonster();
            BuildTimerUi();
            StartChaseAudio();
            IsActive = true;
            RefreshTimerUi();
            return true;
        }

        // =========================================================================
        // Update Harian, Pengejaran NavMesh, & Resolusi Akhir Cerita
        // =========================================================================

        private void Update()
        {
            if (!IsActive || !AutoTick)
            {
                return;
            }
            TickChase(Time.deltaTime);
        }

        public void TickChase(float deltaTime)
        {
            if (!IsActive || ResolvedEnding != StoryEnding.None)
            {
                return;
            }

            bool paused = player != null && (player.IsPaused || player.IsTransitionLocked);
            arena?.SetAudioPaused(paused);
            if (timerCanvas != null) timerCanvas.enabled = !paused;
            if (chaseAudio != null)
            {
                if (paused && chaseAudio.isPlaying) chaseAudio.Pause();
                else if (!paused && !chaseAudio.isPlaying) chaseAudio.UnPause();
            }
            if (paused)
            {
                return;
            }

            float dt = Mathf.Clamp(deltaTime, 0f, 0.1f);
            RemainingSeconds = Mathf.Max(0f, RemainingSeconds - dt);
            RefreshTimerUi();

            // Bertahan sampai waktu habis memilih Hide, termasuk saat Raka masih berada di luar lemari.
            if (RemainingSeconds <= 0f)
            {
                if (HidingWardrobe.Occupied != null)
                {
                    HidingWardrobe.Occupied.Release();
                }
                TriggerEnding(FinalChoice.Hide);
                return;
            }

            UpdateMonsterChase(dt);
        }

        private void UpdateMonsterChase(float dt)
        {
            if (monsterObject == null || monsterController == null || motor == null)
            {
                return;
            }

            bool hiding = player != null && player.IsHiding;
            Vector3 monsterPos = monsterObject.transform.position;
            Vector3 playerPos = motor.transform.position;

            if (!hiding)
            {
                float flatDist = Vector3.Distance(
                    new Vector3(monsterPos.x, 0f, monsterPos.z),
                    new Vector3(playerPos.x, 0f, playerPos.z));

                // Saat Raka berada di luar lemari, jarak horizontal yang cukup dekat memicu Approach (Good Ending).
                if (flatDist <= ApproachTriggerDistance)
                {
                    TriggerEnding(FinalChoice.Approach);
                    return;
                }
            }

            // Monster mengejar Raka. Saat Raka bersembunyi di lemari, monster berpatroli memutari arena.
            Vector3 targetPos = playerPos;
            float speed = ChaseSpeed;

            if (hiding)
            {
                speed = PatrolSpeed;
                if (arena != null && arena.PatrolWaypoints != null && arena.PatrolWaypoints.Length > 0)
                {
                    Transform wp = arena.PatrolWaypoints[patrolIndex % arena.PatrolWaypoints.Length];
                    targetPos = wp != null ? wp.position : monsterPos;
                    float wpDist = Vector3.Distance(
                        new Vector3(monsterPos.x, 0f, monsterPos.z),
                        new Vector3(targetPos.x, 0f, targetPos.z));
                    if (wpDist < 1.35f)
                    {
                        patrolWaitTimer += dt;
                        if (patrolWaitTimer >= 1.1f)
                        {
                            patrolWaitTimer = 0f;
                            patrolIndex = (patrolIndex + 1) % arena.PatrolWaypoints.Length;
                        }
                        speed = 0f;
                    }
                }
                else
                {
                    speed = 0f;
                }
            }

            Vector3 moveStep = Vector3.zero;
            if (speed > 0f && dt > 0f)
            {
                Vector3 nextCorner = NavigateOnArena(monsterPos, targetPos);
                Vector3 toTarget = new Vector3(nextCorner.x - monsterPos.x, 0f, nextCorner.z - monsterPos.z);
                if (toTarget.sqrMagnitude > 0.0025f)
                {
                    Vector3 dir = toTarget.normalized;
                    Quaternion desiredRot = Quaternion.LookRotation(dir, Vector3.up);
                    monsterObject.transform.rotation = Quaternion.RotateTowards(
                        monsterObject.transform.rotation, desiredRot, 300f * dt);
                    moveStep = dir * speed;
                }
            }

            if (monsterController.isGrounded && monsterVerticalVelocity < 0f)
            {
                monsterVerticalVelocity = -2f;
            }
            monsterVerticalVelocity += -20f * dt;

            monsterController.Move((moveStep + Vector3.up * monsterVerticalVelocity) * dt);

            if (monsterVisual != null)
            {
                monsterVisual.TickVisual(
                    ShadowEncounterState.Pursue,
                    dt,
                    Mathf.Clamp01(moveStep.magnitude / ChaseSpeed),
                    0f,
                    motor.transform,
                    hiding);
                monsterVisual.TickSecondaryMotion(dt);
            }

            if (chaseAudio != null && motor != null)
            {
                float d = Vector3.Distance(monsterObject.transform.position, motor.transform.position);
                float proximity = 1f - Mathf.InverseLerp(2f, 24f, d);
                chaseAudio.volume = Mathf.Lerp(0.10f, 0.36f, proximity);
                chaseAudio.pitch = Mathf.Lerp(0.88f, 1.24f, proximity);
            }
        }

        private static Vector3 NavigateOnArena(Vector3 from, Vector3 to)
        {
            if (NavMesh.SamplePosition(from, out NavMeshHit hitFrom, 2.5f, NavMesh.AllAreas) &&
                NavMesh.SamplePosition(to, out NavMeshHit hitTo, 2.5f, NavMesh.AllAreas))
            {
                var path = new NavMeshPath();
                if (NavMesh.CalculatePath(hitFrom.position, hitTo.position, NavMesh.AllAreas, path) &&
                    path.status != NavMeshPathStatus.PathInvalid)
                {
                    for (int i = 0; i < path.corners.Length; i++)
                    {
                        Vector3 corner = path.corners[i];
                        if (Vector3.Distance(new Vector3(from.x, 0f, from.z), new Vector3(corner.x, 0f, corner.z)) > 0.35f)
                        {
                            return corner;
                        }
                    }
                    return hitTo.position;
                }
            }
            return to;
        }

        public StoryEnding TriggerEnding(FinalChoice choice)
        {
            if (ResolvedEnding != StoryEnding.None)
            {
                return ResolvedEnding;
            }

            IsActive = false;
            ResolvedChoice = choice;
            ResolvedEnding = EndingResolver.Resolve(choice, director != null ? director.State : null);

            if (chaseAudio != null && chaseAudio.isPlaying) chaseAudio.Stop();
            if (timerCanvas != null) timerCanvas.enabled = false;
            arena?.SetAudioPaused(true);

            EndingResolved?.Invoke(ResolvedEnding);
            endingPresenter?.Show(ResolvedEnding);
            return ResolvedEnding;
        }
    }
}
