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
    /// Orchestrates Chapter 3: wide open underground basement parking arena, ??? objective,
    /// faint 2-minute countdown timer, Shadow monster pursuit, and resolution into
    /// Good (Approach), Bad (Hide/Survive without C2 phone), or Hidden (Hide/Survive with C2 phone).
    /// </summary>
    public sealed class ChapterThreeFlow : MonoBehaviour
    {
        public const float SurvivalDurationSeconds = 120f;
        public const float ApproachTriggerDistance = 1.80f;
        public const float ChaseSpeed = 2.45f;
        public const float PatrolSpeed = 2.15f;

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

        private void Awake()
        {
            player = GetComponent<FirstPersonPlayer>();
            motor = GetComponent<FirstPersonMotor>();
            director = GetComponent<ChapterDirector>();
            endingPresenter = GetComponent<EndingPresentationController>() ?? gameObject.AddComponent<EndingPresentationController>();
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
                director.ChapterThreeTransitionRequested -= OnChapterThreeRequested;
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
                StartCoroutine(EnterChapterThreeRoutine());
        }

        private IEnumerator EnterChapterThreeRoutine()
        {
            transitioning = true;
            while (player != null && player.IsTransitionLocked)
                yield return null;

            player?.SetTransitionLocked(true);

            var street = FindAnyObjectByType<ChapterTwoStreet>();
            if (street != null) street.Leave();

            yield return null;

            bool entered = BeginChapterThree();
            player?.SetTransitionLocked(false);
            transitioning = false;
            if (!entered)
                Debug.LogError("[NRA] Failed to enter Chapter 3 arena.", this);
        }

        public bool BeginChapterThree()
        {
            if (IsActive) return true;
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

            if (arena == null || !arena.Enter(motor)) return false;

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

        private void Update()
        {
            if (!IsActive || !AutoTick) return;
            TickChase(Time.deltaTime);
        }

        public void TickChase(float deltaTime)
        {
            if (!IsActive || ResolvedEnding != StoryEnding.None) return;

            bool paused = player != null && (player.IsPaused || player.IsTransitionLocked);
            arena?.SetAudioPaused(paused);
            if (timerCanvas != null) timerCanvas.enabled = !paused;
            if (chaseAudio != null)
            {
                if (paused && chaseAudio.isPlaying) chaseAudio.Pause();
                else if (!paused && !chaseAudio.isPlaying) chaseAudio.UnPause();
            }
            if (paused) return;

            float dt = Mathf.Clamp(deltaTime, 0f, 0.1f);
            RemainingSeconds = Mathf.Max(0f, RemainingSeconds - dt);
            RefreshTimerUi();

            // Bertahan sampai waktu habis memilih Hide, termasuk saat Raka masih di luar lemari.
            if (RemainingSeconds <= 0f)
            {
                if (HidingWardrobe.Occupied != null)
                    HidingWardrobe.Occupied.Release();
                TriggerEnding(FinalChoice.Hide);
                return;
            }

            UpdateMonsterChase(dt);
        }

        private void UpdateMonsterChase(float dt)
        {
            if (monsterObject == null || monsterController == null || motor == null) return;

            bool hiding = player != null && player.IsHiding;
            Vector3 monsterPos = monsterObject.transform.position;
            Vector3 playerPos = motor.transform.position;

            if (!hiding)
            {
                float flatDist = Vector3.Distance(
                    new Vector3(monsterPos.x, 0f, monsterPos.z),
                    new Vector3(playerPos.x, 0f, playerPos.z));

                // Saat Raka berada di luar lemari, jarak horizontal yang cukup dekat memilih Approach.
                if (flatDist <= ApproachTriggerDistance)
                {
                    TriggerEnding(FinalChoice.Approach);
                    return;
                }
            }

            // Monster mengejar posisi Raka. Saat Raka bersembunyi, tujuannya beralih ke titik patroli arena.
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
                monsterVerticalVelocity = -2f;
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
                            return corner;
                    }
                    return hitTo.position;
                }
            }
            return to;
        }

        public StoryEnding TriggerEnding(FinalChoice choice)
        {
            if (ResolvedEnding != StoryEnding.None) return ResolvedEnding;

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

        private void SpawnMonster()
        {
            if (monsterObject != null) Destroy(monsterObject);

            Vector3 spawnPos = arena.MonsterSpawn != null
                ? arena.MonsterSpawn.position
                : arena.transform.position + new Vector3(0f, 0.12f, 19.5f);
            Quaternion spawnRot = arena.MonsterSpawn != null
                ? arena.MonsterSpawn.rotation
                : Quaternion.Euler(0f, 180f, 0f);

            monsterObject = new GameObject("C3_ShadowMonster", typeof(CharacterController));
            monsterObject.transform.SetPositionAndRotation(spawnPos, spawnRot);
            monsterController = monsterObject.GetComponent<CharacterController>();
            monsterController.radius = 0.34f;
            monsterController.height = 2.0f;
            monsterController.center = new Vector3(0f, 1.0f, 0f);

            // Reuse the Chapter 1 shadow monster visual if present in scene; fallback to C1ShadowMotion prefab
            Transform visualWrapper = null;
            var existingController = FindAnyObjectByType<ShadowEncounterController>(FindObjectsInactive.Include);
            if (existingController != null && existingController.transform.childCount > 0)
            {
                var cloned = Instantiate(existingController.transform.GetChild(0).gameObject, monsterObject.transform);
                cloned.SetActive(true);
                visualWrapper = cloned.transform;
            }
            else
            {
                var motionPrefab = Resources.Load<GameObject>("C1ShadowMotion");
                if (motionPrefab != null)
                {
                    var cloned = Instantiate(motionPrefab, monsterObject.transform);
                    visualWrapper = cloned.transform;
                }
            }

            if (visualWrapper != null)
            {
                monsterVisual = monsterObject.AddComponent<ShadowPlaceholderVisual>();
                monsterVisual.Configure(visualWrapper, null);
                monsterVisual.SetVisible(true);
                monsterVisual.ResetPostureImmediate();
            }
        }

        private void StartChaseAudio()
        {
            if (chaseAudio == null)
            {
                chaseAudio = gameObject.AddComponent<AudioSource>();
                chaseAudio.playOnAwake = false;
                chaseAudio.loop = true;
                chaseAudio.spatialBlend = 0f;
                chaseAudio.volume = 0.14f;

                const int rate = 22050;
                const float dur = 0.85f;
                int count = Mathf.RoundToInt(rate * dur);
                var data = new float[count];
                for (int i = 0; i < count; i++)
                {
                    float t = (float)i / rate;
                    float beat1 = Mathf.Exp(-Mathf.Pow((t - 0.12f) / 0.04f, 2f)) * Mathf.Sin(2f * Mathf.PI * 50f * t);
                    float beat2 = Mathf.Exp(-Mathf.Pow((t - 0.30f) / 0.05f, 2f)) * Mathf.Sin(2f * Mathf.PI * 44f * t) * 0.72f;
                    data[i] = Mathf.Clamp((beat1 + beat2) * 0.85f, -1f, 1f);
                }
                chaseClip = AudioClip.Create("Chapter3ChasePulse", count, 1, rate, false);
                chaseClip.SetData(data, 0);
                chaseAudio.clip = chaseClip;
            }
            if (!chaseAudio.isPlaying) chaseAudio.Play();
        }

        private void BuildTimerUi()
        {
            if (timerCanvas != null)
            {
                timerCanvas.enabled = true;
                return;
            }

            var go = new GameObject("ChapterThreeTimerUI", typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(transform, false);
            timerCanvas = go.GetComponent<Canvas>();
            timerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            timerCanvas.sortingOrder = 14;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var labelGo = new GameObject("FaintCountdown", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var rect = (RectTransform)labelGo.transform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -26f);
            rect.sizeDelta = new Vector2(220f, 44f);

            faintTimerLabel = labelGo.GetComponent<Text>();
            faintTimerLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            faintTimerLabel.fontSize = 24;
            faintTimerLabel.alignment = TextAnchor.MiddleCenter;
            // Faint, desaturated low-opacity countdown as requested
            faintTimerLabel.color = new Color(0.68f, 0.76f, 0.76f, 0.36f);
            faintTimerLabel.raycastTarget = false;
        }

        private void RefreshTimerUi()
        {
            if (faintTimerLabel == null) return;
            int total = Mathf.CeilToInt(Mathf.Max(0f, RemainingSeconds));
            int mins = total / 60;
            int secs = total % 60;
            faintTimerLabel.text = $"{mins:00}:{secs:00}";
        }
    }
}
